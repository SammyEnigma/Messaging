using System;
using System.Threading.Tasks;

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

        /// <summary>Creates a listener for a topic, optionally filtering the messages to receive.</summary>
        IWorker CreateWorker();
    }

    /// <summary>A reader of messages</summary>
    public interface IWorker : IDisposable
    {
        /// <summary>Blocking call that reads one messag from the underlying transport</summary>
        /// <param name="timeout">Optional timeout to reviece a message</param>
        bool DispatchMessage(TimeSpan timeout);

        /// <summary>Creates a subscription</summary>
        /// <param name="subject">A filter used to only receives messages whoose <see cref="Message.Subject"/> starts with this value</param>
        void Subscribe(Action<IReadOnlyMessage> observer, string subject = null);

        bool Unsubscribe(Action<IReadOnlyMessage> observer);
    }
}