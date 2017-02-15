using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Messaging
{
    public static partial class Extensions
    {
        /// <summary>Create a transport for a <paramref name="destination"/></summary>
        /// <param name="destination">The URL of the transport</param>
        /// <example>To create a transport to an MSMQ public queue: msmq://Computer/queue</example>
        /// <example>To create a transport to an MSMQ private queue: msmq://Computer/PRIVATE$/queue</example>
        /// <exception cref="UriFormatException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public static ITransport Create(this ITransportFactory factory, string destination)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
            var url = new Uri(destination);
            return factory.Create(url);
        }

        /// <summary>Create a transport for a <paramref name="destination"/></summary>
        /// <param name="destination">The URL of the transport</param>
        /// <example>To create a transport to an MSMQ public queue: msmq://Computer/queue</example>
        /// <example>To create a transport to an MSMQ private queue: msmq://Computer/PRIVATE$/queue</example>
        /// <exception cref="ArgumentNullException">If <paramref name="destination"/> is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">If no transport can be created for the <paramref name="destination"/></exception>
        public static ITransport Create(this ITransportFactory factory, Uri destination)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            ITransport trans = null;
            factory?.TryCreate(destination, out trans);
            if (trans == null)
                throw new ArgumentOutOfRangeException(nameof(destination), "Don't know how to create transport for " + destination);
            return trans;
        }

        public static Task Start(this IWorker worker, CancellationToken cancel = default(CancellationToken), TimeSpan? pollInterval = null)
        {
            var poll = pollInterval.GetValueOrDefault(TimeSpan.FromMilliseconds(100));
            return Task.Factory.StartNew(() =>
            {
                while(!cancel.IsCancellationRequested)
                {
                    worker.DispatchMessage(poll);
                }
            }, cancel, TaskCreationOptions.LongRunning, TaskScheduler.Current);
        }

    }

    public static class TimeSpans
    { 
        public static TimeSpan? TryParse(this string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return default(TimeSpan?);
            TimeSpan value;
            return TimeSpan.TryParse(text, out value) ? value : default(TimeSpan?);
        }
    }

}
