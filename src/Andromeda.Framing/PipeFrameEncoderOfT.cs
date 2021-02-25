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

        // TODO: fast path the write logic using the IAsyncEnumerator
        /// <inheritdoc />
        public ValueTask WriteAsync(IAsyncEnumerable<Frame<TMeta>> frames, CancellationToken token = default)
        {
            var writer = _pipe ?? throw new ObjectDisposedException(GetType().Name);

            // try to get the conch; if not, switch to async
            if (_singleWriter is not null && !_singleWriter.Wait(0, token))
                return sendAllSlow();

            return sendAll();

            async ValueTask sendAll()
            {
                try
                {
                    await foreach (var frame in frames.WithCancellation(token))
                    {
                        var writeAsync = writer.WriteFrameAsync(_encoder, in frame, token);
                        var flushResult = writeAsync.IsCompletedSuccessfully ? writeAsync.Result
                            : await writeAsync.ConfigureAwait(false);

                        if (flushResult.IsCanceled || flushResult.IsCompleted)
                            break;
                    }
                }
                finally { Release(); }
            }
            async ValueTask sendAllSlow()
            {
                await _singleWriter.WaitAsync(token).ConfigureAwait(false);
                try
                {
                    await foreach (var frame in frames.WithCancellation(token))
                    {
                        var writeAsync = writer.WriteFrameAsync(_encoder, in frame, token);
                        var flushResult = writeAsync.IsCompletedSuccessfully ? writeAsync.Result
                            : await writeAsync.ConfigureAwait(false);

                        if (flushResult.IsCanceled || flushResult.IsCompleted)
                            break;
                    }
                }
                finally { Release(); }
            }
        }

        /// <inheritdoc />
        public ValueTask WriteAsync(IEnumerable<Frame<TMeta>> frames, CancellationToken token = default)
        {
            var writer = _pipe ?? throw new ObjectDisposedException(GetType().Name);

            // try to get the conch; if not, switch to async
            if (_singleWriter is not null && !_singleWriter.Wait(0, token))
                return sendAllSlow();

            return sendAll();

            async ValueTask sendAll()
            {
                try
                {
                    foreach (var frame in frames)
                    {
                        var writeAsync = writer.WriteFrameAsync(_encoder, in frame, token);
                        var flushResult = writeAsync.IsCompletedSuccessfully ? writeAsync.Result
                            : await writeAsync.ConfigureAwait(false);

                        if (flushResult.IsCanceled || flushResult.IsCompleted)
                            break;
                    }
                }
                finally { Release(); }
            }
            async ValueTask sendAllSlow()
            {
                await _singleWriter.WaitAsync(token).ConfigureAwait(false);
                try
                {
                    foreach (var frame in frames)
                    {
                        var writeAsync = writer.WriteFrameAsync(_encoder, in frame, token);
                        var flushResult = writeAsync.IsCompletedSuccessfully ? writeAsync.Result
                            : await writeAsync.ConfigureAwait(false);

                        if (flushResult.IsCanceled || flushResult.IsCompleted)
                            break;
                    }
                }
                finally { Release(); }
            }
        }

        /// <inheritdoc />
        public ValueTask WriteAsync(in Frame<TMeta> frame, CancellationToken token = default)
        {
            var writer = _pipe ?? throw new ObjectDisposedException(GetType().Name);

            // try to get the conch; if not, switch to async
            if (_singleWriter is not null && !_singleWriter.Wait(0, token))
                return sendAsyncSlowPath(frame);

            var release = true;
            try
            {
                var write = writer.WriteFrameAsync(_encoder, in frame, token); // includes a flush
                if (!write.IsCompletedSuccessfully) release = false;
                return write.IsCompletedSuccessfully ? default : awaitFlushAndRelease(write);
            }
            finally { if(release) Release(); } // don't release if we had to continue with an async path
            async ValueTask awaitFlushAndRelease(ValueTask<FlushResult> flush) { try { await flush; } finally { Release(); } }
            async ValueTask sendAsyncSlowPath(Frame<TMeta> frm)
            {
                await _singleWriter.WaitAsync(token).ConfigureAwait(false);
                try { await writer.WriteFrameAsync(_encoder, frm, token).ConfigureAwait(false); } finally { Release(); }
            }
        }
    }
}
