using System;

namespace Messaging
{
    /// <summary>Creates <see cref="IMessaging"/> based on a Url. Also see extension methods <seealso cref="Extensions.Create(IMessagingFactory, string)"/> and <seealso cref="Extensions.Create(IMessagingFactory, Uri)"/></summary>
    public interface IMessagingFactory
    {
        /// <summary>Tries to create a <see cref="IMessaging"/> for a <paramref name="address"/></summary>
        /// <param name="address">The URL to send to or receive from</param>
        /// <param name="messaging">The <see cref="IMessaging"/> created, or null if no <see cref="IMessaging"/> can be created</param>
        /// <returns>TRUE if a <see cref="IMessaging"/> was created, otherwise FALSE</returns>
        /// <example>To create a <see cref="IMessaging"/> to an MSMQ private queue: msmq://Computer/PRIVATE$/queue</example>
        /// <exception cref="ArgumentNullException">If <paramref name="address"/> is null</exception>
        bool TryCreate(Uri address, out IMessaging messaging);

        bool TryCreateMultiSubject(Uri address, out IMultiSubjectMessaging multiSubject);
    }

    
}