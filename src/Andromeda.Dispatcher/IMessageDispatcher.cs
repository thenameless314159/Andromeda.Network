using Andromeda.Framing;

namespace Andromeda.Dispatcher
{
    public interface IMessageDispatcher<TMeta> : IFrameDispatcher<TMeta>
        where TMeta : class, IMessageMetadata
    {
    }
}
