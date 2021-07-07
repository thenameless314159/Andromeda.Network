using Andromeda.Framing;

namespace Andromeda.Dispatcher.Handlers
{
    public abstract class FrameHandler<TMeta> : FrameHandler where TMeta : class, IFrameMetadata
    {
        private Frame<TMeta>? _frame;
        public new Frame<TMeta> Request {
            get => _frame ?? Frame<TMeta>.Empty;
            set => _frame = value;
        }
    }
}
