namespace Andromeda.Framing
{
    public interface IMessageReader
    {
        bool TryParse<T>(in Frame frame, T message);
    }

    public interface IMessageReader<TMeta> : IMessageReader where TMeta : class, IFrameMetadata
    {
        new bool TryParse<T>(in Frame frame, T message) { var typed = frame.AsTyped<TMeta>(); return TryParse(in typed, message); }
        bool TryParse<T>(in Frame<TMeta> frame, T message);
    }
}
