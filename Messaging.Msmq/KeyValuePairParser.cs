using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text;

namespace Messaging.Msmq
{
    /// <summary>
    /// Parses a comma-separated list of key=value pairs.  Strings must be delimited with double quotes, numbers and true/false/null values are also supported.
    /// Double quotes within quotes must be preceeded with blackslash (\).
    /// </summary>
    /// <example>"ContentType"="text/plain","SomeFlag"=true,OptionalThing=null,TimeoutSecs=123</example>
    static class KeyValuePairParser
    {
        public static IEnumerable<KeyValuePair<string, object>> Parse(byte[] text) => Parse(Encoding.UTF8.GetString(text));

        public static IEnumerable<KeyValuePair<string, object>> Parse(string text)
        {
            var tokens = Lex(text).GetEnumerator();
            bool expectComma = false;
            for(;;)
            {
                if (!tokens.MoveNext())
                    break;

                if (expectComma)
                {
                    var comma = tokens.Current;
                    if (comma.Type != TokenType.Comma)
                        throw new ParseException($"Expected comma but was {comma}");
                    if (!tokens.MoveNext())
                        throw new ParseException("Expected a value after " + comma);
                }

                var name = tokens.Current;
                if (name.Type != TokenType.String)
                    throw new ParseException("Expected a string for key but was a " + name);

                if (!tokens.MoveNext())
                    throw new ParseException("Expected equals after " + name);
                var equals = tokens.Current;
                if (equals.Type != TokenType.Equals)
                    throw new ParseException($"Expected equals after {name} but was {equals}");

                if (!tokens.MoveNext())
                    throw new ParseException("Expected value after " + name);
                var value = tokens.Current;

                yield return new KeyValuePair<string, object>(name.Value, value.Convert());
                expectComma = true;
            }
        }

        public static IEnumerable<Token> Lex(string text)
        {
            var sb = new StringBuilder(20);
            var ch = new CharEnumerator(text);
            while (ch.MoveNext())
            {
                switch (ch.Current)
                {
                    case '"':
                        yield return PaseString(ch, sb);
                        break;
                    case '=':
                        yield return new Token("=", TokenType.Equals);
                        break;
                    case ',':
                        yield return new Token(",", TokenType.Comma);
                        break;
                    case 't':
                        yield return ParseTrue(ch);
                        break;
                    case 'f':
                        yield return ParseFalse(ch);
                        break;
                    case 'n':
                        yield return ParseNull(ch);
                        break;
                    default:
                        if (!char.IsDigit(ch.Current))
                            throw new ParseException($"Unexpected char '{ch.Current}'");
                        yield return ParseNumber(ch, sb);
                        break;
                }
            }
        }

        static Token PaseString(CharEnumerator ch, StringBuilder sb)
        {
            Debug.Assert(ch.Current == '"');
            sb.Clear();
            if (!ch.MoveNext())
                throw new ParseException("Unterminated string");
            for (;;)
            {
                if (ch.Current == '"')
                    break;
                if (ch.Current == '\\' && ch.Next == '"')
                    ch.MoveNext();
                sb.Append(ch.Current);
                ch.MoveNext();
            }
            return new Token(sb.ToString(), TokenType.String);
        }

        static Token ParseTrue(CharEnumerator ch)
        {
            Debug.Assert(ch.Current == 't');
            if (ch.MoveNext() && ch.Current == 'r'
                && ch.MoveNext() && ch.Current == 'u'
                && ch.MoveNext() && ch.Current == 'e')
                return new Token("true", TokenType.True);
            throw new ParseException("Invalid character when reading expecting 'true'");
        }

        static Token ParseFalse(CharEnumerator ch)
        {
            Debug.Assert(ch.Current == 'f');
            if (ch.MoveNext() && ch.Current == 'a'
                && ch.MoveNext() && ch.Current == 'l'
                && ch.MoveNext() && ch.Current == 's'
                && ch.MoveNext() && ch.Current == 'e')
                return new Token("false", TokenType.False);
            throw new ParseException("Invalid character when reading expecting 'false'");
        }

        static Token ParseNull(CharEnumerator ch)
        {
            Debug.Assert(ch.Current == 'n');
            if (ch.MoveNext() && ch.Current == 'u'
                && ch.MoveNext() && ch.Current == 'l'
                && ch.MoveNext() && ch.Current == 'l')
                return new Token(null, TokenType.Null);
            throw new ParseException("Invalid character when reading expecting 'null'");
        }

        static Token ParseNumber(CharEnumerator ch, StringBuilder sb)
        {
            Debug.Assert(char.IsDigit(ch.Current));
            sb.Clear();
            ParseInteger(ch, sb);
            if (ch.Next == '.')
            {
                ch.MoveNext();
                if (!char.IsDigit(ch.Next))
                    throw new ParseException("Number with decimal place but no digits after it");
                ParseInteger(ch, sb);
            }
            return new Token(sb.ToString(), TokenType.Number);
        }

        static void ParseInteger(CharEnumerator ch, StringBuilder sb)
        {
            for (;;)
            {
                sb.Append(ch.Current);
                if (!char.IsDigit(ch.Next))
                    break;
                ch.MoveNext();
            }
        }
    }

    class CharEnumerator : IEnumerator<char>
    {
        string text;
        int index;

        public CharEnumerator(string text)
        {
            this.text = text;
            Reset();
        }

        public char Current => index < 0 || index >= text.Length ? default(char) : text[index];

        public char Next => index + 1 >= text.Length ? default(char) : text[index+1];

        object IEnumerator.Current => Current;

        public void Dispose()
        {
        }

        public bool MoveNext()
        {
            index++;
            return index < text.Length;
        }

        public void Reset()
        {
            index = -1;
        }
    }

    [Serializable]
    internal class ParseException : Exception
    {
        public ParseException()
        {
        }

        public ParseException(string message) : base(message)
        {
        }

        public ParseException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ParseException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    struct Token : IEquatable<Token>
    {
        public string Value { get; }
        public TokenType Type { get; }

        public Token(string value, TokenType type)
        {
            Type = type;
            Value = value;
        }

        public override string ToString() => $"{Type} '{Value}'";

        public object Convert()
        {
            switch (Type)
            {
                case TokenType.String: return Value;
                case TokenType.Number: return decimal.Parse(Value);
                case TokenType.True: return true;
                case TokenType.False: return false;
                case TokenType.Null: return null;
                default: throw new InvalidOperationException("Cannot get a value for a " + Type);
            }
        }

        public bool Equals(Token other) => Type == other.Type && Value == other.Value;

        public override bool Equals(object obj) => obj is Token && Equals((Token)obj);

        public override int GetHashCode() => Type.GetHashCode() + Value == null ? 0 : Value.GetHashCode();
    }

    enum TokenType
    {
        String=1,
        Number,
        True,
        False,
        Equals,
        Comma,
        Null,
    }
}
