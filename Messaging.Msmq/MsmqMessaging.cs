using System;
using MSMQ = System.Messaging;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Messaging.Msmq
{
    /// <summary>Allows sending and receiving messages via a MSMQ message queue</summary>
    class MsmqMessaging : IMessaging
    {
        static readonly Task<MSMQ.Message> noResult = Task.FromResult<MSMQ.Message>(null);
        internal readonly object _gate = new object();
        internal readonly MSMQ.MessageQueue _queue;
        readonly Receiver _receiver;

        public Uri Address { get; }

        public MsmqMessaging(Uri address, MSMQ.MessageQueue queue) 
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));
            if (queue == null)
                throw new ArgumentNullException(nameof(queue));
            _queue = queue;
            Address = address;
            _receiver = Receiver.Create(_queue, Address.AbsolutePath);
        }

        public void Send(IReadOnlyMessage msg)
        {
            using (var m = Converter.ToMsmqMessage(msg))
                _queue.Send(m);
        }

        public void Dispose()
        {
            _queue.Dispose();
        }

        public async Task<IReadOnlyMessage> ReceiveAsync(TimeSpan timeout)
        {
            using (var peeked = await _receiver.PeekAsync(timeout))
                return peeked != null ? _receiver.Receive() : null;
        }

        /// <summary>A message reader WITHOUT a filter on subject/topic/label</summary>
        class Receiver : IDisposable
        {
            public static Receiver Create(MSMQ.MessageQueue queue, string subject)
            {
                return string.IsNullOrEmpty(subject) ? new Receiver(queue) : new FilteredReceiver(queue, subject);
            }

            protected volatile bool disposed;

            protected MSMQ.MessageQueue Queue { get; }

            public Receiver(MSMQ.MessageQueue queue)
            {
                Queue = queue;
            }

            public virtual Task<MSMQ.Message> PeekAsync(TimeSpan timeout)
            {
                return Queue.PeekAsync(timeout);
            }

            public virtual IReadOnlyMessage Receive()
            {
                //TODO: non-transactional recieve support?
                var txn = new MSMQ.MessageQueueTransaction();
                MSMQ.Message msg = ReceiveCore(txn);
                if (msg == null)
                    return null;

                return new ReadOnlyMsmqMessage(msg, txn);
            }

            MSMQ.Message ReceiveCore(MSMQ.MessageQueueTransaction txn)
            {
                try
                {
                    Queue.MessageReadPropertyFilter = Filters.Read; // read full details
                    return Queue.Receive(TimeSpan.Zero, txn);
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

        /// <summary>A message reader with a filter on subject/topic/label</summary>
        class FilteredReceiver : Receiver
        {
            readonly MSMQ.Cursor cursor;
            readonly Stopwatch sw;
            MSMQ.PeekAction peeking;

            public string Subject { get; }

            public FilteredReceiver(MSMQ.MessageQueue queue, string subject) : base(queue)
            {
                Subject = subject;
                cursor = queue.CreateCursor();
                sw = new Stopwatch();
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

            public override IReadOnlyMessage Receive()
            {
                //TODO: this is a transactional receive, what if it's a non-transactional queue?
                var txn = new MSMQ.MessageQueueTransaction();
                MSMQ.Message msg = ReceiveCore(txn);
                if (msg == null)
                    return null;

                Debug.Assert(msg.SubjectMatches(Subject), $"peeking matched the subject to '{Subject}' but the received message label of '{msg.Label}' does not match");
                return new ReadOnlyMsmqMessage(msg, txn);
            }

            MSMQ.Message ReceiveCore(MSMQ.MessageQueueTransaction txn)
            {
                try
                {
                    Queue.MessageReadPropertyFilter = Filters.Read; // read full details
                    return Queue.Receive(TimeSpan.Zero, cursor, txn);
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