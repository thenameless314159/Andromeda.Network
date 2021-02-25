namespace Andromeda.Framing
{
    /// <summary>
    /// A default implementation of <see cref="IFrameMetadata"/>.
    /// </summary>
    public record DefaultFrameMetadata : IFrameMetadata
    {
        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="length">The length of the relative <see cref="Frame"/> payload.</param>
        public DefaultFrameMetadata(int length) => Length = length;

        /// <inheritdoc />
        public int Length { get; init; }
    }
}
