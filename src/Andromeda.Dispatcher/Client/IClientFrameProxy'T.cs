using System.Collections.Generic;
using System.Threading.Tasks;
using Andromeda.Framing;

namespace Andromeda.Network.Client
{
    public interface IClientFrameProxy<TMeta> : IClientFrameProxy where TMeta : class, IFrameMetadata
    {
        ValueTask SendAsync(in Frame<TMeta> frame);
        ValueTask SendAsync(IEnumerable<Frame<TMeta>> frames);
        ValueTask SendAsync(IAsyncEnumerable<Frame<TMeta>> frames);
    }
}
