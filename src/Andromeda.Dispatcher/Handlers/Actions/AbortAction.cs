using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;

namespace Andromeda.Dispatcher.Handlers.Actions
{
    public class AbortAction : IHandlerAction
    {
        private static readonly Type _lifetimeFeature = typeof(IConnectionLifetimeNotificationFeature);
        public AbortAction(string? reason = default, Exception? innerException = default) =>
            (Reason, InnerException) = (reason, innerException);

        public Exception? InnerException { get; init; }
        public string? Reason { get; init; }

        public ValueTask ExecuteAsync(SenderContext context)
        {
            if(!string.IsNullOrWhiteSpace(Reason)) {
                context.Client.Abort(InnerException is not null
                    ? new ConnectionAbortedException(Reason, InnerException)
                    : new ConnectionAbortedException(Reason));

                return default;
            }

            if (InnerException is not null) {
                context.Client.Abort(new ConnectionAbortedException(string.Empty, InnerException));
                return default;
            }

            // Try to close the connection gracefully first
            var lifetimeFeature = context.Client.Features[_lifetimeFeature];
            if (lifetimeFeature is IConnectionLifetimeNotificationFeature feature) {
                feature.RequestClose();
                return default;
            }

            context.Client.Abort();
            return default;
        }
    }
}
