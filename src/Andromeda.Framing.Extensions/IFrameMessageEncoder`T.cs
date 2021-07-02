namespace Andromeda.Framing
{
    public interface IFrameMessageEncoder<TMeta> : IFrameMessageEncoder, IFrameEncoder<TMeta>
        where TMeta : class, IFrameMetadata
    {
    }
}
