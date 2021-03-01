using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace Andromeda.Framing
{
    /// <summary>
    /// Represent a mechanism to read single <see cref="Frame{TMetadata}"/> or consume them
    /// via an IAsyncEnumerable.
    /// </summary>
    public interface IFrameDecoder<TMetadata> : IFrameDecoder where TMetadata : class, IFrameMetadata
    {
        /// <summary>
        /// Read a single <see cref="Frame{TMetadata}"/> from the underlying reader.
        /// </summary>
        /// <param name="token">The cancellation token.</param>
        /// <returns>The read <see cref="Frame{TMetadata}"/>.</returns>
        /// <exception cref="ObjectDisposedException">When the <see cref="IFrameDecoder{TMetadata}"/> is disposed or the underlying reader is completed.</exception>
        new ValueTask<Frame<TMetadata>> ReadFrameAsync(CancellationToken token = default);

        /// <summary>
        /// Start the consumption of <see cref="Frame{TMetadata}"/>.
        /// </summary>
        /// <param name="token">The cancellation token.</param>
        /// <returns>A consumable IAsyncEnumerable of <see cref="Frame{TMetadata}"/>.</returns>
        new IAsyncEnumerable<Frame<TMetadata>> ReadFramesAsync(CancellationToken token = default);
    }
}
