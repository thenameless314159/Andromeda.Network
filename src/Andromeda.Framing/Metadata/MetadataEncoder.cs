using System;

namespace Andromeda.Framing
{
    /// <summary>
    /// An implementable abstraction of <see cref="IMetadataEncoder"/> for a specific type of <see cref="IFrameMetadata"/>.
    /// </summary>
    public abstract class MetadataEncoder<TMetadata> : IMetadataEncoder where TMetadata : class, IFrameMetadata
    {
        /// <inheritdoc />
        public void Write(ref Span<byte> span, IFrameMetadata metadata) => Write(ref span, (TMetadata)metadata);

        /// <inheritdoc />
        public int GetLength(IFrameMetadata metadata) => GetLength((TMetadata)metadata);


        protected abstract void Write(ref Span<byte> span, TMetadata metadata);
        protected abstract int GetLength(TMetadata metadata);
    }
}
