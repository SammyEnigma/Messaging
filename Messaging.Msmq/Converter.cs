﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using MSMQ = System.Messaging;
using static System.StringComparison;

namespace Messaging.Msmq
{
    static class Converter
    {
        static readonly UTF8Encoding utf8 = new UTF8Encoding(false);

        public static MSMQ.Message ToMsmqMessage(this IReadOnlyMessage msg)
        {
            if (msg.HasHeaders)
            {
                return new MSMQ.Message
                {
                    Priority = msg.Headers.Priority.HasValue ? (MSMQ.MessagePriority)msg.Headers.Priority : MSMQ.MessagePriority.Normal,
                    TimeToBeReceived = msg.Headers.TimeToLive.HasValue ? msg.Headers.TimeToLive.Value : MSMQ.Message.InfiniteTimeout,
                    ResponseQueue = GetOrAddQueue(msg.Headers.ReplyTo),
                    Body = msg.Body,
                    Extension = EncodeExtension(msg.Headers)
                };
            } 
            else
            {
                return new MSMQ.Message { Body = msg.Body };
            }
        }

        private static byte[] EncodeExtension(IReadOnlyMessageHeaders headers)
        {
            var sb = new StringBuilder(30);
            foreach (var pair in headers.Where(p => p.Key != nameof(IReadOnlyMessageHeaders.Priority) && p.Key != nameof(IReadOnlyMessageHeaders.TimeToLive)))
            {
                sb.Append('"').Append(pair.Key.Replace("\"", "\\\"")).Append('"').Append("=");
                if (pair.Value is bool)
                    sb.Append(pair.Value);
                else if (pair.Value is int || pair.Value is short || pair.Value is byte || pair.Value is long)
                    sb.Append(pair.Value);
                else if (pair.Value is decimal)
                    sb.Append(pair.Value);
                else if (pair.Value is float || pair.Value is double)
                    sb.Append(pair.Value);
                else if (pair.Value == null)
                    sb.Append("null");
                else
                    sb.Append('"').Append(pair.Value.ToString().Replace("\"", "\\\"")).Append('"');
                sb.Append(",");
            }
            if (sb.Length > 0)
                sb.Length--;
            return utf8.GetBytes(sb.ToString());
        }
    

        static internal MemoryCache Queues { get; } = new MemoryCache("queues");

        public static MSMQ.MessageQueue GetOrAddQueue(Uri uri)
        {
            QueueDetails details;
            return GetOrAddQueue(uri, out details);
        }

        public static MSMQ.MessageQueue GetOrAddQueue(Uri uri, out QueueDetails details)
        {
            details = default(QueueDetails);
            if (uri == null) return null;

            details = UriToQueueName(uri);
            if (!details.Valid) return null;

            var q = (MSMQ.MessageQueue)Queues.Get(details.QueueName);
            if (q == null)
            {
                q = new MSMQ.MessageQueue(details.QueueName);
                Queues.AddOrGetExisting(details.QueueName, q, new CacheItemPolicy { SlidingExpiration = TimeSpan.FromMinutes(20) });
            }
            return (MSMQ.MessageQueue)q;
        }

        internal static Uri QueueNameToUri(MSMQ.MessageQueue queue)
        {
            var fn = FormatName.Parse(queue.FormatName);
            if (fn == null) return null;
            var sb = new StringBuilder(30);
            if (fn.Scheme.Equals("multicast", OrdinalIgnoreCase))
            {
                sb.Append("msmq+pgm://").Append(fn.Host).Append('/');
            }
            else if (IsDirectWithMulticast(queue, fn))
            {
                sb.Append("msmq+pgm://").Append(queue.MulticastAddress).Append(fn.Path.Replace('\\', '/'));
            }
            else
            {
                // not multicast
                if (string.Equals(fn.Scheme, "OS", OrdinalIgnoreCase))
                    sb.Append("msmq+os");
                else if (string.Equals(fn.Scheme, "TCP", OrdinalIgnoreCase))
                    sb.Append("msmq+tcp");
                else if (string.Equals(fn.Scheme, "HTTP", OrdinalIgnoreCase))
                    sb.Append("msmq+http");
                else if (string.Equals(fn.Scheme, "HTTPS", OrdinalIgnoreCase))
                    sb.Append("msmq+https");
                else
                    return null;
                sb.Append("://").Append(fn.Host).Append(fn.Path.Replace('\\', '/'));
            }
            return new Uri(sb.ToString());
        }

        static bool IsDirectWithMulticast(MSMQ.MessageQueue queue, FormatName fn)
        {
            try
            {
                return fn.Scheme.Equals("OS", OrdinalIgnoreCase) && !string.IsNullOrEmpty(queue.MulticastAddress);
            }
            catch (MSMQ.MessageQueueException ex) when (ex.MessageQueueErrorCode == MSMQ.MessageQueueErrorCode.UnsupportedFormatNameOperation)
            {
                return false;
            }
        }

        public static QueueDetails UriToQueueName(Uri uri)
        {
            var name = new StringBuilder(30);
            var host = uri.Host;
            var pathBits = uri.AbsolutePath.Split('/').ToList();

            Debug.Assert(pathBits[0].Length == 0, "URI starts with / so first entry is empty");
            pathBits.RemoveAt(0);

            var @private = "private$".Equals(pathBits[0], OrdinalIgnoreCase);
            if (@private)
                pathBits.RemoveAt(0);

            var queue = pathBits[0];
            pathBits.RemoveAt(0);

            var topic = string.Join("/", pathBits);

            var subqueue = uri.Fragment;

            if (@private && "localhost".Equals(host, OrdinalIgnoreCase))
                host = ".";

            var query = uri.Query;
            bool express = string.Equals("?express=true", query, OrdinalIgnoreCase);

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
                    return new QueueDetails();  // not valid
            }

            if (!string.IsNullOrEmpty(subqueue))
                name.Append(';').Append(subqueue);

            return new QueueDetails(name.ToString(), subqueue, topic, !express);
        }

    }

    struct QueueDetails
    {
        public bool Valid => !string.IsNullOrEmpty(QueueName);

        public string QueueName { get; }
        public string Subqueue { get; }
        public string Subject { get; }
        public bool Recoverable { get; }

        public QueueDetails(string formatName, string subqueue, string topic, bool recoverable)
        {
            QueueName = formatName;
            Subqueue = subqueue;
            Subject = topic;
            Recoverable = recoverable;
        }
    }
}