using System;
using System.IO;
using System.Threading;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Andromeda.Dispatcher.Framing;

namespace Andromeda.Framing
{
    public class PipeMessageDecoder : PipeFrameDecoder, IFrameMessageDecoder
    {
        public PipeMessageDecoder(PipeReader pipe, IMetadataDecoder decoder, IMessageReader? deserializer = default) 
            : base(pipe, decoder) => _deserializer = deserializer;

        public PipeMessageDecoder(Stream stream, IMetadataDecoder decoder, IMessageReader? deserializer = default) 
            : base(stream, decoder) => _deserializer = deserializer;

        private readonly IMessageReader? _deserializer;

        public ValueTask<TMessage?> ReadAsync<TMessage>(CancellationToken token = default) where TMessage : new()
        {
            if (_deserializer is null) throw new InvalidOperationException(
                $"An {nameof(IMessageReader)} must be setup in order to use the TryParse<TMessage> method in the {nameof(PipeMessageDecoder)} !");

            var readFrameAsync = ReadFrameAsync(token);
            if (!readFrameAsync.IsCompletedSuccessfully)
                return readAsyncSlowPath(readFrameAsync);

            var message = new TMessage();
            var frame = readFrameAsync.Result;
            return _deserializer.TryParse(in frame, message) 
                ? ValueTask.FromResult<TMessage?>(message) 
                : default;

            async ValueTask<TMessage?> readAsyncSlowPath(ValueTask<Frame> readFrame) {
                var frm = await readFrame.ConfigureAwait(false); var msg = new TMessage(); 
                return _deserializer.TryParse(in frm, msg) ? msg : default;
            }
        }
    }
}
