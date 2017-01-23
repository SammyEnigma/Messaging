using System;
using System.Linq;
using MSMQ = System.Messaging;

namespace Messaging.Msmq
{

    class MsmqWorker : IDisposable
    {
        static readonly TimeSpan _peekTimeout = TimeSpan.FromSeconds(1);
        static readonly MSMQ.MessagePropertyFilter peekFilter;
        static readonly MSMQ.MessagePropertyFilter readFilter;
        //readonly object gate;

        static MsmqWorker()
        {
            peekFilter = new MSMQ.MessagePropertyFilter();
            peekFilter.ClearAll();
            peekFilter.Label = true;
            peekFilter.LookupId = true; //TODO: by  ID or LookupId, which is faster?

            readFilter = new MSMQ.MessagePropertyFilter { Body = true };
        }

        readonly object _gate;
        readonly MsmqTransport _transport;
        readonly MSMQ.MessageQueue _queue;
        MSMQ.Cursor _cursor;
        volatile bool stop;

        public MsmqWorker(object gate, MsmqTransport transport)
        {
            if (gate == null) throw new ArgumentNullException(nameof(gate));
            if (transport == null) throw new ArgumentNullException(nameof(transport));
            _gate = gate;
            _transport = transport;
            _queue = transport._queue;
        }

        public void Pause()
        {
            stop = true;
        }

        public void Resume()
        {
            stop = false;
            _cursor = _queue.CreateCursor();
            BeginPeek(MSMQ.PeekAction.Current);
        }

        void BeginPeek(MSMQ.PeekAction peekAction)
        {
            if (stop)
            {
                _cursor.Dispose();
                return;
            }
            _queue.MessageReadPropertyFilter = peekFilter;
            
            var result = _queue.BeginPeek(_peekTimeout, _cursor, peekAction, null, PeekComplete);
        }

        void PeekComplete(IAsyncResult result)
        {
            MSMQ.Message msmq = TryEndPeek(result);

            MSMQ.PeekAction nextOrCurrent = MSMQ.PeekAction.Next;

            var subs = _transport.subjects; // local copy in case it changes while this method is running

            if (msmq != null && subs.Values.Any(ms => ms.IsInteresting(msmq.Label))) // did not timeout and subject is interesting
            {
                _queue.MessageReadPropertyFilter = readFilter;
                msmq = Receive(msmq);
                if (msmq != null) // managed to read the message
                {
                    var msg = Converter.FromMsmqMessage(msmq);
                    OnNext(msg);
                }

                nextOrCurrent = MSMQ.PeekAction.Current; // an item was removed an item from the queue
            }

            BeginPeek(nextOrCurrent);
        }

        MSMQ.Message TryEndPeek(IAsyncResult result)
        {
            try
            {
                return _queue.EndPeek(result);
            }
            catch (MSMQ.MessageQueueException ex) when (ex.MessageQueueErrorCode == MSMQ.MessageQueueErrorCode.IOTimeout)
            {
                return null; // timed-out when peeking
            }
        }

        MSMQ.Message Receive(MSMQ.Message peeked)
        {
            throw new NotImplementedException();

        }

        private void OnNext(IReadOnlyMessage msg)
        {
            foreach (var subject in _transport.subjects.Values)
            {
                subject.OnNext(msg); // pass it to all subjects, they will ignore the message if it is not interesting
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}