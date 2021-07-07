using System;
using Andromeda.Framing;
using System.Threading.Tasks;
using System.Collections.Generic;
using Andromeda.Dispatcher.Handlers;
using Andromeda.Dispatcher.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Andromeda.Dispatcher
{
    public class DefaultMessageDispatcher<TMeta> : IMessageDispatcher<TMeta>, IMessageDispatcherBuilder
        where TMeta : class, IMessageMetadata
    {
        private delegate Task<DispatchResult> MessageHandler(in Frame<TMeta> frame, SenderContext context);
        private readonly IDictionary<int, MessageHandler> _handlers = new Dictionary<int, MessageHandler>();
        private readonly IMessageReader<TMeta> _reader;
        private readonly IServiceProvider _services;

        public DefaultMessageDispatcher(IServiceProvider services, IMessageReader<TMeta> reader) =>
            (_services, _reader) = (services, reader);

        public Task<DispatchResult> OnFrameReceivedAsync(in Frame frame, SenderContext context)
        {
            Frame<TMeta> typed;
            try { typed = frame.AsTyped<TMeta>(); }
            catch { return DispatchResultTasks.InvalidFrameMetadata; }

            return OnFrameReceivedAsync(in typed, context);
        }

        public Task<DispatchResult> OnFrameReceivedAsync(in Frame<TMeta> frame, SenderContext context) =>
            !_handlers.TryGetValue(frame.Metadata.MessageId, out var handler)
                ? DispatchResultTasks.NotFound
                : handler(in frame, context);

        public void Map<TMessage>(int withMessageId) where TMessage : new()
        {
            _handlers[withMessageId] = onFrameAsync;
            Task<DispatchResult> onFrameAsync(in Frame<TMeta> frame, SenderContext context)
            {
                using var scope = _services.CreateScope(); var message = new TMessage();
                if (!_reader.TryDecode(in frame, message)) return DispatchResultTasks.InvalidFramePayload;

                var handler = scope.ServiceProvider.GetRequiredService<MessageHandler<TMessage>>();
                handler.RequestServices = scope.ServiceProvider;
                handler.Request = frame.AsUntyped();
                handler.Message = message;
                handler.Context = context;

                return HandleMessageAsync(handler, context);
            }
        }

        private static async Task<DispatchResult> HandleMessageAsync<T>(MessageHandler<T> handler, SenderContext sender)
            where T : new()
        {
            await foreach (var action in handler.OnMessageReceivedAsync().WithCancellation(sender.ConnectionClosed))
            {
                var executeAsync = action.ExecuteAsync(sender);
                if (executeAsync.IsCompletedSuccessfully) continue;
                await executeAsync.ConfigureAwait(false);
            }

            return DispatchResult.Success;
        }
    }
}
