using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;

namespace Andromeda.Dispatcher.Handlers.Actions
{
    public class SendMessageAndAbortAction<T> : IHandlerAction
    {
        public SendMessageAndAbortAction(T message, string? reason = default, Exception? innerException = default) =>
            (Reason, InnerException, _message) = (reason, innerException, message);
        
        public T Message { get => _message; init => _message = value; }
        public Exception? InnerException { get; init; }
        public string? Reason { get; init; }
        private readonly T _message;

        public ValueTask ExecuteAsync(SenderContext context)
        {
            var sendAsync = context.SendAsync(in _message);
            if (!sendAsync.IsCompletedSuccessfully) return awaitSendAndAbort(sendAsync);
            Abort(); return default;

            void Abort() {
                if (!string.IsNullOrWhiteSpace(Reason)) {
                    context.Abort(InnerException is not null
                        ? new ConnectionAbortedException(Reason, InnerException)
                        : new ConnectionAbortedException(Reason));

                    return;
                }

                if (InnerException is not null) {
                    context.Abort(new ConnectionAbortedException(string.Empty, InnerException));
                    return;
                }

                context.Abort();
            }
            async ValueTask awaitSendAndAbort(ValueTask send) {
                try { await send.ConfigureAwait(false); }
                finally { Abort(); }
            }
        }
    }
}
