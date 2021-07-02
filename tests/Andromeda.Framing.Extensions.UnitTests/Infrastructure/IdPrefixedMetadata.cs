namespace Andromeda.Framing.Extensions.UnitTests.Infrastructure
{
    public record IdPrefixedMetadata(int MessageId, int Length) : IFrameMetadata
    {
    }
}
