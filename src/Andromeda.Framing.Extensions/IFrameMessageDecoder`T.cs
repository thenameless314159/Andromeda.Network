namespace Andromeda.Framing
{
    public interface IFrameMessageDecoder<TMeta> : IFrameMessageDecoder, IFrameDecoder<TMeta>
        where TMeta : class, IFrameMetadata
    {
    }
}
