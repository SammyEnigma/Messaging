using System;
using System.Threading.Tasks;

namespace Messaging
{
    /// <summary>Allows for sending and receiving messages</summary>
    public interface IMessaging : IDisposable
    {
        /// <summary>Where messages are sent to and received from</summary>
        Uri Address { get; }

        /// <summary>Sends a message to the destination of this transport</summary>
        /// <exception cref="ArgumentNullException">thrown when <paramref name="msg"/> is not set</exception>
        void Send(IReadOnlyMessage msg);

        /// <summary>A pull-based message receiver, typically used to receive messages from a message queue</summary>
        /// <remarks>Calling <see cref="ReceiveAsync"/> concurrently is NOT allowed</remarks>
        Task<IReadOnlyMessage> ReceiveAsync(TimeSpan timeout);
    }
}