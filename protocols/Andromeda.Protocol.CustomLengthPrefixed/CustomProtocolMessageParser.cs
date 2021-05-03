using System;
using System.Buffers;
using System.Diagnostics;
using Andromeda.Framing;
using Andromeda.Serialization;
using Andromeda.Sizing;

namespace Andromeda.Protocol
{
    public class CustomProtocolMessageParser : IMessageReader, IMessageWriter
    {
        // static because the serialization and sizing store cannot be setup twice and they won't be registered as dependency in the samples
        private static readonly IMessageContainer _messages = MessageContainer.CreateFrom(setupSingleton: false, typeof(ProtocolMessageMetadata).Assembly);
        private static readonly ISerDes _serializer = SerializationBuilder.CreateFor<CustomProtocol>().ConfigureCustomProtocol().Build();
        private static readonly ISizing _sizing = SizingBuilder.CreateFor<CustomProtocol>().ConfigureCustomProtocol().Build();
        private static readonly IMetadataEncoder _encoder = new CustomProtocolParser();
        private class CustomProtocol : SerializationType { }

        public bool TryParse<T>(in Frame frame, T message)
        {
            if (frame.Metadata is not ProtocolMessageMetadata metadata) return false;
            if (metadata.MessageId != _messages.GetId<T>()) return false;
            if (metadata.Length != frame.Payload.Length) return false;
            var payload = frame.Payload;

            // TODO: should also check if the defined protocol model has no property to deserialize
            return frame.IsPayloadEmpty() || _serializer.TryDeserialize(in payload, message);
        }

        public void Encode<T>(T message, IBufferWriter<byte> writer)
        {
            var messageId = _messages.GetId<T>();
            var sizeOfPayload = _sizing.SizeOf(in message);
            var metadata = new ProtocolMessageMetadata((ushort) messageId, sizeOfPayload);

            var metaLen = _encoder.GetLength(metadata);
            var frameSize = metaLen + sizeOfPayload;
            var span = writer.GetSpan(frameSize);
            _encoder.Write(ref span, metadata);

            if (sizeOfPayload > 0) {
                var msgSpan = span[metaLen..];
                _serializer.Serialize(message, ref msgSpan, out var bytesWritten);

                if (bytesWritten != sizeOfPayload) { frameSize = (int)(metaLen + bytesWritten);
                    Trace.TraceWarning($"Serializer bytes written count didn't match with pre-sizing value ! written : {bytesWritten}, sized at : {sizeOfPayload}");
                }
            }

            writer.Advance(frameSize);
        }

        public void Encode<T>(T message, ref Span<byte> buffer, out long bytesWritten) => _serializer.Serialize(in message, ref buffer, out bytesWritten);
    }
}
