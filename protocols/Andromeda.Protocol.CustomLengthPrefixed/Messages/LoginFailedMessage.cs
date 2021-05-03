using Andromeda.Protocol.Attributes;

namespace Andromeda.Protocol.CustomLengthPrefixed
{
    [NetworkMessage(3)]
    public record LoginFailedMessage
    {
        public string Reason { get; set; }

        public LoginFailedMessage(string reason) => Reason = reason;
        public LoginFailedMessage() => Reason = string.Empty;
    }
}
