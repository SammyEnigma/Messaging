using System;
using System.Collections.Generic;
using Rv = TIBCO.Rendezvous;
using System.Collections;
using System.Diagnostics.Contracts;

namespace Messaging.TibcoRv
{
    /// <remarks>Body field is not counted as one of the headers, all methods filter this out</remarks>
    class ReadOnlyRvMessageHeaders : IReadOnlyMessageHeaders
    {
        readonly Rv.Message msg;
        readonly Uri _source;

        internal ReadOnlyRvMessageHeaders(Rv.Message msg, Uri source = null)
        {
            Contract.Requires(msg != null);
            this.msg = msg;
            _source = source;
        }

        public string ContentType
        {
            get
            {
                object value;
                if (!TryGetValue(nameof(ContentType), out value))
                    return null;
                return value.ToString();
            }
        }

        public int? Priority
        {
            get
            {
                object value;
                if (!TryGetValue(nameof(Priority), out value))
                    return null;
                return value == null ? default(int?) : Convert.ToInt32(value);
            }
        }

        public Uri ReplyTo
        {
            get
            {
                object xReplyTo;
                if (TryGetValue(Fields.ReplyTo, out xReplyTo))
                    return new Uri(xReplyTo.ToString());
                return string.IsNullOrWhiteSpace(msg.ReplySubject) ? null : new Uri(_source, Converter.FromRvSubject(msg.ReplySubject));
            }
        }

        public TimeSpan? TimeToLive
        {
            get
            {
                object value;
                if (!TryGetValue(nameof(TimeToLive), out value))
                    return null;                    
                return TimeSpans.TryParse(value?.ToString());
            }
        }

        public object this[string key]
        {
            get
            {
                object val;
                if (!TryGetValue(key, out val))
                    throw new KeyNotFoundException();
                return val;
            }
        }

        public int Count => msg.GetField(Fields.Body) == null ? msg.FieldCountAsInt : msg.FieldCountAsInt - 1;

        public bool ContainsKey(string key) => Fields.Body == key ? false : msg.GetField(key) != null;

        public bool TryGetValue(string key, out object value)
        {
            if (Fields.Body == key)
            {
                value = null;
                return false;
            }
            var field = msg.GetField(key);
            value = field?.Value;
            return field != null;
        }

        public IEnumerable<string> Keys => new KeyEnumerator(msg);

        class KeyEnumerator : IEnumerable<string>
        {
            readonly Rv.Message msg;

            public KeyEnumerator(Rv.Message msg) { this.msg = msg; }

            public IEnumerator<string> GetEnumerator()
            {
                for (uint i = 0; i < msg.FieldCount; i++)
                {
                    Rv.MessageField f = msg.GetFieldByIndex(i);
                    if (Fields.Body == f.Name) continue;
                    yield return f.Name;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public IEnumerable<object> Values => new ValueEnumerator(msg);

        class ValueEnumerator : IEnumerable<object>
        {
            readonly Rv.Message msg;

            public ValueEnumerator(Rv.Message msg) { this.msg = msg; }

            public IEnumerator<object> GetEnumerator()
            {
                for (uint i = 0; i < msg.FieldCount; i++)
                {
                    Rv.MessageField f = msg.GetFieldByIndex(i);
                    if (Fields.Body == f.Name) continue;
                    yield return f.Value;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            for (uint i = 0; i < msg.FieldCount; i++)
            {
                Rv.MessageField f = msg.GetFieldByIndex(i);
                if (Fields.Body == f.Name) continue;
                yield return new KeyValuePair<string, object>(f.Name, f.Value);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
