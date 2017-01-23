using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Messaging.Stomp
{
    class StompServerTransport 
    {
        readonly Stream input;
        readonly Stream output;
        readonly FrameReader inputReader;

        public StompServerTransport(Stream input, Stream output)
        {
            if (output == null)
                throw new ArgumentNullException(nameof(output));
            if (input == null)
                throw new ArgumentNullException(nameof(input));
            this.output = output;
            this.input = input;
            inputReader = new FrameReader(new StreamReader(input, new UTF8Encoding(false)));
            Serve().Ignore();
        }

        public async Task Serve()
        {
            await WaitForConnect();
            for(;;)
            {
                var frame = await inputReader.Read();
                if (frame == null)
                    return;
                switch (frame.Command)
                {
                    case "DISCONNECT":
                        await Shutdown(frame);
                        return;
                    case "SEND":
                        await Dispatch(frame);
                        break;
                    case "SUBSCRIBE":
                        await AddSubscription(frame);
                        break;
                    case "UNSUBSCRIBE":
                        await RemoveSubscription(frame);
                        break;
                    case "ACK":
                        await AcknowledgeMessage(frame);
                        break;
                    case "NACK":
                        await RejectMessage(frame);
                        break;
                    case "BEGIN":
                        await BeginTransaction(frame);
                        break;
                    case "COMMIT":
                        await CommitTransaction(frame);
                        break;
                    case "ABORT":
                        await AbortTransaction(frame);
                        break;
                    default:
                        await UnknownCommand(frame);
                        break;
                }
            }
        }

        private async Task WaitForConnect()
        {
            for (;;)
            {
                var frame = await inputReader.Read();
                if ("CONNECT".Equals(frame.Command, StringComparison.Ordinal))
                {
                    await SendConnected();
                    break;
                }
                await SendConnectError();
            }
        }

        private Task SendConnected()
        {
            return Send(new Frame("CONENCTED", new Dictionary<string, object> { { "version", "1.2" } }));
        }

        private Task SendConnectError()
        {
            return Send(new Frame("ERROR", new Dictionary<string, object> { { "message", "not connected" } }));
        }

        private Task Shutdown(Frame frame)
        {
            throw new NotImplementedException();
        }

        private Task Dispatch(Frame frame)
        {
            throw new NotImplementedException();
        }

        private Task AddSubscription(Frame frame)
        {
            throw new NotImplementedException();
        }

        private Task RemoveSubscription(Frame frame)
        {
            throw new NotImplementedException();
        }

        private Task AcknowledgeMessage(Frame frame)
        {
            throw new NotImplementedException();
        }

        private Task RejectMessage(Frame frame)
        {
            throw new NotImplementedException();
        }

        private Task BeginTransaction(Frame frame)
        {
            throw new NotImplementedException();
        }

        private Task CommitTransaction(Frame frame)
        {
            throw new NotImplementedException();
        }

        private Task AbortTransaction(Frame frame)
        {
            throw new NotImplementedException();
        }

        private Task UnknownCommand(Frame frame)
        {
            throw new NotImplementedException();
        }


        public Task Send(Frame frame)
        {
            lock(output)
            {
                throw new NotImplementedException();
            }
        }

        public IDisposable StartWorker(string name = null)
        {
            throw new NotImplementedException();
        }
    }
}
