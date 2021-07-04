using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Andromeda.Network.Hosting
{
    public class ServerHostedService : IHostedService
    {
        public ServerHostedService(IOptions<ServerHostedServiceOptions> options) => 
            _server = options.Value.ServerBuilder.Build();

        private readonly NetworkServer _server;

        public IEnumerable<EndPoint> EndPoints => _server.EndPoints;

        public Task StartAsync(CancellationToken cancellationToken) => 
            _server.StartAsync(cancellationToken);

        public Task StopAsync(CancellationToken cancellationToken) =>
            _server.StopAsync(cancellationToken);
    }
}
