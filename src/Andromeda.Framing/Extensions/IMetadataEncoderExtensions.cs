using System.Buffers;

namespace Andromeda.Framing
{
    public static class IMetadataEncoderExtensions
    {
        public static void WriteMetadata(this IMetadataEncoder encoder, IBufferWriter<byte> writer, in Frame frame)
        {
            var metaLength = encoder.GetLength(frame.Metadata);
            var metaSpan = writer.GetSpan(metaLength);

            encoder.Write(ref metaSpan, frame.Metadata);
            writer.Advance(metaLength);
        }
    }
}
