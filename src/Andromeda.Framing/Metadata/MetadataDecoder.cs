using System.Buffers;

namespace Andromeda.Framing
{
    /// <summary>
    /// An implementable abstraction of <see cref="IMetadataDecoder"/> for a specific type of <see cref="IFrameMetadata"/>.
    /// </summary>
    /// <typeparam name="TMetadata">The specific <see cref="IFrameMetadata"/> of this <see cref="IMetadataDecoder"/>.</typeparam>
    public abstract class MetadataDecoder<TMetadata> : IMetadataDecoder where TMetadata : class, IFrameMetadata
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
        public int GetMetadataLength(IFrameMetadata metadata) => GetMetadataLength((TMetadata) metadata);
        
        protected abstract bool TryParse(ref SequenceReader<byte> input, out TMetadata? metadata);
        protected abstract int GetMetadataLength(TMetadata metadata);
    }
}
