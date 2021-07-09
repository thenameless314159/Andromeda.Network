using System;
using System.Net;
using System.Threading;
using Andromeda.Framing;
using System.IO.Pipelines;
using System.Threading.Tasks;
using System.Collections.Generic;
using Andromeda.Dispatcher.Client;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;

namespace Andromeda.Dispatcher
{
    public abstract class SenderContext : ConnectionContext, IClient, IClientMessageProxy, IClientFrameProxy
    {
        protected SenderContext(ConnectionContext connection) => _context = connection;
        protected internal ConnectionContext _context;

        public override IFeatureCollection Features => _context.Features;

        public override string ConnectionId { get => _context.ConnectionId;
            set => throw new InvalidOperationException(
                $"{nameof(ConnectionId)} cannot be set on {nameof(SenderContext)} !");
        }

        public override EndPoint? LocalEndPoint { get => _context.LocalEndPoint;
            set => throw new InvalidOperationException(
                $"{nameof(LocalEndPoint)} cannot be set on {nameof(SenderContext)} !");
        }
        
        public override EndPoint? RemoteEndPoint { get => _context.RemoteEndPoint;
            set => throw new InvalidOperationException(
                $"{nameof(RemoteEndPoint)} cannot be set on {nameof(SenderContext)} !");
        }

        public override IDuplexPipe Transport { get => _context.Transport;
            set => throw new InvalidOperationException(
                $"{nameof(Transport)} cannot be set on {nameof(SenderContext)} !");
        }
        
        public override IDictionary<object, object?> Items { get => _context.Items;
            set => throw new InvalidOperationException(
                $"{nameof(Items)} cannot be set on {nameof(SenderContext)} !");
        }
        
        public override CancellationToken ConnectionClosed { get => _context.ConnectionClosed;
            set => throw new InvalidOperationException(
                $"{nameof(ConnectionClosed)} cannot be set on {nameof(SenderContext)} !");
        }

        public abstract ValueTask SendAsync(in Frame frame);
        public abstract ValueTask SendAsync<T>(in T message);
        public abstract ValueTask SendAsync(IEnumerable<Frame> frames);
        public abstract ValueTask SendAsync(IAsyncEnumerable<Frame> frames);

        public override void Abort() => _context.Abort();
        public override void Abort(ConnectionAbortedException abortReason) => _context.Abort(abortReason);
    }
}
