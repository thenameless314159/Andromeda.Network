using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Andromeda.Framing
{
    public static class IDuplexPipeExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (IFrameMessageDecoder, IFrameMessageEncoder) AsFrameMessageDecoderEncoderPair(this IDuplexPipe pipe, IMetadataParser parser,
            IMessageReader reader, IMessageWriter writer, SemaphoreSlim? singleWriter = default) => (
                pipe.Input.AsFrameMessageDecoder(parser, reader), 
                pipe.Output.AsFrameMessageEncoder(parser, writer, singleWriter));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (IFrameMessageDecoder<TMeta>, IFrameMessageEncoder<TMeta>) AsFrameMessageDecoderEncoderPair<TMeta>(this IDuplexPipe pipe, IMetadataParser parser,
            IMessageReader<TMeta> reader, IMessageWriter writer, SemaphoreSlim? singleWriter = default) where TMeta : class, IFrameMetadata =>
            (pipe.Input.AsFrameMessageDecoder(parser, reader), pipe.Output.AsFrameMessageEncoderOf<TMeta>(parser, writer, singleWriter));
    }
}
