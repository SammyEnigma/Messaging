using NUnit.Framework;
using System;
using System.Text;

namespace Messaging.Msmq.UnitTests
{
    [TestFixture]
    public class ConvertionToMSMQMessageTests
    {
        [Test]
        public void set_message_priority_from_header()
        {
            var input = new Message();
            input.Headers.Priority = 3;
            var actual = input.ToMsmqMessage();
            Assert.AreEqual(input.Headers.Priority, (int)actual.Priority);
        }

        [Test]
        public void set_message_TimeToBeReceived_from_header()
        {
            var input = new Message();
            input.Headers.TimeToLive = TimeSpan.FromMinutes(5);
            var actual = input.ToMsmqMessage();
            Assert.AreEqual(input.Headers.TimeToLive.Value, actual.TimeToBeReceived);
        }

        [Test]
        public void set_message_body_from_body()
        {
            var input = new Message { Body = "hello world" };
            var actual = input.ToMsmqMessage();
            Assert.AreEqual(input.Body, actual.Body);
        }

        [Test]
        public void set_replyto_queue_from_reply_to_url()
        {
            var input = new Message();
            input.Headers.ReplyTo = new Uri("msmq://localhost/private$/test_reply_queue");
            var actual = input.ToMsmqMessage();
            Assert.IsNotNull(actual.ResponseQueue);
            Assert.AreEqual(@".\private$\test_reply_queue", actual.ResponseQueue.Path);
        }

        [Test]
        public void set_extention_for_additional_headers()
        {
            var input = new Message();
            input.Headers["hello"] = "world";
            var actual = input.ToMsmqMessage();
            Assert.IsNotNull(actual.Extension);
            var extn = new UTF8Encoding(false).GetString(actual.Extension);
            Assert.AreEqual(@"""hello""=""world""", extn);
        }

        [Test]
        public void priority_is_not_encoded_in_extension()
        {
            var input = new Message { Headers = new MessageHeaders { Priority = 3 } };
            var actual = input.ToMsmqMessage();
            Assert.IsNotNull(actual.Extension);
            var extn = new UTF8Encoding(false).GetString(actual.Extension);
            Assert.AreEqual(@"", extn);
        }

        [Test]
        public void TTL_is_not_encoded_in_extension()
        {
            var input = new Message { Headers = new MessageHeaders { TimeToLive = TimeSpan.FromMinutes(5) } };
            var actual = input.ToMsmqMessage();
            Assert.IsNotNull(actual.Extension);
            var extn = new UTF8Encoding(false).GetString(actual.Extension);
            Assert.AreEqual(@"", extn);
        }

        [Test]
        public void contentType_is_encoded_in_extention()
        {
            var input = new Message { Headers = new MessageHeaders { ContentType = ContentTypes.Json } };
            var actual = input.ToMsmqMessage();
            Assert.IsNotNull(actual.Extension);
            var extn = new UTF8Encoding(false).GetString(actual.Extension);
            Assert.AreEqual(@"""ContentType""=""application/json""", extn);
        }

    }
}
