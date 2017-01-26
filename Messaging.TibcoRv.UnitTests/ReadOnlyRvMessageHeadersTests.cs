using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Rv = TIBCO.Rendezvous;

namespace Messaging.TibcoRv.UnitTests
{
    [TestFixture]
    public class ReadOnlyRvMessageHeadersTests
    {
        [TestCase(ContentTypes.Json)]
        [TestCase(ContentTypes.Xml)]
        [TestCase(ContentTypes.Binary)]
        [TestCase(ContentTypes.PlainText)]
        public void can_read_content_type(string type)
        {
            var rvm = new Rv.Message();
            rvm.AddField("ContentType", type);
            var headers = new ReadOnlyRvMessageHeaders(rvm);
            Assert.AreEqual(type, headers.ContentType);
            Assert.AreEqual(1, headers.Count, "headers.Count");
        }

        [Test]
        public void can_read_null_TTL()
        {
            var rvm = new Rv.Message();
            var headers = new ReadOnlyRvMessageHeaders(rvm);
            Assert.IsNull(headers.TimeToLive);
        }

        [Test]
        public void can_read_TTL()
        {
            var rvm = new Rv.Message();
            var ttl = TimeSpan.FromMinutes(1);
            rvm.AddField("TimeToLive", ttl.ToString());
            var headers = new ReadOnlyRvMessageHeaders(rvm);
            Assert.AreEqual(ttl, headers.TimeToLive);
            Assert.AreEqual(1, headers.Count, "headers.Count");
        }

        [Test]
        public void can_read_null_Priority()
        {
            var rvm = new Rv.Message();
            var headers = new ReadOnlyRvMessageHeaders(rvm);
            Assert.IsNull(headers.Priority);
        }

        [Test]
        public void can_read_Priority()
        {
            var rvm = new Rv.Message();
            rvm.AddField("Priority", 1);
            var headers = new ReadOnlyRvMessageHeaders(rvm);
            Assert.AreEqual(1, headers.Priority);
            Assert.AreEqual(1, headers.Count, "headers.Count");
        }

        [Test]
        public void can_read_PriorityFromString()
        {
            var rvm = new Rv.Message();
            rvm.AddField("Priority", "1");
            var headers = new ReadOnlyRvMessageHeaders(rvm);
            Assert.AreEqual(1, headers.Priority);
        }

        [TestCase("user", "caustin")]
        [TestCase("custom", "thing")]
        public void can_read_custom_header_string_value(string name, string value)
        {
            var rvm = new Rv.Message();
            rvm.AddField(name, value);
            var headers = new ReadOnlyRvMessageHeaders(rvm);
            Assert.AreEqual(value, headers[name]);
            Assert.AreEqual(1, headers.Count, "headers.Count");
        }

        [TestCase("user", 123)]
        public void can_read_custom_header_int_value(string name, int value)
        {
            var rvm = new Rv.Message();
            rvm.AddField(name, value);
            var headers = new ReadOnlyRvMessageHeaders(rvm);
            Assert.AreEqual(value, headers[name]);
            Assert.AreEqual(1, headers.Count, "headers.Count");
        }

        [TestCase("user", ExpectedException = typeof(KeyNotFoundException))]
        [TestCase("custom", ExpectedException = typeof(KeyNotFoundException))]
        public void indexer_returns_null_for_undefined_property(string name)
        {
            var rvm = new Rv.Message();
            var headers = new ReadOnlyRvMessageHeaders(rvm);
            var ignoreed = headers[name];
        }

        [Test, ExpectedException(ExpectedException = typeof(KeyNotFoundException))]
        public void cannot_access_the_body_via_the_header()
        {
            var rvm = new Rv.Message();
            rvm.AddField("Body", "value");
            var headers = new ReadOnlyRvMessageHeaders(rvm);
            var ignoreed = headers["Body"];
        }

        [TestCase("user", "caustin")]
        [TestCase("custom", "thing")]
        public void ContainsKey_returns_true_for_defined_string_property(string name, string value)
        {
            var rvm = new Rv.Message();
            rvm.AddField(name, value);
            var headers = new ReadOnlyRvMessageHeaders(rvm);
            Assert.IsTrue(headers.ContainsKey(name));
        }

        [TestCase("user", 123)]
        public void ContainsKey_returns_true_for_defined_int_property(string name, int value)
        {
            var rvm = new Rv.Message();
            rvm.AddField(name, value);
            var headers = new ReadOnlyRvMessageHeaders(rvm);
            Assert.IsTrue(headers.ContainsKey(name));
        }

        [TestCase("user")]
        [TestCase("custom")]
        public void ContainsKey_returns_false_for_undefined_property(string name)
        {
            var rvm = new Rv.Message();
            var headers = new ReadOnlyRvMessageHeaders(rvm);
            Assert.IsFalse(headers.ContainsKey(name));
        }

        [TestCase("user", "caustin")]
        [TestCase("custom", "thing")]
        public void TryGetValue_returns_true_for_defined_string_property(string name, string value)
        {
            var rvm = new Rv.Message();
            rvm.AddField(name, value);
            var headers = new ReadOnlyRvMessageHeaders(rvm);
            object valueOut;
            Assert.IsTrue(headers.TryGetValue(name, out valueOut));
            Assert.AreEqual(value, valueOut, "valueOut");
        }

        [TestCase("user", 123)]
        public void TryGetValue_returns_true_for_defined_int_property(string name, int value)
        {
            var rvm = new Rv.Message();
            rvm.AddField(name, value);
            var headers = new ReadOnlyRvMessageHeaders(rvm);
            object valueOut;
            Assert.IsTrue(headers.TryGetValue(name, out valueOut));
            Assert.AreEqual(value, valueOut, "valueOut");
        }

        [TestCase("user")]
        [TestCase("custom")]
        public void TryGetValue_returns_false_for_undefined_property(string name)
        {
            var rvm = new Rv.Message();
            var headers = new ReadOnlyRvMessageHeaders(rvm);
            object valueOut;
            Assert.IsFalse(headers.TryGetValue(name, out valueOut));
        }

        [Test]
        public void can_enumerate_zero_keys()
        {
            var rvm = new Rv.Message();
            var headers = new ReadOnlyRvMessageHeaders(rvm);            
            Assert.AreEqual(0, headers.Keys.Count());
        }

        [Test]
        public void enumerating_keys_ignores_Body()
        {
            var rvm = new Rv.Message();
            rvm.AddField("Body", "value");
            var headers = new ReadOnlyRvMessageHeaders(rvm);            
            Assert.AreEqual(0, headers.Keys.Count());
        }

        [Test]
        public void can_enumerate_one_key()
        {
            var rvm = new Rv.Message();
            rvm.AddField("custom", "value");
            var headers = new ReadOnlyRvMessageHeaders(rvm);            
            Assert.AreEqual(1, headers.Keys.Count());
            Assert.AreEqual("custom", headers.Keys.First());
        }

        [Test]
        public void can_enumerate_multiple_keys()
        {
            var rvm = new Rv.Message();
            rvm.AddField("custom1", "value1");
            rvm.AddField("custom2", "value2");
            var headers = new ReadOnlyRvMessageHeaders(rvm);            
            Assert.AreEqual("custom1", headers.Keys.First());
            Assert.AreEqual("custom2", headers.Keys.ElementAt(1));
            Assert.AreEqual(2, headers.Keys.Count());
        }

        [Test]
        public void can_enumerate_zero_values()
        {
            var rvm = new Rv.Message();
            var headers = new ReadOnlyRvMessageHeaders(rvm);            
            Assert.AreEqual(0, headers.Values.Count());
        }

        [Test]
        public void enumerating_values_ignores_Body()
        {
            var rvm = new Rv.Message();
            rvm.AddField("Body", "value");
            var headers = new ReadOnlyRvMessageHeaders(rvm);            
            Assert.AreEqual(0, headers.Values.Count());
        }

        [Test]
        public void can_enumerate_one_value()
        {
            var rvm = new Rv.Message();
            rvm.AddField("custom", "value");
            var headers = new ReadOnlyRvMessageHeaders(rvm);            
            Assert.AreEqual(1, headers.Values.Count());
            Assert.AreEqual("value", headers.Values.First());
        }

        [Test]
        public void can_enumerate_multiple_value()
        {
            var rvm = new Rv.Message();
            rvm.AddField("custom1", "value1");
            rvm.AddField("custom2", "value2");
            var headers = new ReadOnlyRvMessageHeaders(rvm);            
            Assert.AreEqual("value1", headers.Values.First());
            Assert.AreEqual("value2", headers.Values.ElementAt(1));
            Assert.AreEqual(2, headers.Values.Count());
        }

        [Test]
        public void can_enumerate_zero_pairs()
        {
            var rvm = new Rv.Message();
            var headers = new ReadOnlyRvMessageHeaders(rvm);            
            Assert.AreEqual(0, headers.Count());
        }

        [Test]
        public void enumerating_pairs_ignores_Body()
        {
            var rvm = new Rv.Message();
            rvm.AddField("Body", "value");
            var headers = new ReadOnlyRvMessageHeaders(rvm);            
            Assert.AreEqual(0, headers.Count());
        }

        [Test]
        public void can_enumerate_one_pair()
        {
            var rvm = new Rv.Message();
            rvm.AddField("custom", "value");
            var headers = new ReadOnlyRvMessageHeaders(rvm);            
            Assert.AreEqual(1, headers.Count());
            Assert.AreEqual(new KeyValuePair<string, string>("custom","value"), headers.First());
        }

        [Test]
        public void can_enumerate_multiple_pairs()
        {
            var rvm = new Rv.Message();
            rvm.AddField("custom1", "value1");
            rvm.AddField("custom2", "value2");
            var headers = new ReadOnlyRvMessageHeaders(rvm);            
            Assert.AreEqual(new KeyValuePair<string, string>("custom1", "value1"), headers.First());
            Assert.AreEqual(new KeyValuePair<string, string>("custom2", "value2"), headers.ElementAt(1));
            Assert.AreEqual(2, headers.Count());
        }


    }
}
