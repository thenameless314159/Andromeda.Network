using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace Andromeda.Framing
{
    /// <inheritdoc />
    public class PipeFrameDecoder : IFrameDecoder
    {
        public PipeFrameDecoder(PipeReader pipe, IMetadataDecoder decoder, SemaphoreSlim? singleReader = default) =>
            (_singleReader, _decoder, _pipe) = (singleReader, decoder, pipe);
        
        public PipeFrameDecoder(Stream stream, IMetadataDecoder decoder, SemaphoreSlim? singleReader = default) =>
            (_singleReader, _decoder, _pipe) = (singleReader, decoder, PipeReader.Create(stream));

        protected readonly IMetadataDecoder _decoder;
        protected SemaphoreSlim? _singleReader;
        protected ReadResult? _lastReadResult;
        protected Frame? _lastFrameRead;
        protected PipeReader? _pipe;
        private long _framesRead;

        /// <inheritdoc />
        public long FramesRead => Interlocked.Read(ref _framesRead);

        /// <inheritdoc />
        public ValueTask<Frame> ReadFrameAsync(CancellationToken token = default)
        {
            async ValueTask<Frame> readFrameSynchronizedAsyncSlowPath()
            {
                await _singleReader!.WaitAsync(token).ConfigureAwait(false);
                var r = true;
                try
                {
                    if (!TryAdvanceToNextFrame()) throw new ObjectDisposedException(GetType().Name);
                    if (_lastFrameRead.HasValue) { r = false; var f = _lastFrameRead.Value;
                        return await CompleteReadAsync(f, token).ConfigureAwait(false);
                    }

                    Frame? frame = default;
                    while (!token.IsCancellationRequested)
                    {
                        var readFrame = _pipe?.ReadFrameWithResultAsync(_decoder, token)
                                        ?? throw new ObjectDisposedException(GetType().Name);

                        var (f, readResult) = !readFrame.IsCompletedSuccessfully
                            ? await readFrame.ConfigureAwait(false)
                            : readFrame.Result;

                        if (Release(in f, in readResult)) { frame = f;
                            break;
                        }

                        // advance if the readResult is not completed and we couldn't read a frame
                        if (readResult.IsCompleted || !TryAdvanceToNextFrame()) break;
                        if (!_lastFrameRead.HasValue) continue;

                        // complete the read if we couldn't release but a frame was still parsed
                        r = false; var lf = _lastFrameRead!.Value;
                        return await CompleteReadAsync(lf, token).ConfigureAwait(false);
                    }
                    return frame ?? Frame.Empty;
                }
                finally { if(r) _singleReader!.Release(); }
            }
            async ValueTask<Frame> continueReadAsync(ValueTask<(Frame? Frame, ReadResult ReadResult)> readTask)
            {
                var r = true;
                try
                {
                    var (f, readResult) = await readTask.ConfigureAwait(false);
                    if (f.HasValue && Release(in f, in readResult)) 
                        return f.Value;

                    Frame? frame = default;
                    while (!token.IsCancellationRequested)
                    {
                        var readFrame = _pipe?.ReadFrameWithResultAsync(_decoder, token) 
                                         ?? throw new ObjectDisposedException(GetType().Name);

                        (f, readResult) = !readFrame.IsCompletedSuccessfully
                            ? await readFrame.ConfigureAwait(false)
                            : readFrame.Result;

                        if (Release(in f, in readResult)) { frame = f; 
                            break;
                        }

                        // advance if the readResult is not completed and we couldn't read a frame
                        if (readResult.IsCompleted || !TryAdvanceToNextFrame()) break;
                        if (!_lastFrameRead.HasValue) continue;

                        // complete the read if we couldn't release but a frame was still parsed
                        r = false; var lf = _lastFrameRead!.Value;
                        return await CompleteReadAsync(lf, token).ConfigureAwait(false);
                    }
                    
                    return frame ?? Frame.Empty;
                }
                finally { if(r) _singleReader?.Release(); }
                
            }

            // try to get the conch; if not, switch to async
            if (_singleReader is not null && !_singleReader.Wait(0, token))
                return readFrameSynchronizedAsyncSlowPath();

            var release = true;
            try
            {
                if(!TryAdvanceToNextFrame()) throw new ObjectDisposedException(GetType().Name);

                ref var lastFrame = ref _lastFrameRead;
                if (lastFrame.HasValue) { release = false; var f = lastFrame.Value; 
                    return CompleteReadAsync(in f, token);
                }

                Frame? frame = default;
                while (!token.IsCancellationRequested)
                {
                    var readFrameAsync = _pipe?.ReadFrameWithResultAsync(_decoder, token)
                                         ?? throw new ObjectDisposedException(GetType().Name);

                    if (!readFrameAsync.IsCompletedSuccessfully) { release = false;
                        return continueReadAsync(readFrameAsync);
                    }

                    var (f, readResult) = readFrameAsync.Result;
                    if (Release(in f, in readResult)) { frame = f;
                        break;
                    }

                    // advance if the readResult is not completed and we couldn't read a frame
                    if (readResult.IsCompleted || !TryAdvanceToNextFrame()) 
                        throw new ObjectDisposedException(GetType().Name);

                    lastFrame = ref _lastFrameRead;
                    if (!lastFrame.HasValue) continue;

                    // complete the read if we couldn't release but a frame was still parsed
                    release = false; var lf = lastFrame!.Value;
                    return CompleteReadAsync(in lf, token);
                }
                return ValueTask.FromResult(frame ?? Frame.Empty);
            }
            // don't release if we had to continue with an async path
            finally { if(release) _singleReader?.Release(); }
        }

        private ValueTask<Frame> CompleteReadAsync(in Frame frame, CancellationToken token = default)
        {
            async ValueTask<Frame> completeReadAsyncSlow(ValueTask<ReadResult> read)
            {
                try
                {
                    var result = await read.ConfigureAwait(false);
                    var f = _lastFrameRead ?? throw new InvalidOperationException();
                    if (result.Buffer.Length < f.Metadata.Length)
                        throw new InvalidOperationException();

                    var p = result.Buffer.Slice(0, f.Metadata.Length);
                    f = new Frame(p, f.Metadata);
                    _lastFrameRead = f;
                    return f;
                }
                finally { _singleReader?.Release(); }
            }

            try
            {
                var r = _pipe?.TryReadAsync(token)
                        ?? throw new ObjectDisposedException(GetType().Name);

                if (!r.IsCompletedSuccessfully) return completeReadAsyncSlow(r);
                var result = r.Result;
                if (result.Buffer.Length < frame.Metadata.Length)
                    throw new InvalidOperationException(
                        "Couldn't parse remaining payload of frame with " +
                        $"{frame.Metadata}, readBytes: {result.Buffer.Length}");

                var payload = result.Buffer.Slice(0, frame.Metadata.Length);
                var frameWithPayload = new Frame(payload, frame.Metadata);
                _lastFrameRead = frameWithPayload;

                return ValueTask.FromResult(frameWithPayload);
            }
            finally { _singleReader?.Release(); }
        }

        /// <inheritdoc />
        public IAsyncEnumerable<Frame> ReadFramesAsync() => new FramesDecoderEnumerable(this);

        public virtual ValueTask DisposeAsync() { Dispose(); return default; }

        // Should we also complete the pipe ? I don't know since this should be done by the transport
        // that own the pipe, but this is not within the scope of this library so maybe we should...
        public virtual void Dispose()
        {
            var semaphore = Interlocked.Exchange(ref _singleReader, null);
            var pipe = Interlocked.Exchange(ref _pipe, null);
            pipe?.CancelPendingRead();
            semaphore?.Dispose();

            GC.SuppressFinalize(this);
        }

        protected bool TryAdvanceToNextFrame()
        {
            // If a frame is present it means a read has already been performed by this instance
            // therefore the pipe need to be advanced before reading a new frame.
            ref var lastReadResult = ref _lastReadResult;
            ref var lastFrame = ref _lastFrameRead;
            bool couldAdvance;

            // Advance if we couldn't parse any frame but still have read something
            if (!lastFrame.HasValue && lastReadResult.HasValue) {
                couldAdvance = _pipe?.TryAdvanceTo(lastReadResult.Value.Buffer.Start) ?? false;
                lastReadResult = default;
                return couldAdvance;
            }

            // Advance after the already consumed metadata if a frame with an incomplete payload was read
            if (lastFrame.HasValue && lastReadResult.HasValue && 
                lastFrame.Value.Payload.Length != lastFrame.Value.Metadata.Length)
            {
                couldAdvance = _pipe?.TryAdvanceTo(lastReadResult.Value.Buffer
                    .GetPosition(_decoder.GetMetadataLength(lastFrame.Value.Metadata), 
                        lastReadResult.Value.Buffer.Start)) ?? false;

                return couldAdvance;
            }

            couldAdvance = !lastFrame.HasValue || (_pipe?.TryAdvanceTo(lastFrame.Value.Payload.End) ?? false);
            lastReadResult = default;
            lastFrame = default;
            return couldAdvance;
        }

        protected virtual bool Release(in Frame? frame, in ReadResult readResult)
        {
            if (!frame.HasValue) {
                _lastReadResult = readResult;
                _lastFrameRead = default;
                return false;
            }

            if (frame.Value.Payload.Length != frame.Value.Metadata.Length) {
                _lastReadResult = readResult;
                _lastFrameRead = frame.Value;
                return false;
            }

            // If the access to the pipe is not synchronized, increment using Interlocked class
            if (_singleReader is not null) _framesRead++;
            else Interlocked.Increment(ref _framesRead);
            _lastFrameRead = frame;
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

            public Frame Current => _decoder._lastFrameRead.GetValueOrDefault();

            public ValueTask DisposeAsync() => default;

            public async ValueTask<bool> MoveNextAsync()
            {
                try 
                {
                    var readFrameAsync = _decoder.ReadFrameAsync(_token);
                    if (readFrameAsync.IsCompleted) return 
                        readFrameAsync.IsCompletedSuccessfully
                        && readFrameAsync.Result.Metadata != null!;

                    await readFrameAsync.ConfigureAwait(false);
                }
                catch (OperationCanceledException) { return false; }
                catch (ObjectDisposedException) { return false; }
                return false;
            }
        }
    }
}
