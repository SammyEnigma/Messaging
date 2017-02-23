using System;
using System.Collections.Generic;
using System.Linq;

namespace Messaging
{
    /// <summary>A <see cref="IMessagingFactory"/> that delegates <see cref="IMessaging"/> creation to the other <see cref="IMessagingFactory"/></summary>
    public class CompositeMessagingFactory : IMessagingFactory
    {
        readonly IMessagingFactory[] factories;

        public CompositeMessagingFactory(IEnumerable<IMessagingFactory> factories) : this(factories?.ToArray())
        {
        }

        public CompositeMessagingFactory(params IMessagingFactory[] factories)
        {
            if (factories == null)
                throw new ArgumentNullException(nameof(factories));
            if (factories.Length == 0)
                throw new ArgumentOutOfRangeException(nameof(factories), "At least one factory must be supplied");

            this.factories = factories;
        }

        public bool TryCreate(Uri address, out IMessaging messaging)
        {
            foreach (var f in factories)
            {
                if (f.TryCreate(address, out messaging))
                    return true;
            }
            messaging = null;
            return false;
        }

        public bool TryCreateMultiSubject(Uri address, out IMultiSubjectMessaging subscriptionGroup)
        {
            foreach (var f in factories)
            {
                if (f.TryCreateMultiSubject(address, out subscriptionGroup))
                    return true;
            }
            subscriptionGroup = null;
            return false;
        }
    }
}
