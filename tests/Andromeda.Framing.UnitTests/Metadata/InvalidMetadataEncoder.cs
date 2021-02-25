using System;

namespace Andromeda.Framing.UnitTests.Metadata
{
    public class InvalidMetadataEncoder : IMetadataEncoder
    {
        public int GetLength(IFrameMetadata metadata) => -1;
        public void Write(ref Span<byte> span, IFrameMetadata metadata)
        {
        }
    }
}
