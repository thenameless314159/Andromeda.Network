using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace Andromeda.Framing
{
    public interface IFrameDecoder<TMetadata> : IFrameDecoder where TMetadata : class, IFrameMetadata
    {
        new ValueTask<Frame<TMetadata>> ReadFrameAsync(CancellationToken token = default);
        new IAsyncEnumerable<Frame<TMetadata>> ReadFramesAsync();
    }
}
