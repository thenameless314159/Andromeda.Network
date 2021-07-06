using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;

namespace Andromeda.Dispatcher.Handlers.Actions
{
    // Note: this should be returned by the implemented IHandler
    public interface IHandlerAction
    {
        ValueTask ExecuteAsync(ConnectionContext context);
    }
}
