using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Andromeda.Framing
{
    public static class PipeWriterExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IFrameMessageEncoder<TMetadata> AsFrameMessageEncoderOf<TMetadata>(this PipeWriter w, IMetadataEncoder encoder,
            IMessageWriter writer, SemaphoreSlim? singleWriter = default) where TMetadata : class, IFrameMetadata =>
            new PipeMessageEncoder<TMetadata>(w, encoder, writer, singleWriter);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IFrameMessageEncoder<TMetadata> AsFrameMessageEncoder<TMetadata>(this PipeWriter w, MetadataEncoder<TMetadata> encoder,
            IMessageWriter writer, SemaphoreSlim? singleWriter = default) where TMetadata : class, IFrameMetadata =>
            new PipeMessageEncoder<TMetadata>(w, encoder, writer, singleWriter);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IFrameMessageEncoder AsFrameMessageEncoder(this PipeWriter w, IMetadataEncoder encoder, IMessageWriter writer, 
            SemaphoreSlim? singleWriter = default) => new PipeMessageEncoder(w, encoder, writer, singleWriter);
    }
}
