using System;

namespace Messaging.Msmq
{

    class MsmqTransportFactory : ITransportFactory
    {
        public ITransport New(Uri destination)
        {
            throw new NotImplementedException();
        }
    }
}
