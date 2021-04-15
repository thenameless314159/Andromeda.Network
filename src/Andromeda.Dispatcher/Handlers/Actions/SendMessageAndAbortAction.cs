using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;

namespace Andromeda.Dispatcher.Handlers.Actions
{
    public class SendMessageAndAbortAction<T> : IHandlerAction
    {
        private static readonly Type _lifetimeFeature = typeof(IConnectionLifetimeNotificationFeature);
        public SendMessageAndAbortAction(T message, string? reason = default, Exception? innerException = default) =>
            (Reason, InnerException, _message) = (reason, innerException, message);
        
        public T Message { get => _message; init => _message = value; }
        public Exception? InnerException { get; init; }
        public string? Reason { get; init; }
        private readonly T _message;
        public ValueTask ExecuteAsync(SenderContext context)
        {
            var sendAsync = context.Proxy.SendAsync(in _message);
            if (!sendAsync.IsCompletedSuccessfully) return awaitSendAndAbort(sendAsync);
            Abort(); return default;

            void Abort()
            {
                if (!string.IsNullOrWhiteSpace(Reason)) {
                    context.Client.Abort(InnerException is not null
                        ? new ConnectionAbortedException(Reason, InnerException)
                        : new ConnectionAbortedException(Reason));

                    return;
                }

                if (InnerException is not null) {
                    context.Client.Abort(new ConnectionAbortedException(string.Empty, InnerException));
                    return;
                }

                // Try to close the connection gracefully first
                var lifetimeFeature = context.Client.Features[_lifetimeFeature];
                if (lifetimeFeature is not IConnectionLifetimeNotificationFeature feature) return;
                feature.RequestClose();
            }
            async ValueTask awaitSendAndAbort(ValueTask send) {
                try { await send.ConfigureAwait(false); }
                finally { Abort(); }
            }
        }
    }
}
