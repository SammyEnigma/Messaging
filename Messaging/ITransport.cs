using System;

namespace Messaging
{
    /// <summary>Allows for sending and receiving messages</summary>
    public interface ITransport : IDisposable
    {
        /// <summary>Where messages are send to and received from</summary>
        Uri Destination { get; }

        /// <summary>Sends a message to the destination of this transport</summary>
        /// <exception cref="ArgumentNullException">thrown when <paramref name="msg"/> is not set</exception>
        void Send(IReadOnlyMessage msg);

        /// <summary>Creates a <see cref="IWorker"/> to receive messages from the <see cref="Destination"/></summary>
        IWorker CreateWorker();
    }
}