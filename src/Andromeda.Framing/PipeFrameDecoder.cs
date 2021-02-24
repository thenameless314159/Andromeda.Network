using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Andromeda.Framing.Extensions;

namespace Andromeda.Framing
{
    [SuppressMessage("Reliability", "CA2012", Justification = "fast paths")]
    public class PipeFrameDecoder : IFrameDecoder
    {
        public PipeFrameDecoder(PipeReader pipe, IMetadataDecoder decoder, SemaphoreSlim? singleReader = default) =>
            (_singleReader, _decoder, _pipe) = (singleReader, decoder, pipe);
        
        public PipeFrameDecoder(Stream stream, IMetadataDecoder decoder, SemaphoreSlim? singleReader = default) =>
            (_singleReader, _decoder, _pipe) = (singleReader, decoder, PipeReader.Create(stream));

        protected readonly IMetadataDecoder _decoder;
        protected SemaphoreSlim? _singleReader;
        protected Frame? _lastFrameRead;
        protected PipeReader _pipe;
        private long _framesRead;

        public long FramesRead => Interlocked.Read(ref _framesRead);

        public ValueTask<Frame> ReadFrameAsync(CancellationToken token = default)
        {
            async ValueTask<Frame> readFrameSynchronizedAsyncSlowPath()
            {
                await _singleReader!.WaitAsync(token).ConfigureAwait(false);
                var p = _pipe ?? throw new ObjectDisposedException(GetType().Name);

                try
                {
                    var last = _lastFrameRead;
                    if (last.HasValue) p.AdvanceTo(last.Value.Payload.End);
                    while (!token.IsCancellationRequested)
                    {
                        var readFrame = p.ReadFrameAsync(_decoder, token);
                        var result = !readFrame.IsCompletedSuccessfully 
                            ? await readFrame.ConfigureAwait(false)
                            : readFrame.Result;

                        if (!Release(in result)) continue;
                        return result!.Value;
                    }
                }
                finally { _singleReader!.Release(); }
                return Frame.Empty;
            }
            async ValueTask<Frame> continueReadAsync(ValueTask<Frame?> readTask, PipeReader r)
            {
                try
                {
                    var frameRead = await readTask.ConfigureAwait(false);
                    if (frameRead.HasValue && Release(in frameRead)) 
                        return frameRead.Value;

                    while (!token.IsCancellationRequested)
                    {
                        var readFrame = r.ReadFrameAsync(_decoder, token);
                        var result = !readFrame.IsCompletedSuccessfully
                            ? await readFrame.ConfigureAwait(false)
                            : readFrame.Result;

                        if (!Release(in result)) continue;
                        return result!.Value;
                    }
                }
                finally { _singleReader?.Release(); }
                return Frame.Empty;
            }

            // try to get the conch; if not, switch to async
            if (_singleReader is not null && !_singleReader.Wait(0, token))
                return readFrameSynchronizedAsyncSlowPath();

            var pipe = _pipe ?? throw new ObjectDisposedException(GetType().Name);
            var release = true;

            try
            {
                // If a frame is present it means a read has already been performed by this instance
                // therefore the pipe need to be advanced before reading a new frame.
                ref var lastFrame = ref _lastFrameRead;
                if(lastFrame.HasValue) pipe.AdvanceTo(lastFrame.Value.Payload.End);

                while (!token.IsCancellationRequested)
                {
                    var readFrameAsync = pipe.ReadFrameAsync(_decoder, token);
                    if (!readFrameAsync.IsCompletedSuccessfully) { release = false;
                        return continueReadAsync(readFrameAsync, pipe);
                    }

                    var result = readFrameAsync.Result;
                    if (!Release(in result)) continue;
                    return ValueTask.FromResult(result!.Value);
                }
            }
            // don't release if we had to continue with an async path
            finally { if(release) _singleReader?.Release(); }
            return ValueTask.FromResult(Frame.Empty);
        }

        public IAsyncEnumerable<Frame> ReadFramesAsync() => new FramesDecoderEnumerable(this);

        public virtual ValueTask DisposeAsync() { Dispose(); return default; }

        // Should we also complete the pipe ? I don't know since this should be done by the transport
        // that own the pipe, but this is not within the scope of this library so maybe we should...
        public virtual void Dispose()
        {
            var semaphore = Interlocked.Exchange(ref _singleReader, null);
            var pipe = Interlocked.Exchange(ref _pipe, null!);
            pipe?.CancelPendingRead();
            semaphore?.Dispose();

            GC.SuppressFinalize(this);
        }

        protected bool Release(in Frame? frameRead)
        {
            if (!frameRead.HasValue) return false;

            // If the access to the pipe is already synchronized, increment using Interlocked class
            if (_singleReader is not null) _framesRead++;
            else Interlocked.Increment(ref _framesRead);
            _lastFrameRead = frameRead;
            _singleReader?.Release();
            return true;
        }

        private sealed class FramesDecoderEnumerable : IAsyncEnumerable<Frame>
        {
            public FramesDecoderEnumerable(PipeFrameDecoder decoder) => _decoder = decoder;
            private readonly PipeFrameDecoder _decoder;

            public IAsyncEnumerator<Frame> GetAsyncEnumerator(CancellationToken token = default) =>
                new FramesDecoderEnumerator(_decoder, token);
        }
        private sealed class FramesDecoderEnumerator : IAsyncEnumerator<Frame>
        {
            public FramesDecoderEnumerator(PipeFrameDecoder decoder, CancellationToken token) => 
                (_decoder, _token) = (decoder, token);

            private readonly PipeFrameDecoder _decoder;
            private readonly CancellationToken _token;

            public Frame Current { get; private set; }

            // Maybe should dispose decoder here ? 
            public ValueTask DisposeAsync() => default;

            public ValueTask<bool> MoveNextAsync()
            {
                async ValueTask<bool> moveNextAsync(ValueTask<Frame> readAsync) 
                {
                    Current = await readAsync.ConfigureAwait(false);
                    return true;
                }
                if (_token.IsCancellationRequested)
                    return ValueTask.FromResult(false);

                var readFrameAsync = _decoder.ReadFrameAsync(_token);
                if (readFrameAsync.IsCompletedSuccessfully)
                    return moveNextAsync(readFrameAsync);

                Current = readFrameAsync.Result;
                return ValueTask.FromResult(true);
            }
        }
    }
}
