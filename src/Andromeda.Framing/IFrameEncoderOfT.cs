using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace Andromeda.Framing
{
    /// <summary>
    /// Represent a mechanism to write single or multiple <see cref="Frame{TMetadata}"/>.
    /// </summary>
    public interface IFrameEncoder<TMetadata> : IFrameEncoder where TMetadata : class, IFrameMetadata
    {
        /// <summary>
        /// Write all the <see cref="Frame{TMetadata}"/> in the underlying writer.
        /// </summary>
        /// <param name="frames">The multiple <see cref="Frame{TMetadata}"/> to write.</param>
        /// <param name="token">The cancellation token.</param>
        ValueTask WriteAsync(IAsyncEnumerable<Frame<TMetadata>> frames, CancellationToken token = default);

        /// <summary>
        /// Write all the <see cref="Frame{TMetadata}"/> in the underlying writer.
        /// </summary>
        /// <param name="frames">The multiple <see cref="Frame{TMetadata}"/> to write.</param>
        /// <param name="token">The cancellation token.</param>
        ValueTask WriteAsync(IEnumerable<Frame<TMetadata>> frames, CancellationToken token = default);

        /// <summary>
        /// Write a single <see cref="Frame{TMetadata}"/> in the underlying writer.
        /// </summary>
        /// <param name="frame">The single <see cref="Frame{TMetadata}"/> to write.</param>
        /// <param name="token">The cancellation token.</param>
        ValueTask WriteAsync(in Frame<TMetadata> frame, CancellationToken token = default);
    }
}
