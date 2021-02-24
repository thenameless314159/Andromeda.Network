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
    public class PipeFrameDecoder<TMeta> : PipeFrameDecoder, IFrameDecoder<TMeta>
        where TMeta : class, IFrameMetadata
    {
        public PipeFrameDecoder(PipeReader pipe, IMetadataDecoder decoder, SemaphoreSlim? singleReader = default) : base(pipe, decoder, singleReader) { }
        public PipeFrameDecoder(Stream stream, IMetadataDecoder decoder, SemaphoreSlim? singleReader = default) : base(stream, decoder, singleReader) { }

        protected bool Release(in Frame<TMeta>? frameRead) { var untyped = frameRead?.AsUntyped(); return Release(in untyped); }

        public new ValueTask<Frame<TMeta>> ReadFrameAsync(CancellationToken token = default)
        {
            async ValueTask<Frame<TMeta>> readFrameSynchronizedAsyncSlowPath()
            {
                await _singleReader!.WaitAsync(token).ConfigureAwait(false);
                var p = _pipe ?? throw new ObjectDisposedException(GetType().Name);

                try
                {
                    var last = _lastFrameRead;
                    if (last.HasValue) p.AdvanceTo(last.Value.Payload.End);
                    while (!token.IsCancellationRequested)
                    {
                        var readFrame = p.ReadFrameAsync<TMeta>(_decoder, token);
                        var result = !readFrame.IsCompletedSuccessfully
                            ? await readFrame.ConfigureAwait(false)
                            : readFrame.Result;

                        if (!Release(in result)) continue;
                        return result!.Value;
                    }
                }
                finally { _singleReader!.Release(); }
                return Frame<TMeta>.Empty;
            }
            async ValueTask<Frame<TMeta>> continueReadAsync(ValueTask<Frame<TMeta>?> readTask, PipeReader r)
            {
                try
                {
                    var frameRead = await readTask.ConfigureAwait(false);
                    if (frameRead.HasValue && Release(in frameRead))
                        return frameRead.Value;

                    while (!token.IsCancellationRequested)
                    {
                        var readFrame = r.ReadFrameAsync<TMeta>(_decoder, token);
                        var result = !readFrame.IsCompletedSuccessfully
                            ? await readFrame.ConfigureAwait(false)
                            : readFrame.Result;

                        if (!Release(in result)) continue;
                        return result!.Value;
                    }
                }
                finally { _singleReader?.Release(); }
                return Frame<TMeta>.Empty;
            }

            if (_singleReader is not null && !_singleReader.Wait(0, token))
                return readFrameSynchronizedAsyncSlowPath();

            var pipe = _pipe ?? throw new ObjectDisposedException(GetType().Name);
            var release = true;  

            try
            {
                // If a frame is present it means a read has already been performed by this instance
                // therefore the pipe need to be advanced before reading a new frame.
                ref var lastFrame = ref _lastFrameRead;
                if (lastFrame.HasValue) pipe.AdvanceTo(lastFrame.Value.Payload.End);

                while (!token.IsCancellationRequested)
                {
                    var readFrameAsync = pipe.ReadFrameAsync<TMeta>(_decoder, token);
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
            return ValueTask.FromResult(Frame<TMeta>.Empty);
        }

        public new IAsyncEnumerable<Frame<TMeta>> ReadFramesAsync() => new FramesDecoderEnumerable(this);

        private sealed class FramesDecoderEnumerable : IAsyncEnumerable<Frame<TMeta>>
        {
            public FramesDecoderEnumerable(PipeFrameDecoder<TMeta> decoder) => _decoder = decoder;
            private readonly PipeFrameDecoder<TMeta> _decoder;

            public IAsyncEnumerator<Frame<TMeta>> GetAsyncEnumerator(CancellationToken token = default) =>
                new FramesDecoderEnumerator(_decoder, token);
        }
        private sealed class FramesDecoderEnumerator : IAsyncEnumerator<Frame<TMeta>>
        {
            public FramesDecoderEnumerator(PipeFrameDecoder<TMeta> decoder, CancellationToken token) =>
                (_decoder, _token) = (decoder, token);

            private readonly PipeFrameDecoder<TMeta> _decoder;
            private readonly CancellationToken _token;

            public Frame<TMeta> Current { get; private set; }

            public ValueTask DisposeAsync() => _decoder.DisposeAsync();
            public ValueTask<bool> MoveNextAsync()
            {
                async ValueTask<bool> moveNextAsync(ValueTask<Frame<TMeta>> readAsync)
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
