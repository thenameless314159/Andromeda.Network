using System.Collections.Generic;
using Andromeda.Dispatcher.Handlers.Actions;

namespace Andromeda.Dispatcher.Handlers
{
    public abstract class MessageHandler<TMessage> : FrameHandler where TMessage : new()
    {
        public abstract IAsyncEnumerable<IHandlerAction> OnMessageReceivedAsync();

        private TMessage? _message;
        public TMessage Message {
            get => _message ?? new TMessage();
            set => _message = value;
        }
    }
}
