using System;

namespace Messaging
{
    /// <summary>A way of reading messages for many subjects using a single thread</summary>
    /// <remarks>Useful for receiving the price of many markets, i.e. multicast</remarks>
    public interface IMultiSubjectMessaging : IDisposable
    {
        Uri Address { get; }

        /// <summary>Sends a message to the <see cref="Address"/></summary>
        /// <exception cref="ArgumentNullException">thrown when <paramref name="msg"/> is not set</exception>
        void Send(IReadOnlyMessage msg);

        /// <summary>Blocking call that reads one messag from the underlying transport</summary>
        /// <param name="timeout">Optional timeout to reviece a message</param>
        bool DispatchMessage(TimeSpan timeout);

        /// <summary>Creates a subscription</summary>
        /// <param name="subject">A filter used to only receives messages whoose <see cref="Message.Subject"/> starts with this value</param>
        void Subscribe(Action<IReadOnlyMessage> observer, string subject = null);

        /// <summary>Removes a subscription</summary>
        bool Unsubscribe(Action<IReadOnlyMessage> observer);
    }
}