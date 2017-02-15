using System;

namespace Messaging
{
    /// <summary>A reader of messages</summary>
    public interface IWorker : IDisposable
    {
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