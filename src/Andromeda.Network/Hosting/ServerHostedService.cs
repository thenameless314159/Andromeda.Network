using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Andromeda.Network.Hosting
{
    internal sealed class ServerHostedService : IHostedService
    {
        public ServerHostedService(INetworkServer server) => _server = server;
        private readonly INetworkServer _server;

        public Task StartAsync(CancellationToken cancellationToken) => 
            _server.StartAsync(cancellationToken);

        public Task StopAsync(CancellationToken cancellationToken) =>
            _server.StopAsync(cancellationToken);
    }
}
