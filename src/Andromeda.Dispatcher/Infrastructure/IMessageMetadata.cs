namespace Andromeda.Framing
{
    /// <summary>
    /// Represent an id-prefixed frame metadata.
    /// </summary>
    public interface IMessageMetadata : IFrameMetadata
    {
        int MessageId { get; }
    }
}
