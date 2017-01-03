using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using MSMQ = System.Messaging;

namespace Messaging.Msmq
{

    static class Converter
    {
        public static MSMQ.Message ToMsmqMessage(IReadOnlyMessage msg)
        {
            return new MSMQ.Message
            {
                Priority = msg.Headers.Priority.HasValue ? (MSMQ.MessagePriority)msg.Headers.Priority : MSMQ.MessagePriority.Normal,
                TimeToBeReceived = msg.Headers.TimeToLive.HasValue ? msg.Headers.TimeToLive.Value : MSMQ.Message.InfiniteTimeout,
                ResponseQueue = GetOrAddQueue(msg.ReplyTo),
            };
        }

        static internal MemoryCache Queues { get; } = new MemoryCache("queues");

        public static MSMQ.MessageQueue GetOrAddQueue(Uri uri)
        {
            if (uri == null) return null;
            var details = UriToQueueName(uri);
            var q = Queues.Get(details.QueueName);
            if (q == null)
                q = Queues.AddOrGetExisting(details.QueueName, new MSMQ.MessageQueue(details.QueueName), new CacheItemPolicy { SlidingExpiration = TimeSpan.FromMinutes(20) });

            return (MSMQ.MessageQueue)q;
        }

        public static QueueDetails UriToQueueName(Uri uri)
        {
            var name = new StringBuilder(30);
            var host = uri.Host;
            var pathBits = uri.AbsolutePath.Split('/').ToList();

            Debug.Assert(pathBits[0].Length == 0, "URI starts with / so first entry is empty");
            pathBits.RemoveAt(0);

            var @private = "private$".Equals(pathBits[0], StringComparison.OrdinalIgnoreCase);
            if (@private)
                pathBits.RemoveAt(0);

            var queue = pathBits[0];
            pathBits.RemoveAt(0);

            var topic = string.Join("/", pathBits);

            var subqueue = uri.Fragment;

            if (@private && ".".Equals(host, StringComparison.Ordinal))
                host = Environment.MachineName;

            switch (uri.Scheme)
            {
                case "msmq":
                    name.Append(host).Append('\\');
                    if (@private)
                        name.Append(@"private$\");
                    name.Append(queue);
                    break;
                case "msmq+os":
                    name.Append("FORMATNAME:DIRECT=OS:").Append(host).Append('\\');
                    if (@private)
                        name.Append("private$\\");
                    name.Append(queue);
                    break;
                case "msmq+tcp":
                    name.Append("FORMATNAME:DIRECT=TCP:").Append(host).Append('\\');
                    if (@private)
                        name.Append("private$\\");
                    name.Append(queue);
                    break;
                case "msmq+http":
                    name.Append("FORMATNAME:DIRECT=HTTP://").Append(host).Append("/msmq/");
                    if (@private)
                        name.Append("private$\\");
                    name.Append(queue);
                    break;
                case "msmq+https":
                    name.Append("FORMATNAME:DIRECT=HTTPS://").Append(host).Append("/msmq/");
                    if (@private)
                        name.Append("private$\\");
                    name.Append(queue);
                    break;
                case "msmq+pgm":
                    name.Append("FORMATNAME:MULTICAST=").Append(host).Append(':').Append(uri.Port);
                    break;
                default:
                    throw new NotSupportedException($"scheme '{uri.Scheme}' is not supported for uri {uri}");
            }
            return new QueueDetails(name.ToString(), subqueue, topic);
        }

        internal static IReadOnlyMessage FromMsmqMessage(MSMQ.Message msmq)
        {
            throw new NotImplementedException();
        }
    }

    struct QueueDetails
    {
        public string QueueName { get; }
        public string Subqueue { get; }
        public string Topic { get; }

        public QueueDetails(string formatName, string subqueue, string topic)
        {
            QueueName = formatName;
            Subqueue = subqueue;
            Topic = topic;
        }
    }
}