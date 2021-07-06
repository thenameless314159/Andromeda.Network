using System.IO.Pipelines;
using System.Runtime.CompilerServices;

namespace Andromeda.Framing
{
    public static class PipeReaderExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IFrameMessageDecoder<TMetadata> AsFrameMessageDecoder<TMetadata>(this PipeReader r, IMetadataDecoder decoder, IMessageReader<TMetadata> reader)
            where TMetadata : class, IFrameMetadata => new PipeMessageDecoder<TMetadata>(r, decoder, reader);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IFrameMessageDecoder AsFrameMessageDecoder(this PipeReader r, IMetadataDecoder decoder, IMessageReader reader) => new PipeMessageDecoder(r, decoder, reader);
    }
}
