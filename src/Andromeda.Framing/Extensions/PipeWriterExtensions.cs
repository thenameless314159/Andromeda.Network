using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Andromeda.Framing.Extensions
{
    public static class PipeWriterExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IFrameEncoder<TMetadata> AsFrameEncoder<TMetadata>(this PipeWriter w, MetadataEncoder<TMetadata> encoder, bool usePipeSynchronization = false)
            where TMetadata : class, IFrameMetadata => new PipeFrameEncoder<TMetadata>(w, encoder, usePipeSynchronization ? new SemaphoreSlim(1, 1) : default);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static IFrameEncoder<TMetadata> AsFrameEncoder<TMetadata>(this PipeWriter w, IMetadataEncoder encoder, bool usePipeSynchronization = false)
            where TMetadata : class, IFrameMetadata => new PipeFrameEncoder<TMetadata>(w, encoder, usePipeSynchronization ? new SemaphoreSlim(1, 1) : default);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IFrameEncoder AsFrameEncoder(this PipeWriter w, IMetadataEncoder encoder, bool usePipeSynchronization = false) =>
            new PipeFrameEncoder(w, encoder, usePipeSynchronization ? new SemaphoreSlim(1, 1) : default);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueTask<FlushResult> WriteFrameAsync(this PipeWriter writer, IMetadataEncoder encoder,
            in Frame frame, CancellationToken token = default) => !encoder.TryWriteMetadata(writer, frame.Metadata) 
                // Returns a completed flushResult in case we couldn't write the metadata
                ? ValueTask.FromResult(new FlushResult(token.IsCancellationRequested, true)) 
                : writer.WriteFramePayloadAsync(in frame, token);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueTask<FlushResult> WriteFrameAsync<TMeta>(this PipeWriter writer, IMetadataEncoder encoder,
            in Frame<TMeta> frame, CancellationToken token = default) where TMeta : class, IFrameMetadata
        {
            // Returns a completed flushResult in case we couldn't write the metadata
            if (!encoder.TryWriteMetadata(writer, frame.Metadata))
                return ValueTask.FromResult(new FlushResult(token.IsCancellationRequested, true));

            var untyped = frame.AsUntyped();
            return writer.WriteFramePayloadAsync(in untyped, token);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueTask<FlushResult> WriteFramePayloadAsync(this PipeWriter writer, in Frame frame, 
            CancellationToken token = default) => writer.WriteSequenceAsync(frame.Payload, token);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueTask<FlushResult> WriteSequenceAsync(this PipeWriter writer, ReadOnlySequence<byte> buffer,
            CancellationToken token = default) => !buffer.IsSingleSegment
            ? writer.WriteMultiSegmentSequenceAsync(buffer, token)
            : writer.WriteMemoryAsync(buffer.First, token) ;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static async ValueTask<FlushResult> WriteMultiSegmentSequenceAsync(this PipeWriter writer, ReadOnlySequence<byte> buffer,
            CancellationToken token = default)
        {
            FlushResult flushResult = default;
            foreach (var segment in buffer)
            {
                var writeAsync = writer.WriteMemoryAsync(segment, token);
                flushResult = writeAsync.IsCompletedSuccessfully ? writeAsync.Result
                    : await writeAsync.ConfigureAwait(false);

                if (flushResult.IsCanceled || flushResult.IsCompleted) return flushResult;
            }
            return flushResult;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueTask<FlushResult> WriteMemoryAsync(this PipeWriter writer, ReadOnlyMemory<byte> buffer,
            CancellationToken token = default)
        {
            const int chunkSize = 1024 * 8;
            return buffer.Length < chunkSize ? writer.WriteAsync(buffer, token) : writeBigMemoryAsync();
            ValueTask<FlushResult> writeBigMemoryAsync()
            {
                var i = 0;
                for (var c = buffer.Length / chunkSize; i < c; i++) // write by blocks of 8192 bytes
                {
                    var memory = writer.GetMemory(chunkSize);
                    buffer.Slice(i * chunkSize, chunkSize).CopyTo(memory);
                    writer.Advance(chunkSize);
                }

                var remaining = buffer.Length % chunkSize;
                if (remaining == 0) return writer.FlushAsync(token);

                var mem = writer.GetMemory(remaining);
                buffer[^remaining..].CopyTo(mem);
                writer.Advance(remaining);

                return writer.FlushAsync(token);
            }
        }
    }
}
