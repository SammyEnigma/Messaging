using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using MSMQ = System.Messaging;

namespace Messaging.Msmq
{
    class ReadOnlyMsmqMessageHeaders : IReadOnlyMessageHeaders
    {
        readonly MSMQ.Message _msg;
        readonly Dictionary<string, object> _extensions;

        public ReadOnlyMsmqMessageHeaders(MSMQ.Message msg)
        {
            Contract.Requires(msg != null);
            _msg = msg;

            _extensions = new Dictionary<string, object>();
            foreach (var p in KeyValuePairParser.Parse(msg.Extension))
            {
                _extensions.Add(p.Key, p.Value);
            }
        }

        public object this[string key]
        {
            get
            {
                object value;
                if (!TryGetValue(key, out value))
                    throw new KeyNotFoundException(key);
                return value;
            }
        }

        public string ContentType
        {
            get
            {
                object value;
                if (_extensions.TryGetValue(nameof(ContentType), out value))
                    return value.ToString();
                return null;
            }
        }

        public int Count
        {
            get
            {
                var count = _extensions.Count + 1; //+1 for priority
                if (TimeToLive.HasValue)
                    count++;
                return count;
            }
        }

        public IEnumerable<string> Keys
        {
            get
            {
                foreach (var item in _extensions.Keys)
                {
                    yield return item;
                }

                yield return nameof(Priority);

                var ttl = TimeToLive;
                if (ttl.HasValue)
                    yield return nameof(TimeToLive);
            }
        }

        public int? Priority => (int)_msg.Priority;

        public Uri ReplyTo
        {
            get
            {
                Uri replyTo = null;
                if (_msg.ResponseQueue != null)
                {
                    replyTo = Converter.QueueNameToUri(_msg.ResponseQueue);
                }
                if (replyTo == null && ContainsKey(nameof(ReplyTo)))
                {
                    object val = this[nameof(ReplyTo)];
                    replyTo = new Uri(val.ToString());
                }
                return replyTo;
            }
        }

        public TimeSpan? TimeToLive => _msg.TimeToBeReceived == MSMQ.Message.InfiniteTimeout ? default(TimeSpan?) : _msg.TimeToBeReceived; 

        public IEnumerable<object> Values
        {
            get
            {
                foreach (var item in _extensions.Values)
                {
                    yield return item;
                }

                yield return Priority;

                var ttl = TimeToLive;
                if (ttl.HasValue)
                    yield return ttl;
            }
        }

        public bool ContainsKey(string key)
        {
            if (_extensions.ContainsKey(key))
                return true;
            if (key == nameof(Priority))
                return true;
            if (key == nameof(TimeToLive))
                return TimeToLive.HasValue;
            return false;
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            foreach (var item in _extensions)
            {
                yield return item;
            }

            yield return new KeyValuePair<string, object>(nameof(Priority), Priority);

            var ttl = TimeToLive;
            if (ttl.HasValue)
                yield return new KeyValuePair<string, object>(nameof(TimeToLive), ttl);
        }

        public bool TryGetValue(string key, out object value)
        {
            if (_extensions.TryGetValue(key, out value))
            {
                return true;
            }
            if (key == nameof(Priority))
            {
                value = Priority;
                return true;
            }
            if (key == nameof(TimeToLive) && TimeToLive.HasValue)
            {
                value = TimeToLive;
                return true;
            }
            return false;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    
}