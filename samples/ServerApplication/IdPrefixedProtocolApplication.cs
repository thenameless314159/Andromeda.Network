using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using Andromeda.Serialization;
using System.Threading.Tasks;
using Andromeda.Framing;
using Andromeda.Sizing;
using Protocols;
using System;

namespace Applications
{
    public class IdPrefixedProtocolApplication : ConnectionHandler
    {
        public IdPrefixedProtocolApplication(ISerDes serializer, ISizing sizing, ILogger<IdPrefixedProtocolApplication> logger)
        {
            _logger = logger;
            _parser = new IdPrefixedMetadataParser();
            _reader = new IdPrefixedMessageReader(serializer);
            _writer = new IdPrefixedMessageWriter(_parser, serializer, sizing);
        }

        private readonly HandshakeMessage _handshake = new() {SupportedOperators = new [] {'+', '-', '/', '*'}};
        private readonly IMessageReader<IdPrefixedMetadata> _reader;
        private readonly IMetadataParser _parser;
        private readonly IMessageWriter _writer;
        private readonly ILogger _logger;

        public override async Task OnConnectedAsync(ConnectionContext connection)
        {
            await using var encoder = connection.Transport.Output.AsFrameMessageEncoder(_parser, _writer);
            await using var decoder = connection.Transport.Input.AsFrameMessageDecoder(_parser, _reader);

            try {
                await encoder.WriteAsync(_handshake);
                _logger.LogInformation("{Message} was successfully sent to connection with Id={ConnectionId} !", nameof(HandshakeMessage), connection.ConnectionId);

                while (true)
                {
                    var message = await decoder.ReadAsync<ArithmeticOperation>();
                    if (message is null) break; // we only process arithmetic operation here

                    _logger.LogInformation("Received an arithmetic operation to process from connection with Id={ConnectionId} : {Operation}",
                        connection.ConnectionId, string.Join(' ', message.Left, message.Operator, message.Right));

                    var result = message.Operator switch {
                        '+' => message.Left + message.Right,
                        '-' => message.Left - message.Right,
                        '/' => message.Left / message.Right,
                        '*' => message.Left * message.Right,

                        _ => throw new InvalidOperationException(
                            $"Operator '{message.Operator}' is invalid or not supported !")
                    };

                    await encoder.WriteAsync(new ArithmeticOperationResult {Result = result});
                    _logger.LogInformation("Successfully processed arithmetic operation from connection with Id={ConnectionId}, result : {result}",
                        connection.ConnectionId, result);
                }
            }
            catch (ObjectDisposedException) { /* connection closed, don't let this out */}
        }
    }
}
