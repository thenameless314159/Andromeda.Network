using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Andromeda.Framing
{
    public static class IMetadataEncoderExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryWriteMetadata(this IMetadataEncoder encoder, IBufferWriter<byte> writer, IFrameMetadata metadata)
        {
            var metaLength = encoder.GetLength(metadata);
            if (metaLength < 0) throw new InvalidOperationException(
                "Cannot write frame with a negative metadataLength from " + 
                nameof(IMetadataDecoder) + '.' + nameof(encoder.GetLength) + " !");

            Span<byte> metaSpan;
            // If the writer has already been disposed or completed it'll throw at this point
            try { metaSpan = writer.GetSpan(metaLength); }
            catch (OperationCanceledException) { return false; }
            catch (InvalidOperationException){ return false; }
            
            // don't catch exceptions from user implementation
            encoder.Write(ref metaSpan, metadata);
            writer.Advance(metaLength);
            return true;
        }
    }
}
