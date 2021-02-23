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
    }
}
