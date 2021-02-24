using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Andromeda.Framing.Extensions
{
    public static class PipeWriterExtensions
    {
        public static IFrameEncoder<TMetadata> AsFrameEncoder<TMetadata>(this PipeWriter w, MetadataEncoder<TMetadata> encoder, bool usePipeSynchronization = false)
            where TMetadata : class, IFrameMetadata => new PipeFrameEncoder<TMetadata>(w, encoder, usePipeSynchronization ? new SemaphoreSlim(1, 1) : default);

        internal static IFrameEncoder<TMetadata> AsFrameEncoder<TMetadata>(this PipeWriter w, IMetadataEncoder encoder, bool usePipeSynchronization = false)
            where TMetadata : class, IFrameMetadata => new PipeFrameEncoder<TMetadata>(w, encoder, usePipeSynchronization ? new SemaphoreSlim(1, 1) : default);

        public static IFrameEncoder AsFrameEncoder(this PipeWriter w, IMetadataEncoder encoder, bool usePipeSynchronization = false) =>
            new PipeFrameEncoder(w, encoder, usePipeSynchronization ? new SemaphoreSlim(1, 1) : default);

        public static ValueTask<FlushResult> WriteFrameAsync(this PipeWriter writer, IMetadataEncoder encoder,
            in Frame frame, CancellationToken token = default) => !encoder.TryWriteMetadata(writer, frame.Metadata) 
                // Returns a completed flushResult in case we couldn't write the metadata
                ? ValueTask.FromResult(new FlushResult(token.IsCancellationRequested, true)) 
                : writer.WriteFramePayloadAsync(in frame, token);

        public static ValueTask<FlushResult> WriteFrameAsync<TMeta>(this PipeWriter writer, IMetadataEncoder encoder,
            in Frame<TMeta> frame, CancellationToken token = default) where TMeta : class, IFrameMetadata
        {
            // Returns a completed flushResult in case we couldn't write the metadata
            if (!encoder.TryWriteMetadata(writer, frame.Metadata))
                return ValueTask.FromResult(new FlushResult(token.IsCancellationRequested, true));

            var untyped = frame.AsUntyped();
            return writer.WriteFramePayloadAsync(in untyped, token);
        }

        public static ValueTask<FlushResult> WriteFramePayloadAsync(this PipeWriter writer, in Frame frame, 
            CancellationToken token = default) => 
            frame.Payload.IsSingleSegment
                ? writer.WriteMemoryAsync(frame.Payload.First, token)
                : writer.WriteSequenceAsync(frame.Payload, token);

        public static async ValueTask<FlushResult> WriteSequenceAsync(this PipeWriter writer, ReadOnlySequence<byte> buffer,
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

        public static ValueTask<FlushResult> WriteMemoryAsync(this PipeWriter writer, ReadOnlyMemory<byte> buffer,
            CancellationToken token = default)
        {
            const int chunkSize = 1024 * 8;
            return buffer.Length < chunkSize ? writer.WriteAsync(buffer, token) : writeBigMemoryAsync();
            async ValueTask<FlushResult> writeBigMemoryAsync()
            {
                var i = 0; FlushResult flushResult = default;
                for (var c = buffer.Length / chunkSize; i < c; i++) // write by blocks of 8192 bytes
                {
                    var writeAsync = writer.WriteAsync(buffer.Slice(i * chunkSize, chunkSize), token);
                    flushResult = writeAsync.IsCompletedSuccessfully ? writeAsync.Result
                        : await writeAsync.ConfigureAwait(false);

                    if (flushResult.IsCanceled || flushResult.IsCompleted) return flushResult;
                }

                if (buffer.Length % chunkSize == 0) return flushResult;

                var lastWriteAsync = writer.WriteAsync(buffer[(i * chunkSize)..], token);
                return lastWriteAsync.IsCompletedSuccessfully ? lastWriteAsync.Result
                    : await lastWriteAsync.ConfigureAwait(false);
            }
        }
    }
}
