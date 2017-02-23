using System;
using System.Threading;
using System.Threading.Tasks;

namespace Messaging
{
    public static class Extensions
    {
        /// <summary>Create a <see cref="IMessaging"/> for a <paramref name="address"/> </summary>
        /// <param name="address">The URL to create</param>
        /// <example>To create a <see cref="IMessaging"/> to an MSMQ public queue: msmq://Computer/queue</example>
        /// <example>To create a <see cref="IMessaging"/> to an MSMQ private queue: msmq://Computer/PRIVATE$/queue</example>
        /// <exception cref="UriFormatException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public static IMessaging Create(this IMessagingFactory factory, string address)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
            var url = new Uri(address);
            return factory.Create(url);
        }

        /// <summary>Create a <see cref="IMessaging"/> for a <paramref name="address"/></summary>
        /// <param name="address">The URL to create</param>
        /// <example>To create a <see cref="IMessaging"/> to an MSMQ public queue: msmq://Computer/queue</example>
        /// <example>To create a <see cref="IMessaging"/> to an MSMQ private queue: msmq://Computer/PRIVATE$/queue</example>
        /// <exception cref="ArgumentNullException">If <paramref name="address"/> is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">If no transport can be created for the <paramref name="address"/></exception>
        public static IMessaging Create(this IMessagingFactory factory, Uri address)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            IMessaging messaging = null;
            factory?.TryCreate(address, out messaging);
            if (messaging == null)
                throw new ArgumentOutOfRangeException(nameof(address), "Don't know how to create transport for " + address);
            return messaging;
        }

        /// <summary>Create a <see cref="IMessaging"/> for a <paramref name="address"/> </summary>
        /// <param name="address">The URL to create</param>
        /// <example>To create a <see cref="IMessaging"/> to an MSMQ public queue: msmq://Computer/queue</example>
        /// <example>To create a <see cref="IMessaging"/> to an MSMQ private queue: msmq://Computer/PRIVATE$/queue</example>
        /// <exception cref="UriFormatException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public static IMultiSubjectMessaging CreateMultiSubject(this IMessagingFactory factory, string address)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
            var url = new Uri(address);
            return factory.CreateMultiSubject(url);
        }

        /// <summary>Create a <see cref="IMultiSubjectMessaging"/> for a <paramref name="address"/></summary>
        /// <param name="address">The URL to create</param>
        /// <example>To create a <see cref="IMultiSubjectMessaging"/> to an MSMQ public queue: msmq://Computer/queue</example>
        /// <example>To create a <see cref="IMultiSubjectMessaging"/> to an MSMQ private queue: msmq://Computer/PRIVATE$/queue</example>
        /// <exception cref="ArgumentNullException">If <paramref name="address"/> is null</exception>
        /// <exception cref="ArgumentOutOfRangeException">If no transport can be created for the <paramref name="address"/></exception>
        public static IMultiSubjectMessaging CreateMultiSubject(this IMessagingFactory factory, Uri address)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            IMultiSubjectMessaging messaging = null;
            factory?.TryCreateMultiSubject(address, out messaging);
            if (messaging == null)
                throw new ArgumentOutOfRangeException(nameof(address), "Don't know how to create transport for " + address);
            return messaging;
        }

        /// <summary>Starts a worker Task which dispatches messages in a loop, checking for cancellation every <paramref name="pollInterval"/></summary>
        /// <param name="messaging"></param>
        /// <param name="cancel">Allows cancellation of the returned Task</param>
        /// <param name="pollInterval">Defaults to 100ms</param>
        public static Task Start(this IMultiSubjectMessaging messaging, CancellationToken cancel = default(CancellationToken), TimeSpan? pollInterval = null)
        {
            var poll = pollInterval.GetValueOrDefault(TimeSpan.FromMilliseconds(100));
            return Task.Factory.StartNew(() =>
            {
                while(!cancel.IsCancellationRequested)
                {
                    messaging.DispatchMessage(poll);
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
