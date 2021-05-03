using System;
using System.Buffers;
using System.Buffers.Binary;
using Andromeda.Framing;

namespace Andromeda.Protocol
{
    public class CustomProtocolParser : MetadataParser<ProtocolMessageMetadata>
    {
        protected override bool TryParse(ref SequenceReader<byte> input, out ProtocolMessageMetadata? metadata)
        {
            if (input.TryReadLittleEndian(out short messageId) && input.TryReadLittleEndian(out int length)) {
                metadata = new ProtocolMessageMetadata((ushort)messageId, length); 
                return true;
            }

            metadata = default;
            return false;
        }

        protected override void Write(ref Span<byte> span, ProtocolMessageMetadata metadata) {
            BinaryPrimitives.WriteUInt16LittleEndian(span, metadata.MessageId);
            BinaryPrimitives.WriteInt32LittleEndian(span, metadata.Length);
        }

        private const int _headerSize = sizeof(ushort) + sizeof(int);
        protected override int GetLength(ProtocolMessageMetadata metadata) => _headerSize;
    }
}
