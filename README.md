# Messaging

HTTP-like abstraction over messaging libraries, so your code doesn't depend on a concrete library.

# Usage

You create a `Transport` for a `URI` using the `TransportFactory`.
With a `Transport` you can `Send()` a message or `CreateReceiver()` with an optional filter by message subject.
A `IReceiver` can `ReceiveAsync()` which returns a task that completes when the message is received.

## TIBCO RV

Transport maps naturally to a RV Transport which support sending.

RV separates receiving into:
* a Listener, which subscribes to a subject and add messages to a RV Queue
* a Dispatcher thread which reads from the queue (or group of queues).  You can write your own Dispatcher by calling `Poll` or `TimedDispatch` on the Queue (or group of queues).

The separation works well for RV as your tend to have many subject subscriptions being read by one (or a small number) of Dispatcher threads.

## MSMQ

Transport maps to MSMQ Queue.

In MSMQ you cannot async receive a message from a queue, but you can `BeginPeek()` which asynchronously notifies you when a message is ready to receive.



