using Andromeda.Framing.Extensions.UnitTests.Models;

namespace Andromeda.Framing.Extensions.UnitTests.Infrastructure
{
    public class IdPrefixedMessageReader : MessageReader<IdPrefixedMetadata>
    {
        public IdPrefixedMessageReader() : base(SerializationProvider.Serializer)
        {
        }

        protected override bool AreMetadataValidFor<T>(T message, IdPrefixedMetadata metadata) => message switch {
            HelloMessage when metadata.MessageId == 1 => true,
            TestMessage when metadata.MessageId == 2 => true,
            EmptyMessage when metadata.MessageId == 3 && metadata.Length < 1 => true,
            _ => false
        };
    }
}
