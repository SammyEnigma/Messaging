using System;
using System.Threading;

#if DEBUG
namespace Messaging
{
    class Demo : IObserver<IReadOnlyMessage>
    {
        ITransportFactory _factory;
        IDisposable _worker;

        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public void OnError(Exception error)
        {
            Console.WriteLine($"Got error {error}");
        }

        public void OnNext(IReadOnlyMessage msg)
        {
            Console.WriteLine($"Got message for {msg.Subject}");
            msg.Acknowledge(); // do nothing else for this demo
        }

        void SendDemo()
        {
            ITransport t = _factory.Create("msmq://host/PRIVATE$/queue");
            var msg = new Message
            {
                Subject = "Ticket.Generation",
                Headers = new MessageHeaders { ContentType = ContentTypes.PlainText },
                Body = "hello",
            };
            msg.Headers["BasketId"] = 123;
            msg.Headers["OrderSetId"] = 234;
            msg.Headers["OrderId"] = 345;
            msg.Headers["DataType"] = "my.data.type";
            t.Send(msg);

            bool stop = false;
            using (var worker = t.CreateWorker())
            {
                worker.Subscribe(_ => { Console.WriteLine(msg.Subject); }, "Ticket.Generation");
                while (!stop)
                    worker.DispatchMessage(TimeSpan.FromSeconds(2));
            }
        }
    }
}
#endif