using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Andromeda.Framing
{
    /// <inheritdoc cref="IFrameDecoder{TMetadata}"/>
    public class PipeFrameDecoder<TMeta> : PipeFrameDecoder, IFrameDecoder<TMeta>
        where TMeta : class, IFrameMetadata
    {
        /// <inheritdoc />
        public PipeFrameDecoder(PipeReader pipe, IMetadataDecoder decoder, SemaphoreSlim? singleReader = default) : base(pipe, decoder, singleReader) { }

        /// <inheritdoc />
        public PipeFrameDecoder(Stream stream, IMetadataDecoder decoder, SemaphoreSlim? singleReader = default) : base(stream, decoder, singleReader) { }

        /// <inheritdoc />
        public new ValueTask<Frame<TMeta>> ReadFrameAsync(CancellationToken token = default)
        {
            static async ValueTask<Frame<TMeta>> awaitAndReturn(ValueTask<Frame> readTask) {
                var r = await readTask.ConfigureAwait(false);
                return r.AsTyped<TMeta>();
            }

            var readAsync = base.ReadFrameAsync(token);
            return readAsync.IsCompletedSuccessfully 
                ? ValueTask.FromResult(readAsync.Result.AsTyped<TMeta>())
                : awaitAndReturn(readAsync);
        }

        /// <inheritdoc />
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

            public Frame<TMeta> Current => _decoder._lastFrameRead?.AsTyped<TMeta>() ?? default;

            // Is this needed ? 
            public ValueTask DisposeAsync()
            {
                async ValueTask disposeAsyncSlowPath()
                {
                    await _decoder._singleReader!.WaitAsync(cancellationToken: default);
                    try { _decoder.TryAdvanceToNextFrame(); }
                    finally { _decoder._singleReader!.Release(); }
                }

                // don't use the stored token since it might already been cancelled at this point,
                // anyway if he is already cancelled the single reader will be null
                if (_decoder._singleReader is not null && !_decoder._singleReader.Wait(0, default))
                    return disposeAsyncSlowPath();

                try { _decoder.TryAdvanceToNextFrame(); }
                finally { _decoder._singleReader?.Release(); }
                return default;
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                try
                {
                    var readFrameAsync = _decoder.ReadFrameAsync(_token);
                    if (readFrameAsync.IsCompleted) return readFrameAsync.IsCompletedSuccessfully;
                    await readFrameAsync.ConfigureAwait(false);
                }
                catch (OperationCanceledException) { return false; }
                catch (ObjectDisposedException) { return false; }
                return false;
            }
        }
    }
}
