using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;

namespace Andromeda.Network.Middleware
{
    public static class IConnectionBuilderExtensions
    {
        /// <summary>
        /// Emits verbose logs for bytes read from and written to the connection.
        /// </summary>
        public static TBuilder UseConnectionLogging<TBuilder>(this TBuilder builder, string? loggerName = null, ILoggerFactory? loggerFactory = null) where TBuilder : IConnectionBuilder
        {
            loggerFactory ??= builder.ApplicationServices.GetRequiredService<ILoggerFactory>();
            var logger = loggerName == null ? loggerFactory.CreateLogger<LoggingConnectionMiddleware>() : loggerFactory.CreateLogger(loggerName);
            builder.Use(next => new LoggingConnectionMiddleware(next, logger).OnConnectionAsync);
            return builder;
        }
    }
}
