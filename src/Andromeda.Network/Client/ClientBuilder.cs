using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Andromeda.Network.Internal;
using Microsoft.AspNetCore.Connections;

namespace Andromeda.Network
{
    public class ClientBuilder : IConnectionBuilder
    {
        private readonly ConnectionBuilder _connectionBuilder;

        public ClientBuilder() : this(EmptyServiceProvider.Instance)
        {
        }

        public ClientBuilder(IServiceProvider serviceProvider) =>
            _connectionBuilder = new ConnectionBuilder(serviceProvider);

        private IConnectionFactory ConnectionFactory { get; set; } = new ThrowConnectionFactory();

        public IServiceProvider ApplicationServices => _connectionBuilder.ApplicationServices;

        public Client Build()
        {
            // Middleware currently a single linear execution flow without a return value.
            // We need to return the connection when it reaches the innermost middleware (D in this case)
            // Then we need to wait until dispose is called to unwind that pipeline.

            // A -> 
            //      B -> 
            //           C -> 
            //                D
            //           C <-
            //      B <-
            // A <-

            _connectionBuilder.Run(connection =>
            {
                // REVIEW: Do we throw in this case? It's edgy but possible to call next with a different
                // connection delegate that originally given
                if (connection is not ConnectionContextWithDelegate connectionContextWithDelegate)
                    return Task.CompletedTask;
                
                connectionContextWithDelegate.Initialized.TrySetResult(connectionContextWithDelegate);

                // This task needs to stay around until the connection is disposed
                // only then can we unwind the middleware chain
                return connectionContextWithDelegate.ExecutionTask;
            });

            var application = _connectionBuilder.Build();
            return new Client(ConnectionFactory, application);
        }

        public ClientBuilder UseConnectionFactory(IConnectionFactory connectionFactory)
        {
            ConnectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            return this;
        }

        public ClientBuilder Use(Func<IConnectionFactory, IConnectionFactory> middleware)
        {
            ConnectionFactory = middleware(ConnectionFactory);
            return this;
        }

        public IConnectionBuilder Use(Func<ConnectionDelegate, ConnectionDelegate> middleware) =>
            _connectionBuilder.Use(middleware);

        ConnectionDelegate IConnectionBuilder.Build() => _connectionBuilder.Build();

        private class ThrowConnectionFactory : IConnectionFactory
        {
            public ValueTask<ConnectionContext> ConnectAsync(EndPoint endpoint, CancellationToken cancellationToken = default) =>
                throw new InvalidOperationException("No transport configured. Set the ConnectionFactory property.");
        }
    }
}
