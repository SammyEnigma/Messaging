using Messaging.Subscriptions;
using System;
using System.Diagnostics.Contracts;
using Rv = TIBCO.Rendezvous;

namespace Messaging.TibcoRv
{
    /// <summary>transport to reliable RV messages</summary>
    public class RvTransport : ITransport, IDisposable
    {
        readonly object _gate = new object(); // shared the lock with the transport and workers so we dont drop messages when removing subscriptions
        readonly Rv.Transport _transport;
        readonly Rv.QueueGroup _queues;

        public Uri Destination { get; }

        public RvTransport(Uri destination, Rv.Transport transport)
        {
            Contract.Requires(destination != null);
            Contract.Requires(transport != null);
            Contract.Requires(string.IsNullOrEmpty(destination.PathAndQuery), "You cannot specify the topic for RV transports");
            Destination = destination;
            _transport = transport;
            _queues = new Rv.QueueGroup();
            _queues.Add(new Rv.Queue());
            _queues.Add(Rv.Queue.Default);  // default queue must be dispatched
        }

        public void Send(IReadOnlyMessage msg)
        {
            var rvm = Converter.ToRvMessge(msg, Destination);
            _transport.Send(rvm);
        }

        public void Dispose()
        {
            _transport.Dispose();
        }

        public IWorker CreateWorker() => new RvWorker(_transport, Destination);
    }

    //public class TibcoRvCmTransport : Transport, IDisposable
    //{
    //    readonly Rv.CMTransport _transport;
    //    readonly Rv.QueueGroup _queues;

    //    public TibcoRvCmTransport(Uri destination, Rv.CMTransport transport) : base(destination)
    //    {
    //        Contract.Requires(transport != null);
    //        _transport = transport;
    //        _queues = new Rv.QueueGroup();
    //        _queues.Add(new Rv.Queue());
    //        _queues.Add(Rv.Queue.Default);  // default queue must be dispatched
    //    }

    //    public override IObservable<IReadOnlyMessage> NewListener(string subject = null)
    //    {
    //        if (subject == null) subject = ">"; // all messages!
    //        var l = new Rv.CMListener(Rv.Queue.Default, _transport, subject, null);
    //        l.SetExplicitConfirmation(); // always do this?  or make this configurable?
    //        throw new NotImplementedException();
    //        //return new TibcoRvSubject(l, Destination);
    //    }

    //    public override void Send(IReadOnlyMessage msg)
    //    {
    //        var rv = Converter.ToRvMessge(msg, Destination);
    //        var cm = new Rv.CMMessage(rv);
    //        if (msg.Headers.TimeToLive.HasValue)
    //            cm.TimeLimit = msg.Headers.TimeToLive.Value.Seconds;
    //        _transport.Send(cm);
    //    }

    //    public void Dispose()
    //    {
    //        try
    //        {
    //            _transport.Destroy();
    //        }
    //        catch
    //        {
    //            // Dispose methods must not throw exceptions
    //        }
    //        GC.SuppressFinalize(this);  // RV does not do this, so we have to
    //    }

    //    public override IDisposable StartWorker(string name = "")
    //    {
    //        return new DisposableDispatcher(_queues, name);
    //    }
    //}

}
