using Messaging.Subscriptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using MSMQ = System.Messaging;

namespace Messaging.Msmq
{
    class MsmqMultiSubjectMessaging : IMultiSubjectMessaging
    {
        readonly object gate = new object();
        readonly MSMQ.MessageQueue queue;
        Array<Subscription> subscriptions;
        Array<Task<MSMQ.Message>> peeks;
        bool disposed;

        public Uri Address { get; }

        public MsmqMultiSubjectMessaging(MSMQ.MessageQueue queue, Uri address)
        {
            if (queue == null)
                throw new ArgumentNullException(nameof(queue));
            if (address == null)
                throw new ArgumentNullException(nameof(address));
            this.queue = queue;
            Address = address;
        }

        public void Send(IReadOnlyMessage msg)
        {
            using (var m = Converter.ToMsmqMessage(msg))
                queue.Send(m);
        }

        public bool DispatchMessage(TimeSpan timeout)
        {
            lock (gate)
            {
                if (disposed)
                    ThrowObjectDisposed();

                if (subscriptions.Count == 1)
                    return SingleDispatch(timeout);

                if (subscriptions.Count > 1)
                    return MultiDispatch(timeout);

                return false;
            }
        }

        bool SingleDispatch(TimeSpan timeout)
        {
            var sub = subscriptions[0];
            using (var peek = sub.Peek(timeout))
            {
                if (peek == null)
                    return false;
            }
            return sub.DispatchMessage(timeout);
        }

        bool MultiDispatch(TimeSpan timeout)
        {
            if (DispatchPreviouslyCompletedPeek())
                return true;
            AddNewPeekTasks(timeout);
            return WaitForPeekTimeoutsAndDispatch();
        }

        bool DispatchPreviouslyCompletedPeek()
        {
            int i = 0;
            foreach (var task in peeks.Inner)
            {
                if (task == null)
                {
                    // ignore it
                }
                else if (task.IsFaulted)
                {
                    peeks[i] = null; // we need to peek again
                }
                else if (task.IsCompleted)
                {
                    peeks[i].Result.Dispose();
                    peeks[i] = null; // we need to peek again
                    if (task.Result != null && subscriptions[i].DispatchMessage(TimeSpan.Zero))
                        return true;
                }
                i++;
            }
            return false;
        }

        void AddNewPeekTasks(TimeSpan timeout)
        {
            int i = 0;
            foreach (var peekTask in peeks.Inner)
            {
                if (peeks[i] == null)
                    peeks[i] = subscriptions[i].PeekAsync(timeout);
                i++;
            }
        }

        bool WaitForPeekTimeoutsAndDispatch()
        {
            // wait for a task to complete, the peek will timeout so no timeout needed here
            var peekIndexes = new Dictionary<Task, int>();
            int i = 0;
            foreach (var task in peeks.Inner)
            {
                peekIndexes.Add(task, i);
                i++;
            }

            var outstanding = peeks;
            while (outstanding.Count > 0)
            {
                int idx = Task.WaitAny(outstanding.Inner);
                if (idx >= 0)
                {
                    var task = outstanding[idx];
                    if (task.IsFaulted)
                    {
                        // clear the task so the next DispatchTimeout starts a new peek
                        var peeksIdx = peekIndexes[outstanding[idx]];
                        peeks[peeksIdx] = null;

                        outstanding = outstanding.RemoveAt(idx);
                    }
                    else if (task.IsCompleted)
                    {
                        // clear the task so the next DispatchTimeout starts a new peek
                        var peeksIdx = peekIndexes[outstanding[idx]];
                        peeks[peeksIdx] = null;

                        outstanding[idx].Result?.Dispose();
                        outstanding = outstanding.RemoveAt(idx);

                        if (task.Result != null && subscriptions[idx].DispatchMessage(TimeSpan.Zero))
                            return true;
                    }
                }
            }

            return false;
        }

        public void Dispose()
        {
            lock (gate)
            {
                if (disposed)
                    return;

                foreach (var s in subscriptions)
                {
                    s.Dispose();
                }
                queue.Dispose();
                subscriptions = new Array<Subscription>();
                disposed = true;
            }
        }

        public void Subscribe(Action<IReadOnlyMessage> observer, string subject = null)
        {
            var sub = Subscription.Create(queue, observer, subject);
            lock (gate)
            {
                if (disposed)
                    ThrowObjectDisposed();
                subscriptions = subscriptions.Add(sub);
                peeks = peeks.Add(null);
            }
        }

        private static void ThrowObjectDisposed()
        {
            throw new ObjectDisposedException("Worker has been disposed");
        }

        public bool Unsubscribe(Action<IReadOnlyMessage> observer)
        {
            lock (gate)
            {
                if (disposed)
                    ThrowObjectDisposed();
                int idx = subscriptions.IndexOf(s => s.Action == observer);
                if (idx < 0)
                    return false;
                subscriptions = subscriptions.RemoveAt(idx);
                peeks = peeks.RemoveAt(idx);
                return true;
            }
        }


        /// <summary>A subscription WITHOUT a filter on subject/topic/label</summary>
        class Subscription : IDisposable
        {
            public static Subscription Create(MSMQ.MessageQueue queue, Action<IReadOnlyMessage> action, string subject)
            {
                return string.IsNullOrEmpty(subject) ? new Subscription(queue, action) : new FilteredSubscription(queue, action, subject);
            }

            protected volatile bool disposed;

            public MSMQ.MessageQueue Queue { get; }
            public Action<IReadOnlyMessage> Action { get; }

            public Subscription(MSMQ.MessageQueue queue, Action<IReadOnlyMessage> action)
            {
                Queue = queue;
                Action = action;
            }

            public virtual MSMQ.Message Peek(TimeSpan timeout) => Queue.Peek(timeout);

            public virtual Task<MSMQ.Message> PeekAsync(TimeSpan timeout) => Queue.PeekAsync(timeout);

            public virtual bool DispatchMessage(TimeSpan timeout)
            {
                if (disposed)
                    throw new ObjectDisposedException(GetType().Name);

                MSMQ.Message mqMsg = Receive(timeout);
                if (mqMsg == null)
                    return false;

                var msg = new ReadOnlyMsmqMessage(mqMsg);
                Action(msg);
                return true;
            }

            MSMQ.Message Receive(TimeSpan timeout)
            {
                try
                {
                    Queue.MessageReadPropertyFilter = Filters.Read; // read full details
                    return Queue.Receive(timeout);
                }
                catch (MSMQ.MessageQueueException ex) when (ex.MessageQueueErrorCode == MSMQ.MessageQueueErrorCode.IOTimeout)
                {
                    // we have already peeked to see if a message is available, but another thread or process may have already read the message from the queue, hence the timeout
                    return null;
                }
            }

            public void Dispose()
            {
                disposed = true;
            }
        }

        class FilteredSubscription : Subscription
        {
            readonly MSMQ.Cursor cursor;
            readonly Stopwatch sw;
            MSMQ.PeekAction peeking;

            public string Subject { get; }

            public FilteredSubscription(MSMQ.MessageQueue queue, Action<IReadOnlyMessage> action, string subject) : base(queue, action)
            {
                Subject = subject;
                cursor = queue.CreateCursor();
                sw = new Stopwatch();
            }

            public override MSMQ.Message Peek(TimeSpan timeout)
            {
                if (disposed)
                    throw new ObjectDisposedException(GetType() + " " + Subject);

                sw.Reset();
                sw.Start();
                peeking = MSMQ.PeekAction.Current;
                for (;;)
                {
                    // quit if we have run out of time
                    var remaning = timeout - sw.Elapsed;
                    if (remaning < TimeSpan.Zero)
                        return null;

                    // peek the next message
                    var msg = Queue.Peek(remaning, cursor, peeking);
                    if (msg.SubjectMatches(Subject))
                        return msg;

                    // subject did not match, try the next message
                    msg.Dispose();
                    peeking = MSMQ.PeekAction.Next;
                }
            }

            public async override Task<MSMQ.Message> PeekAsync(TimeSpan timeout)
            {
                if (disposed)
                    throw new ObjectDisposedException(GetType() + " " + Subject);

                sw.Reset();
                sw.Start();
                peeking = MSMQ.PeekAction.Current;
                for (;;)
                {
                    // quit if we have run out of time
                    var remaning = timeout - sw.Elapsed;
                    if (remaning < TimeSpan.Zero)
                        return null;

                    // peek the next message
                    var msg = await Queue.PeekAsync(remaning, cursor, peeking);
                    if (msg.SubjectMatches(Subject))
                        return msg;

                    // subject did not match, try the next message
                    msg.Dispose();
                    peeking = MSMQ.PeekAction.Next;
                }
            }

            public override bool DispatchMessage(TimeSpan timeout)
            {
                if (disposed)
                    throw new ObjectDisposedException(GetType() + " " + Subject);

                MSMQ.Message msg = Receive(timeout);
                if (msg == null)
                    return false;

                Debug.Assert(msg.SubjectMatches(Subject), $"peeking matched the subject to '{Subject}' but the received message label of '{msg.Label}' does not match");

                var rom = new ReadOnlyMsmqMessage(msg);
                Action(rom);
                return true;
            }

            MSMQ.Message Receive(TimeSpan timeout)
            {
                try
                {
                    Queue.MessageReadPropertyFilter = Filters.Read; // read full details
                    return Queue.Receive(timeout, cursor);
                }
                catch (MSMQ.MessageQueueException ex) when (ex.MessageQueueErrorCode == MSMQ.MessageQueueErrorCode.IOTimeout)
                {
                    // we have already peeked to see if a message is available, but another thread or process may have already read the message from the queue, hence the timeout
                    return null;
                }
            }

        }
    }

}