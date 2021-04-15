using System.Threading;
using System.Threading.Tasks;

namespace Andromeda.Framing
{
    public interface IMessageEncoder
    {
        ValueTask WriteAsync<TMessage>(in TMessage message, CancellationToken token = default);
    }
}
