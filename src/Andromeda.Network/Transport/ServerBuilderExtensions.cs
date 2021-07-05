using Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System;

namespace Andromeda.Network
{
    public static partial class ServerBuilderExtensions
    {
        public static ServerBuilder UseSockets(this ServerBuilder serverBuilder, Action<SocketsServerBuilder> configure)
        {
            var socketsBuilder = new SocketsServerBuilder();
            configure(socketsBuilder);

            socketsBuilder.Apply(serverBuilder);
            return serverBuilder;
        }

        public static ClientBuilder UseSockets(this ClientBuilder clientBuilder, ILoggerFactory? factory = default) => clientBuilder.UseConnectionFactory(
            new SocketConnectionFactory(Options.Create(new SocketTransportOptions()), factory ?? NullLoggerFactory.Instance));
    }
}
