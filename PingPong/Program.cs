using Messaging;
using Messaging.Msmq;
using Messaging.TibcoRv;
using System;
using System.Messaging;
using System.Threading;
using System.Threading.Tasks;

namespace PingPong
{
    class Program
    {
        static void Main(string[] args)
        {
            if (!MessageQueue.Exists(@".\private$\ping"))
                MessageQueue.Create(@".\private$\ping");

            new Program().Run();
        }

        ITransport rvt;
        IWorker rvw;
        ITransport mqt;
        IWorker mqw;
        TimeSpan delay = TimeSpan.FromMilliseconds(0);
        int pings;
        int pongs;

        public Program()
        {
            var factory = new CompositeTransportFactory(new MsmqTransportFactory(), new RvTransportFactory());
            rvt = factory.Create("rv://7500/pong");
            rvw = rvt.CreateWorker();
            rvw.Subscribe(OnRvPong);

            mqt = factory.Create("msmq://localhost/private$/ping");
            mqw = mqt.CreateWorker();
            mqw.Subscribe(OnMsmqPing);
        }

        public void Run()
        {
            var t1 = rvw.Start();
            var t2 = mqw.Start();

            var reply = new Messaging.Message { Subject = "ping", Body = "hello" };
            mqt.Send(reply);

            Task.WaitAll(t1, t2);
        }

        private void OnRvPong(IReadOnlyMessage msg)
        {
            if (++pongs % 1000 == 0)
                Console.WriteLine($"{pongs} pongs of {msg.GetType().Name}");
            msg.Dispose();
            
            //Thread.Sleep(delay);
            using (var reply = new Messaging.Message { Subject = "ping", Body = "hello" })
                mqt.Send(reply);           
        }

        private void OnMsmqPing(IReadOnlyMessage msg)
        {
            if (++pings % 1000 == 0)
                Console.WriteLine($"{pings} pings of {msg.GetType().Name}");
            msg.Dispose();

            //Thread.Sleep(delay);
            using (var reply = new Messaging.Message { Subject = "pong", Body = "world" })
                rvt.Send(reply);
        }
    }
}
