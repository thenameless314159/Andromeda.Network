using System.Collections.Generic;
using System.Threading.Tasks;
using Andromeda.Framing;

namespace Andromeda.Dispatcher.Client
{
    public interface IClientFrameProxy
    {
        ValueTask SendAsync(in Frame frame);
        ValueTask SendAsync(IEnumerable<Frame> frames);
        ValueTask SendAsync(IAsyncEnumerable<Frame> frames);
    }
}
