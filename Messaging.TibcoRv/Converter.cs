using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Xml;
using TIBCO.Rendezvous;
using Rv = TIBCO.Rendezvous;

namespace Messaging.TibcoRv
{
    static class Fields
    {
        public static string ReplyTo = "X-ReplyTo";
        public static string Body = "Body";
    }

    static class Converter
    {
        public static string ToRvSubject(this string path) => path.TrimStart('/').Replace('/', '.').TrimStart('/');

        public static string FromRvSubject(this string subject) => "/" + subject.Replace('.', '/');

        public static bool IsRvScheme(this string scheme) => scheme == "rv" || scheme.StartsWith("rv+", StringComparison.Ordinal);

        public static Rv.Message ToRvMessge(IReadOnlyMessage msg, Uri source)
        {
            Contract.Requires(msg != null);
            Contract.Requires(source != null);

            var rvm = new Rv.Message();
            if (msg.Subject != null)
            {
                rvm.SendSubject = ToRvSubject(msg.Subject);
            }
            if (msg.Headers.ReplyTo != null)
            {
                AddReplyTo(source, rvm, msg.Headers.ReplyTo);
            }
            if (msg.Headers.Priority.HasValue)
            {
                AddPriority(msg, rvm);
            }
            foreach (KeyValuePair<string, object> pair in msg.Headers)
            {
                AddRvField(rvm, pair.Key, pair.Value);
            }
            if (msg.Body != null)
            {
                AddRvField(rvm, Fields.Body, msg.Body);
            }
            return rvm;
        }

        static void AddReplyTo(Uri source, Rv.Message rvm, Uri replyTo)
        {
            if (source.Scheme.IsRvScheme() && source.Host == replyTo.Host && source.Port == replyTo.Port)
                rvm.ReplySubject = ToRvSubject(replyTo.AbsolutePath);
            else
                rvm.AddField(Fields.ReplyTo, replyTo.ToString());
        }

        static void AddPriority(IReadOnlyMessage msg, Rv.Message rvm)
        {
            rvm.AddField(nameof(msg.Headers.Priority), msg.Headers.Priority.Value);
        }

        static void AddRvField(Rv.Message rv, string name, object value)
        {
            if (value is string)
                rv.AddField(name, (string)value);
            else if (value is int)
                rv.AddField(name, (int)value);
            else if (value is long)
                rv.AddField(name, (long)value);
            else if (value is DateTime)
                rv.AddField(name, (DateTime)value);
            else if (value is XmlDocument)
                rv.AddField(name, (XmlDocument)value);
            else if (value is byte[])
                rv.AddField(name, (byte[])value);
            else if (value is short)
                rv.AddField(name, (short)value);
            else if (value is byte)
                rv.AddField(name, (byte)value);
            else if (value is double)
                rv.AddField(name, (double)value);
            else if (value is float)
                rv.AddField(name, (float)value);
            else
                rv.AddField(name, value.ToString());
        }

        public static Message FromRvMessage(Rv.Message rv, Uri source)
        {
            Contract.Requires(rv != null);
            Contract.Requires(source != null);

            var msg = new Message { Subject = rv.SendSubject };
            if (!string.IsNullOrWhiteSpace(rv.ReplySubject))
                msg.Headers.ReplyTo = new Uri(source, rv.ReplySubject.FromRvSubject());

            for (uint i = 0; i < rv.FieldCount; i++)
            {
                MessageField f = rv.GetFieldByIndex(i);
                if (f.Name == Fields.ReplyTo)
                {
                    var xReplyTo = f.Value?.ToString();
                    if (!string.IsNullOrWhiteSpace(xReplyTo))
                    {
                        msg.Headers.ReplyTo = new Uri(f.Value.ToString());
                        continue;
                    }
                }
                msg.Headers.Add(f.Name, f.Value);
            }
            return msg;
        }
    }

}
