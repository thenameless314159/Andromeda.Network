using System.Collections.Generic;

using System.Threading.Tasks;
using System.Threading;

namespace Andromeda.Framing
{
    public interface IFrameEncoder<TMetadata> : IFrameEncoder where TMetadata : class, IFrameMetadata
    {
        ValueTask WriteAsync(IAsyncEnumerable<Frame<TMetadata>> frames, CancellationToken token = default);
        ValueTask WriteAsync(IEnumerable<Frame<TMetadata>> frames, CancellationToken token = default);
        ValueTask WriteAsync(in Frame<TMetadata> frame, CancellationToken token = default);
    }
}
