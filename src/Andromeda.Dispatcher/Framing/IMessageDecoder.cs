using System.Threading;
using System.Threading.Tasks;

namespace Andromeda.Framing
{
    public interface IMessageDecoder
    {
        ValueTask<TMessage?> ReadAsync<TMessage>(CancellationToken token = default) where TMessage : new();
    }
}
