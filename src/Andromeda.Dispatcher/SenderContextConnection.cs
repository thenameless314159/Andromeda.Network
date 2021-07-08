using System.Threading;
using System.Collections.Generic;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;

namespace Andromeda.Dispatcher
{
    public abstract class SenderContextConnection : SenderContext
    {
        public override CancellationToken ConnectionClosed => _context.ConnectionClosed;
        public override IDictionary<object, object?> Items => _context.Items;
        public override IFeatureCollection Features => _context.Features;
        public override string Id => _context.ConnectionId;

        protected SenderContextConnection(ConnectionContext connection) => _context = connection;
        protected internal ConnectionContext _context;

        public override void Abort() => _context.Abort();
        public override void Abort(ConnectionAbortedException abortReason) => _context.Abort(abortReason);
    }
}
