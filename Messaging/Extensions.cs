using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public static Transport New(this TransportFactory factory, string destination)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            var url = new Uri(destination);
            return factory.New(url);
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
