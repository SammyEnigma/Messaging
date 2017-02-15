using System;

namespace Messaging
{
    /// <summary>Creates <see cref="ITransport"/> based on a Url. Also see extension methods <seealso cref="Extensions.Create(ITransportFactory, string)"/> and <seealso cref="Extensions.Create(ITransportFactory, Uri)"/></summary>
    public interface ITransportFactory
    {
        /// <summary>Tries to create a transport for a <paramref name="destination"/></summary>
        /// <param name="destination">The URL of the transport</param>
        /// <param name="transport">The transport created, or null if no transport cal be created</param>
        /// <returns>TRUE if a transport was created, otherwise FALSE</returns>
        /// <example>To create a transport to an MSMQ private queue: msmq://Computer/PRIVATE$/queue</example>
        /// <exception cref="ArgumentNullException">If <paramref name="destination"/> is null</exception>
        bool TryCreate(Uri destination, out ITransport transport);
    }

    
}