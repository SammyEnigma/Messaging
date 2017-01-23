using System;
using System.Diagnostics.Contracts;
using TIBCO.Rendezvous;
using Rv = TIBCO.Rendezvous;

namespace Messaging.TibcoRv
{
    class ReadOnlyRvMessage : IReadOnlyMessage, IDisposable
    {
        readonly Rv.Message _msg;
        readonly Uri _source;
        ReadOnlyRvMessageHeaders _headers;
        long _pressure;

        public ReadOnlyRvMessage(Rv.Message msg, Uri source)
        {
            Contract.Requires(source != null);
            Contract.Requires(msg != null);
            _source = source;
            _msg = msg;
            _pressure = msg.Size;
            GC.AddMemoryPressure(_pressure); // RV does not add memory pressure, so we add it here so the CLR knows the underlying RV message contains unmanaged bytes
        }

        public string Subject => _msg.SendSubject;

        public IReadOnlyMessageHeaders Headers
        {
            get
            {
                if (_headers == null)
                    _headers = new ReadOnlyRvMessageHeaders(_msg, _source); // lazy creation of the headers
                return _headers;
            }
        }

        public object Body => _msg.GetField(Fields.Body)?.Value;

        public void Acknowledge()
        {
            if (_msg is CMMessage)
            {
                //TODO: check with works, does GetSource() return the CMListener or just a Listener?
                var cmlistener = _msg.GetSource() as Rv.CMListener;
                cmlistener?.ConfirmMessage(_msg); //TODO: ReadOnlyCmMessage?
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (_pressure < 0) return;
            // release the memory pressure we added in the ctor
            GC.RemoveMemoryPressure(_pressure); 
            _pressure = -1;
            if (disposing)
                _msg.Dispose(); // dispose of the native RV message
        }

        ~ReadOnlyRvMessage()
        {
            Dispose(false); // make sure memory pressure is removed
        }
    }
}
