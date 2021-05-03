using Andromeda.Protocol.Attributes;

namespace Andromeda.Protocol.CustomLengthPrefixed
{
    [NetworkMessage(5)]
    public record CommandMessage
    {
        public string Command { get; set; }

        public CommandMessage(string command) => Command = command;
        public CommandMessage() => Command = string.Empty;
    }
}
