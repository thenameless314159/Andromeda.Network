using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Andromeda.Framing
{
    /// <inheritdoc />
    public class PipeFrameDecoder : IFrameDecoder
    {
        public PipeFrameDecoder(Stream stream, IMetadataDecoder decoder) => (_decoder, _pipe) = (decoder, PipeReader.Create(stream));
        public PipeFrameDecoder(PipeReader pipe, IMetadataDecoder decoder) => (_decoder, _pipe) = (decoder, pipe);
        
        /// <inheritdoc />
        public long FramesRead => Interlocked.Read(ref _framesRead);

        protected readonly IMetadataDecoder _decoder;
        protected SequencePosition _nextFrame;
        protected bool _hasAdvanced = true;
        protected bool _isThisCompleted;
        protected PipeReader? _pipe;
        protected bool _isConsuming;
        protected bool _isCompleted;
        protected bool _isCanceled;
        protected long _framesRead;
        protected Frame _frame;

        /// <inheritdoc />
        public ValueTask<Frame> ReadFrameAsync(CancellationToken token = default) => ReadFrameAsync(throwOnConsuming: true, token);

        /// <inheritdoc />
        public async IAsyncEnumerable<Frame> ReadFramesAsync([EnumeratorCancellation] CancellationToken token = default)
        {
            if(_isConsuming) ThrowIfAlreadyConsuming();
            _isConsuming = true;

            try {
                while (!token.IsCancellationRequested) {
                    try { await ReadFrameAsync(false, token).ConfigureAwait(false); }
                    catch (ObjectDisposedException) { yield break; }
                    yield return _frame;
                }
            }
            finally { _isConsuming = false; }
        }
        
        private async ValueTask<Frame> ReadFrameAsync(bool throwOnConsuming, CancellationToken token = default)
        {
            if (throwOnConsuming && _isConsuming) ThrowIfAlreadyConsuming();
            if (_isCompleted || _isThisCompleted) throw new ObjectDisposedException(GetType().Name);
            if (!TryAdvanceToNextFrame()) throw new ObjectDisposedException(GetType().Name);
            var reader = _pipe ?? throw new ObjectDisposedException(GetType().Name);

            try {
                for (var attempt = 1; /* true */; attempt++)
                {
                    var readResult = await reader.ReadAsync(token).ConfigureAwait(false);
                    if (TryReadFrame(in readResult, out _frame)) return _frame;
                    Trace.TraceWarning($"Couldn't read frame with a single attempt, current try: {attempt}.");
                }
            }
            catch (InvalidOperationException e) when (e.Message == "Reading is not allowed after reader was completed.") {
                throw new ObjectDisposedException(GetType().Name, e);
            }
        }

        protected virtual bool TryReadFrame(in ReadResult readResult, out Frame frame)
        {
            _isCompleted = readResult.IsCompleted && readResult.Buffer.IsEmpty && !readResult.IsCanceled;
            _isCanceled = readResult.IsCanceled; frame = default;

            if (_isCanceled) return false;
            var buffer = readResult.Buffer;
            _nextFrame = buffer.Start;
            _hasAdvanced = false; 

            if (buffer.TryParseFrame(_decoder, out frame)) { 
                _nextFrame = frame.Payload.End; _framesRead++;
                // If the payload is empty there's no need for the reader to hold on to the bytes.
                return !frame.IsPayloadEmpty() || TryAdvanceToNextFrame();
            }

            if (_isCompleted && !buffer.IsEmpty) throw new InvalidDataException(
                "Connection terminated while reading a message.");

            TryAdvanceToNextFrame();
            frame = default;
            return false;
        }

        protected virtual bool TryAdvanceToNextFrame()
        {
            if (_hasAdvanced) return true;
            if (_pipe is null) return false;
            if (!_pipe.TryAdvanceTo(_nextFrame)) 
                return false;

            _hasAdvanced = true;
            return true;
        }

        private static void ThrowIfAlreadyConsuming() => throw new InvalidOperationException(
            "Reading is not allowed while consuming frames via IAsyncEnumerable.");

        public virtual ValueTask DisposeAsync() { Dispose(); return default; }

        public virtual void Dispose() {
            var pipe = Interlocked.Exchange(ref _pipe, null);
            if (pipe is null) return;
            _isThisCompleted = true;

            // Should we also complete the pipe ? I don't know since this should be done by the transport
            // that own the pipe, but this is not within the scope of this library so maybe we should...
            pipe.CancelPendingRead();
            GC.SuppressFinalize(this);
        }
    }
}
