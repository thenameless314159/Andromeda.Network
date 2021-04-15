using System.Threading.Tasks;

namespace Andromeda.Dispatcher.Handlers.Actions
{
    // Note: this should be returned by the implemented IHandler
    public interface IHandlerAction
    {
        ValueTask ExecuteAsync(SenderContext context);
    }
}
