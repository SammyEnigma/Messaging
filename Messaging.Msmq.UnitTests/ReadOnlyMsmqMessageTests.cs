using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MSMQ = System.Messaging;

namespace Messaging.Msmq.UnitTests
{
    [TestFixture]
    public class ReadOnlyMsmqMessageTests
    {
        [Test]
        public void label_is_mapped_to_subject()
        {
            var mqm = new MSMQ.Message { Label = "/say/hello" };
            var msg = new ReadOnlyMsmqMessage(mqm);
            Assert.AreEqual("/say/hello", msg.Subject);
        }

        [Test]
        public void can_read_body_string()
        {
            var mqm = new MSMQ.Message("hello world");
            var msg = new ReadOnlyMsmqMessage(mqm);
            Assert.AreEqual("hello world", msg.Body);
        }

        [Test]
        public void can_replyto_another_msmq_queue()
        {
            const string QName = @".\private$\ReadOnlyMsmqMessageTests";
            var messageQueue = MSMQ.MessageQueue.Exists(QName) ? new MSMQ.MessageQueue(QName) : MSMQ.MessageQueue.Create(QName);
            var mqm = new MSMQ.Message("hello world") { ResponseQueue = messageQueue };
            var msg = new ReadOnlyMsmqMessage(mqm);
            Assert.AreEqual(new Uri($"msmq+os://{Environment.MachineName}/private$/ReadOnlyMsmqMessageTests"), msg.Headers.ReplyTo);
        }
    }
}
