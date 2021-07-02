namespace Andromeda.Framing
{
    public interface IMessageReader<TMeta> : IMessageReader where TMeta : class, IFrameMetadata
    {
        bool TryDecode<T>(in Frame<TMeta> frame, T message);
    }
}
