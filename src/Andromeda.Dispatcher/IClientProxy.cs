using System.Threading.Tasks;

namespace Andromeda.Dispatcher
{
    public interface IClientProxy
    {
        ValueTask SendAsync<TMessage>(in TMessage message);
    }
}
