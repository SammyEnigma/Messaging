using System;

namespace Messaging
{
    /// <summary>Creates <see cref="ITransport"/> based on a Url</summary>
    public interface ITransportFactory
    {
        /// <summary>Create a transport for a <paramref name="url"/></summary>
        /// <param name="destination">The URL of the transport</param>
        /// <example>To create a transport to an MSMQ public queue: msmq://Computer/queue</example>
        /// <example>To create a transport to an MSMQ private queue: msmq://Computer/PRIVATE$/queue</example>
        /// <exception cref="ArgumentNullException"></exception>
        ITransport New(Uri destination);
    }

}