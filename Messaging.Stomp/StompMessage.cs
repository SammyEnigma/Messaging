using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messaging.Stomp
{
    class StompMessage : IReadOnlyMessage
    {
        readonly Frame frame;
        readonly StompMessageHeaders headers;

        public StompMessage(Frame frame)
        {
            if (frame == null)
                throw new ArgumentNullException(nameof(frame));
            this.frame = frame;
            headers = new StompMessageHeaders(frame.Headers);
        }

        public object Body => frame.Body;

        public IReadOnlyMessageHeaders Headers => headers;

        public string Subject => headers["subject"]?.ToString();

        public void Acknowledge()
        {
            throw new NotImplementedException();
        }
    }
}
