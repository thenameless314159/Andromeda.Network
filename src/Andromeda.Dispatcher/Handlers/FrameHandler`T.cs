using Andromeda.Framing;

namespace Andromeda.Dispatcher.Handlers
{
    public abstract class FrameHandler<TMeta> : FrameHandler where TMeta : class, IFrameMetadata
    {
        public new Frame<TMeta> Request {
            get => base.Request.AsTyped<TMeta>();
            set => base.Request = value.AsUntyped();
        }
    }
}
