using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace Andromeda.Framing
{
    public interface IFrameDecoder : IAsyncDisposable, IDisposable
    {
        ValueTask<Frame> ReadFrameAsync(CancellationToken token = default);
        IAsyncEnumerable<Frame> ReadFramesAsync();

        long FramesRead { get; }
    }
}
