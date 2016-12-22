using System;

namespace Messaging
{
    /// <summary>Allows for sending and receiving messages</summary>
    /// <remarks>Abstract class so we can extend it (with default behaviour) rather than doing breaking changes to and interface</remarks>
    public abstract class Transport
    {
        /// <summary>Sets <see cref="Destination"/></summary>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="destination"/> is null</exception>
        protected Transport(Uri destination)
        {
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            Destination = destination;
        }

        /// <summary>Where messages are send to and received from</summary>
        public Uri Destination { get; }

        /// <summary>Sends a message to the destination of this transport</summary>
        /// <exception cref="ArgumentNullException">thrown when <paramref name="msg"/> is not set</exception>
        public abstract void Send(IReadOnlyMessage msg);

        /// <summary>Creates a listener for a topic, optionally filtering the messages to receive.</summary>
        /// <param name="subject">A filter used to only receives messages whoose <see cref="Message.Subject"/> starts with this value</param>
        public abstract IObservable<IReadOnlyMessage> NewListener(string subject = null);

        /// <summary>Create a worker that reads the messages for the listeners, <see cref="NewListener(string)"/></summary>
        /// <returns>A worker that stops when <see cref="IDisposable.Dispose"/> is called</returns>
        /// <remarks>This creates a <see cref="System.Threading.Thread"/> to read messages</remarks>
        public abstract IDisposable StartWorker(string name = null);
    }

}