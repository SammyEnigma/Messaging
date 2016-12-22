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
        public void keys_contains_ttl()
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

    }
}
