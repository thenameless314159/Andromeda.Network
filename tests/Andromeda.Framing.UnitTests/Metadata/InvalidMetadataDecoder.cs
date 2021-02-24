using System.Buffers;

namespace Andromeda.Framing.UnitTests.Metadata
{
    public class InvalidMetadataDecoder : IMetadataDecoder
    {
        public bool TryParse(ref SequenceReader<byte> input, out IFrameMetadata? metadata)
        {
            metadata = new DefaultFrameMetadata(-1);
            return true;
        }
    }
}
