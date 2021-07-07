using System;
using Andromeda.Dispatcher.Infrastructure;
using Andromeda.Dispatcher.Handlers.Actions;

namespace Andromeda.Dispatcher.Handlers
{
    public abstract class HandlerBase
    {
        private IServiceProvider? _services;
        public IServiceProvider RequestServices {
            get => _services ?? NullServiceProvider.Instance;
            set => _services = value;
        }

        private SenderContext? _context;
        public virtual SenderContext Context {
            get => _context ?? throw new InvalidOperationException();
            set => _context = value;
        }

        public virtual IHandlerAction Send<T>(T message) => new SendMessageAction<T>(message);
        public virtual IHandlerAction Abort(string? reason = default, Exception? innerException = default) => new AbortAction(reason, innerException);
        public virtual IHandlerAction SendAndAbort<T>(T message, string? reason = default, Exception? innerException = default) => 
            new SendMessageAndAbortAction<T>(message, reason, innerException);
    }
}
