using System;
using System.Collections.Generic;
using Rv = TIBCO.Rendezvous;

namespace Messaging.TibcoRv
{
    /// <remarks>
    /// Do we want one thread per queue/subject?  We don't want it like this when receiving prices for many markets!
    /// </remarks>
    internal class RvWorker : IWorker
    {
        readonly Rv.Transport _transport;
        readonly Rv.Queue _queue;
        volatile bool _stop;
        readonly Dictionary<Action<IReadOnlyMessage>, Rv.Listener> _subscriptions = new Dictionary<Action<IReadOnlyMessage>, Rv.Listener>();

        public string Subject { get; }

        public Uri Uri { get; }

        public RvWorker(Rv.Transport transport, Uri uri)
        {
            if (transport == null)
                throw new ArgumentNullException(nameof(transport));
            if (uri == null)
                throw new ArgumentNullException(nameof(uri));
            _transport = transport;
            _queue = new Rv.Queue();
            Uri = uri;
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
            subject = Converter.ToRvSubject(subject);
            lock (_subscriptions)
            {
                var l = new Rv.Listener(_queue, _transport, subject, null);
                l.MessageReceived += (sender, args) => {
                    var msg = new ReadOnlyRvMessage(args.Message, Uri);
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