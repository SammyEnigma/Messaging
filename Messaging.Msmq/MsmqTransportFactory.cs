using System;

namespace Messaging.Msmq
{

    class MsmqTransportFactory : TransportFactory
    {

        public override Transport New(Uri destination)
        {
            throw new NotImplementedException();
        }
    }
}
