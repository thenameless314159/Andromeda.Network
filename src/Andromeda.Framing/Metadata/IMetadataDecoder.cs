using System.Buffers;

namespace Andromeda.Framing
{
    /// <summary>
    /// Contains logic to parse <see cref="IFrameMetadata"/> of a specific type of <see cref="Frame"/> or <see cref="Frame{TMetadata}"/>.
    /// </summary>
    public interface IMetadataDecoder
    {
        /// <summary>
        /// Attempt to parse <see cref="IFrameMetadata"/> of a <see cref="Frame"/> or <see cref="Frame{TMetadata}"/>
        /// from the specified input.
        /// </summary>
        /// <param name="input">The input buffer.</param>
        /// <param name="metadata">The parsed metadata.</param>
        /// <returns>Whether the parsing was successful or not.</returns>
        bool TryParse(ref SequenceReader<byte> input, out IFrameMetadata? metadata);

        /// <summary>
        /// Get the length of the specified <see cref="IFrameMetadata"/>.
        /// </summary>
        /// <param name="metadata">The metadata.</param>
        /// <returns>The length of the decoded metadata.</returns>
        int GetMetadataLength(IFrameMetadata metadata);
    }
}
