using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;

namespace Andromeda.Dispatcher.Handlers.Actions
{
    public interface IHandlerAction
    {
        ValueTask ExecuteAsync(SenderContext context);
    }
}
