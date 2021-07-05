using System;
using System.Buffers;
using Andromeda.Framing;
using System.Buffers.Binary;
#nullable enable

namespace Protocols
{
    public class LengthPrefixedMetadataParser : MetadataParser<DefaultFrameMetadata>
    {
        protected override bool TryParse(ref SequenceReader<byte> input, out DefaultFrameMetadata? metadata)
        {
            if (input.TryReadBigEndian(out short length)) {
                metadata = new DefaultFrameMetadata(length);
                return true;
            }

            metadata = null;
            return false;
        }

        protected override void Write(ref Span<byte> span, DefaultFrameMetadata metadata) => BinaryPrimitives
            .WriteInt16BigEndian(span, (short)metadata.Length);

        protected override int GetLength(DefaultFrameMetadata metadata) => sizeof(short);
    }
}
