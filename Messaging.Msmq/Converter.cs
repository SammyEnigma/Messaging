using System;
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
            string name = UriToFormatName(uri);
            var q = Queues.Get(name);
            if (q == null)
                q = Queues.AddOrGetExisting(name, new MSMQ.MessageQueue(name), new CacheItemPolicy { SlidingExpiration = TimeSpan.FromMinutes(20) });

            return (MSMQ.MessageQueue)q;
        }

        static string UriToFormatName(Uri uri)
        {
            var name = new StringBuilder(30);
            var computerOrPublic = uri.Host;

            var pathBits = uri.AbsolutePath.Split('/').ToList();
            var @private = !"public".Equals(computerOrPublic, StringComparison.OrdinalIgnoreCase);
            pathBits.RemoveAt(0);

            var queue = pathBits[0];
            pathBits.RemoveAt(0);

            var topic = string.Join("/", pathBits);

            var subqueue = uri.Fragment;

            if (@private && ".".Equals(computerOrPublic, StringComparison.Ordinal))
                computerOrPublic = Environment.MachineName;

            switch (uri.Scheme)
            {
                case "msmq":
                    if (@private)
                        name.Append("PRIVATE=");
                    else
                        name.Append("PUBLIC=");
                    name.Append(computerOrPublic);
                    break;
                case "msmq+os":
                    name.Append("DIRECT=OS:").Append(computerOrPublic);
                    if (@private)
                        name.Append("\\private$\\");
                    break;
                case "msmq+tcp":
                    name.Append("DIRECT=TCP:").Append(computerOrPublic);
                    if (@private)
                        name.Append("\\private$\\");
                    break;
                case "msmq+http":
                    name.Append("DIRECT=HTTP://").Append(computerOrPublic).Append("/msmq/");
                    if (@private)
                        name.Append("private$\\");
                    break;
                case "msmq+https":
                    name.Append("DIRECT=HTTPS://").Append(computerOrPublic).Append("/msmq/");
                    if (@private)
                        name.Append("private$\\");
                    break;
                default:
                    throw new NotSupportedException($"scheme '{uri.Scheme}' is not supported for uri {uri}");
            }
            name.Append(queue);
            return name.ToString();
        }

        internal static IReadOnlyMessage FromMsmqMessage(MSMQ.Message msmq)
        {
            throw new NotImplementedException();
        }
    }
}