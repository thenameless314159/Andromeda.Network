using System.Net;
using System.Threading;
using System.Collections.Generic;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;

namespace Andromeda.Network.Client
{
    public interface IClient
    { 
        CancellationToken ConnectionClosed { get; }
        IDictionary<object, object?> Items { get; }
        IFeatureCollection Features { get; }
        string Id { get; }
       
        void Abort(ConnectionAbortedException abortedException);
        void Abort() => Abort(new ConnectionAbortedException("The connection was aborted by the application via Andromeda.Dispatcher.IClient.Abort()."));
    }
}
