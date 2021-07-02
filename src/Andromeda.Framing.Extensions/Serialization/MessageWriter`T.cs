using Andromeda.Serialization;

namespace Andromeda.Framing
{
    public abstract class MessageWriter<TMeta> : MessageWriter where TMeta : class, IFrameMetadata
    {
        protected MessageWriter(IMetadataEncoder encoder, ISerializer serializer) : base(encoder, serializer)
        {
        }

        protected abstract TMeta GetMetadataOf<T>(in T message);
        protected abstract TMeta CreateCopyOf(TMeta metadata, int newLength);

        protected override IFrameMetadata GetFrameMetadataOf<T>(in T message) => GetMetadataOf(in message);
        protected override IFrameMetadata CreateCopyOf(IFrameMetadata metadata, int newLength) => CreateCopyOf((TMeta) metadata, newLength);
    }
}
