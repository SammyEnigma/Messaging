using NUnit.Framework;
using System.Linq;

namespace Messaging.Msmq.UnitTests
{

    [TestFixture]
    public class KeyValuePairParserTests
    {
        [Test]
        public void can_lex_empty_string()
        {
            Assert.AreEqual(false, KeyValuePairParser.Lex("").Any());
        }

        [Test]
        public void can_lex_single_null()
        {
            var token = KeyValuePairParser.Lex("null").FirstOrDefault();
            Assert.AreEqual(TokenType.Null, token.Type);
            Assert.AreEqual(null, token.Value);
        }

        [TestCase("1", TokenType.Number)]
        [TestCase("=", TokenType.Equals)]
        [TestCase(",", TokenType.Comma)]
        [TestCase("true", TokenType.True)]
        [TestCase("false", TokenType.False)]
        public void can_lex_single_token(string input, object expected)
        {
            TokenType expectedTT = (TokenType)expected;
            var token = KeyValuePairParser.Lex(input).FirstOrDefault();
            Assert.AreEqual(expectedTT, token.Type);
            Assert.AreEqual(input, token.Value);
        }

        [TestCase(@"""abc""", "abc")]
        [TestCase(@"""a\""bc""", "a\"bc")]
        [TestCase(@"""""", "")]
        public void can_lex_string(string input, string expected)
        {
            var token = KeyValuePairParser.Lex(input).FirstOrDefault();
            Assert.AreEqual(TokenType.String, token.Type);
            Assert.AreEqual(expected, token.Value);
        }

        [Test]
        public void can_lex_sequence_of_tokens()
        {
            var tokens = KeyValuePairParser.Lex("\"first\"=123,\"second\"=true").GetEnumerator();
            Assert.AreEqual(true, tokens.MoveNext(), "first");
            Assert.AreEqual(new Token("first", TokenType.String), tokens.Current);
            Assert.AreEqual(true, tokens.MoveNext(), "=");
            Assert.AreEqual(new Token("=", TokenType.Equals), tokens.Current);
            Assert.AreEqual(true, tokens.MoveNext(), "123");
            Assert.AreEqual(new Token("123", TokenType.Number), tokens.Current);
            Assert.AreEqual(true, tokens.MoveNext(), ",");
            Assert.AreEqual(new Token(",", TokenType.Comma), tokens.Current);

            Assert.AreEqual(true, tokens.MoveNext(), "second");
            Assert.AreEqual(new Token("second", TokenType.String), tokens.Current);
            Assert.AreEqual(true, tokens.MoveNext(), "=");
            Assert.AreEqual(new Token("=", TokenType.Equals), tokens.Current);
            Assert.AreEqual(true, tokens.MoveNext(), "true");
            Assert.AreEqual(new Token("true", TokenType.True), tokens.Current);
            Assert.AreEqual(false, tokens.MoveNext(), "end");
        }

        [Test]
        public void can_parse_zero_pairs()
        {
            Assert.AreEqual(false, KeyValuePairParser.Parse("").Any());
        }

        [Test]
        public void can_parse_a_single_pair()
        {
            var first = KeyValuePairParser.Parse("\"first\"=123").SingleOrDefault();
            Assert.AreEqual("first", first.Key, "first.Key");
            Assert.AreEqual(123, first.Value, "first.Value");
        }

        [Test]
        public void can_parse_a_multiple_pairs()
        {
            var seq = KeyValuePairParser.Parse("\"first\"=123,\"second\"=true");
            var first = seq.ElementAt(0);
            Assert.AreEqual("first", first.Key, "first.Key");
            Assert.AreEqual(123, first.Value, "first.Value");

            var second = seq.ElementAt(1);
            Assert.AreEqual("second", second.Key, "second.Key");
            Assert.AreEqual(true, second.Value, "second.Value");

            Assert.AreEqual(2, seq.Count());
        }
    }
}
