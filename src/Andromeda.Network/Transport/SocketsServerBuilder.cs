using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using Andromeda.Network.Internal;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;

namespace Andromeda.Network
{
    public class SocketsServerBuilder
    {
        private readonly List<(EndPoint? EndPoint, int Port, Action<IConnectionBuilder> Application, 
            Action<ServerLogPolicy>? LogPolicy)> _bindings = new();

        public SocketTransportOptions Options { get; } = new();

        public SocketsServerBuilder ListenLocalhost(int port, Action<IConnectionBuilder> configure, Action<ServerLogPolicy>? configureLogPolicy = default)
        {
            _bindings.Add((null, port, configure, configureLogPolicy));
            return this;
        }

        public SocketsServerBuilder Listen(EndPoint endPoint, Action<IConnectionBuilder> configure, Action<ServerLogPolicy>? configureLogPolicy = default)
        {
            _bindings.Add((endPoint, 0, configure, configureLogPolicy));
            return this;
        }

        public SocketsServerBuilder Listen(IPAddress address, int port, Action<IConnectionBuilder> configure, Action<ServerLogPolicy>? configureLogPolicy = default) =>
            Listen(new IPEndPoint(address, port), configure, configureLogPolicy);

        public SocketsServerBuilder ListenAnyIP(int port, Action<IConnectionBuilder> configure, Action<ServerLogPolicy>? configureLogPolicy = default) =>
            Listen(IPAddress.Any, port, configure, configureLogPolicy);

        public SocketsServerBuilder ListenUnixSocket(string socketPath, Action<IConnectionBuilder> configure, Action<ServerLogPolicy>? configureLogPolicy = default) =>
            Listen(new UnixDomainSocketEndPoint(socketPath), configure, configureLogPolicy);

        internal void Apply(ServerBuilder builder)
        {
            var socketTransportFactory = new SocketTransportFactory(Microsoft.Extensions.Options.Options.Create(Options),
                builder.ApplicationServices.GetLoggerFactory());

            foreach (var binding in _bindings)
            {
                if (binding.EndPoint is null) {
                    var connectionBuilder = new ConnectionBuilder(builder.ApplicationServices);
                    binding.Application(connectionBuilder);
                    builder.ListenLocalhost(binding.Port, socketTransportFactory, connectionBuilder.Build(), binding.LogPolicy);
                }
                else builder.Listen(binding.EndPoint, socketTransportFactory, binding.Application, binding.LogPolicy);
            }
        }
    }
}
