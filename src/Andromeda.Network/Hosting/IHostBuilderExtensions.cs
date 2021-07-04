using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace Andromeda.Network.Hosting
{
    public static class IHostBuilderExtensions
    {
        public static IHostBuilder ConfigureServer(this IHostBuilder builder, Action<ServerBuilder> configure) =>
            builder.ConfigureServices((_, services) =>
            {
                services.AddHostedService<ServerHostedService>();

                services.AddOptions<ServerHostedServiceOptions>()
                    .Configure<IServiceProvider>((options, sp) => {
                        options.ServerBuilder = new ServerBuilder(sp);
                        configure(options.ServerBuilder);
                    });
            });
    }
}
