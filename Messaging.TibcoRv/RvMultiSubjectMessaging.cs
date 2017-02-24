using System;
using System.Collections.Generic;
using Rv = TIBCO.Rendezvous;

namespace Messaging.TibcoRv
{
    abstract class BaseRvMultiSubjectMessaging : IMultiSubjectMessaging
    {
        protected readonly Rv.Queue _queue;
        protected readonly Dictionary<Action<IReadOnlyMessage>, Rv.Listener> _subscriptions = new Dictionary<Action<IReadOnlyMessage>, Rv.Listener>();

        public Uri Address { get; }

        public BaseRvMultiSubjectMessaging(Uri address)
        {
            if (address == null)
                throw new ArgumentNullException(nameof(address));
            _queue = new Rv.Queue();
            Address = address;
        }

        protected abstract Rv.Transport Transport { get; }

        /// <summary>Sends a message to the destination of this transport</summary>
        /// <exception cref="T:System.ArgumentNullException">thrown when <paramref name="msg" /> is not set</exception>
        public void Send(IReadOnlyMessage msg)
        {
            using (var rvm = Converter.ToRvMessge(msg, Address))
                Transport.Send(rvm);
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
                var l = CreateListener(observer, rvSubject);
                l.MessageReceived += (sender, args) =>
                {
                    var msg = new ReadOnlyRvMessage(args.Message, Address);
                    observer(msg);
                };
                _subscriptions.Add(observer, l);
            }
        }

        protected abstract Rv.Listener CreateListener(Action<IReadOnlyMessage> observer, string rvSubject);

        public bool Unsubscribe(Action<IReadOnlyMessage> observer, string subject = null)
        {
            subject = subject ?? Address.AbsolutePath;
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

    class RvMultiSubjectMessaging : BaseRvMultiSubjectMessaging
    {
        readonly Rv.Transport _transport;

        protected override Rv.Transport Transport => _transport;

        public RvMultiSubjectMessaging(Rv.Transport transport, Uri address) : base(address)
        {
            if (transport == null)
                throw new ArgumentNullException(nameof(transport));
            _transport = transport;
        }

        protected override Rv.Listener CreateListener(Action<IReadOnlyMessage> observer, string rvSubject)
        {
            return new Rv.Listener(_queue, Transport, rvSubject, null);
        }
    }

    class RvCertifiedMultiSubjectMessaging : BaseRvMultiSubjectMessaging
    {
        readonly Rv.CMTransport _transport;

        protected override Rv.Transport Transport => _transport;

        public RvCertifiedMultiSubjectMessaging(Rv.CMTransport transport, Uri address) : base(address)
        {
            if (transport == null)
                throw new ArgumentNullException(nameof(transport));
            _transport = transport;
        }

        protected override Rv.Listener CreateListener(Action<IReadOnlyMessage> observer, string rvSubject)
        {
            // default listener
            var l = new Rv.CMListener(_queue, _transport, rvSubject, null);
            l.SetExplicitConfirmation();
            return l;
        }
    }
}