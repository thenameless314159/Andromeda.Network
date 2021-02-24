using System.Buffers;

namespace Andromeda.Framing
{
    public abstract class MetadataDecoder<TMetadata> : IMetadataDecoder where TMetadata : class, IFrameMetadata
    {
        protected abstract bool TryParse(ref SequenceReader<byte> input, out TMetadata? metadata);

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
    }
}
