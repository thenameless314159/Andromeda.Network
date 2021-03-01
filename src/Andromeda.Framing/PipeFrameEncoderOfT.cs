using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Andromeda.Framing
{
    /// <inheritdoc cref="IFrameEncoder{TMetadata}"/>
    public class PipeFrameEncoder<TMeta> : PipeFrameEncoder, IFrameEncoder<TMeta> 
        where TMeta : class, IFrameMetadata
    {
        /// <inheritdoc />
        public PipeFrameEncoder(PipeWriter pipe, IMetadataEncoder encoder, SemaphoreSlim? singleWriter = default) : base(pipe, encoder, singleWriter)
        {
        }

        /// <inheritdoc />
        public PipeFrameEncoder(Stream stream, IMetadataEncoder encoder, SemaphoreSlim? singleWriter = default) : base(stream, encoder, singleWriter)
        {
        }

        /// <inheritdoc />
        public ValueTask WriteAsync(IAsyncEnumerable<Frame<TMeta>> frames, CancellationToken token = default)
        {
            var writer = _pipe ?? throw new ObjectDisposedException(GetType().Name);

            // try to get the conch; if not, switch to async
            return TryWaitForSingleWriter(token) ? sendAll() : sendAllSlow();
            async ValueTask sendAll()
            {
                var framesWritten = 0;
                try {
                    await foreach (var frame in frames.WithCancellation(token))
                    {
                        var writeAsync = writer.WriteFrameAsync(_encoder, in frame, token);
                        var flushResult = writeAsync.IsCompletedSuccessfully ? writeAsync.Result
                            : await writeAsync.ConfigureAwait(false);

                        framesWritten++;
                        if (flushResult.IsCanceled || flushResult.IsCompleted) break;
                    }
                }
                finally { Release(framesWritten); }
            }
            async ValueTask sendAllSlow()
            {
                await WaitForSingleWriterAsync(token).ConfigureAwait(false);
                var framesWritten = 0;
                try {
                    await foreach (var frame in frames.WithCancellation(token))
                    {
                        var writeAsync = writer.WriteFrameAsync(_encoder, in frame, token);
                        var flushResult = writeAsync.IsCompletedSuccessfully ? writeAsync.Result
                            : await writeAsync.ConfigureAwait(false);

                        framesWritten++;
                        if (flushResult.IsCanceled || flushResult.IsCompleted) break;
                    }
                }
                finally { Release(framesWritten); }
            }
        }

        /// <inheritdoc />
        public ValueTask WriteAsync(IEnumerable<Frame<TMeta>> frames, CancellationToken token = default)
        {
            var writer = _pipe ?? throw new ObjectDisposedException(GetType().Name);

            // try to get the conch; if not, switch to async
            return TryWaitForSingleWriter(token) ? sendAll() : sendAllSlow();
            async ValueTask sendAll()
            {
                var framesWritten = 0;
                try {
                    foreach (var frame in frames)
                    {
                        var writeAsync = writer.WriteFrameAsync(_encoder, in frame, token);
                        var flushResult = writeAsync.IsCompletedSuccessfully ? writeAsync.Result
                            : await writeAsync.ConfigureAwait(false);

                        framesWritten++;
                        if (flushResult.IsCanceled || flushResult.IsCompleted) break;
                    }
                }
                finally { Release(framesWritten); }
            }
            async ValueTask sendAllSlow()
            {
                await WaitForSingleWriterAsync(token).ConfigureAwait(false);
                var framesWritten = 0;
                try {
                    foreach (var frame in frames)
                    {
                        var writeAsync = writer.WriteFrameAsync(_encoder, in frame, token);
                        var flushResult = writeAsync.IsCompletedSuccessfully ? writeAsync.Result
                            : await writeAsync.ConfigureAwait(false);

                        framesWritten++;
                        if (flushResult.IsCanceled || flushResult.IsCompleted) break;
                    }
                }
                finally { Release(framesWritten); }
            }
        }

        /// <inheritdoc />
        public ValueTask WriteAsync(in Frame<TMeta> frame, CancellationToken token = default)
        {
            var writer = _pipe ?? throw new ObjectDisposedException(GetType().Name);

            // try to get the conch; if not, switch to async
            if (!TryWaitForSingleWriter(token)) return sendAsyncSlowPath(frame);

            var release = true;
            try {
                var write = writer.WriteFrameAsync(_encoder, in frame, token); // includes a flush
                if (write.IsCompletedSuccessfully) return default;

                release = false; return awaitFlushAndRelease(write);
            }
            finally { if (release) Release(); } // don't release here if we had to continue with an async path
            async ValueTask awaitFlushAndRelease(ValueTask<FlushResult> flush)
            { try { await flush.ConfigureAwait(false); } finally { Release(); } }
            async ValueTask sendAsyncSlowPath(Frame<TMeta> frm)
            {
                await WaitForSingleWriterAsync(token).ConfigureAwait(false);
                try {
                    var writeAsync = writer.WriteFrameAsync(_encoder, in frm, token);
                    if (!writeAsync.IsCompletedSuccessfully) await writeAsync.ConfigureAwait(false);
                }
                finally { Release(); }
            }
        }
    }
}
