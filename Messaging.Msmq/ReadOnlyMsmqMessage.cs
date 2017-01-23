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

        public Uri ReplyTo
        {
            get
            {
                Uri replyTo = null;
                if (_msg.ResponseQueue != null)
                {
                    replyTo = Converter.QueueNameToUri(_msg.ResponseQueue);
                }
                if (replyTo == null && Headers.ContainsKey(nameof(ReplyTo)))
                {
                    object val = Headers[nameof(ReplyTo)];
                    replyTo = new Uri(val.ToString());
                }
                return replyTo;
            }
        }

        public string Subject => _msg.Label;

        public void Acknowledge()
        {
            throw new NotImplementedException();
        }
    }
}
