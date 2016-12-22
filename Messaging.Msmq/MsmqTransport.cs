using Messaging.Subscriptions;
using System;
using System.Collections.Immutable;
using MSMQ = System.Messaging;

namespace Messaging.Msmq
{
    public class MsmqTransport : Transport
    {
        readonly EventHandler _subAdded;
        readonly EventHandler _subRemoved;
        internal readonly object _gate = new object();
        internal readonly MSMQ.MessageQueue _queue;
        internal ImmutableDictionary<string, MessageSubject> subjects = ImmutableDictionary<string, MessageSubject>.Empty;
        int subscriptionCount;

        protected MsmqTransport(Uri destination, MSMQ.MessageQueue queue) : base(destination)
        {
            if (queue == null) throw new ArgumentNullException(nameof(queue));
            _queue = queue;
            _subAdded = SubscriptionAdded;
            _subRemoved = SubscriptionRemoved;
        }

        public override void Send(IReadOnlyMessage msg)
        {
            var m = Converter.ToMsmqMessage(msg);
            _queue.Send(m);
        }

        public override IObservable<IReadOnlyMessage> NewListener(string subject = null)
        {
            MessageSubject ms;
            if (subject == null) subject = "";
            lock (_gate)
            {
                if (subjects.TryGetValue(subject, out ms)) return ms;
                ms = new MessageSubject(_gate, subject); // sahare the same lock
                ms.FirstSubscriptionAdded += _subAdded;
                ms.LastSubscriptionRemoved += _subRemoved;
                subjects = subjects.Add(subject, ms);
            }
            return ms;
        }

        private void SubscriptionAdded(object sender, EventArgs args)
        {
            lock (_gate)
            {
                subscriptionCount++;
                if (subscriptionCount == 1)
                    ResumeWorkers();
            }
        }

        private void ResumeWorkers()
        {
            throw new NotImplementedException();
        }

        private void SubscriptionRemoved(object sender, EventArgs args)
        {
            lock (_gate)
            {
                subscriptionCount--;
                if (subscriptionCount == 0)
                    PauseWorkers();
            }
        }

        private void PauseWorkers()
        {
            throw new NotImplementedException();
        }

        public override IDisposable StartWorker(string name = null)
        {

            throw new NotImplementedException();
        }

    }
}