using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace Andromeda.Framing
{
    /// <summary>
    /// Extension methods to write <see cref="IFrameMetadata"/> of a <see cref="Frame"/> or <see cref="Frame{TMetadata}"/>
    /// with a <see cref="IMetadataDecoder"/> in a <see cref="IBufferWriter{T}"/>.
    /// </summary>
    public static class IMetadataEncoderExtensions
    {
        /// <summary>
        /// Try to write the <see cref="IFrameMetadata"/> in the specified writer using the current <see cref="IMetadataEncoder"/>.
        /// </summary>
        /// <param name="encoder">The metadata encoder.</param>
        /// <param name="writer">The buffer writer.</param>
        /// <param name="metadata">The metadata to write.</param>
        /// <returns>Whether the metadata could be written or not.</returns>
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
