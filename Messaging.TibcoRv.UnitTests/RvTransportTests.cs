using NUnit.Framework;
using System;
using System.Threading;
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
                using (var trans = new RvMessaging(new Uri("rv+ipc://localhost"), rvt))
                    trans.Send(input);

                Assert.IsTrue(evt.WaitOne(2000), "timeouted waiting for RV message");
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
            using (var group = new RvMultiSubjectMessaging(rvt, new Uri("rv+ipc://localhost")))
            {
                IReadOnlyMessage got = null;
                var evt = new AutoResetEvent(false);
                group.Subscribe(msg => { got = msg; evt.Set(); }, "/say/hello");
                rvt.Send(new Rv.Message { SendSubject = "say.hello" });
                Assert.IsTrue(group.DispatchMessage(TimeSpan.FromSeconds(2)));
                Assert.NotNull(got);
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
                    { nameof(MessageHeaders.ContentType), ContentTypes.PlainText },
                    { "ReplyTo" , new Uri("rv://rendezvous/reply/topic") }
                }                
            };
        }
    }
}
