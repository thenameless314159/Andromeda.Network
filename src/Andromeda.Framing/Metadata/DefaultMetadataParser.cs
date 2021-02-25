using System;
using System.Buffers;

namespace Andromeda.Framing
{
    /// <summary>
    /// A default implementation of <see cref="IMetadataParser"/> who can be constructed
    /// using both <see cref="IMetadataDecoder"/> and <see cref="IMetadataEncoder"/> arguments.
    /// </summary>
    public sealed class DefaultMetadataParser : IMetadataParser
    {
        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="decoder">The metadata decoder.</param>
        /// <param name="encoder">The metadata encoder.</param>
        public DefaultMetadataParser(IMetadataDecoder decoder, IMetadataEncoder encoder) =>
            (_decoder, _encoder) = (decoder, encoder);

        private readonly IMetadataDecoder _decoder;
        private readonly IMetadataEncoder _encoder;

        /// <inheritdoc />
        public bool TryParse(ref SequenceReader<byte> input, out IFrameMetadata? metadata) => _decoder
            .TryParse(ref input, out metadata);

        /// <inheritdoc />
        public int GetLength(IFrameMetadata metadata) => _encoder.GetLength(metadata);

        /// <inheritdoc />
        public int GetMetadataLength(IFrameMetadata metadata) => _decoder.GetMetadataLength(metadata);

        /// <inheritdoc />
        public void Write(ref Span<byte> span, IFrameMetadata metadata) => _encoder.Write(ref span, metadata);
    }
}
