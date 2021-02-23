using Microsoft.Extensions.Logging;

namespace Andromeda.Network
{
    public record ServerLogPolicy
    {
        public bool LogStartedListening { get; set; } = false;
        public bool LogStoppedListening { get; set; } = false;
        public bool LogConnectionAborted { get; set; } = false;
        public bool LogConnectionAccepted { get; set; } = false;
        public bool LogConnectionCompleted { get; set; } = false;
        public LogLevel MessageLogLevel { get; set; } = LogLevel.None;
    }
}
