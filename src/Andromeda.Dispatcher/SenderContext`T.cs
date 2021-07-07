using Andromeda.Framing;
using System.Threading.Tasks;
using Andromeda.Network.Client;
using System.Collections.Generic;
using Microsoft.AspNetCore.Connections;

namespace Andromeda.Dispatcher
{
    public abstract class SenderContext<TMeta> : SenderContext, IClientFrameProxy<TMeta> where TMeta : class, IFrameMetadata
    {
        protected SenderContext(ConnectionContext transport, IFrameMessageEncoder<TMeta> encoder) : base(transport, encoder)
        {
        }

        public ValueTask SendAsync(in Frame<TMeta> frame) => ((IFrameMessageEncoder<TMeta>)Encoder)
            .WriteAsync(in frame, _transport.ConnectionClosed);

        public ValueTask SendAsync(IEnumerable<Frame<TMeta>> frames) => ((IFrameMessageEncoder<TMeta>)Encoder)
            .WriteAsync(frames, _transport.ConnectionClosed);

        public ValueTask SendAsync(IAsyncEnumerable<Frame<TMeta>> frames) => ((IFrameMessageEncoder<TMeta>)Encoder)
            .WriteAsync(frames, _transport.ConnectionClosed);
    }
}
