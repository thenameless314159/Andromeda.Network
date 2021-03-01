using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace Andromeda.Framing
{
    /// <summary>
    /// Represent a mechanism to read single <see cref="Frame"/> or consume them
    /// via an IAsyncEnumerable.
    /// </summary>
    public interface IFrameDecoder : IAsyncDisposable, IDisposable
    {
        /// <summary>
        /// Read a single <see cref="Frame"/> from the underlying reader.
        /// </summary>
        /// <param name="token">The cancellation token.</param>
        /// <returns>The read <see cref="Frame"/>.</returns>
        /// <exception cref="ObjectDisposedException">When the <see cref="IFrameDecoder"/> is disposed or the underlying reader is completed.</exception>
        ValueTask<Frame> ReadFrameAsync(CancellationToken token = default);

        /// <summary>
        /// Start the consumption of <see cref="Frame"/>.
        /// </summary>
        /// <param name="token">The cancellation token.</param>
        /// <returns>A consumable IAsyncEnumerable of <see cref="Frame"/>.</returns>
        IAsyncEnumerable<Frame> ReadFramesAsync(CancellationToken token = default);

        /// <summary>
        /// Get the number of frames read so far with this instance of <see cref="IFrameDecoder"/>.
        /// </summary>
        long FramesRead { get; }
    }
}
