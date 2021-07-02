using System;
using System.Buffers;
using System.Buffers.Binary;

namespace Andromeda.Framing.Extensions.UnitTests.Infrastructure
{
    public class IdPrefixedMetadataParser : MetadataParser<IdPrefixedMetadata>
    {
        protected override bool TryParse(ref SequenceReader<byte> input, out IdPrefixedMetadata metadata)
        {
            metadata = default;
            if (!input.TryReadBigEndian(out short messageId)) return false;
            if (!input.TryReadBigEndian(out int length)) return false;
            metadata = new IdPrefixedMetadata(messageId, length);
            return true;
        }

        protected override void Write(ref Span<byte> span, IdPrefixedMetadata metadata)
        {
            BinaryPrimitives.WriteInt16BigEndian(span, (short)metadata.MessageId);
            BinaryPrimitives.WriteInt32BigEndian(span[2..], metadata.Length);
        }

        protected override int GetLength(IdPrefixedMetadata metadata) => 
            /*messageId:*/sizeof(short) + /*length:*/ sizeof(int);
    }

}
