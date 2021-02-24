using System;
using System.Buffers;

namespace Andromeda.Framing.Extensions
{
    internal static class ReadOnlySequenceExtensions
    {
        public static bool TryParseFrame(this ref ReadOnlySequence<byte> buffer, IMetadataDecoder decoder, out Frame frame)
        {
            frame = default;

            var reader = new SequenceReader<byte>(buffer);
            if (!decoder.TryParse(ref reader, out var metadata))
                return false;

            if (metadata!.Length < 0) throw new InvalidOperationException(
                $"Payload of parsed frame with {metadata} cannot have negative length !");

            if (reader.Remaining < metadata.Length) return false;
            var payload = metadata.Length == 0
                ? ReadOnlySequence<byte>.Empty
                : buffer.Slice(reader.Position, metadata.Length);

            frame = new Frame(payload, metadata);
            return true;
        }
    }
}
