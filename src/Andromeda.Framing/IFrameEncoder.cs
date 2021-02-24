using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace Andromeda.Framing
{
    public interface IFrameEncoder : IAsyncDisposable, IDisposable
    {
        ValueTask WriteAsync(IAsyncEnumerable<Frame> frames, CancellationToken token = default);
        ValueTask WriteAsync(IEnumerable<Frame> frames, CancellationToken token = default);
        ValueTask WriteAsync(in Frame frame, CancellationToken token = default);
        long FramesWritten { get; }
    }
}
