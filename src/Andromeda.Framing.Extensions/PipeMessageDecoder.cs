﻿using System.IO;
using System.Threading;
using System.IO.Pipelines;
using System.Threading.Tasks;

namespace Andromeda.Framing
{
    public class PipeMessageDecoder : PipeFrameDecoder, IFrameMessageDecoder
    {
        public PipeMessageDecoder(PipeReader pipe, IMetadataDecoder decoder, IMessageReader deserializer) 
            : base(pipe, decoder) => _deserializer = deserializer;

        public PipeMessageDecoder(Stream stream, IMetadataDecoder decoder, IMessageReader deserializer) 
            : base(stream, decoder) => _deserializer = deserializer;

        private readonly IMessageReader _deserializer;
        private Frame _unreadFrame = Frame.Empty;

        public ValueTask<TMessage?> ReadAsync<TMessage>(CancellationToken token = default) where TMessage : new()
        {
            if (!_unreadFrame.IsEmptyFrame()) { var msg = new TMessage();
                if (!_deserializer.TryDecode(in _unreadFrame, msg))
                    return default;

                _unreadFrame = Frame.Empty;
                return ValueTask.FromResult<TMessage?>(msg);
            }

            var readFrameAsync = ReadFrameAsync(token);
            if (!readFrameAsync.IsCompletedSuccessfully)
                return readAsyncSlowPath(readFrameAsync);

            var message = new TMessage();
            var frame = readFrameAsync.Result;
            if (_deserializer.TryDecode(in frame, message))
                return ValueTask.FromResult<TMessage?>(message);

            _unreadFrame = frame;
            return default;
            async ValueTask<TMessage?> readAsyncSlowPath(ValueTask<Frame> readFrame) {
                var frm = await readFrame.ConfigureAwait(false); var msg = new TMessage();
                if (_deserializer.TryDecode(in frm, msg)) return msg;
                _unreadFrame = frm;
                return default;
            }
        }

        public ValueTask<bool> TryReadAsync<TMessage>(TMessage message, CancellationToken token = default)
        {
            if (!_unreadFrame.IsEmptyFrame()) {
                if (!_deserializer.TryDecode(in _unreadFrame, message))
                    return ValueTask.FromResult(false); ;

                _unreadFrame = Frame.Empty;
                return ValueTask.FromResult(true);
            }

            var readFrameAsync = ReadFrameAsync(token);
            if (!readFrameAsync.IsCompletedSuccessfully)
                return readAsyncSlowPath(readFrameAsync);

            var frame = readFrameAsync.Result;
            if (_deserializer.TryDecode(in frame, message))
                return ValueTask.FromResult(true);

            _unreadFrame = frame;
            return ValueTask.FromResult(false);

            async ValueTask<bool> readAsyncSlowPath(ValueTask<Frame> readFrame) {
                var frm = await readFrame.ConfigureAwait(false);
                if (_deserializer.TryDecode(in frm, message))
                    return true;

                _unreadFrame = frm;
                return false;
            }
        }
    }
}
