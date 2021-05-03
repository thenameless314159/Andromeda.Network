using Andromeda.Protocol.Attributes;

namespace Andromeda.Protocol.CustomLengthPrefixed
{
    [NetworkMessage(2)]
    public record LoginMessage
    {
        public string Username { get; set; }
        public string Password { get; set; }

        public LoginMessage(string username, string password)
        {
            Username = username;
            Password = password;
        }

        public LoginMessage()
        {
            Username = string.Empty;
            Password = string.Empty;
        }
    }
}
