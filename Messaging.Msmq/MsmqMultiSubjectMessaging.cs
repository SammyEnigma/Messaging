using System;
using System.Collections.Generic;
using MSMQ = System.Messaging;

namespace Messaging.Msmq
{
    class MsmqMultiSubjectMessaging : MsmqSender, IMultiSubjectMessaging
    {
        readonly object _gate = new object();
        readonly Dictionary<string, Action<IReadOnlyMessage>> _subscriptions = new Dictionary<string, Action<IReadOnlyMessage>>();
        readonly MultiFilteredReciever _receiver;
        readonly string _defaultSubject;
        bool _disposed;

        public Uri Address { get; }

        public MsmqMultiSubjectMessaging(Uri address, MSMQ.MessageQueue queue, QueueDetails details) : base(queue, details.Recoverable)
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));
            Address = address;
            _receiver = new MultiFilteredReciever(queue, _isTransactional, _subscriptions);
            _defaultSubject = details.Subject;
        }

        public bool DispatchMessage(TimeSpan timeout)
        {
            lock (_gate)
            {
                if (_disposed)
                    ThrowObjectDisposed();

                using (var peek = _receiver.Peek(timeout))
                {
                    if (peek == null)
                        return false;
                }
                var msg = _receiver.Receive();
                if (msg == null)
                    return false;

                var action = _subscriptions[msg.Subject];
                action(msg);
                return true;
            }
        }
        
        public override void Dispose()
        {
            lock (_gate)
            {
                if (_disposed)
                    return;
                base.Dispose();
                _subscriptions.Clear();
                _queue.Dispose();
                _disposed = true;
            }
        }

        public void Subscribe(Action<IReadOnlyMessage> observer, string subject = null)
        {
            lock (_gate)
            {
                if (_disposed)
                    ThrowObjectDisposed();

                if (subject == null)
                    subject = _defaultSubject;

                Action<IReadOnlyMessage> existing;
                if (_subscriptions.TryGetValue(subject, out existing))
                    _subscriptions[subject] = (Action<IReadOnlyMessage>)Delegate.Combine(existing, observer);
                else
                    _subscriptions.Add(subject, observer);
            }
        }

        private static void ThrowObjectDisposed()
        {
            throw new ObjectDisposedException("Worker has been disposed");
        }

        public bool Unsubscribe(Action<IReadOnlyMessage> observer, string subject = null)
        {
            lock (_gate)
            {
                if (_disposed)
                    ThrowObjectDisposed();

                if (subject == null)
                    subject = _defaultSubject;

                Action<IReadOnlyMessage> existing;
                if (!_subscriptions.TryGetValue(subject, out existing))
                    return false; // could not find the observer for that subject

                var remaining = (Action<IReadOnlyMessage>)Delegate.Remove(existing, observer);
                if (remaining == null)
                    _subscriptions.Remove(subject);
                else
                    _subscriptions[subject] = remaining;
                return true; // observer was removed
            }
        }

    }

    class MultiFilteredReciever : FilteredReceiver
    {
        readonly IReadOnlyDictionary<string, Action<IReadOnlyMessage>> _subscriptions;

        public MultiFilteredReciever(MSMQ.MessageQueue queue, bool isTransactional, IReadOnlyDictionary<string, Action<IReadOnlyMessage>> subscriptions) 
            : base(queue, isTransactional, "")
        {
            _subscriptions = subscriptions;
        }

        protected override bool IsInterestingSubject(MSMQ.Message msg) => _subscriptions.ContainsKey(msg.Label);
    }
}