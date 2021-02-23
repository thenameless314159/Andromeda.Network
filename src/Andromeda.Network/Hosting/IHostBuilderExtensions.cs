using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Andromeda.Network.Hosting
{
    public static class IHostBuilderExtensions
    {
        public static IHostBuilder ConfigureServer(this IHostBuilder builder, Action<ServerBuilder> configure) => builder.ConfigureServices((_, services) =>
        {
            services.AddHostedService<ServerHostedService>();
            services.TryAddSingleton(sp =>
            {
                var server = new ServerBuilder(sp);
                configure(server);

                return server.Build();
            });
        });
    }
}
