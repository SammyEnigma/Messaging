using Messaging.Subscriptions;
using System;
using System.Threading.Tasks;
using MSMQ = System.Messaging;

namespace Messaging.Msmq
{
    class MsmqWorker : IWorker
    {
        readonly object gate = new object();
        readonly MSMQ.MessageQueue queue;
        readonly Uri uri;
        ImmutableArray<Subscription> subscriptions;
        Task<MSMQ.Message>[] tasks = new Task<MSMQ.Message>[0];
        bool disposed;

        public MsmqWorker(MSMQ.MessageQueue queue, Uri uri)
        {
            if (queue == null)
                throw new ArgumentNullException(nameof(queue));
            if (uri == null)
                throw new ArgumentNullException(nameof(uri));
            this.queue = queue;
            this.uri = uri;
        }

        public bool DispatchMessage(TimeSpan timeout)
        {
            lock(gate)
            {
                if (subscriptions.Count == 1)
                    return subscriptions[0].DispatchMessage();

                if (subscriptions.Count > 1)
                   return MultiDispatch(timeout);

                return false;
            }
        }

        private bool MultiDispatch(TimeSpan timeout)
        {
            throw new NotImplementedException();
            //int i = 0;
            //foreach (var s in subscriptions)
            //{
            //    tasks[i++] = s.PeekAsync(timeout);
            //}

            //int idx = Task.WaitAny(tasks, timeout);
            //if (idx < 0)
            //    return false;

            //var sub = subscriptions[idx];
            //sub.DispatchMessage();
            //return true;
        }

        public void Dispose()
        {
            lock (gate)
            {
                foreach (var s in subscriptions)
                {
                    s.Dispose();
                }
                subscriptions = new ImmutableArray<Subscription>();
            }
        }

        public void Subscribe(Action<IReadOnlyMessage> observer, string subject = null)
        {
            var sub = new Subscription(queue, observer, subject);
            lock (gate)
            {
                subscriptions = subscriptions.Add(sub);
                tasks = new Task<MSMQ.Message>[subscriptions.Count];
            }
        }

        public bool Unsubscribe(Action<IReadOnlyMessage> observer)
        {
            lock (gate)
            {
                int idx = subscriptions.IndexOf(s => s.Action == observer);
                if (idx < 0)
                    return false;
                subscriptions = subscriptions.RemoveAt(idx);
                tasks = new Task<MSMQ.Message>[subscriptions.Count];
                return true;
            }
        }

        class Subscription : IDisposable
        {
            static readonly MSMQ.MessagePropertyFilter peekFilter;
            static readonly MSMQ.MessagePropertyFilter readFilter;

            static Subscription()
            {
                // setup the filter used for peeking
                peekFilter = new MSMQ.MessagePropertyFilter();
                peekFilter.ClearAll();
                peekFilter.Label = true;
                //peekFilter.LookupId = true; //TODO: by  ID or LookupId, which is faster?

                // we read the body too
                readFilter = new MSMQ.MessagePropertyFilter { Body = true };
            }

            readonly object gate = new object();
            Task<MSMQ.Message> lastPeek;
            bool disposed;

            public MSMQ.MessageQueue Queue { get; }
            public Action<IReadOnlyMessage> Action { get; }
            public string Subject { get; }

            public Subscription(MSMQ.MessageQueue queue, Action<IReadOnlyMessage> action, string subject)
            {
                Queue = queue;
                Subject = subject;
                Action = action;
            }

            public MSMQ.Message Peek(TimeSpan timeout)
            {
                try
                {
                    lock (gate)
                    {
                        Queue.MessageReadPropertyFilter = peekFilter; // just peek minimal details
                        return Queue.Peek(timeout);
                    }
                }
                catch (MSMQ.MessageQueueException ex) when (ex.MessageQueueErrorCode == MSMQ.MessageQueueErrorCode.IOTimeout)
                {
                    return null;
                }
            }

            public IAsyncResult PeekAsync(TimeSpan timeout)
            {
                lock (gate)
                {
                    Queue.MessageReadPropertyFilter = peekFilter; // just peek minimal details
                    return Queue.BeginPeek(timeout);
                }
            }

            MSMQ.Message EndPeek(IAsyncResult res)
            {
                try
                {
                    lock (gate)
                    {
                        return Queue.EndPeek(res);
                    }
                }
                catch (MSMQ.MessageQueueException ex) when (ex.MessageQueueErrorCode == MSMQ.MessageQueueErrorCode.IOTimeout)
                {
                    return null;
                }
            }

            public bool DispatchMessage()
            {
                lock (gate)
                {
                    if (disposed)
                        throw new ObjectDisposedException(GetType() + " " + Subject);

                    // clear the peek task so we peek the next message in the queue
                    lastPeek?.Dispose();
                    lastPeek = null;

                    MSMQ.Message mqMsg;
                    try
                    {
                        Queue.MessageReadPropertyFilter = readFilter; // read full details
                        mqMsg = Queue.Receive(TimeSpan.FromMilliseconds(1));
                    }
                    catch (MSMQ.MessageQueueException ex) when (ex.MessageQueueErrorCode == MSMQ.MessageQueueErrorCode.IOTimeout)
                    {
                        // we have already peeked to see if a message is available, but another thread or process may have already read the message from the queue, hence the timeout
                        return false;
                    }

                    if (!SubjectMatches(mqMsg))
                    {
                        mqMsg.Dispose();
                        return false;
                    }

                    var msg = new ReadOnlyMsmqMessage(mqMsg);
                    Action(msg);
                    return true;
                }
            }

            public void Dispose()
            {
                lock (gate)
                {
                    disposed = true;
                }
            }

            internal bool SubjectMatches(MSMQ.Message msg)
            {
                if (string.IsNullOrEmpty(Subject)) return true;
                return string.Equals(Subject, msg.Label);
            }
        }
    }
}