using System.Collections.Generic;

namespace Messaging.Stomp
{
    class Frame
    {
        public string Command { get; }
        public Dictionary<string, object> Headers { get; }
        public object Body { get; }

        public Frame(string command, Dictionary<string, object> headers, object body)
        {
            if (headers == null)
                throw new System.ArgumentNullException(nameof(headers));
            if (command == null)
                throw new System.ArgumentNullException(nameof(command));
            this.Command = command;
            this.Headers = headers;
            this.Body = body;
        }
    }
}