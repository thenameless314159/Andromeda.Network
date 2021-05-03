using Andromeda.Protocol.Attributes;

namespace Andromeda.Protocol.CustomLengthPrefixed
{
    [NetworkMessage(1)]
    public record HandshakeMessage
    {
        public string ProtocolRequired { get; set; }

        public HandshakeMessage(string protocolRequired) => ProtocolRequired = protocolRequired;
        public HandshakeMessage() => ProtocolRequired = string.Empty;
    }
}
