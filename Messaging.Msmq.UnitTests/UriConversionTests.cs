using NUnit.Framework;
using System;
using System.Messaging;

namespace Messaging.Msmq.UnitTests
{
    public class UriConversionTests
    {
        [TestCase("msmq://host123/queue", @"host123\queue")]
        [TestCase("msmq://host123/private$/queue", @"host123\private$\queue")]
        [TestCase("msmq+os://host123/queue", @"FORMATNAME:DIRECT=OS:host123\queue")]
        [TestCase("msmq+os://host123/private$/queue", @"FORMATNAME:DIRECT=OS:host123\private$\queue")]
        [TestCase("msmq+tcp://1.2.3.4/queue", @"FORMATNAME:DIRECT=TCP:1.2.3.4\queue")]
        [TestCase("msmq+tcp://1.2.3.4/private$/queue", @"FORMATNAME:DIRECT=TCP:1.2.3.4\private$\queue")]
        [TestCase("msmq+http://host123/queue", @"FORMATNAME:DIRECT=HTTP://host123/msmq/queue")]
        [TestCase("msmq+https://host123/queue", @"FORMATNAME:DIRECT=HTTPS://host123/msmq/queue")]
        [TestCase("msmq+pgm://224.1.2.3:7770/private$/queue", @"FORMATNAME:MULTICAST=224.1.2.3:7770")]
        public void can_convert_uri_to_format_name(string uri, string expected)
        {
            var actual = Converter.UriToQueueName(new Uri(uri));
            Assert.AreEqual(expected, actual.QueueName);
        }

        [TestCase(@".\private$\uri_t1", true, @"msmq+os://localhost/private$/uri_t1")]
        [TestCase(@"localhost\private$\uri_t2", true, @"msmq+os://localhost/private$/uri_t2")]
        [TestCase(@"FormatName:DIRECT=OS:.\private$\uri_t1", false, @"msmq+os://localhost/private$/uri_t1")]
        [TestCase(@"FormatName:DIRECT=OS:.\private$\uri_t1;subqueue", false, @"msmq+os://localhost/private$/uri_t1#subqueue")]
        [TestCase(@"FormatName:DIRECT=OS:localhost\private$\uri_t2", false, @"msmq+os://localhost/private$/uri_t2")]
        [TestCase(@"FormatName:DIRECT=TCP:127.0.0.1\private$\uri_t1", false, @"msmq+tcp://127.0.0.1/private$/uri_t1")]
        [TestCase(@"FormatName:DIRECT=HTTP://my.host.com/msmq/uri_t1", false, @"msmq+http://my.host.com/msmq/uri_t1")]
        public void can_convert_queue_to_uri(string pathOrFormatName, bool create, string expected)
        {
            MessageQueue q = GetOrCreateQueue(pathOrFormatName, create);
            var actual = Converter.QueueNameToUri(q);
            expected = ReplaceLocalHostWithMachineName(expected);
            Assert.AreEqual(new Uri(expected), actual);
        }

        [TestCase(@".\private$\uri_pgm_t1", @"224.1.2.3:7770", true, @"msmq+pgm://224.1.2.3:7770/private$/uri_pgm_t1")]
        [TestCase(@"FormatName:DIRECT=OS:.\private$\uri_pgm_t1", @"224.1.2.3:7770", false, @"msmq+pgm://224.1.2.3:7770/private$/uri_pgm_t1")]
        [TestCase(@"formatname:multicast=234.1.1.1:8081", @"", false, @"msmq+pgm://234.1.1.1:8081/")]
        public void can_convert_multicast_queue_to_uri(string path, string multicastAddress, bool create, string expected)
        {
            expected = ReplaceLocalHostWithMachineName(expected);
            MessageQueue q = GetOrCreateQueue(path, create);
            if (!string.IsNullOrEmpty(multicastAddress))
                q.MulticastAddress = multicastAddress;
            var actual = Converter.QueueNameToUri(q);
            Assert.AreEqual(new Uri(expected), actual);
        }

        static MessageQueue GetOrCreateQueue(string pathOrFormatName, bool create)
        {
            pathOrFormatName = ReplaceLocalHostWithMachineName(pathOrFormatName);
            return create && !MessageQueue.Exists(pathOrFormatName) ? MessageQueue.Create(pathOrFormatName) : new MessageQueue(pathOrFormatName);
        }

        static string ReplaceLocalHostWithMachineName(string expected)
        {
            if (expected.Contains("localhost"))
                return expected.Replace("localhost", Environment.MachineName);
            return expected;
        }
    }
}
