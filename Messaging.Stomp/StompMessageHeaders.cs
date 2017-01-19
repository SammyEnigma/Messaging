using System;
using System.Collections;
using System.Collections.Generic;

namespace Messaging.Stomp
{
    class StompMessageHeaders : IReadOnlyMessageHeaders
    {
        readonly Dictionary<string, object> inner;

        public StompMessageHeaders(Dictionary<string, object> inner)
        {
            if (inner == null)
                throw new ArgumentNullException(nameof(inner));
            this.inner = inner;
        }

        public object this[string key] => inner[key];

        /// <summary>The MIME type of the message, <see cref="ContentTypes"/> for common settings</summary>
        public string ContentType
        {
            get
            {
                object val;
                return TryGetValue("content-type", out val) ? (string)val : "";
            }
        }

        public int? ContentLength
        {
            get
            {
                object val;
                if (!TryGetValue("content-length", out val) || !(val is string))
                    return null;
                int length;
                int.TryParse((string)val, out length);
                return length;
            }
        }

        public int Count => inner.Count;

        public IEnumerable<string> Keys => inner.Keys;

        /// <summary>The priority of this message (optional)</summary>
        /// <example>1</example>
        /// <example>2</example>
        /// <remarks>Not supported by Tibco RV</remarks>
        public int? Priority
        {
            get
            {
                object val;
                if (!TryGetValue(nameof(Priority), out val) || !(val is string))
                    return null;
                int priority;
                int.TryParse((string)val, out priority);
                return priority;
            }
        }

        /// <summary>The expiry time for this message (optional)</summary>
        public TimeSpan? TimeToLive
        {
            get
            {
                object val;
                if (!TryGetValue(nameof(Priority), out val) || !(val is string))
                    return null;
                TimeSpan ttl;
                TimeSpan.TryParse((string)val, out ttl);
                return ttl;
            }
        }

        public IEnumerable<object> Values => inner.Values;

        public bool ContainsKey(string key) => inner.ContainsKey(key);

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => inner.GetEnumerator();

        public bool TryGetValue(string key, out object value) => inner.TryGetValue(key, out value);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}