using System;
using System.Collections.Generic;

namespace Messaging.Subscriptions
{
    public class MessageSubject : IObservable<IReadOnlyMessage>
    {
        readonly object _gate; // shared the lock with the transport and workers so we dont drop messages when removing subscriptions
        protected ImmutableArray<MessageSubscription> _subscriptions = ImmutableArray<MessageSubscription>.Empty;

        public string Subject { get; }

        public MessageSubject(object gate, string subject)
        {
            if (gate == null) throw new ArgumentNullException(nameof(gate));
            if (subject == null) throw new ArgumentNullException(nameof(subject));
            _gate = gate;   // share the lock with the transport and workers so we dont drop messages when removing subscriptions
            Subject = subject;
        }

        public IDisposable Subscribe(IObserver<IReadOnlyMessage> observer)
        {
            var sub = new MessageSubscription(s => RemoveSubscription(s), observer);
            lock (_gate)
            {
                _subscriptions = _subscriptions.Add(sub);
                if (_subscriptions.Count == 1)
                    OnFirstSubscriptionAdded();
            }
            return sub;
        }

        protected virtual void OnFirstSubscriptionAdded()
        {
            FirstSubscriptionAdded?.Invoke(this, EventArgs.Empty);
        }

        void RemoveSubscription(MessageSubscription sub)
        {
            lock (_gate)
            {
                _subscriptions = _subscriptions.Remove(sub);
                if (_subscriptions.Count == 0)
                    OnLastSubscriptionRemoved();
            }
        }

        protected virtual void OnLastSubscriptionRemoved()
        {
            LastSubscriptionRemoved?.Invoke(this, EventArgs.Empty);
        }

        public event EventHandler FirstSubscriptionAdded;

        public event EventHandler LastSubscriptionRemoved;

        public void OnNext(IReadOnlyMessage msg)
        {
            if (IsInteresting(msg.Subject))
            {
                foreach (var ob in Observers())
                {
                    ob.OnNext(msg);
                }
            }
        }

        public bool IsInteresting(string subject)
        {
            if (_subscriptions.Count == 0) return false;
            if (string.IsNullOrEmpty(subject)) return string.IsNullOrEmpty(Subject);
            return Subject.Length == 0 || subject.StartsWith(Subject, StringComparison.OrdinalIgnoreCase);
        }

        protected IEnumerable<IObserver<IReadOnlyMessage>> Observers()
        {
            var copy = _subscriptions;
            return copy.Select(sub => sub.Observer);
        }
    }

    public class MessageSubscription : IDisposable
    {
        readonly Action<MessageSubscription> disposed;

        public IObserver<IReadOnlyMessage> Observer { get; }

        public MessageSubscription(Action<MessageSubscription> disposed, IObserver<IReadOnlyMessage> observer)
        {
            if (disposed == null) throw new ArgumentNullException(nameof(disposed));
            if (observer == null) throw new ArgumentNullException(nameof(observer));
            Observer = observer;
            this.disposed = disposed;
        }

        public void Dispose()
        {
            disposed(this);
        }
    }

}
