using Messaging;
using Messaging.Msmq;
using Messaging.TibcoRv;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Messaging;
using System.Text;
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
            Console.WriteLine($"Pong recevied as {msg.GetType().Name}, sending ping after short delay");
            Thread.Sleep(500);
            var reply = new Messaging.Message { Subject="ping", Body="hello" };
            mqt.Send(reply);
        }

        private void OnMsmqPing(IReadOnlyMessage msg)
        {
            Console.WriteLine($"Ping recevied as {msg.GetType().Name}, sending pong after short delay");
            Thread.Sleep(500);
            var reply = new Messaging.Message { Subject = "pong", Body = "world" };
            rvt.Send(reply);
        }
    }
}
