using System;
using System.IO;
using System.Threading;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Andromeda.Dispatcher.Framing;

namespace Andromeda.Framing
{
    public class PipeMessageEncoder : PipeFrameEncoder, IFrameMessageEncoder
    {
        public PipeMessageEncoder(PipeWriter pipe, IMetadataEncoder encoder, IMessageWriter? serializer = default, SemaphoreSlim? singleWriter = default) 
            : base(pipe, encoder, singleWriter) => _serializer = serializer;

        public PipeMessageEncoder(Stream stream, IMetadataEncoder encoder, IMessageWriter? serializer = default, SemaphoreSlim? singleWriter = default) 
            : base(stream, encoder, singleWriter) => _serializer = serializer;

        private readonly IMessageWriter? _serializer;

        public ValueTask WriteAsync<TMessage>(in TMessage message, CancellationToken token = default)
        {
            if (_serializer is null) throw new InvalidOperationException(
                $"An {nameof(IMessageWriter)} must be setup in order to use the WriteAsync<TMessage> method in the {nameof(PipeMessageEncoder)} !");

            var writer = _pipe ?? throw new ObjectDisposedException(nameof(PipeMessageEncoder));

            // try to get the conch; if not, switch to async
            if (!TryWaitForSingleWriter(token)) return sendAsyncSlowPath(message);

            var release = true;
            try {
                _serializer.Encode(message, writer);
                var flush = writer.FlushAsync(token); // includes a flush
                if (flush.IsCompletedSuccessfully) return default;

                release = false; return awaitFlushAndRelease(flush);
            }
            finally { if (release) Release(); } // don't release here if we had to continue with an async path
            async ValueTask awaitFlushAndRelease(ValueTask<FlushResult> f)
            { try { await f.ConfigureAwait(false); } finally { Release(); } }
            async ValueTask sendAsyncSlowPath(TMessage msg)
            {
                await WaitForSingleWriterAsync(token).ConfigureAwait(false);
                try {
                    _serializer.Encode(msg, writer);
                    var flush = writer.FlushAsync(token); // includes a flush
                    if (!flush.IsCompletedSuccessfully) await flush.ConfigureAwait(false);
                }
                finally { Release(); }
            }
        }
    }
}
