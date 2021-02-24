using System;
using System.Buffers;

namespace Andromeda.Framing
{
    public sealed class DefaultMetadataParser : IMetadataParser
    {
        public DefaultMetadataParser(IMetadataDecoder decoder, IMetadataEncoder encoder) =>
            (_decoder, _encoder) = (decoder, encoder);

        private readonly IMetadataDecoder _decoder;
        private readonly IMetadataEncoder _encoder;

        public bool TryParse(ref SequenceReader<byte> input, out IFrameMetadata? metadata) => _decoder
            .TryParse(ref input, out metadata);

        public int GetLength(IFrameMetadata metadata) => _encoder.GetLength(metadata);
        public void Write(ref Span<byte> span, IFrameMetadata metadata) => _encoder.Write(ref span, metadata);
    }
}
