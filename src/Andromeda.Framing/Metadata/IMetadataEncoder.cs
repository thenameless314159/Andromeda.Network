using System;

namespace Andromeda.Framing
{
    public interface IMetadataEncoder
    {
        int GetLength(IFrameMetadata metadata);
        void Write(ref Span<byte> span, IFrameMetadata metadata);
    }
}
