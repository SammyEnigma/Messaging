using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rv = TIBCO.Rendezvous;

namespace Messaging.TibcoRv.UnitTests
{
    [TestFixture]
    public class ReadOnlyRvMessageTests
    {
        [Test]
        public void empty_message_has_no_subject()
        {
            var rvm = new Rv.Message();
            var msg = new ReadOnlyRvMessage(rvm, null);
            Assert.AreEqual(null, msg.Subject);
        }

        [Test]
        public void message_Subject_comes_from_rv_SendSubject()
        {
            var rvm = new Rv.Message();
            rvm.SendSubject = "test.topic";
            var msg = new ReadOnlyRvMessage(rvm, null);
            Assert.AreEqual("test.topic", msg.Subject);
        }

        [Test]
        public void empty_message_has_no_reply_uri()
        {
            var rvm = new Rv.Message();
            var msg = new ReadOnlyRvMessage(rvm, null);
            Assert.AreEqual(null, msg.Headers.ReplyTo);
        }

        [Test]
        public void empty_message_has_null_body()
        {
            var rvm = new Rv.Message();
            var msg = new ReadOnlyRvMessage(rvm, null);
            Assert.AreEqual(null, msg.Body);
        }

        [Test]
        public void empty_message_has_zero_headers()
        {
            var rvm = new Rv.Message();
            var msg = new ReadOnlyRvMessage(rvm, null);
            Assert.AreEqual(0, msg.Headers.Count);
        }

        [Test]
        public void can_create_message_with_body()
        {
            var rvm = new Rv.Message();
            rvm.AddField("Body", "hello");
            var msg = new ReadOnlyRvMessage(rvm, null);
            Assert.AreEqual("hello", msg.Body);
        }

        [Test]
        public void ReplyTo_is_set_from_source_and_rv_ReplySubject_with_slashes_replacing_dots()
        {
            var rvm = new Rv.Message();            
            rvm.ReplySubject = "test.topic";
            var msg = new ReadOnlyRvMessage(rvm, new Uri("rv://service"));
            Assert.AreEqual(new Uri("rv://service/test/topic"), msg.Headers.ReplyTo);
        }
    }
}
