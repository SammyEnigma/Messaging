using System;
using System.Collections.Generic;
using System.Linq;

namespace Messaging
{
    /// <summary>A <see cref="ITransportFactory"/> that delegates <see cref="ITransport"/> creation to the other <see cref="ITransportFactory"/></summary>
    public class CompositeTransportFactory : ITransportFactory
    {
        readonly ITransportFactory[] factories;

        public CompositeTransportFactory(IEnumerable<ITransportFactory> factories) : this(factories?.ToArray())
        {
        }

        public CompositeTransportFactory(params ITransportFactory[] factories)
        {
            if (factories == null)
                throw new ArgumentNullException(nameof(factories));
            if (factories.Length == 0)
                throw new ArgumentOutOfRangeException(nameof(factories), "At least one factory must be supplied");

            this.factories = factories;
        }

        public bool TryCreate(Uri destination, out ITransport transport)
        {
            foreach (var f in factories)
            {
                if (f.TryCreate(destination, out transport))
                    return true;
            }
            transport = null;
            return false;
        }
    }
}
