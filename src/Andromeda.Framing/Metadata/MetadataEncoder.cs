using System;

namespace Andromeda.Framing
{
    public abstract class MetadataEncoder<TMetadata> : IMetadataEncoder where TMetadata : class, IFrameMetadata
    {
        public void Write(ref Span<byte> span, IFrameMetadata metadata) => Write(ref span, (TMetadata)metadata);
        public int GetLength(IFrameMetadata metadata) => GetLength((TMetadata)metadata);

        protected abstract void Write(ref Span<byte> span, TMetadata metadata);
        protected abstract int GetLength(TMetadata metadata);
    }
}
