using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace Andromeda.Framing
{
    /// <summary>
    /// Represent a mechanism to write single or multiple <see cref="Frame"/>.
    /// </summary>
    public interface IFrameEncoder : IAsyncDisposable, IDisposable
    {
        /// <summary>
        /// Write all the <see cref="Frame"/> in the underlying writer.
        /// </summary>
        /// <param name="frames">The multiple <see cref="Frame"/> to write.</param>
        /// <param name="token">The cancellation token.</param>
        ValueTask WriteAsync(IAsyncEnumerable<Frame> frames, CancellationToken token = default);

        /// <summary>
        /// Write all the <see cref="Frame"/> in the underlying writer.
        /// </summary>
        /// <param name="frames">The multiple <see cref="Frame"/> to write.</param>
        /// <param name="token">The cancellation token.</param>
        ValueTask WriteAsync(IEnumerable<Frame> frames, CancellationToken token = default);

        /// <summary>
        /// Write a single <see cref="Frame"/> in the underlying writer.
        /// </summary>
        /// <param name="frame">The single <see cref="Frame"/> to write.</param>
        /// <param name="token">The cancellation token.</param>
        ValueTask WriteAsync(in Frame frame, CancellationToken token = default);

        /// <summary>
        /// Get the number of frames written so far with this instance of <see cref="IFrameEncoder"/>.
        /// </summary>
        long FramesWritten { get; }
    }
}
