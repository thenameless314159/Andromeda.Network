using Andromeda.Framing;

namespace Andromeda.Dispatcher.Handlers
{
    public abstract class FrameHandler : HandlerBase
    {
        private Frame? _frame;
        public virtual Frame Request {
            get => _frame ?? Frame.Empty;
            set => _frame = value;
        }
    }
}
