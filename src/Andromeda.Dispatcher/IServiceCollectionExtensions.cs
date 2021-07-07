using System;
using Andromeda.Framing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Andromeda.Dispatcher
{
    public static class IServiceCollectionExtensions
    {
        public static IServiceCollection AddDefaultMessageDispatcher<TMeta>(this IServiceCollection services, Action<IMessageDispatcherBuilder> configure,
            IMessageReader<TMeta>? reader = default) where TMeta : class, IMessageMetadata
        {
            services.TryAddSingleton<IMessageDispatcher<TMeta>>(sp => {
                var r = reader ??= sp.GetRequiredService<IMessageReader<TMeta>>();
                var dispatcher = new DefaultMessageDispatcher<TMeta>(sp, r);
                configure(dispatcher);
                return dispatcher;
            });

            return services;
        }
    }
}
