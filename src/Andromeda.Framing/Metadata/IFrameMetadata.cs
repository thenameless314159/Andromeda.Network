namespace Andromeda.Framing
{
    /// <summary>
    /// Represent the metadata of a <see cref="Frame"/> or <see cref="Frame{TMetadata}"/>.
    /// </summary>
    public interface IFrameMetadata
    {
        /// <summary>
        /// Get the payload length of the current <see cref="Frame"/> or <see cref="Frame{TMetadata}"/>.
        /// </summary>
        int Length { get; }
    }
}
