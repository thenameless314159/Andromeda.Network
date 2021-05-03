using System.Collections.Generic;
using Andromeda.Dispatcher.Handlers;
using Andromeda.Dispatcher.Handlers.Actions;

namespace Andromeda.Protocol
{
    public interface IMessageHandler<TMessage> : IHandler
    {
        TMessage Message { get; set; }

        IAsyncEnumerable<IHandlerAction> OnMessageReceivedAsync();
    }
}
