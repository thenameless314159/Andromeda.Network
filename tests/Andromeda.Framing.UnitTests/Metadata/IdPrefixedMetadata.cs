namespace Andromeda.Framing.UnitTests.Metadata
{
    public record IdPrefixedMetadata(int MessageId, int Length) : IFrameMetadata
    {
    }
}
