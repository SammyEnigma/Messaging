using System;

namespace Messaging
{
    /// <summary>Creates <see cref="Transport"/> based on a Url</summary>
    /// <remarks>Abstract class so we can extend it (with default behaviour) rather than doing breaking changes to and interface</remarks>
    public abstract class TransportFactory
    {
        /// <summary>Create a transport for a <paramref name="url"/></summary>
        /// <param name="destination">The URL of the transport</param>
        /// <example>To create a transport to an MSMQ public queue: msmq://Computer/queue</example>
        /// <example>To create a transport to an MSMQ private queue: msmq://Computer/PRIVATE$/queue</example>
        /// <exception cref="ArgumentNullException"></exception>
        public abstract Transport New(Uri destination);
    }

}