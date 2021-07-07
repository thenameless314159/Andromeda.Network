using System.Threading.Tasks;

namespace Andromeda.Dispatcher.Handlers.Actions
{
    public interface IHandlerAction
    {
        ValueTask ExecuteAsync(SenderContext context);
    }
}
