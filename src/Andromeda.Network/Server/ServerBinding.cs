using Microsoft.AspNetCore.Connections;
using System.Net;

namespace Andromeda.Network
{
    public record ServerBinding(EndPoint EndPoint, ConnectionDelegate Application, ServerLogPolicy LogPolicy, IConnectionListenerFactory ConnectionListenerFactory);

    public record LocalHostBinding : ServerBinding
    {
        public LocalHostBinding(int port, ConnectionDelegate application, ServerLogPolicy logPolicy, IConnectionListenerFactory connectionListenerFactory) 
            : base(new IPEndPoint(IPAddress.Loopback, port), application, logPolicy, connectionListenerFactory)
        {
        }
    }
}
