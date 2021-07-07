using System;
using System.Collections.Generic;
using Andromeda.Framing;
using System.Threading.Tasks;
using Andromeda.Dispatcher.Infrastructure;

namespace Andromeda.Dispatcher
{
    public class DefaultMessageDispatcher<TMeta> : IMessageDispatcher<TMeta>, IMessageDispatcherBuilder
        where TMeta : class, IMessageMetadata
    {
        protected delegate Task<DispatchResult> MessageHandler(in Frame<TMeta> frame, SenderContext context);
        protected readonly IDictionary<int, MessageHandler> _handlers = new Dictionary<int, MessageHandler>();

        public Task<DispatchResult> OnFrameReceivedAsync(in Frame frame, SenderContext context)
        {
            Frame<TMeta> typed;
            try { typed = frame.AsTyped<TMeta>(); }
            catch { return DispatchResultTasks.InvalidFrameMetadata; }

            return OnFrameReceivedAsync(in typed, context);
        }

        public Task<DispatchResult> OnFrameReceivedAsync(in Frame<TMeta> frame, SenderContext context)
        {
            return DispatchResultTasks.NotFound;
        }

        public void Map<TMessage>(int withMessageId) where TMessage : new()
        {
        }
    }
}
