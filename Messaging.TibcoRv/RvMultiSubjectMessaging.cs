using System;
using System.Collections.Generic;
using Rv = TIBCO.Rendezvous;

namespace Messaging.TibcoRv
{
    class RvMultiSubjectMessaging : IMultiSubjectMessaging
    {
        readonly Rv.Transport _transport;
        readonly Rv.Queue _queue;
        volatile bool _stop;
        readonly Dictionary<Action<IReadOnlyMessage>, Rv.Listener> _subscriptions = new Dictionary<Action<IReadOnlyMessage>, Rv.Listener>();

        public Uri Address { get; }

        public RvMultiSubjectMessaging(Rv.Transport transport, Uri address)
        {
            if (transport == null)
                throw new ArgumentNullException(nameof(transport));
            if (address == null)
                throw new ArgumentNullException(nameof(address));
            _transport = transport;
            _queue = new Rv.Queue();
            Address = address;
        }

        /// <summary>Sends a message to the destination of this transport</summary>
        /// <exception cref="T:System.ArgumentNullException">thrown when <paramref name="msg" /> is not set</exception>
        public void Send(IReadOnlyMessage msg)
        {
            using (var rvm = Converter.ToRvMessge(msg, Address))
                _transport.Send(rvm);
        }

        public void Dispose()
        {
            lock (_subscriptions)
            {
                foreach (Rv.Listener l in _subscriptions.Values)
                {
                    l.Dispose();
                }
                _subscriptions.Clear();
            }
        }

        public bool DispatchMessage(TimeSpan timeout)
        {
            lock(_subscriptions)
            {
                return _queue.TimedDispatch(timeout.TotalSeconds);
            }
        }

        public void Subscribe(Action<IReadOnlyMessage> observer, string subject = null)
        {
            subject = subject ?? Address.AbsolutePath;
            var rvSubject =  Converter.ToRvSubject(subject);
            lock (_subscriptions)
            {
                var l = new Rv.Listener(_queue, _transport, rvSubject, null);
                l.MessageReceived += (sender, args) => {
                    var msg = new ReadOnlyRvMessage(args.Message, Address);
                    observer(msg);
                };
                _subscriptions.Add(observer, l);
            }
        }

        public bool Unsubscribe(Action<IReadOnlyMessage> observer)
        {
            lock (_subscriptions)
            {
                Rv.Listener l;
                if (_subscriptions.TryGetValue(observer, out l))
                {
                    l.Dispose();
                    _subscriptions.Remove(observer);
                    return true;
                }
                return false;
            }
        }

    }
}