using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Andromeda.Dispatcher;
using Andromeda.Dispatcher.Handlers;
using Andromeda.Dispatcher.Handlers.Actions;
using Andromeda.Framing;
using Andromeda.Serialization;
using Andromeda.Sizing;
using Applications;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using Protocols;

namespace Applications
{
    public class MessageDispatcherApplication : ConnectionHandler
    {
        public MessageDispatcherApplication(ISerializer serializer, ISizing sizing,
            IMessageDispatcher<IdPrefixedMetadata> dispatcher,
            ILogger<MessageDispatcherApplication> logger)
        {
            _logger = logger;
            _dispatcher = dispatcher;
            _parser = new IdPrefixedMetadataParser();
            _writer = new IdPrefixedMessageWriter(_parser, serializer, sizing);
            
        }

        private readonly HandshakeMessage _handshake = new() { SupportedOperators = new[] { '+', '-', '/', '*' } };
        private readonly IMessageDispatcher<IdPrefixedMetadata> _dispatcher;
        private readonly MetadataParser<IdPrefixedMetadata> _parser;
        private readonly IMessageWriter _writer;
        private readonly ILogger _logger;

        public override async Task OnConnectedAsync(ConnectionContext connection)
        {
            await using var encoder = connection.Transport.Output.AsFrameMessageEncoder(_parser, _writer);
            await using var decoder = connection.Transport.Input.AsFrameDecoder(_parser);

            try
            {
                await encoder.WriteAsync(_handshake);
                _logger.LogInformation("{Message} was successfully sent to connection with Id={ConnectionId} !", nameof(HandshakeMessage), connection.ConnectionId);

                var context = new DefaultSenderContext(connection, encoder);
                await foreach (var frame in decoder.ReadFramesAsync(connection.ConnectionClosed)) 
                {
                    _logger.LogDebug("Received a frame with {Metadata} from connection with Id={ConnectionId}", frame.Metadata, connection.ConnectionId);
                    var result = await _dispatcher.OnFrameReceivedAsync(in frame, context);

                    if (result == DispatchResult.Success)
                        _logger.LogDebug(
                            "Successfully handled received frame with {Metadata} from connection with Id={Id}!",
                            frame.Metadata, connection.ConnectionId);
                    else 
                        _logger.LogWarning(
                            "Couldn't handle received frame with {Metadata} from connection with Id={Id} ! Result : {result}",
                            frame.Metadata, connection.ConnectionId, result);
                }
            }
            catch (ObjectDisposedException) { /* connection closed, don't let this out */}
        }
    }

    public class ArithmeticOperationHandler : MessageHandler<ArithmeticOperation>
    {
        public ArithmeticOperationHandler(ILogger<ArithmeticOperationHandler> logger) => _logger = logger;
        private readonly ILogger _logger;

        public override async IAsyncEnumerable<IHandlerAction> OnMessageReceivedAsync()
        {
            _logger.LogInformation("Received an arithmetic operation to process from connection with Id={ConnectionId} : {Operation}",
                Context.Id, string.Join(' ', Message.Left, Message.Operator, Message.Right));

            await Task.Delay(100); // remove async warning
            var result = Message.Operator switch {
                '+' => Message.Left + Message.Right,
                '-' => Message.Left - Message.Right,
                '/' => Message.Left / Message.Right,
                '*' => Message.Left * Message.Right,

                _ => throw new InvalidOperationException(
                    $"Operator '{Message.Operator}' is invalid or not supported !")
            };

            yield return Send(new ArithmeticOperationResult {Result = result});
            _logger.LogInformation("Successfully processed arithmetic operation from connection with Id={ConnectionId}, result : {result}",
                Context.Id, result);
        }
    }
}
