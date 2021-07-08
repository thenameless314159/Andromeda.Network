using Andromeda.Framing;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Andromeda.Dispatcher.Client;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;

namespace Andromeda.Dispatcher
{
    public abstract class SenderContext : IClient, IClientProxy, IClientFrameProxy
    {
        public abstract CancellationToken ConnectionClosed { get; }
        public abstract IDictionary<object, object?> Items { get; }
        public abstract IFeatureCollection Features { get; }
        public abstract string Id { get; }

        public abstract ValueTask SendAsync(in Frame frame);
        public abstract ValueTask SendAsync<T>(in T message);
        public abstract ValueTask SendAsync(IEnumerable<Frame> frames);
        public abstract ValueTask SendAsync(IAsyncEnumerable<Frame> frames);

        public abstract void Abort(ConnectionAbortedException abortReason);
        public abstract void Abort();
        
    }
}
