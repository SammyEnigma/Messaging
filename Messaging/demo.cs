//using System;

//#if DEBUG
//namespace Messaging
//{
//    class Demo : IObserver<IReadOnlyMessage>
//    {
//        TransportFactory _factory;
//        IDisposable _worker;

//        public void OnCompleted()
//        {
//            throw new NotImplementedException();
//        }

//        public void OnError(Exception error)
//        {
//            Console.WriteLine($"Got error {error}");
//        }

//        public void OnNext(IReadOnlyMessage msg)
//        {
//            Console.WriteLine($"Got message for {msg.Subject}");
//            msg.Acknowledge(); // do nothing else for this demo
//        }

//        void SendDemo()
//        {
//            Transport t = _factory.New("msmq://host/PRIVATE$/queue");
//            var msg = new Message
//            {
//                Subject = "Ticket.Generation",
//                Headers = new MessageHeaders { ContentType=ContentTypes.PlainText },
//                Body = "hello",
//            };
//            msg.Headers["BasketId"] = 123;
//            msg.Headers["OrderSetId"] = 234;
//            msg.Headers["OrderId"] = 345;
//            msg.Headers["DataType"] = "my.data.type";
//            t.Send(msg);

//            var listener = t.NewListener("Ticket.Generation");
//            var disposable = listener.Subscribe(this); //TODO: use RX to subscribe here
//            //TODO: wait for messages

//            _worker = t.StartWorker();
//        }
//    }
//}
//#endif