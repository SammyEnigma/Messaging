using NUnit.Framework;
using System;
using System.Linq;
using System.Text;
using MSMQ = System.Messaging;

namespace Messaging.Msmq.UnitTests
{
    [TestFixture]
    public class ReadOnlyMsmqMessageHeaderTests
    {
        [TestCase(MSMQ.MessagePriority.Normal, 3)]
        [TestCase(MSMQ.MessagePriority.AboveNormal, 4)]
        public void can_read_priority(MSMQ.MessagePriority input, int expected)
        {
            var msg = new MSMQ.Message { Priority = input };
            var headers = new ReadOnlyMsmqMessageHeaders(msg);
            Assert.AreEqual(expected, headers.Priority);
        }

        [Test]
        public void empty_message_has_normal_priority()
        {
            var msg = new MSMQ.Message();
            var headers = new ReadOnlyMsmqMessageHeaders(msg);
            Assert.AreEqual(3, headers.Priority);
            Assert.AreEqual(1, headers.Count);
        }

        [Test]
        public void can_read_null_ttl()
        {
            var msg = new MSMQ.Message();
            var headers = new ReadOnlyMsmqMessageHeaders(msg);
            Assert.AreEqual(null, headers.TimeToLive);
            Assert.AreEqual(1, headers.Count, "plus one for priority");
        }

        [Test]
        public void can_read_ttl()
        {
            var msg = new MSMQ.Message { TimeToBeReceived = TimeSpan.FromMinutes(3) };
            var headers = new ReadOnlyMsmqMessageHeaders(msg);
            Assert.AreEqual(TimeSpan.FromMinutes(3), headers.TimeToLive);
            Assert.AreEqual(2, headers.Count, "plus one for priority");
        }

        [Test]
        public void can_read_extension_as_header_value()
        {
            var msg = new MSMQ.Message { Extension = Encoding.UTF8.GetBytes("\"first\"=true") };
            var headers = new ReadOnlyMsmqMessageHeaders(msg);
            Assert.AreEqual(true, headers["first"]);
            Assert.AreEqual(2, headers.Count, "plus one for priority");
        }

        [Test]
        public void keys_contains_priorty()
        {
            var msg = new MSMQ.Message();
            var headers = new ReadOnlyMsmqMessageHeaders(msg);
            Assert.AreEqual(nameof(headers.Priority), headers.Keys.FirstOrDefault());
        }

        [Test]
        public void values_contains_priorty()
        {
            var msg = new MSMQ.Message();
            var headers = new ReadOnlyMsmqMessageHeaders(msg);
            Assert.AreEqual(3, headers.Values.FirstOrDefault());
        }

        [Test]
        public void keys_dont_contain_ttl_by_default()
        {
            var msg = new MSMQ.Message();
            var headers = new ReadOnlyMsmqMessageHeaders(msg);
            Assert.AreEqual(false, headers.Keys.Any(k => k == nameof(headers.TimeToLive)));
        }

        [Test]
        public void values_dont_contain_ttl_by_default()
        {
            var msg = new MSMQ.Message();
            var headers = new ReadOnlyMsmqMessageHeaders(msg);
            Assert.AreEqual(3, headers.Values.FirstOrDefault(), "priority");
            Assert.AreEqual(1, headers.Values.Count(), "just priority");
        }

        [Test]
        public void keys_contains_ttl_when_set_on_message()
        {
            var msg = new MSMQ.Message { TimeToBeReceived = TimeSpan.FromMinutes(3) };
            var headers = new ReadOnlyMsmqMessageHeaders(msg);
            Assert.AreEqual(nameof(headers.TimeToLive), headers.Keys.ElementAt(1)); // first is Priority
        }

        [Test]
        public void values_contains_ttl()
        {
            var msg = new MSMQ.Message { TimeToBeReceived = TimeSpan.FromMinutes(3) };
            var headers = new ReadOnlyMsmqMessageHeaders(msg);
            Assert.AreEqual(TimeSpan.FromMinutes(3), headers.Values.ElementAt(1)); // first is Priority
        }

        [Test]
        public void reply_to_returns_null_when_unset()
        {
            var mqm = new MSMQ.Message("hello world") {  };
            var msg = new ReadOnlyMsmqMessage(mqm);
            Assert.AreEqual(null, msg.Headers.ReplyTo);
        }

        [Test]
        public void can_replyto_another_msmq_queue()
        {
            var msg = CreateMessageWithResponseQueue();
            var headers = new ReadOnlyMsmqMessageHeaders(msg);
            var expected = new Uri($"msmq+os://{Environment.MachineName}/private$/ReadOnlyMsmqMessageTests");
            Assert.AreEqual(expected, headers.ReplyTo);
        }

        [Test]
        public void can_replyto_another_transport_via_header_stored_in_extension()
        {
            var msg = CreateMessageWithReplyToExtension();
            var headers = new ReadOnlyMsmqMessageHeaders(msg);
            Assert.AreEqual(new Uri($"tibrv://localhost/ReadOnlyMsmq/MessageTests"), headers.ReplyTo);
        }

        [Test]
        public void keys_dont_contain_ReplyTo_by_default()
        {
            var msg = new MSMQ.Message();
            var headers = new ReadOnlyMsmqMessageHeaders(msg);
            Assert.AreEqual(false, headers.Keys.Any(k => k == nameof(headers.ReplyTo)));
        }

        [Test]
        public void values_dont_contain_ReplyTo_by_default()
        {
            var msg = new MSMQ.Message();
            var headers = new ReadOnlyMsmqMessageHeaders(msg);
            Assert.AreEqual(3, headers.Values.FirstOrDefault(), "priority");
            Assert.AreEqual(1, headers.Values.Count(), "just priority");
        }

        [Test]
        public void keys_contains_ReplyTo_when_ResponseQueue_set_on_message()
        {
            var msg = CreateMessageWithResponseQueue();
            var headers = new ReadOnlyMsmqMessageHeaders(msg);
            Assert.AreEqual(2, headers.Keys.Count(), string.Join(",", headers.Keys)); // Priority is always there
            Assert.AreEqual(nameof(headers.ReplyTo), headers.Keys.ElementAt(0));
        }

        [Test]
        public void keys_contains_ReplyTo_when_Replyto_set_in_message_extension()
        {
            var msg = CreateMessageWithReplyToExtension();
            var headers = new ReadOnlyMsmqMessageHeaders(msg);
            Assert.AreEqual(2, headers.Keys.Count(), string.Join(",", headers.Keys)); // Priority is always there
            Assert.AreEqual(nameof(headers.ReplyTo), headers.Keys.ElementAt(0));
        }

        [Test]
        public void Values_contains_ReplyTo_when_ResponseQueue_set_on_message()
        {
            var msg = CreateMessageWithResponseQueue();
            var headers = new ReadOnlyMsmqMessageHeaders(msg);
            Assert.AreEqual(2, headers.Values.Count(), string.Join(",", headers.Values)); // Priority is always there
            var expected = new Uri($"msmq+os://{Environment.MachineName}/private$/ReadOnlyMsmqMessageTests");
            Assert.AreEqual(expected, headers.Values.ElementAt(0)); 
        }

        [Test]
        public void Values_contains_ReplyTo_when_Replyto_set_in_message_extension()
        {
            var msg = CreateMessageWithReplyToExtension();
            var headers = new ReadOnlyMsmqMessageHeaders(msg);
            Assert.AreEqual(2, headers.Values.Count(), string.Join(",", headers.Values)); // Priority is always there
            Assert.AreEqual(new Uri("tibrv://localhost/ReadOnlyMsmq/MessageTests"), headers.Values.ElementAt(0)); 
        }

        [Test]
        public void ContainsKey_ReplyTo_when_ResponseQueue_set_on_message()
        {
            var msg = CreateMessageWithResponseQueue();
            var headers = new ReadOnlyMsmqMessageHeaders(msg);
            Assert.AreEqual(true, headers.ContainsKey(nameof(headers.ReplyTo))); 
        }

        [Test]
        public void ContainsKey_ReplyTo_when_Replyto_set_in_message_extension()
        {
            var msg = CreateMessageWithReplyToExtension();
            var headers = new ReadOnlyMsmqMessageHeaders(msg);
            Assert.AreEqual(true, headers.ContainsKey(nameof(headers.ReplyTo)));
        }

        [Test]
        public void enumeration_contains_ReplyTo_when_ResponseQueue_set_on_message()
        {
            var msg = CreateMessageWithResponseQueue();
            var headers = new ReadOnlyMsmqMessageHeaders(msg);
            var found = headers.SingleOrDefault(pair => pair.Key == nameof(headers.ReplyTo));
            Assert.IsNotNull(found);
            Assert.AreEqual(new Uri($"msmq+os://{Environment.MachineName}/private$/ReadOnlyMsmqMessageTests"), found.Value);
        }

        [Test]
        public void enumeration_contains_ReplyTo_when_Replyto_set_in_message_extension()
        {
            var msg = CreateMessageWithReplyToExtension();
            var headers = new ReadOnlyMsmqMessageHeaders(msg);
            var found = headers.SingleOrDefault(pair => pair.Key == nameof(headers.ReplyTo));
            Assert.IsNotNull(found);
            Assert.AreEqual(new Uri("tibrv://localhost/ReadOnlyMsmq/MessageTests"), found.Value);
        }

        [Test]
        public void can_try_getValue_ReplyTo_when_ResponseQueue_set_on_message()
        {
            var msg = CreateMessageWithResponseQueue();
            var headers = new ReadOnlyMsmqMessageHeaders(msg);
            object actual;
            Assert.IsTrue(headers.TryGetValue(nameof(headers.ReplyTo), out actual));
            Assert.AreEqual(new Uri($"msmq+os://{Environment.MachineName}/private$/ReadOnlyMsmqMessageTests"), actual);
        }

        [Test]
        public void can_try_getValue_ReplyTo_when_Replyto_set_in_message_extension()
        {
            var msg = CreateMessageWithReplyToExtension();
            var headers = new ReadOnlyMsmqMessageHeaders(msg);
            object actual;
            Assert.IsTrue(headers.TryGetValue(nameof(headers.ReplyTo), out actual));
            Assert.AreEqual(new Uri("tibrv://localhost/ReadOnlyMsmq/MessageTests"), actual);
        }

        static MSMQ.Message CreateMessageWithResponseQueue(string path = @".\private$\ReadOnlyMsmqMessageTests")
        {
            var messageQueue = MSMQ.MessageQueue.Exists(path) ? new MSMQ.MessageQueue(path) : MSMQ.MessageQueue.Create(path);
            return new MSMQ.Message("hello world") { ResponseQueue = messageQueue };
        }

        static MSMQ.Message CreateMessageWithReplyToExtension(string path = @"""ReplyTo""=""tibrv://localhost/ReadOnlyMsmq/MessageTests""")
        {
            var extension = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(path);
            var msg = new MSMQ.Message("hello world") { Extension = extension };
            return msg;
        }

    }
}
