using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Andromeda.Framing;
using Andromeda.Network.Client;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;

namespace Andromeda.Dispatcher
{
    public abstract class SenderContext : IClient, IClientProxy, IClientFrameProxy
    {
        public CancellationToken ConnectionClosed => _transport.ConnectionClosed;
        public IDictionary<object, object?> Items => _transport.Items;
        public IFeatureCollection Features => _transport.Features;
        public string Id => _transport.ConnectionId;

        protected SenderContext(ConnectionContext transport, IFrameMessageEncoder encoder) { 
            _transport = transport;
            Encoder = encoder;
        }

        protected internal virtual IFrameMessageEncoder Encoder { get; }
        protected internal readonly ConnectionContext _transport;

        public void Abort(ConnectionAbortedException abortedException) => _transport.Abort(abortedException);
        public void Abort() => _transport.Abort();

        public ValueTask SendAsync(in Frame frame) => Encoder.WriteAsync(in frame, _transport.ConnectionClosed);
        public ValueTask SendAsync(IEnumerable<Frame> frames) => Encoder.WriteAsync(frames, _transport.ConnectionClosed);
        public ValueTask SendAsync(IAsyncEnumerable<Frame> frames) => Encoder.WriteAsync(frames, _transport.ConnectionClosed);
        public ValueTask SendAsync<TMessage>(in TMessage message) => Encoder.WriteAsync(in message, _transport.ConnectionClosed);
    }
}
