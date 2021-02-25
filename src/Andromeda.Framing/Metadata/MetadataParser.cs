using System;
using System.Buffers;

namespace Andromeda.Framing
{
    /// <summary>
    /// An implementable abstraction of <see cref="IMetadataParser"/> for a specific type of <see cref="IFrameMetadata"/>.
    /// </summary>
    public abstract class MetadataParser<TMetadata> : IMetadataParser where TMetadata : class, IFrameMetadata
    {
        /// <inheritdoc />
        public bool TryParse(ref SequenceReader<byte> input, out IFrameMetadata? metadata)
        {
            if (!TryParse(ref input, out TMetadata? meta))
            {
                metadata = default;
                return false;
            }

            metadata = meta;
            return true;
        }


        /// <inheritdoc />
        public void Write(ref Span<byte> span, IFrameMetadata metadata) => Write(ref span, (TMetadata)metadata);

        /// <inheritdoc />
        public int GetLength(IFrameMetadata metadata) => GetLength((TMetadata)metadata);

        /// <inheritdoc />
        public int GetMetadataLength(IFrameMetadata metadata) => GetLength(metadata);
        

        protected abstract bool TryParse(ref SequenceReader<byte> input, out TMetadata? metadata);
        protected abstract void Write(ref Span<byte> span, TMetadata metadata);
        protected abstract int GetLength(TMetadata metadata);
    }
}
