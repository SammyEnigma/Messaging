using System;
using System.Collections.Generic;

namespace Messaging
{

    public class MessageHeaders : Dictionary<string, object>, IReadOnlyMessageHeaders
    {
        /// <summary>The MIME type of the message, <see cref="ContentTypes"/> for common settings</summary>
        public string ContentType
        {
            get
            {
                object val;
                return TryGetValue(nameof(ContentType), out val) ? (string)val : "";
            }
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                    this[nameof(ContentType)] = value;
                else
                    Remove(nameof(ContentType));
            }
        }

        /// <summary>The priority of this message (optional)</summary>
        /// <example>1</example>
        /// <example>2</example>
        /// <remarks>Not supported by Tibco RV</remarks>
        public int? Priority
        {
            get
            {
                object val;
                return TryGetValue(nameof(Priority), out val)  && val is int? (int)val : (int?)null;
            }
            set
            {
                if (value != null)
                    this[nameof(Priority)] = value.Value;
                else
                    Remove(nameof(Priority));
            }
        }

        /// <summary>The expiry time for this message (optional)</summary>
        public TimeSpan? TimeToLive
        {
            get
            {
                object val;
                return TryGetValue(nameof(TimeToLive), out val) && val is TimeSpan ? (TimeSpan)val : (TimeSpan?)null;
            }
            set
            {
                if (value != null)
                    this[nameof(TimeToLive)] = value.Value;
                else
                    Remove(nameof(TimeToLive));
            }
        }

    }
}