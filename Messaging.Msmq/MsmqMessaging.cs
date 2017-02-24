using System;
using MSMQ = System.Messaging;
using System.Threading.Tasks;
using System.Diagnostics;

namespace Messaging.Msmq
{
    /// <summary>Allows sending and receiving messages via a MSMQ message queue</summary>
    class MsmqMessaging : MsmqSender, IMessaging
    {
        static readonly Task<MSMQ.Message> noResult = Task.FromResult<MSMQ.Message>(null);
        readonly Receiver _receiver;

        public Uri Address { get; }

        public MsmqMessaging(Uri address, MSMQ.MessageQueue queue, QueueDetails details) : base(queue, details.Recoverable)
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));
            Address = address;
            _receiver = Receiver.Create(queue, _isTransactional, details.Subject);
        }

        public async Task<IReadOnlyMessage> ReceiveAsync(TimeSpan timeout)
        {
            using (var peeked = await _receiver.PeekAsync(timeout))
                return peeked != null ? _receiver.Receive() : null;
        }
    }

    /// <summary>A message reader WITHOUT a filter on subject/topic/label</summary>
    class Receiver
    {
        protected readonly MSMQ.MessageQueue _queue;
        protected bool _isTransactional;

        public static Receiver Create(MSMQ.MessageQueue queue, bool isTransactional, string subject)
        {
            return string.IsNullOrEmpty(subject) ? new Receiver(queue, isTransactional) : new FilteredReceiver(queue, isTransactional, subject);
        }

        public Receiver(MSMQ.MessageQueue queue, bool isTransactional)
        {
            _queue = queue;
            _isTransactional = isTransactional;
        }

        public virtual MSMQ.Message Peek(TimeSpan timeout) => _queue.Peek(timeout);

        public virtual Task<MSMQ.Message> PeekAsync(TimeSpan timeout) => _queue.PeekAsync(timeout);

        public IReadOnlyMessage Receive()
        {
            var txn = _isTransactional ? new MSMQ.MessageQueueTransaction() : null;
            txn?.Begin();

            MSMQ.Message msg = Receive(txn);
            if (msg == null)
                return null;

            return new ReadOnlyMsmqMessage(msg, txn);
        }

        MSMQ.Message Receive(MSMQ.MessageQueueTransaction txn)
        {
            try
            {
                _queue.MessageReadPropertyFilter = Filters.Read; // read full details
                return RecieveCore(txn);
            }
            catch (MSMQ.MessageQueueException ex) when (ex.MessageQueueErrorCode == MSMQ.MessageQueueErrorCode.IOTimeout)
            {
                // we have already peeked to see if a message is available, but another thread or process may have already read the message from the queue, hence the timeout
                return null;
            }
            catch (MSMQ.MessageQueueException ex) when (ex.MessageQueueErrorCode == MSMQ.MessageQueueErrorCode.TransactionUsage)
            {
                // we attempted a transactions receive on a non-transactional queue
                _isTransactional = false;
                txn.Dispose();
                return Receive(null); // recurse without transaction
            }
        }

        protected virtual MSMQ.Message RecieveCore(MSMQ.MessageQueueTransaction txn)
        {
            return txn == null ? _queue.Receive(TimeSpan.Zero) : _queue.Receive(TimeSpan.Zero, txn);
        }

    }

    /// <summary>A message reader with a filter on subject/topic/label</summary>
    class FilteredReceiver : Receiver
    {
        readonly string _subject;
        readonly MSMQ.Cursor _cursor;
        readonly Stopwatch _stopWatch;

        public FilteredReceiver(MSMQ.MessageQueue queue, bool isTransactional, string subject) : base(queue, isTransactional)
        {
            _subject = subject;
            _cursor = queue.CreateCursor();
            _stopWatch = new Stopwatch();
        }

        public override MSMQ.Message Peek(TimeSpan timeout)
        {
            _stopWatch.Reset();
            _stopWatch.Start();
            var peekType = MSMQ.PeekAction.Current;
            for (;;)
            {
                // quit if we have run out of time
                var remaning = timeout - _stopWatch.Elapsed;
                if (remaning < TimeSpan.Zero)
                    return null;

                // peek the next message
                var msg = _queue.Peek(remaning, _cursor, peekType);
                if (IsInterestingSubject(msg))
                    return msg;

                // subject did not match, try the next message
                msg.Dispose();
                peekType = MSMQ.PeekAction.Next;
            }
        }

        public async override Task<MSMQ.Message> PeekAsync(TimeSpan timeout)
        {
            _stopWatch.Reset();
            _stopWatch.Start();
            var peekType = MSMQ.PeekAction.Current;
            for (;;)
            {
                // quit if we have run out of time
                var remaning = timeout - _stopWatch.Elapsed;
                if (remaning < TimeSpan.Zero)
                    return null;

                // peek the next message
                var msg = await _queue.PeekAsync(remaning, _cursor, peekType);
                if (IsInterestingSubject(msg))
                    return msg;

                // subject did not match, try the next message
                msg.Dispose();
                peekType = MSMQ.PeekAction.Next;
            }
        }

        protected virtual bool IsInterestingSubject(MSMQ.Message msg) => msg.SubjectMatches(_subject);

        protected override MSMQ.Message RecieveCore(MSMQ.MessageQueueTransaction txn)
        {
            var msg = txn == null ? _queue.Receive(TimeSpan.Zero, _cursor) : _queue.Receive(TimeSpan.Zero, _cursor, txn);
            Debug.Assert(IsInterestingSubject(msg), $"peeking said this message was interesting, but the received message label of '{msg.Label}' is not!");
            return msg;
        }
    }

}