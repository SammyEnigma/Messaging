using System;
using MSMQ = System.Messaging;
using System.Threading.Tasks;

namespace Messaging.Msmq
{
    static class MsmqExtensions
    {
        public static Task<MSMQ.Message> PeekAsync(this MSMQ.MessageQueue queue, TimeSpan timeout)
        {
            queue.MessageReadPropertyFilter = Filters.Peek; // just peek minimal details
            return Task.Factory.FromAsync(queue.BeginPeek(timeout, queue), EndPeek);
        }

        public static Task<MSMQ.Message> PeekAsync(this MSMQ.MessageQueue queue, TimeSpan timeout, MSMQ.Cursor cursor, MSMQ.PeekAction peekAction)
        {
            queue.MessageReadPropertyFilter = Filters.Peek; // just peek minimal details
            return Task.Factory.FromAsync(queue.BeginPeek(timeout, cursor, peekAction, queue, null), EndPeek);
        }

        static MSMQ.Message EndPeek(IAsyncResult res)
        {
            try
            {
                var queue = (MSMQ.MessageQueue)res.AsyncState;
                return queue.EndPeek(res);
            }
            catch (MSMQ.MessageQueueException ex) when (ex.MessageQueueErrorCode == MSMQ.MessageQueueErrorCode.IOTimeout)
            {
                return null;
            }
        }

        public static bool SubjectMatches(this MSMQ.Message msg, string subject)
        {
            if (string.IsNullOrEmpty(subject)) return true;
            return string.Equals(subject, msg.Label, StringComparison.Ordinal);
        }

    }
}