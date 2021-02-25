using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Andromeda.Framing
{
    /// <inheritdoc />
    public class PipeFrameEncoder : IFrameEncoder
    {
        /// <summary>ctor</summary>
        /// <param name="pipe">The pipe writer.</param>
        /// <param name="encoder">The metadata encoder.</param>
        /// <param name="singleWriter">The thread synchronizer.</param>
        public PipeFrameEncoder(PipeWriter pipe, IMetadataEncoder encoder, SemaphoreSlim? singleWriter = default) =>
            (_singleWriter, _encoder, _pipe) = (singleWriter, encoder, pipe);

        /// <summary>ctor</summary>
        /// <param name="stream">The stream to write in.</param>
        /// <param name="encoder">The metadata encoder.</param>
        /// <param name="singleWriter">The thread synchronizer.</param>
        public PipeFrameEncoder(Stream stream, IMetadataEncoder encoder, SemaphoreSlim? singleWriter = default) =>
            (_singleWriter, _encoder, _pipe) = (singleWriter, encoder, PipeWriter.Create(stream));

        protected readonly IMetadataEncoder _encoder;
        protected SemaphoreSlim? _singleWriter;
        private long _framesWritten;
        protected PipeWriter _pipe;

        /// <inheritdoc />
        public long FramesWritten => Interlocked.Read(ref _framesWritten);

        // TODO: fast path the write logic using the IAsyncEnumerator
        /// <inheritdoc />
        public ValueTask WriteAsync(IAsyncEnumerable<Frame> frames, CancellationToken token = default)
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
        public ValueTask WriteAsync(IEnumerable<Frame> frames, CancellationToken token = default)
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
        public ValueTask WriteAsync(in Frame frame, CancellationToken token = default)
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
            async ValueTask sendAsyncSlowPath(Frame frm) { await _singleWriter.WaitAsync(token).ConfigureAwait(false);
                try { await writer.WriteFrameAsync(_encoder, frm, token).ConfigureAwait(false); } finally { Release(); }
            }
        }
        
        public ValueTask DisposeAsync() { Dispose(); return default; }

        // Should we also complete the pipe ? I don't know since this should be done by the transport
        // that own the pipe, but this is not within the scope of this library so maybe we should...
        public virtual void Dispose()
        {
            var semaphore = Interlocked.Exchange(ref _singleWriter, null);
            var pipe = Interlocked.Exchange(ref _pipe, null!);
            pipe?.CancelPendingFlush();
            semaphore?.Dispose();

            GC.SuppressFinalize(this);
        }

        protected void Release(int framesWritten = 1)
        {
            // If the access to the pipe is already synchronized, add or increment using Interlocked class
            if (_singleWriter is not null) _framesWritten += framesWritten;
            else if (framesWritten == 1) Interlocked.Increment(ref _framesWritten);
            else Interlocked.Add(ref _framesWritten, framesWritten);
            _singleWriter?.Release();
        }
    }
}
