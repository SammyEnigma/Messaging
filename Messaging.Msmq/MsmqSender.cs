using System;
using MSMQ = System.Messaging;

namespace Messaging.Msmq
{
    abstract class MsmqSender : IDisposable
    {
        protected readonly MSMQ.MessageQueue _queue;
        protected bool _isTransactional;
        bool _recoverable; // if false then express message that can be lost on machine reboot

        public MsmqSender(MSMQ.MessageQueue queue, bool recoverable)
        {
            if (queue == null)
                throw new ArgumentNullException(nameof(queue));
            this._queue = queue;
            _isTransactional = queue.IsTransactional();
            _recoverable = recoverable;
        }

        public void Send(IReadOnlyMessage msg)
        {
            try
            {
                using (var m = Converter.ToMsmqMessage(msg))
                {
                    m.Recoverable = _recoverable;
                    if (_isTransactional)
                        _queue.Send(m, MSMQ.MessageQueueTransactionType.Single);
                    else
                        _queue.Send(m);
                }
            }
            catch (MSMQ.MessageQueueException ex) when (ex.MessageQueueErrorCode == MSMQ.MessageQueueErrorCode.TransactionUsage)
            {
                // we attempted a transactions receive on a non-transactional queue
                _isTransactional = !_isTransactional;
                Send(msg); // recurse
            }
        }

        public virtual void Dispose()
        {
            _queue.Dispose();
        }
    }
}