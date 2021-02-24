using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Andromeda.Framing.Extensions
{
    public static class PipeReaderExtensions
    {
        public static IFrameDecoder<TMetadata> AsFrameDecoder<TMetadata>(this PipeReader r, MetadataDecoder<TMetadata> decoder, bool usePipeSynchronization = false)
            where TMetadata : class, IFrameMetadata => new PipeFrameDecoder<TMetadata>(r, decoder, usePipeSynchronization ? new SemaphoreSlim(1, 1) : default);

        internal static IFrameDecoder<TMetadata> AsFrameDecoder<TMetadata>(this PipeReader r, IMetadataDecoder decoder, bool usePipeSynchronization = false)
            where TMetadata : class, IFrameMetadata => new PipeFrameDecoder<TMetadata>(r, decoder, usePipeSynchronization ? new SemaphoreSlim(1, 1) : default);

        public static IFrameDecoder AsFrameDecoder(this PipeReader r, IMetadataDecoder decoder, bool usePipeSynchronization = false) =>
            new PipeFrameDecoder(r, decoder, usePipeSynchronization ? new SemaphoreSlim(1, 1) : default);
        
        /// <summary>
        /// Attempt to read a frame with the specified <see cref="IMetadataDecoder"/> <see cref="decoder"/>.
        /// This method does not advance the pipe, you'll have to do it after a successful read.
        /// </summary>
        /// <param name="r">The pipe reader.</param>
        /// <param name="decoder">The frame metadata decoder.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>The read frame on success or null.</returns>
        public static ValueTask<Frame?> ReadFrameAsync(this PipeReader r, IMetadataDecoder decoder,
            CancellationToken token = default)
        {
            async ValueTask<Frame?> readAsyncSlowPath(ValueTask<ReadResult> readTask)
            {
                var result = await readTask.ConfigureAwait(false);
                if (!IsReadResultValid(in result)) return default;

                var b = result.Buffer;
                return b.TryParseFrame(decoder, out var frame) ? frame : default;
            }

            var readAsync = r.ReadAsync(token);
            if (!readAsync.IsCompletedSuccessfully)
                return readAsyncSlowPath(readAsync);

            var readResult = readAsync.Result;
            if (!IsReadResultValid(in readResult))
                return default;

            var buffer = readResult.Buffer;
            return buffer.TryParseFrame(decoder, out var readFrame) 
                ? new ValueTask<Frame?>(readFrame) 
                : default;
        }

        /// <summary>
        /// Attempt to read a frame with the specified <see cref="IMetadataDecoder"/> <see cref="decoder"/>.
        /// This method does not advance the pipe, you'll have to do it after a successful read.
        /// </summary>
        /// <param name="r">The pipe reader.</param>
        /// <param name="decoder">The frame metadata decoder.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>The read frame on success or null.</returns>
        public static ValueTask<Frame<TMeta>?> ReadFrameAsync<TMeta>(this PipeReader r, IMetadataDecoder decoder,
            CancellationToken token = default) where TMeta : class, IFrameMetadata
        {
            async ValueTask<Frame<TMeta>?> readAsyncSlowPath(ValueTask<ReadResult> readTask)
            {
                var result = await readTask.ConfigureAwait(false);
                if (!IsReadResultValid(in result)) return default;

                var b = result.Buffer;
                return b.TryParseFrame(decoder, out var frame) ? frame.AsTyped<TMeta>() : default;
            }

            var readAsync = r.ReadAsync(token);
            if (!readAsync.IsCompletedSuccessfully)
                return readAsyncSlowPath(readAsync);

            var readResult = readAsync.Result;
            if (!IsReadResultValid(in readResult))
                return default;

            var buffer = readResult.Buffer;
            return buffer.TryParseFrame(decoder, out var readFrame)
                ? new ValueTask<Frame<TMeta>?>(readFrame.AsTyped<TMeta>())
                : default;
        }

        private static bool IsReadResultValid(in ReadResult result)
        {
            if (result.IsCanceled) return false;
            return result.Buffer.IsEmpty switch {
                true when result.IsCompleted => throw new ObjectDisposedException(nameof(PipeReader)),
                true when !result.IsCompleted => false,
                _ => true
            };
        }
    }
}
