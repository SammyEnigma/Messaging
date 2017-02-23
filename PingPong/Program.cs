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

        IMultiSubjectMessaging rvMessaging;
        IMultiSubjectMessaging mqMessaging;
        TimeSpan delay = TimeSpan.FromMilliseconds(0);
        int pings;
        int pongs;

        public Program()
        {
            var factory = new CompositeMessagingFactory(new MsmqMessagingFactory(), new RvMessagingFactory());
            factory.TryCreateMultiSubject(new Uri("rv://7500/pong"), out rvMessaging);
            rvMessaging.Subscribe(OnRvPong);

            factory.TryCreateMultiSubject(new Uri("msmq://localhost/private$/ping"), out mqMessaging);
            mqMessaging.Subscribe(OnMsmqPing);
        }

        public void Run()
        {
            var t1 = rvMessaging.Start();
            var t2 = mqMessaging.Start();

            var reply = new Messaging.Message { Subject = "ping", Body = "hello" };
            mqMessaging.Send(reply);

            Task.WaitAll(t1, t2);
        }

        private void OnRvPong(IReadOnlyMessage msg)
        {
            if (++pongs % 1000 == 0)
                Console.WriteLine($"{pongs} pongs of {msg.GetType().Name}");
            msg.Dispose();
            
            //Thread.Sleep(delay);
            using (var reply = new Messaging.Message { Subject = "ping", Body = "hello" })
                mqMessaging.Send(reply);           
        }

        private void OnMsmqPing(IReadOnlyMessage msg)
        {
            if (++pings % 1000 == 0)
                Console.WriteLine($"{pings} pings of {msg.GetType().Name}");
            msg.Dispose();

            //Thread.Sleep(delay);
            using (var reply = new Messaging.Message { Subject = "pong", Body = "world" })
                rvMessaging.Send(reply);
        }
    }
}
