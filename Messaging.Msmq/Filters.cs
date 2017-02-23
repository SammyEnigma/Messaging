using MSMQ = System.Messaging;

namespace Messaging.Msmq
{
    static class Filters
    {
        public static readonly MSMQ.MessagePropertyFilter Peek;
        public static readonly MSMQ.MessagePropertyFilter Read;

        static Filters()
        {
            // setup the filter used for peeking
            Peek = new MSMQ.MessagePropertyFilter();
            Peek.ClearAll();
            Peek.Label = true;

            // we read the body too
            Read = new MSMQ.MessagePropertyFilter { Body = true };
        }
    }
}