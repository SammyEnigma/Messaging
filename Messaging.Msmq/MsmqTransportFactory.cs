using System;

namespace Messaging.Msmq
{
    public class MsmqTransportFactory : ITransportFactory
    {
        public bool TryCreate(Uri destination, out ITransport transport)
        {
            var q = Converter.GetOrAddQueue(destination);
            if (q == null)
            {
                transport = null;
                return false;
            }
            transport = new MsmqTransport(destination, q);
            return true;
        }
    }
}
