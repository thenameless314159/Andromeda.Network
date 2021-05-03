using Andromeda.Protocol.CustomLengthPrefixed;
using Microsoft.AspNetCore.Connections;
using System.Security.Claims;
using System.Threading.Tasks;
using Andromeda.Dispatcher;
using Andromeda.Protocol;
using System;
using System.Collections.Generic;
using System.Reflection;
using Andromeda.Dispatcher.Handlers;
using Andromeda.Dispatcher.Handlers.Actions;
using Andromeda.Protocol.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ServerApplication
{
    public class CustomProtocolApplication : ConnectionHandler
    {
        private readonly CustomProtocolMessageParser _messageParser = new();
        private readonly CustomProtocolParser _metadataParser = new();
        private readonly IFrameDispatcher _dispatcher;
        private readonly ILogger _logger;

        public CustomProtocolApplication(IServiceProvider sp)
        {
            var dispatcher = new CustomProtocolFrameDispatcher(sp, _messageParser);
            dispatcher.Map<CommandMessage>(typeof(CommandMessage)
                .GetCustomAttribute<NetworkMessageAttribute>()!.Id);

            _logger = sp.GetRequiredService<ILogger<CustomProtocolApplication>>();
            _dispatcher = dispatcher;
        }

        public override async Task OnConnectedAsync(ConnectionContext connection)
        {
            await using var client = new DefaultClient(connection, _metadataParser, _messageParser, _messageParser);
            try {
                await client.SendAsync(new HandshakeMessage("Hello there !"));
                var loginMessage = await client.ReceiveAsync<LoginMessage>();

                // an unexpected frame was received
                if (loginMessage is null) return;

                if (loginMessage.Username != "Nameless" && loginMessage.Password != "password") {
                    await client.SendAsync(new LoginFailedMessage("Invalid credentials !"));
                    return;
                }

                await client.SendAsync(new LoginSuccessMessage());

                // initialize the context once logged in
                var context = new SenderContext(client, new ClaimsPrincipal(new ClaimsIdentity(
                    new []{ new Claim("username", "Nameless")})));

                // start the receive loop once the client has successfully been logged in
                await foreach (var frame in client.ReceiveFramesAsync())
                {
                    _logger.LogDebug("Received a frame with {metadata} from client with Id={Id}", frame.Metadata, client.Id);
                    var result = await _dispatcher.OnFrameReceivedAsync(in frame, context).ConfigureAwait(false);

                    _logger.LogInformation("Frame of client with Id={Id} and {metadata} has successfully been dispatched with result : {result}", 
                        client.Id, frame.Metadata, result);
                }
            }
            catch (OperationCanceledException) { /* client disconnected, don't let this out */ }
            catch (ObjectDisposedException) { /* client disconnected, don't let this out */ }
            catch (Exception e) { _logger.LogError(e, "unexepected error occurred"); }
        }
    }

    public class CommandMessageHandler : FrameHandler, IMessageHandler<CommandMessage>
    {
        public CommandMessage Message { get; set; }

        public async IAsyncEnumerable<IHandlerAction> OnMessageReceivedAsync()
        {
            const string abortCommandReason = "Aborted via CommandMessageHandler 'abort' command.";
            const string invalidCommand = "Invalid command provided : ";
            
            // Simulate async some work
            await Task.Delay(10).ConfigureAwait(false);
            yield return Message.Command switch {
                "handshakeAndAbort" => SendAndAbort(new HandshakeMessage("Hello there !")),
                "handshake" => Send(new HandshakeMessage("Hello there !")),
                "abort" => Abort(abortCommandReason),

                _ => Abort("Invalid command provided !", new ArgumentException(invalidCommand + Message.Command, nameof(Message)))
            };
        }
    }
}
