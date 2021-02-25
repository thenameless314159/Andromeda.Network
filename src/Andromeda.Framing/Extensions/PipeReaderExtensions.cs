using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Andromeda.Framing.Extensions
{
    public static class PipeReaderExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IFrameDecoder<TMetadata> AsFrameDecoder<TMetadata>(this PipeReader r, MetadataDecoder<TMetadata> decoder, bool usePipeSynchronization = false)
            where TMetadata : class, IFrameMetadata => new PipeFrameDecoder<TMetadata>(r, decoder, usePipeSynchronization ? new SemaphoreSlim(1, 1) : default);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static IFrameDecoder<TMetadata> AsFrameDecoder<TMetadata>(this PipeReader r, IMetadataDecoder decoder, bool usePipeSynchronization = false)
            where TMetadata : class, IFrameMetadata => new PipeFrameDecoder<TMetadata>(r, decoder, usePipeSynchronization ? new SemaphoreSlim(1, 1) : default);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueTask<Frame?> ReadFrameAsync(this PipeReader r, IMetadataDecoder decoder,
            CancellationToken token = default)
        {
            var readAsync = r.ReadFrameWithResultAsync(decoder, token);
            return readAsync.IsCompletedSuccessfully
                ? ValueTask.FromResult(readAsync.Result.Frame)
                : ValueTask.FromResult(default(Frame?));
        }

        /// <summary>
        /// Attempt to read a frame with the specified <see cref="IMetadataDecoder"/> <see cref="decoder"/>.
        /// This method does not advance the pipe, you'll have to do it after a successful read.
        /// </summary>
        /// <param name="r">The pipe reader.</param>
        /// <param name="decoder">The frame metadata decoder.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>The read frame on success or null.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueTask<Frame<TMeta>?> ReadFrameAsync<TMeta>(this PipeReader r, IMetadataDecoder decoder,
            CancellationToken token = default) where TMeta : class, IFrameMetadata
        {
            var readAsync = r.ReadFrameWithResultAsync<TMeta>(decoder, token);
            return readAsync.IsCompletedSuccessfully
                ? ValueTask.FromResult(readAsync.Result.Frame)
                : ValueTask.FromResult(default(Frame<TMeta>?));
        }

        /// <summary>
        /// Attempt to read a frame with the specified <see cref="IMetadataDecoder"/> <see cref="decoder"/>.
        /// This method does not advance the pipe, you'll have to do it after a successful read.
        /// </summary>
        /// <param name="r">The pipe reader.</param>
        /// <param name="decoder">The frame metadata decoder.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>The read frame on success or null and the relative readResult.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueTask<(Frame? Frame, ReadResult ReadResult)> ReadFrameWithResultAsync(this PipeReader r, IMetadataDecoder decoder,
            CancellationToken token = default)
        {
            async ValueTask<(Frame?, ReadResult)> readAsyncSlowPath(ValueTask<ReadResult> readTask)
            {
                var result = await readTask.ConfigureAwait(false);
                if (result.IsCanceled) return default;

                var b = result.Buffer;
                if (b.IsEmpty && result.IsCompleted) return (default, result);
                b.TryParseFrame(decoder, out var frame); // TODO: Warn on false ?
                return (frame, result);
            }

            var readAsync = r.TryReadAsync(token);
            if (!readAsync.IsCompletedSuccessfully)
                return readAsyncSlowPath(readAsync);

            var readResult = readAsync.Result;
            if (readResult.IsCanceled)
                return default;

            var buffer = readResult.Buffer;
            if (buffer.IsEmpty && readResult.IsCompleted) return 
                new ValueTask<(Frame?, ReadResult)>((default, readResult));

            buffer.TryParseFrame(decoder, out var readFrame); // TODO: Warn on false ?
            return new ValueTask<(Frame?, ReadResult)>((readFrame, readResult));
        }

        /// <summary>
        /// Attempt to read a frame with the specified <see cref="IMetadataDecoder"/> <see cref="decoder"/>.
        /// This method does not advance the pipe, you'll have to do it after a successful read.
        /// </summary>
        /// <param name="r">The pipe reader.</param>
        /// <param name="decoder">The frame metadata decoder.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>The read frame on success or null and the relative readResult.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ValueTask<(Frame<TMeta>? Frame, ReadResult ReadResult)> ReadFrameWithResultAsync<TMeta>(this PipeReader r, IMetadataDecoder decoder,
            CancellationToken token = default) where TMeta : class, IFrameMetadata
        {
            async ValueTask<(Frame<TMeta>?, ReadResult)> readAsyncSlowPath(ValueTask<ReadResult> readTask)
            {
                var result = await readTask.ConfigureAwait(false);
                if (result.IsCanceled) return default;

                var b = result.Buffer;
                b.TryParseFrame(decoder, out var frame); // TODO: Warn on false ?
                return (frame.AsTyped<TMeta>(), result);
            }

            var readAsync = r.TryReadAsync(token);
            if (!readAsync.IsCompletedSuccessfully)
                return readAsyncSlowPath(readAsync);

            var readResult = readAsync.Result;
            if (readResult.IsCanceled)
                return default;

            var buffer = readResult.Buffer;
            buffer.TryParseFrame(decoder, out var readFrame); // TODO: Warn on false ?
            return new ValueTask<(Frame<TMeta>?, ReadResult)>((readFrame.AsTyped<TMeta>(), readResult));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ReadResult CreateCompletedReadResult(CancellationToken t) => new(ReadOnlySequence<byte>.Empty, t.IsCancellationRequested, true);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static async ValueTask<ReadResult> TryReadAsync(this PipeReader r, CancellationToken token = default)
        {
            // Suppress exceptions if the pipe has already been completed
            try { return await r.ReadAsync(token).ConfigureAwait(false); }
            catch (OperationCanceledException) { return CreateCompletedReadResult(token); }
            catch (InvalidOperationException) { return CreateCompletedReadResult(token); }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryAdvanceTo(this PipeReader r, SequencePosition consumed, SequencePosition examined)
        {
            // Suppress exceptions if the pipe has already been completed
            try { r.AdvanceTo(consumed, examined); return true; }
            catch (OperationCanceledException) { return false; }
            catch (InvalidOperationException) { return false; }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryAdvanceTo(this PipeReader r, SequencePosition consumed)
        {
            // Suppress exceptions if the pipe has already been completed
            try { r.AdvanceTo(consumed, consumed); return true; }
            catch (OperationCanceledException) { return false; }
            catch (InvalidOperationException) { return false; }
        }
    }
}
