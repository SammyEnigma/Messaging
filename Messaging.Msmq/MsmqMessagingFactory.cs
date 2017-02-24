using System;

namespace Messaging.Msmq
{
    public class MsmqMessagingFactory : IMessagingFactory
    {
        public bool TryCreate(Uri address, out IMessaging transport)
        {
            QueueDetails details;
            var q = Converter.GetOrAddQueue(address, out details);
            if (q == null)
            {
                transport = null;
                return false;
            }
            transport = new MsmqMessaging(address, q, details);
            return true;
        }

        public bool TryCreateMultiSubject(Uri address, out IMultiSubjectMessaging subscriptionGroup)
        {
            QueueDetails details;
            var q = Converter.GetOrAddQueue(address, out details);
            if (q == null)
            {
                subscriptionGroup = null;
                return false;
            }
            subscriptionGroup = new MsmqMultiSubjectMessaging(address, q, details);
            return true;
        }
    }
}
