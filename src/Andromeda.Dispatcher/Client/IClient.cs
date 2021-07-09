using System.Threading;
using System.Collections.Generic;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;

namespace Andromeda.Dispatcher.Client
{
    public interface IClient
    {
        CancellationToken ConnectionClosed { get; }
        IDictionary<object, object?> Items { get; }
        IFeatureCollection Features { get; }
        string ConnectionId { get; }
       
        void Abort(ConnectionAbortedException abortedException);
        void Abort();
    }
}
