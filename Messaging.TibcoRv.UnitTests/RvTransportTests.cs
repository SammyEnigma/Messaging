using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Rv = TIBCO.Rendezvous;

namespace Messaging.TibcoRv.UnitTests
{
    [TestFixture]
    public class RvTransportTests
    {
        [SetUp]
        public void OpenRv()
        {
            Rv.Environment.Open();
        }
        
        [TearDown]
        public void CloseRv()
        {
            Rv.Environment.Close();
        }

        [Test]
        public void can_send_a_message()
        {
            var rvt = Rv.IntraProcessTransport.UniqueInstance;

            var q = Rv.Queue.Default;
            var listener = new Rv.Listener(q, rvt, "say.hello", null);
            Rv.Dispatcher dis = null;
            try
            {
                // start a listener that sets the event when it gets a message
                Rv.Message got = null;
                var evt = new AutoResetEvent(false);
                listener.MessageReceived += (sender, args) => { got = args.Message; evt.Set(); };
                dis = new Rv.Dispatcher(q, 10.0);

                Message input = HelloWorldMessage("/say/hello");
                using (var trans = new TibcoRvTransport(new Uri("rv+ipc://localhost"), rvt))
                    trans.Send(input);

                Assert.IsTrue(evt.WaitOne(5000), "timeouted waiting for RV message");
                Assert.IsNotNull(got);
            }
            finally
            {
                dis.Destroy();
                listener.Destroy();
            }
        }


        [Test]
        public void can_receive_a_message()
        {
            var rvt = Rv.IntraProcessTransport.UniqueInstance;

            Message input = HelloWorldMessage("/say/hello");
            using (var trans = new TibcoRvTransport(new Uri("rv+ipc://localhost"), rvt))
            {
                var subject = trans.NewListener("/say/hello");

                // subscribe and start a worker to receive the messageS
                IReadOnlyMessage got = null;
                var evt = new AutoResetEvent(false);
                using (var subscription = subject.Subscribe(msg => { got = msg; evt.Set(); }))
                using (trans.StartWorker())
                {
                    rvt.Send(new Rv.Message { SendSubject = "say.hello" });
                    Assert.IsTrue(evt.WaitOne(5000), "timeouted waiting for RV message");
                   Assert.IsNotNull(got);
                }
            }
        }

        static Message HelloWorldMessage(string subject)
        {
            return new Message
            {
                Subject = subject,
                Body = "hello world",
                Headers = new MessageHeaders
                {
                    { nameof(MessageHeaders.ContentType), ContentTypes.PlainText }
                },
                ReplyTo = new Uri("rv://rendezvous/reply/topic"),
            };
        }
    }
}
