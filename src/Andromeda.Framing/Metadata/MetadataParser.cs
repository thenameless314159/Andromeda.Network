using System;
using System.Buffers;

namespace Andromeda.Framing
{
    public abstract class MetadataParser<TMetadata> : IMetadataParser where TMetadata : class, IFrameMetadata
    {
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

        

        public void Write(ref Span<byte> span, IFrameMetadata metadata) => Write(ref span, (TMetadata)metadata);
        public int GetLength(IFrameMetadata metadata) => GetLength((TMetadata)metadata);
        public int GetMetadataLength(IFrameMetadata metadata) => GetLength(metadata);
        

        protected abstract bool TryParse(ref SequenceReader<byte> input, out TMetadata? metadata);
        protected abstract void Write(ref Span<byte> span, TMetadata metadata);
        protected abstract int GetLength(TMetadata metadata);
    }
}
