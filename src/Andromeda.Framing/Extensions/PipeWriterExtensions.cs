using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Andromeda.Framing
{
    /// <summary>
    /// Extension methods to write frames in any <see cref="PipeWriter"/> and to construct <see cref="IFrameEncoder"/>.
    /// </summary>
    public static class PipeWriterExtensions
    {
        /// <summary>
        /// Create an <see cref="IFrameEncoder{TMetadata}"/> from the specified <see cref="PipeWriter"/> using the provided
        /// <see cref="MetadataEncoder{TMetadata}"/>.
        /// </summary>
        /// <typeparam name="TMetadata">The specific <see cref="IFrameMetadata"/>.</typeparam>
        /// <param name="w">The pipe writer.</param>
        /// <param name="encoder">The metadata encoder.</param>
        /// <param name="singleWriter">Whether the access to the pipe should be thread synchronized or not.</param>
        /// <returns>An <see cref="IFrameEncoder{TMetadata}"/> instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IFrameEncoder<TMetadata> AsFrameEncoder<TMetadata>(this PipeWriter w, MetadataEncoder<TMetadata> encoder, bool singleWriter = false)
            where TMetadata : class, IFrameMetadata => new PipeFrameEncoder<TMetadata>(w, encoder, singleWriter ? new SemaphoreSlim(1, 1) : default);

        /// <summary>
        /// Create an <see cref="IFrameEncoder"/> from the specified <see cref="PipeWriter"/> using the provided <see cref="IMetadataEncoder"/>.
        /// </summary>
        /// <param name="w">The pipe writer.</param>
        /// <param name="encoder">The metadata encoder.</param>
        /// <param name="singleWriter">Whether the access to the pipe should be thread synchronized or not.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IFrameEncoder<TMetadata> AsFrameEncoder<TMetadata>(this PipeWriter w, MetadataParser<TMetadata> encoder, bool singleWriter = false)
            where TMetadata : class, IFrameMetadata => new PipeFrameEncoder<TMetadata>(w, encoder, singleWriter ? new SemaphoreSlim(1, 1) : default);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static IFrameEncoder<TMetadata> AsFrameEncoder<TMetadata>(this PipeWriter w, IMetadataEncoder encoder, bool singleWriter = false)
            where TMetadata : class, IFrameMetadata => new PipeFrameEncoder<TMetadata>(w, encoder, singleWriter ? new SemaphoreSlim(1, 1) : default);

        /// <summary>
        /// Create an <see cref="IFrameEncoder"/> from the specified <see cref="PipeWriter"/> using the provided <see cref="IMetadataEncoder"/>.
        /// </summary>
        /// <param name="w">The pipe writer.</param>
        /// <param name="encoder">The metadata encoder.</param>
        /// <param name="singleWriter">Whether the access to the pipe should be thread synchronized or not.</param>
        /// <returns>An <see cref="IFrameEncoder"/> instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IFrameEncoder AsFrameEncoder(this PipeWriter w, IMetadataEncoder encoder, bool singleWriter = false) =>
            new PipeFrameEncoder(w, encoder, singleWriter ? new SemaphoreSlim(1, 1) : default);


        /// <summary>
        /// Write a <see cref="Frame"/> in the current writer using the specified <see cref="IMetadataEncoder"/>.
        /// </summary>
        /// <param name="writer">The pipe writer.</param>
        /// <param name="encoder">The metadata encoder.</param>
        /// <param name="frame">The frame to write.</param>
        /// <param name="token">The cancellation token.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueTask<FlushResult> WriteFrameAsync(this PipeWriter writer, IMetadataEncoder encoder,
            in Frame frame, CancellationToken token = default) => !encoder.TryWriteMetadata(writer, frame.Metadata) 
                // Returns a completed flushResult in case we couldn't write the metadata
                ? ValueTask.FromResult(new FlushResult(token.IsCancellationRequested, true)) 
                : frame.IsPayloadEmpty() 
                    ? writer.FlushAsync(token)
                    : !frame.Payload.IsSingleSegment
                        ? writer.WriteMultiSegmentSequenceAsync(frame.Payload, token)
                        : writer.WriteMemoryAsync(frame.Payload.First, token);

        /// <summary>
        /// Write a <see cref="Frame{TMetadata}"/> in the current writer using the specified <see cref="IMetadataEncoder"/>.
        /// </summary>
        /// <param name="writer">The pipe writer.</param>
        /// <param name="encoder">The metadata encoder.</param>
        /// <param name="frame">The frame to write.</param>
        /// <param name="token">The cancellation token.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueTask<FlushResult> WriteFrameAsync<TMeta>(this PipeWriter writer, IMetadataEncoder encoder,
            in Frame<TMeta> frame, CancellationToken token = default) where TMeta : class, IFrameMetadata
        {
            // Returns a completed flushResult in case we couldn't write the metadata
            if (!encoder.TryWriteMetadata(writer, frame.Metadata))
                return ValueTask.FromResult(new FlushResult(token.IsCancellationRequested, true));

            return frame.IsPayloadEmpty()
                ? writer.FlushAsync(token)
                : !frame.Payload.IsSingleSegment
                    ? writer.WriteMultiSegmentSequenceAsync(frame.Payload, token)
                    : writer.WriteMemoryAsync(frame.Payload.First, token);
        }

        /// <summary>
        /// Write a buffer in the current writer.
        /// If the buffer length exceed 8192 bytes it'll be written by chunk.
        /// </summary>
        /// <param name="writer">The pipe writer.</param>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueTask<FlushResult> WriteSequenceAsync(this PipeWriter writer, ReadOnlySequence<byte> buffer,
            CancellationToken token = default) => !buffer.IsSingleSegment
            ? writer.WriteMultiSegmentSequenceAsync(buffer, token)
            : writer.WriteMemoryAsync(buffer.First, token);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ValueTask<FlushResult> WriteMultiSegmentSequenceAsync(this PipeWriter writer, ReadOnlySequence<byte> buffer,
            CancellationToken token = default)
        {
            foreach (var segment in buffer)
                writer.WriteBigMemory(segment);

            return writer.FlushAsync(token);
        }

        /// <summary>
        /// Write a buffer in the current writer.
        /// If the buffer length exceed 8192 bytes it'll be written by chunk.
        /// </summary>
        /// <param name="writer">The pipe writer.</param>
        /// <param name="buffer">The buffer to write.</param>
        /// <param name="token">The cancellation token.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueTask<FlushResult> WriteMemoryAsync(this PipeWriter writer, ReadOnlyMemory<byte> buffer,
            CancellationToken token = default)
        {
            const int chunkSize = 1024 * 8;
            return buffer.Length < chunkSize ? writer.WriteAsync(buffer, token) : writeBigMemoryAsync();
            ValueTask<FlushResult> writeBigMemoryAsync()
            {
                writer.WriteBigMemory(buffer);
                return writer.FlushAsync(token);
            }
        }

        private static void WriteBigMemory(this IBufferWriter<byte> writer, ReadOnlyMemory<byte> buffer)
        {
            var i = 0; const int chunkSize = 1024 * 8;
            for (var c = buffer.Length / chunkSize; i < c; i++) // write by blocks of 8192 bytes
            {
                var memory = writer.GetMemory(chunkSize);
                buffer.Slice(i * chunkSize, chunkSize).CopyTo(memory);
                writer.Advance(chunkSize);
            }

            var remaining = buffer.Length % chunkSize;
            if (remaining == 0) return;

            var mem = writer.GetMemory(remaining);
            buffer[^remaining..].CopyTo(mem);
            writer.Advance(remaining);
        }
    }
}
