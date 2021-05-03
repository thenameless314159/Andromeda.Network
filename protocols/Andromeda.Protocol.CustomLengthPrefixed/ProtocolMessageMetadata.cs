using Andromeda.Framing;

namespace Andromeda.Protocol
{
    public record ProtocolMessageMetadata(ushort MessageId, int Length) : IFrameMetadata
    {
    }
}
