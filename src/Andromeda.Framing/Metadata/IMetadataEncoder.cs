using System;

namespace Andromeda.Framing
{
    /// <summary>
    /// Contains logic to write <see cref="IFrameMetadata"/> of a specific type of <see cref="Frame"/> or <see cref="Frame{TMetadata}"/>.
    /// </summary>
    public interface IMetadataEncoder
    {
        /// <summary>
        /// Write the <see cref="IFrameMetadata"/> in the specified span.
        /// </summary>
        /// <param name="span">The span.</param>
        /// <param name="metadata">The metadata.</param>
        void Write(ref Span<byte> span, IFrameMetadata metadata);

        /// <summary>
        /// Get the length of the specified <see cref="IFrameMetadata"/>.
        /// </summary>
        /// <param name="metadata">The metadata.</param>
        /// <returns>The length of the encoded metadata.</returns>
        int GetLength(IFrameMetadata metadata);

        
    }
}
