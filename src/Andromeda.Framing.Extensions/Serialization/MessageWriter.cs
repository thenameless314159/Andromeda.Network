using System.Buffers;
using Andromeda.Serialization;

namespace Andromeda.Framing
{
    public abstract class MessageWriter : IMessageWriter
    {
        protected MessageWriter(IMetadataEncoder encoder, ISerializer serializer) =>
            (_encoder, _serializer) = (encoder, serializer);
        
        private readonly IMetadataEncoder _encoder;
        private readonly ISerializer _serializer;

        // TODO: handle buffer too small, shouldn't be an issue since it should be slightly bigger than the size hint
        // TODO: but the sizing and serialization is implemented by the user therefore we can't rly trust it
        public void Encode<T>(in T message, IBufferWriter<byte> writer)
        {
            var metadata = GetFrameMetadataOf(in message);
            var metaLen = _encoder.GetLength(metadata);
            var frameSize = metaLen + metadata.Length;
            var span = writer.GetSpan(frameSize);

            // The payload is written first in order to handle potential differences between the
            // number of bytes written and the pre-calculated length
            if (metadata.Length > 0) { var payload = span[metaLen..];
                _serializer.Serialize(in message, in payload, out var bytesWritten);

                // If the number of bytes written doesn't match with the pre-calculated length
                // we must create a copy of the metadata with the correct payload length 
                if (bytesWritten != metadata.Length) { frameSize = metaLen + bytesWritten;
                    metadata = CreateCopyOf(metadata, newLength: bytesWritten);
                }
            }

            _encoder.Write(ref span, metadata);
            writer.Advance(frameSize);
        }

        protected abstract IFrameMetadata GetFrameMetadataOf<T>(in T message);
        protected abstract IFrameMetadata CreateCopyOf(IFrameMetadata metadata, int newLength);
    }
}
