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
            bool transactional = false;
            if (MessageQueue.Exists(@".\private$\ping"))
                MessageQueue.Delete(@".\private$\ping");
            
            MessageQueue.Create(@".\private$\ping", transactional);

            new Program().Run();
        }

        IMultiSubjectMessaging rvMessaging;
        IMessaging mqMessaging;
        TimeSpan delay = TimeSpan.FromMilliseconds(0);
        int pings;
        int pongs;

        public Program()
        {
            var factory = new CompositeMessagingFactory(new MsmqMessagingFactory(), new RvMessagingFactory());
            rvMessaging = factory.CreateMultiSubject("rv://7500/pong");
            rvMessaging.Subscribe(OnRvPong);

            mqMessaging = factory.Create("msmq://localhost/private$/ping?express=true");            
        }

        public void Run()
        {
            var t1 = rvMessaging.Start();

            var t2 = MsmqLoop();

            // start the piong pong
            var reply = new Messaging.Message { Subject = "ping", Body = "hello" };
            mqMessaging.Send(reply);

            Task.WaitAll(t1, t2);
        }

        private async Task MsmqLoop()
        {
            for (;;)
            {
                var msg = await mqMessaging.ReceiveAsync(TimeSpan.FromHours(1));
                OnMsmqPing(msg);
            }
        }

        private void OnRvPong(IReadOnlyMessage msg)
        {
            if (++pongs % 1000 == 0)
                Console.WriteLine($"{pongs} pongs of {msg.GetType().Name}");
            msg.Acknowledge();
            msg.Dispose();
            
            //Thread.Sleep(delay);
            using (var reply = new Messaging.Message { Subject = "ping", Body = "hello" })
                mqMessaging.Send(reply);           
        }

        private void OnMsmqPing(IReadOnlyMessage msg)
        {
            if (++pings % 1000 == 0)
                Console.WriteLine($"{pings} pings of {msg.GetType().Name}");
            msg.Acknowledge();
            msg.Dispose();

            //Thread.Sleep(delay);
            using (var reply = new Messaging.Message { Subject = "pong", Body = "world" })
                rvMessaging.Send(reply);
        }
    }
}
