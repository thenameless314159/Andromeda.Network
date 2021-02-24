using System.IO.Pipelines;

namespace Andromeda.Framing.Extensions
{
    public static class IDuplexPipeExtensions
    {
        public static (IFrameDecoder, IFrameEncoder) AsFrameDecoderEncoderPair(this IDuplexPipe pipe, IMetadataParser parser,
            bool synchronizeReader = false, bool synchronizeWriter = true) =>
            (pipe.Input.AsFrameDecoder(parser, synchronizeReader), pipe.Output.AsFrameEncoder(parser, synchronizeWriter));

        public static (IFrameDecoder, IFrameEncoder) AsFrameDecoderEncoderPair(this IDuplexPipe pipe, IMetadataDecoder decoder, 
            IMetadataEncoder encoder, bool synchronizeReader = false, bool synchronizeWriter = true) =>
            (pipe.Input.AsFrameDecoder(decoder, synchronizeReader), pipe.Output.AsFrameEncoder(encoder, synchronizeWriter));

        public static (IFrameDecoder<TMeta>, IFrameEncoder<TMeta>) AsFrameDecoderEncoderPair<TMeta>(this IDuplexPipe pipe, MetadataParser<TMeta> parser,
            bool synchronizeReader = false, bool synchronizeWriter = true) where TMeta : class, IFrameMetadata =>
            (pipe.Input.AsFrameDecoder<TMeta>(parser, synchronizeReader), pipe.Output.AsFrameEncoder<TMeta>(parser, synchronizeWriter));

        public static (IFrameDecoder<TMeta>, IFrameEncoder<TMeta>) AsFrameDecoderEncoderPair<TMeta>(this IDuplexPipe pipe, MetadataDecoder<TMeta> decoder,
            MetadataEncoder<TMeta> encoder, bool synchronizeReader = false, bool synchronizeWriter = true) where TMeta : class, IFrameMetadata =>
            (pipe.Input.AsFrameDecoder(decoder, synchronizeReader), pipe.Output.AsFrameEncoder(encoder, synchronizeWriter));
    }
}
