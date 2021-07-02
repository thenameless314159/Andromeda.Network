using System;
using System.Net;
using System.Threading;
using Andromeda.Framing;
using System.Threading.Tasks;
using System.Collections.Generic;
using Andromeda.Dispatcher.Framing;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;

namespace Andromeda.Dispatcher
{
    public class DefaultClient : IClient, IClientProxy, IClientFrameProxy, IAsyncDisposable
    {
        public CancellationToken ConnectionClosed => _context.ConnectionClosed;
        public IDictionary<object, object?> Items => _context.Items;
        public EndPoint? RemoteEndPoint => _context.RemoteEndPoint;
        public EndPoint? LocalEndPoint => _context.LocalEndPoint;
        public IFeatureCollection Features => _context.Features;
        public string Id => _context.ConnectionId;

        protected internal virtual IFrameMessageEncoder Encoder => _encoder ??= new PipeMessageEncoder(_context.Transport.Output, _parser, _writer, SingleWriter);
        protected internal virtual IFrameMessageDecoder Decoder => _decoder ??= new PipeMessageDecoder(_context.Transport.Input, _parser, _reader);

        public DefaultClient(ConnectionContext context, IMetadataParser parser,
            IMessageReader? reader = default, IMessageWriter? writer = default) =>
            (_context, _parser, _reader, _writer) = (context, parser, reader, writer);

        protected internal SemaphoreSlim SingleWriter => _singleWriter ??= new SemaphoreSlim(1, 1);
        protected internal readonly ConnectionContext _context;
        protected internal readonly IMetadataParser _parser;
        protected internal readonly IMessageReader? _reader;
        protected internal readonly IMessageWriter? _writer;
        protected IFrameMessageDecoder? _decoder;
        protected IFrameMessageEncoder? _encoder;
        protected SemaphoreSlim? _singleWriter;

        public virtual void Abort(ConnectionAbortedException abortedException) => _context.Abort(abortedException);
        public virtual ValueTask SendAsync(in Frame frame) => Encoder.WriteAsync(in frame, ConnectionClosed);
        public virtual ValueTask SendAsync(IEnumerable<Frame> frames) => Encoder.WriteAsync(frames, ConnectionClosed);
        public virtual ValueTask SendAsync(IAsyncEnumerable<Frame> frames) => Encoder.WriteAsync(frames, ConnectionClosed);
        public virtual ValueTask SendAsync<TMessage>(in TMessage message) => Encoder.WriteAsync(in message, ConnectionClosed);

        public virtual ValueTask<TMessage?> ReceiveAsync<TMessage>() where TMessage : new() => Decoder.ReadAsync<TMessage>(ConnectionClosed);
        public virtual IAsyncEnumerable<Frame> ReceiveFramesAsync() => Decoder.ReadFramesAsync(ConnectionClosed);
        public virtual ValueTask<Frame> ReceiveFrameAsync() => Decoder.ReadFrameAsync(ConnectionClosed);


        public virtual async ValueTask DisposeAsync() 
        {
            try { // Doesn't dispose the connection context here since its lifetime should be handled by the provider of the instance
                if(_decoder is not null) await _decoder.DisposeAsync().ConfigureAwait(false);
                _decoder = default;

                // the semaphore should be disposed by the encoder
                if(_encoder is not null) await _encoder.DisposeAsync().ConfigureAwait(false);
                 _encoder = default; _singleWriter = default;
            }
            catch { /* shouldn't let any exception out at this point */ }
        }
    }
}
