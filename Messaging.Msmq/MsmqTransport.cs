using Messaging.Subscriptions;
using System;
using System.Collections.Immutable;
using MSMQ = System.Messaging;

namespace Messaging.Msmq
{
    public class MsmqTransport : ITransport
    {
        internal readonly object _gate = new object();
        internal readonly MSMQ.MessageQueue _queue;

        public Uri Destination { get; }

        protected MsmqTransport(Uri destination, MSMQ.MessageQueue queue) 
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            if (queue == null)
                throw new ArgumentNullException(nameof(queue));
            _queue = queue;
            Destination = destination;
        }

        public void Send(IReadOnlyMessage msg)
        {
            var m = Converter.ToMsmqMessage(msg);
            _queue.Send(m);
        }

        public IWorker CreateWorker() => new MsmqWorker(_queue, Destination);

        public void Dispose()
        {
            _queue.Dispose();
        }
    }
}