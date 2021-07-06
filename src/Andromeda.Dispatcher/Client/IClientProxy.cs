using System.Threading.Tasks;

namespace Andromeda.Network.Client
{
    public interface IClientProxy
    {
        ValueTask SendAsync<TMessage>(in TMessage message);
    }
}
