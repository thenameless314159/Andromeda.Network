using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Connections;
using Andromeda.Network.Internal;
using System.Collections.Generic;
using System.Net;
using System;

namespace Andromeda.Network
{
    public class ServerBuilder
    {
        public ServerBuilder(IServiceProvider serviceProvider) => ApplicationServices = serviceProvider;
        public ServerBuilder() : this(EmptyServiceProvider.Instance) { }

        public IList<ServerBinding> Bindings { get; } = new List<ServerBinding>();
        public TimeSpan HeartBeatInterval { get; set; } = TimeSpan.FromSeconds(1);
        public IServiceProvider ApplicationServices { get; }

        public NetworkServer Build() => new(this);
    }

    public static partial class ServerBuilderExtensions
    {
        public static ServerBuilder ListenLocalhost<TTransport>(this ServerBuilder builder, int port, Action<IConnectionBuilder> configure,
            Action<ServerLogPolicy>? configureLogPolicy = default) where TTransport : IConnectionListenerFactory =>
            builder.ListenLocalhost(port, ActivatorUtilities.CreateInstance<TTransport>(builder.ApplicationServices), configure, configureLogPolicy);

        public static ServerBuilder ListenLocalhost<TTransport>(this ServerBuilder builder, int port, ConnectionDelegate application,
            Action<ServerLogPolicy>? configureLogPolicy = default) where TTransport : IConnectionListenerFactory =>
            builder.ListenLocalhost(port, ActivatorUtilities.CreateInstance<TTransport>(builder.ApplicationServices), application, configureLogPolicy);

        public static ServerBuilder Listen<TTransport>(this ServerBuilder builder, EndPoint endPoint, Action<IConnectionBuilder> configure,
            Action<ServerLogPolicy>? configureLogPolicy = default) where TTransport : IConnectionListenerFactory =>
            builder.Listen(endPoint, ActivatorUtilities.CreateInstance<TTransport>(builder.ApplicationServices), configure, configureLogPolicy);

        public static ServerBuilder Listen<TTransport>(this ServerBuilder builder, EndPoint endPoint, ConnectionDelegate application,
            Action<ServerLogPolicy>? configureLogPolicy = default) where TTransport : IConnectionListenerFactory =>
            builder.Listen(endPoint, ActivatorUtilities.CreateInstance<TTransport>(builder.ApplicationServices), application, configureLogPolicy);

        public static ServerBuilder ListenLocalhost(this ServerBuilder builder, int port, IConnectionListenerFactory connectionListenerFactory,
            Action<IConnectionBuilder> configure, Action<ServerLogPolicy>? configureLogPolicy = default)
        {
            var connectionBuilder = new ConnectionBuilder(builder.ApplicationServices);
            configure(connectionBuilder);

            var logPolicy = new ServerLogPolicy();
            configureLogPolicy?.Invoke(logPolicy);

            builder.Bindings.Add(new LocalHostBinding(port, connectionBuilder.Build(), logPolicy, connectionListenerFactory));
            return builder;
        }

        public static ServerBuilder Listen(this ServerBuilder builder, EndPoint endPoint, IConnectionListenerFactory connectionListenerFactory,
            Action<IConnectionBuilder> configure, Action<ServerLogPolicy>? configureLogPolicy = default)
        {
            var connectionBuilder = new ConnectionBuilder(builder.ApplicationServices);
            configure(connectionBuilder);

            var logPolicy = new ServerLogPolicy();
            configureLogPolicy?.Invoke(logPolicy);

            builder.Bindings.Add(new ServerBinding(endPoint, connectionBuilder.Build(), logPolicy, connectionListenerFactory));
            return builder;
        }

        public static ServerBuilder ListenLocalhost(this ServerBuilder builder, int port, IConnectionListenerFactory connectionListenerFactory,
            ConnectionDelegate application, Action<ServerLogPolicy>? configureLogPolicy = default)
        {
            var logPolicy = new ServerLogPolicy();
            configureLogPolicy?.Invoke(logPolicy);

            builder.Bindings.Add(new LocalHostBinding(port, application, logPolicy, connectionListenerFactory));
            return builder;
        }

        public static ServerBuilder Listen(this ServerBuilder builder, EndPoint endPoint, IConnectionListenerFactory connectionListenerFactory,
            ConnectionDelegate application, Action<ServerLogPolicy>? configureLogPolicy = default)
        {
            var logPolicy = new ServerLogPolicy();
            configureLogPolicy?.Invoke(logPolicy);

            builder.Bindings.Add(new ServerBinding(endPoint, application, logPolicy, connectionListenerFactory));
            return builder;
        }
    }
}
