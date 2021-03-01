using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Andromeda.Framing
{
    internal static class ReadOnlySequenceExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryParseFrame(this ref ReadOnlySequence<byte> buffer, IMetadataDecoder decoder, out Frame frame)
        {
            frame = default;

            var reader = new SequenceReader<byte>(buffer);
            if (!decoder.TryParse(ref reader, out var metadata))
                return false;

            if (metadata!.Length < 0) throw new InvalidOperationException(
                $"Payload of parsed frame with {metadata} cannot have negative length !");

            if (reader.Remaining < metadata.Length) return false;

            frame = new Frame(buffer.Slice(reader.Position, metadata.Length), metadata);
            return true;
        }
    }
}
