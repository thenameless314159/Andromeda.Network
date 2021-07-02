using Andromeda.Serialization;

namespace Andromeda.Framing
{
    public abstract class MessageReader<TMeta> : MessageReader, IMessageReader<TMeta> where TMeta : class, IFrameMetadata
    {
        protected MessageReader(IDeserializer deserializer) : base(deserializer)
        {
        }

        protected abstract bool AreMetadataValidFor<T>(T message, TMeta metadata);

        protected override bool AreMetadataValidFor<T>(T message, IFrameMetadata metadata) =>
            AreMetadataValidFor(message, (TMeta) metadata);

        public bool TryDecode<T>(in Frame<TMeta> frame, T message) 
        {
            if (!AreMetadataValidFor(message, frame.Metadata)) return false;
            if (frame.IsPayloadEmpty()) return true;

            var payload = frame.Payload;
            return _deserializer.TryDeserialize(in payload, message);
        }
    }
}
