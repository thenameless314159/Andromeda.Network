using System;
using System.Text;
using Andromeda.Framing.Extensions.UnitTests.Models;

namespace Andromeda.Framing.Extensions.UnitTests.Infrastructure
{
    public class IdPrefixedMessageWriter : MessageWriter<IdPrefixedMetadata>
    {
        public IdPrefixedMessageWriter() : base(new IdPrefixedMetadataParser(), SerializationProvider.Serializer)
        {
        }

        // would be from a static generic store in real-life scenario
        protected override IdPrefixedMetadata GetMetadataOf<T>(in T message) => message switch {
            HelloMessage m => new IdPrefixedMetadata(1, sizeof(int) + Encoding.ASCII.GetByteCount(m.Message) + sizeof(int)),
            TestMessage => new IdPrefixedMetadata(2, sizeof(byte) + sizeof(long)),
            EmptyMessage => new IdPrefixedMetadata(3, 0),
            SmallerBytesWrittenMessage => new IdPrefixedMetadata(4, sizeof(int)),
            _ => throw new ArgumentException("Invalid message provided !", nameof(message))
        };

        protected override IdPrefixedMetadata CreateCopyOf(IdPrefixedMetadata metadata, int newLength) => 
            new(metadata.MessageId, newLength);
    }
}
