using NUnit.Framework;
using MSMQ = System.Messaging;

namespace Messaging.Msmq.UnitTests
{
    [TestFixture]
    public class ReadOnlyMsmqMessageTests
    {
        [Test]
        public void no_label_is_mapped_to_empty_subject()
        {
            var mqm = new MSMQ.Message { };
            var msg = new ReadOnlyMsmqMessage(mqm);
            Assert.AreEqual("", msg.Subject);
        }

        [Test]
        public void label_is_mapped_to_subject()
        {
            var mqm = new MSMQ.Message { Label = "/say/hello" };
            var msg = new ReadOnlyMsmqMessage(mqm);
            Assert.AreEqual("/say/hello", msg.Subject);
        }

        [Test]
        public void unset_body_returns_null()
        {
            var mqm = new MSMQ.Message();
            var msg = new ReadOnlyMsmqMessage(mqm);
            Assert.AreEqual(null, msg.Body);
        }

        [Test]
        public void can_read_body_string()
        {
            var mqm = new MSMQ.Message("hello world");
            var msg = new ReadOnlyMsmqMessage(mqm);
            Assert.AreEqual("hello world", msg.Body);
        }

    }
}
