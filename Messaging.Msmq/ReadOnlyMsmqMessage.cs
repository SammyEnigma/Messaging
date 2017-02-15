using System;
using System.Diagnostics.Contracts;
using MSMQ = System.Messaging;

namespace Messaging.Msmq
{
    class ReadOnlyMsmqMessage : IReadOnlyMessage
    {
        readonly MSMQ.Message _msg;
        ReadOnlyMsmqMessageHeaders _headers;

        public ReadOnlyMsmqMessage(MSMQ.Message mqm)
        {
            Contract.Requires(mqm != null);
            this._msg = mqm;
        }

        public object Body => _msg.Body;

        public IReadOnlyMessageHeaders Headers
        {
            get
            {
                if (_headers == null)
                    _headers = new ReadOnlyMsmqMessageHeaders(_msg); // lazy creation of the headers
                return _headers;
            }
        }

        public string Subject => _msg.Label;

        public bool HasHeaders => _msg.Extension != null || _msg.Extension.Length > 0;

        public void Acknowledge()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            _msg.Dispose();
        }
    }
}
