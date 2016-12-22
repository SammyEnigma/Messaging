using System;
using System.Collections.Generic;
using System.Linq;

namespace Messaging
{
    /// <summary>A mutable message that the sender creates</summary>
    public class Message : IReadOnlyMessage
    {
        MessageHeaders headers;
        string subject;

        /// <summary>The subject (topic) of this message.  Subjects are rooted paths delimited with slashs, e.g. /root/some/subject. 
        /// Think about this as the subject of an e-mail</summary>
        /// <remarks>This is the Label for MSMQ messages, the SendSubject of Tibco RV messages</remarks>
        public string Subject
        {
            get { return subject; }
            set
            {
                if (subject != null && subject.FirstOrDefault() != '/')
                    throw new ArgumentOutOfRangeException(nameof(value), "Subject must start with a leading slash");
                subject = value;
            }
        }

        /// <summary>Address to send a reply to this message.  Can be null</summary>
        /// <example>msmq://host/PRIVATE$/queue</example>
        /// <example>rv://topic/subtopic/etc</example>
        public Uri ReplyTo { get; set; }

        /// <summary>System and user defined message attributes.  Will never be null</summary>
        /// <exception cref="ArgumentNullException">Thrown if you try to set the message headers to null</exception>
        public MessageHeaders Headers
        {
            get
            {
                if (headers == null)
                    headers = new MessageHeaders(); // lazy creation of headers
                return headers;
            }
            set
            {
                if (value == null) throw new ArgumentNullException();
                headers = value;
            }
        }

        /// <summary>System and user defined message attributes</summary>
        IReadOnlyMessageHeaders IReadOnlyMessage.Headers => Headers;

        /// <summary>
        /// The content of the message.  
        /// Remember to set <see cref="MessageHeaders.ContentType"/> to the correct content type, otherwise 'text/plain' or 'application/octet-stream' will be set as the content type
        /// </summary>
        public object Body { get; set; }

        /// <summary>Manually acknowlege a message, ignored if messages are automatically acknowledged</summary>
        /// <remarks>Used to acknowlegde MSMQ transactional messages, and RV certified messages</remarks>
        public void Acknowledge()
        {
            // no nothing by default
        }
    }


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