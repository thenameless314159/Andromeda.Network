using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;

namespace Andromeda.Dispatcher.Handlers.Actions
{
    public class AbortAction : IHandlerAction
    {
        public AbortAction(string? reason = default, Exception? innerException = default) =>
            (Reason, InnerException) = (reason, innerException);

        public Exception? InnerException { get; init; }
        public string? Reason { get; init; }

        public ValueTask ExecuteAsync(SenderContext context)
        {
            if(!string.IsNullOrWhiteSpace(Reason)) {
                context.Abort(InnerException is not null
                    ? new ConnectionAbortedException(Reason, InnerException)
                    : new ConnectionAbortedException(Reason));

                return default;
            }

            if (InnerException is not null) {
                context.Abort(new ConnectionAbortedException(string.Empty, InnerException));
                return default;
            }

            context.Abort();
            return default;
        }
    }
}
