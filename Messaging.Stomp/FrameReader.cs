using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Messaging.Stomp
{
    class FrameReader
    {
        readonly TextReader input;
        StringBuilder buf;

        public FrameReader(TextReader input)
        {
            this.input = input;
            buf = new StringBuilder(128);
        }

        public async Task<Frame> Read()
        {
            var command = await ReadNoneBlankLine();
            var headers = await ReadHeaders();
            var body = ReadBody();
            CheckBody(command, body);
            return new Frame(command, headers, body);
        }

        static void CheckBody(string command, string body)
        {
            switch (command)
            {
                case "SEND":
                case "MESSAGE":
                case "ERROR":
                    if (body.Length == 0)
                        throw new IOException($"Got a {command} with a body, which is not allowed");
                    break;
                default:
                    if (body.Length > 0)
                        throw new IOException($"Got a body for {command}, which is not allowed");
                    break;
            }
        }

        async Task<string> ReadNoneBlankLine()
        {
            string line;
            for (;;)
            {
                line = await input.ReadLineAsync();
                if (!string.IsNullOrWhiteSpace(line)) break;
            }
            return line;
        }

        async Task<Dictionary<string, object>> ReadHeaders()
        {
            var headers = new Dictionary<string, object>();
            for (;;)
            {
                var line = await input.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) break;
                int colon = line.IndexOf(':');
                if (colon < 0)
                    throw new IOException($"Got a header without a colon seperator");
                string key = line.Substring(0, colon).ReplaceHeaderStringLiterals();
                string value = line.Substring(colon + 1).ReplaceHeaderStringLiterals();
                headers.Add(key, value);
            }
            return headers;
        }

        string ReadBody()
        {
            buf.Clear();
            for (;;)
            {
                int ch = input.Read();
                if (ch == 0) break;
                buf.Append((char)ch);
            }
            return buf.ToString();
        }
    }

    static class Extensions
    {
        public static string ReplaceHeaderStringLiterals(this string line)
        {
            line = line.Replace(@"\r", "\r");
            line = line.Replace(@"\n", "\n");
            line = line.Replace(@"\c", ":");
            line = line.Replace(@"\\", @"\");
            return line;
        }
    }
}