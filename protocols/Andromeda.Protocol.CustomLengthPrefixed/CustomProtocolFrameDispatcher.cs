using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Andromeda.Dispatcher;
using Andromeda.Framing;
using Microsoft.Extensions.DependencyInjection;

namespace Andromeda.Protocol
{
    public class CustomProtocolFrameDispatcher : IFrameDispatcher
    {
        protected delegate Task<DispatchResult> Handler(Frame frame, SenderContext context);
        protected readonly IDictionary<int, Handler> _handlers = new Dictionary<int, Handler>();
        private readonly IServiceProvider _provider;
        private readonly IMessageReader _decoder;

        public CustomProtocolFrameDispatcher(IServiceProvider sp, IMessageReader decoder)
        {
            _decoder = decoder;
            _provider = sp;
        }

        public Task<DispatchResult> OnFrameReceivedAsync(in Frame frame, SenderContext context) {
            if (frame.Metadata is not ProtocolMessageMetadata metadata) throw new ArgumentException("Invalid frame metadata !", nameof(frame));
            return !_handlers.TryGetValue(metadata.MessageId, out var handler)
                ? Task.FromResult(DispatchResult.NotFound)
                : handler(frame, context);
        }

        public void Map<TMessage>(int withId) where TMessage : class, new()
        {
            _handlers[withId] = onFrame;
            async Task<DispatchResult> onFrame(Frame frame, SenderContext context)
            {
                var message = new TMessage();
                if (!_decoder.TryParse(in frame, message))
                    return DispatchResult.InvalidFramePayload;

                using var scope = _provider.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<IMessageHandler<TMessage>>();
                handler.RequestServices = scope.ServiceProvider;
                handler.Context = context;
                handler.Message = message;

                var token = context.Client.ConnectionClosed;
                // double pass the cancellation token in case enumerator cancellation attribute is not implemented on the class
                await foreach (var action in handler.OnMessageReceivedAsync().WithCancellation(token))
                    await action.ExecuteAsync(context).ConfigureAwait(false);

                return DispatchResult.Success;
            }
        }
    }
}
