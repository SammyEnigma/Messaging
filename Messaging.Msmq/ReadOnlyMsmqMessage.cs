using System;
using System.Diagnostics.Contracts;
using MSMQ = System.Messaging;

namespace Messaging.Msmq
{
    class ReadOnlyMsmqMessage : IReadOnlyMessage
    {
        readonly MSMQ.Message _msg;
        readonly MSMQ.MessageQueueTransaction _txn;
        ReadOnlyMsmqMessageHeaders _headers;
        bool _committed;

        public ReadOnlyMsmqMessage(MSMQ.Message mqm, MSMQ.MessageQueueTransaction txn = null)
        {
            Contract.Requires(mqm != null);
            _msg = mqm;
            _txn = txn;
        }

        public string Subject => _msg.Label;

        public bool HasHeaders => _msg.Extension != null || _msg.Extension.Length > 0;

        public IReadOnlyMessageHeaders Headers
        {
            get
            {
                if (_headers == null)
                    _headers = new ReadOnlyMsmqMessageHeaders(_msg); // lazy creation of the headers
                return _headers;
            }
        }

        public object Body => _msg.Body;

        public void Acknowledge()
        {
            if (_committed)
                return;

            _committed = true;
            _txn?.Commit();
            _txn?.Dispose();
        }

        public void Dispose()
        {
            if (!_committed)
                _txn?.Abort();
            _txn?.Dispose();
            _msg.Dispose();
        }
    }
}
