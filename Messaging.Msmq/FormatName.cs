using System;
using static System.StringComparison;

namespace Messaging.Msmq
{
    class FormatName
    {
        public string Scheme { get; private set; }
        public string Host { get; private set; }
        public string Path { get; private set; }
        public string Subqueue { get; private set; }

        public FormatName(string scheme, string host, string path)
        {
            Scheme = scheme;
            Host = host == "." ? Environment.MachineName : host;
            Path = path;
        }

        public static FormatName Parse(string text)
        {
            var addressScheme = ParseAddressSchema(ref text);
            if (string.IsNullOrEmpty(addressScheme))
                return null;
            if (string.Equals(addressScheme, "DIRECT", OrdinalIgnoreCase))
                return ParseDirect(text);
            if (string.Equals(addressScheme, "MULTICAST", OrdinalIgnoreCase))
                return new FormatName("MULTICAST", text, "");
            return null;
        }

        static string ParseAddressSchema(ref string text)
        {
            int equals = text.IndexOf('=');
            var addressScheme = text.Substring(0, equals);
            text = text.Substring(equals + 1);
            return addressScheme;
        }

        static FormatName ParseDirect(string text)
        {
            string scheme = ParseSchema(ref text);

            if (IsDirectOs(scheme) || IsDirectTcp(scheme))
                return ParseOsOrTcp(text, scheme);
            if (IsDirectHttp(scheme))
                return PasrseHttpOrHttps(text, scheme);
            return null;
        }

        static FormatName ParseOsOrTcp(string text, string scheme)
        {
            var backslash = text.IndexOf('\\');
            if (backslash < 0) return null;
            var host = text.Substring(0, backslash);
            var path = text.Substring(backslash);
            string subqueue = ParseSubqueue(ref path);
            return new FormatName(scheme, host, path) { Subqueue = subqueue };
        }

        static string ParseSubqueue(ref string path)
        {
            int semicolon = path.LastIndexOf(';');
            string subq = null;
            if (semicolon > 0)
            {
                subq = path.Substring(semicolon + 1);
                path = path.Substring(0, semicolon);
            }
            return subq;
        }

        static FormatName PasrseHttpOrHttps(string text, string scheme)
        {
            if (!text.StartsWith("//", Ordinal)) return null;
            text = text.Substring(2);
            var slash = text.IndexOf('/');
            if (slash < 0) return null;
            var host = text.Substring(0, slash);
            var path = text.Substring(slash);
            return new FormatName(scheme, host, path);
        }

        static string ParseSchema(ref string text)
        {
            int colon = text.IndexOf(':');
            string scheme = text.Substring(0, colon);
            text = text.Substring(colon + 1);
            return scheme;
        }

        static bool IsMulticast(string scheme) => string.Equals("MULTICAST", scheme, OrdinalIgnoreCase);

        static bool IsDirectTcp(string scheme) => string.Equals("TCP", scheme, OrdinalIgnoreCase);

        static bool IsDirectOs(string scheme) => string.Equals("OS", scheme, OrdinalIgnoreCase);

        static bool IsDirectHttp(string scheme) => string.Equals("HTTP", scheme, OrdinalIgnoreCase) || string.Equals("HTTPS", scheme, OrdinalIgnoreCase);

    }
}
