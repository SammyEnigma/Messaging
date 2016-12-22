using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messaging.TibcoRv
{
    [TestFixture]
    public class ConvertToRvMessageTests
    {
        [Test]
        public void can_convert_string_body()
        {
            var input = new Message
            {
                Body = "hello world",
            };
            var output = Converter.ToRvMessge(input, new Uri("rv://service/test/topic"));
            Assert.AreEqual("hello world", output.GetField("Body")?.Value);
        }

        [Test]
        public void can_convert_byte_array_body()
        {
            var input = new Message
            {
                Body = Encoding.UTF8.GetBytes("hello world"),
            };
            var output = Converter.ToRvMessge(input, new Uri("rv://service/test/topic"));
            Assert.AreEqual("hello world", Encoding.UTF8.GetString((byte[])output.GetField("Body")?.Value));
        }

        [Test]
        public void can_convert_subject_replacing_slashing_with_dots()
        {
            var input = new Message
            {
                Subject = "/test/topic",
            };
            var output = Converter.ToRvMessge(input, new Uri("rv://service/test/topic"));
            Assert.AreEqual("test.topic", output.SendSubject);
        }

        [Test]
        public void can_convert_replyto_to_rv_subject_on_same_service()
        {
            var input = new Message
            {
                ReplyTo = new Uri("rv://service/other/topic"),
            };
            var output = Converter.ToRvMessge(input, new Uri("rv://service/test/topic"));
            Assert.AreEqual("other.topic", output.ReplySubject);
        }

        [Test]
        public void can_convert_content_type_to_rv_field()
        {
            var input = new Message
            {
                Headers = new MessageHeaders { { "ContentType", ContentTypes.PlainText } }
            };
            var output = Converter.ToRvMessge(input, new Uri("rv://service/test/topic"));
            Assert.AreEqual(ContentTypes.PlainText, output.GetField("ContentType")?.Value);
        }

        [Test]
        public void can_convert_custom_header_to_rv_field()
        {
            var input = new Message
            {
                Headers = new MessageHeaders { { "custom1", "hello" } }
            };
            var output = Converter.ToRvMessge(input, new Uri("rv://service/test/topic"));
            Assert.AreEqual("hello", output.GetField("custom1")?.Value);
        }
    }
}
