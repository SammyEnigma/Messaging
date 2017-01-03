using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
