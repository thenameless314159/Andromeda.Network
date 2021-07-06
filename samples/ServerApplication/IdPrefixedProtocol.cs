using System;
using System.Buffers;
using Andromeda.Sizing;
using Andromeda.Framing;
using System.Buffers.Binary;
using Andromeda.Serialization;

namespace Protocols
{
    public record IdPrefixedMetadata(short MessageId, int Length) : IFrameMetadata;
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
            BinaryPrimitives.WriteInt16BigEndian(span, metadata.MessageId);
            BinaryPrimitives.WriteInt32BigEndian(span[2..], metadata.Length);
        }

        protected override int GetLength(IdPrefixedMetadata metadata) =>
            /*messageId:*/sizeof(short) + /*length:*/ sizeof(int);
    }

    public class IdPrefixedMessageReader : MessageReader<IdPrefixedMetadata>
    {
        public IdPrefixedMessageReader(IDeserializer deserializer) : base(deserializer)
        {
        }

        protected override bool AreMetadataValidFor<T>(T message, IdPrefixedMetadata metadata)
        {
            var messageId = MetadataProvider.GetIdOf<T>();
            if (!messageId.HasValue) return false;

            return metadata.MessageId == messageId.Value;
        }
    }

    public class IdPrefixedMessageWriter : MessageWriter<IdPrefixedMetadata>
    {
        public IdPrefixedMessageWriter(IMetadataEncoder encoder, ISerializer serializer, ISizing sizing) 
            : base(encoder, serializer) => _sizing = sizing;

        private readonly ISizing _sizing;

        protected override IdPrefixedMetadata GetMetadataOf<T>(in T message)
        {
            var messageId = MetadataProvider.GetIdOf<T>() ??
                            throw new ArgumentException("Provided type is not a valid message !");

            var sizeOf = _sizing.SizeOf(in message);

            return new IdPrefixedMetadata((short) messageId, sizeOf);
        }

        protected override IdPrefixedMetadata CreateCopyOf(IdPrefixedMetadata metadata, 
            int newLength) => new(metadata.MessageId, newLength);
    }
}
