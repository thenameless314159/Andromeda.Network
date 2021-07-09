using System.Threading.Tasks;

namespace Andromeda.Dispatcher.Client
{
    public interface IClientMessageProxy
    {
        ValueTask SendAsync<TMessage>(in TMessage message);
    }
}
