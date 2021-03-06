﻿using System;
using System.Collections.Generic;

namespace Messaging
{
    /// <summary>Read only view of a message</summary>
    [System.ComponentModel.ImmutableObject(true)]
    public interface IReadOnlyMessage : IDisposable
    {
        /// <summary>The subject (topic) of this message.  Think about this as the subject of an e-mail.</summary>
        /// <remarks>This is the Label for MSMQ messages, the SendSubject of Tibco RV messages</remarks>
        string Subject { get; }

        /// <summary>Does this message have any <see cref="Headers"/>?</summary>
        /// <remarks>Prevents extra garbage when sending messages</remarks>
        bool HasHeaders { get; }

        /// <summary>System and user defined message attributes</summary>
        IReadOnlyMessageHeaders Headers { get; }

        /// <summary>The main content of the message</summary>
        object Body { get; }

        /// <summary>Acknowlege the receipt of a message.  If <see cref="Acknowledge"/> is NOT called before <see cref="IDisposable.Dispose"/> then the message will be re-delivered.</summary>
        /// <remarks>ignored if messages are automatically acknowledged</remarks>
        void Acknowledge();
    }

    /// <summary>Read only view of message headers</summary>
    [System.ComponentModel.ImmutableObject(true)]
    public interface IReadOnlyMessageHeaders : IReadOnlyDictionary<string, object>
    {
        /// <summary>The MIME type of the message, <see cref="ContentTypes"/> for common settings</summary>
        string ContentType { get; }

        /// <summary>the priority of this message</summary>
        /// <example>1</example>
        /// <example>2</example>
        /// <remarks>Ignored by Tibco RV, as priority is not supported.</remarks>
        int? Priority { get; }

        /// <summary>Where to send a reply to, can be NULL</summary>
        /// <example>msmq://host/PRIVATE$/queue</example>
        /// <example>rv://service/topic/subtopic/etc</example>
        Uri ReplyTo { get; }

        /// <summary>Sets an expiry time for this message</summary>
        TimeSpan? TimeToLive { get; }
    }

    public static class ContentTypes
    {
        public const string PlainText = "text/plain";
        public const string Xml = "text/xml";
        public const string Json = "application/json";
        public const string Binary = "application/octet-stream";
    }

}