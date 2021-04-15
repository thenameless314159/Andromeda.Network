using System;
using System.Threading;
using System.Security.Principal;

namespace Andromeda.Dispatcher.Handlers
{
    public interface IHandler
    {
        CancellationToken RequestAborted => Context.Client.ConnectionClosed;
        IClient Connection => Context.Client;
        IPrincipal User => Context.Identity;

        IServiceProvider RequestServices { get; set; }
        public SenderContext Context { get; set; }
    }
}
