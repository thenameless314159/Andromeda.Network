using System.Buffers;

namespace Andromeda.Framing
{
    public interface IMetadataDecoder
    {
        bool TryParse(ref SequenceReader<byte> input, out IFrameMetadata? metadata);
    }
}
