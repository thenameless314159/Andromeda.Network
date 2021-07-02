using Andromeda.Serialization;

namespace Andromeda.Framing
{
    public abstract class MessageReader : IMessageReader
    {
        protected MessageReader(IDeserializer deserializer) => _deserializer = deserializer;
        protected readonly IDeserializer _deserializer;

        protected abstract bool AreMetadataValidFor<T>(T message, IFrameMetadata metadata);

        public bool TryDecode<T>(in Frame frame, T message)
        {
            if (!AreMetadataValidFor(message, frame.Metadata)) return false;
            if (frame.IsPayloadEmpty()) return true;

            var payload = frame.Payload;
            return _deserializer.TryDeserialize(in payload, message);
        }
    }
}
