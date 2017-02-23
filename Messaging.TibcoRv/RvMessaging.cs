using System;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;
using Rv = TIBCO.Rendezvous;

namespace Messaging.TibcoRv
{
    /// <summary>transport to reliable RV messages</summary>
    class RvMessaging : IMessaging, IDisposable
    {
        readonly object _gate = new object(); // shared the lock with the transport and workers so we dont drop messages when removing subscriptions
        readonly Rv.Transport _transport;

        public Uri Address { get; }

        public RvMessaging(Uri address, Rv.Transport transport)
        {
            Contract.Requires(address != null);
            Contract.Requires(transport != null);
            Address = address;
            _transport = transport;
        }
        
        /// <summary>Sends a message to the destination of this transport</summary>
        /// <exception cref="T:System.ArgumentNullException">thrown when <paramref name="msg" /> is not set</exception>
        public void Send(IReadOnlyMessage msg)
        {
            using (var rvm = Converter.ToRvMessge(msg, Address))
                _transport.Send(rvm);
        }

        public void Dispose()
        {
            _transport.Dispose();
        }

        public Task<IReadOnlyMessage> ReceiveAsync(TimeSpan timeout)
        {
            //TODO: is this safe to do?  What if you created just to send, we dont want to fill up the queue, so when do we subscribe?  Or limit the queue?
            //what if we had explicit Start and Stop methods?
            throw new NotImplementedException();
        }
    }
}
