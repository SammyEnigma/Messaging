using System;
using System.Diagnostics.Contracts;
using Rv = TIBCO.Rendezvous;

namespace Messaging.TibcoRv
{
    /// <summary>transport to reliable RV messages</summary>
    public class RvTransport : ITransport, IDisposable
    {
        readonly object _gate = new object(); // shared the lock with the transport and workers so we dont drop messages when removing subscriptions
        readonly Rv.Transport _transport;
        readonly Rv.QueueGroup _queues;

        public Uri Destination { get; }

        public RvTransport(Uri destination, Rv.Transport transport)
        {
            Contract.Requires(destination != null);
            Contract.Requires(transport != null);
            Contract.Requires(string.IsNullOrEmpty(destination.PathAndQuery), "You cannot specify the topic for RV transports");
            Destination = destination;
            _transport = transport;
            _queues = new Rv.QueueGroup();
            _queues.Add(new Rv.Queue());
            _queues.Add(Rv.Queue.Default);  // default queue must be dispatched
        }

        /// <summary>Sends a message to the destination of this transport</summary>
        /// <exception cref="T:System.ArgumentNullException">thrown when <paramref name="msg" /> is not set</exception>
        public void Send(IReadOnlyMessage msg)
        {
            using (var rvm = Converter.ToRvMessge(msg, Destination))
                _transport.Send(rvm);
        }

        public void Dispose()
        {
            _transport.Dispose();
        }

        /// <summary>Creates a <see cref="T:Messaging.IWorker" /> to receive messages from the <see cref="P:Messaging.ITransport.Destination" /></summary>
        public IWorker CreateWorker() => new RvWorker(_transport, Destination);
    }

}
