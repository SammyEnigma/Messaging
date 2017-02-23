using System;

namespace Messaging.Msmq
{
    public class MsmqMessagingFactory : IMessagingFactory
    {
        public bool TryCreate(Uri address, out IMessaging transport)
        {
            var q = Converter.GetOrAddQueue(address);
            if (q == null)
            {
                transport = null;
                return false;
            }
            transport = new MsmqMessaging(address, q);
            return true;
        }

        public bool TryCreateMultiSubject(Uri address, out IMultiSubjectMessaging subscriptionGroup)
        {
            var q = Converter.GetOrAddQueue(address);
            if (q == null)
            {
                subscriptionGroup = null;
                return false;
            }
            subscriptionGroup = new MsmqMultiSubjectMessaging(q, address);
            return true;
        }
    }
}
