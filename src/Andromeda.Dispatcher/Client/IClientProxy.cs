using System.Threading.Tasks;

namespace Andromeda.Dispatcher.Client
{
    public interface IClientProxy
    {
        ValueTask SendAsync<TMessage>(in TMessage message);
    }
}
