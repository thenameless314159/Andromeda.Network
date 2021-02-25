using System.IO.Pipelines;
using System.Runtime.CompilerServices;

namespace Andromeda.Framing
{
    /// <summary>
    /// Extension methods of <see cref="IDuplexPipe"/> to construct <see cref="IFrameEncoder"/> or <see cref="IFrameDecoder"/>.
    /// </summary>
    public static class IDuplexPipeExtensions
    {
        /// <summary>
        /// Create an <see cref="IFrameDecoder"/> and <see cref="IFrameEncoder"/> pair from the current
        /// <see cref="IDuplexPipe"/> using the provided <see cref="IMetadataParser"/>.
        /// </summary>
        /// <param name="pipe">The duplex pipe.</param>
        /// <param name="parser">The metadata parser.</param>
        /// <param name="synchronizeReader">Whether the access to the pipe reader should be thread synchronized or not.</param>
        /// <param name="synchronizeWriter">Whether the access to the pipe writer should be thread synchronized or not.</param>
        /// <returns>An <see cref="IFrameDecoder"/> and <see cref="IFrameEncoder"/> pair.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (IFrameDecoder, IFrameEncoder) AsFrameDecoderEncoderPair(this IDuplexPipe pipe, IMetadataParser parser,
            bool synchronizeReader = false, bool synchronizeWriter = true) =>
            (pipe.Input.AsFrameDecoder(parser, synchronizeReader), pipe.Output.AsFrameEncoder(parser, synchronizeWriter));

        /// <summary>
        /// Create an <see cref="IFrameDecoder"/> and <see cref="IFrameEncoder"/> pair from the current
        /// <see cref="IDuplexPipe"/> using the provided <see cref="IMetadataDecoder"/> and <see cref="IMetadataEncoder"/>.
        /// </summary>
        /// <param name="pipe">The duplex pipe.</param>
        /// <param name="decoder">The metadata decoder.</param>
        /// <param name="encoder">The metadata encoder.</param>
        /// <param name="synchronizeReader">Whether the access to the pipe reader should be thread synchronized or not.</param>
        /// <param name="synchronizeWriter">Whether the access to the pipe writer should be thread synchronized or not.</param>
        /// <returns>An <see cref="IFrameDecoder"/> and <see cref="IFrameEncoder"/> pair.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (IFrameDecoder, IFrameEncoder) AsFrameDecoderEncoderPair(this IDuplexPipe pipe, IMetadataDecoder decoder, 
            IMetadataEncoder encoder, bool synchronizeReader = false, bool synchronizeWriter = true) =>
            (pipe.Input.AsFrameDecoder(decoder, synchronizeReader), pipe.Output.AsFrameEncoder(encoder, synchronizeWriter));

        /// <summary>
        /// Create an <see cref="IFrameDecoder{TMetadata}"/> and <see cref="IFrameEncoder{TMetadata}"/> pair from the current
        /// <see cref="IDuplexPipe"/> using the provided <see cref="MetadataParser{TMetadata}"/>.
        /// </summary>
        /// <param name="pipe">The duplex pipe.</param>
        /// <param name="parser">The metadata parser.</param>
        /// <param name="synchronizeReader">Whether the access to the pipe reader should be thread synchronized or not.</param>
        /// <param name="synchronizeWriter">Whether the access to the pipe writer should be thread synchronized or not.</param>
        /// <returns>An <see cref="IFrameDecoder{TMetadata}"/> and <see cref="IFrameEncoder{TMetadata}"/> pair.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (IFrameDecoder<TMeta>, IFrameEncoder<TMeta>) AsFrameDecoderEncoderPair<TMeta>(this IDuplexPipe pipe, MetadataParser<TMeta> parser,
            bool synchronizeReader = false, bool synchronizeWriter = true) where TMeta : class, IFrameMetadata =>
            (pipe.Input.AsFrameDecoder<TMeta>(parser, synchronizeReader), pipe.Output.AsFrameEncoder<TMeta>(parser, synchronizeWriter));

        /// <summary>
        /// Create an <see cref="IFrameDecoder{TMetadata}"/> and <see cref="IFrameEncoder{TMetadata}"/> pair from the current
        /// <see cref="IDuplexPipe"/> using the provided <see cref="MetadataDecoder{TMetadata}"/> and <see cref="MetadataEncoder{TMetadata}"/>.
        /// </summary>
        /// <param name="pipe">The duplex pipe.</param>
        /// <param name="decoder">The metadata decoder.</param>
        /// <param name="encoder">The metadata encoder.</param>
        /// <param name="synchronizeReader">Whether the access to the pipe reader should be thread synchronized or not.</param>
        /// <param name="synchronizeWriter">Whether the access to the pipe writer should be thread synchronized or not.</param>
        /// <returns>An <see cref="IFrameDecoder{TMetadata}"/> and <see cref="IFrameEncoder{TMetadata}"/> pair.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (IFrameDecoder<TMeta>, IFrameEncoder<TMeta>) AsFrameDecoderEncoderPair<TMeta>(this IDuplexPipe pipe, MetadataDecoder<TMeta> decoder,
            MetadataEncoder<TMeta> encoder, bool synchronizeReader = false, bool synchronizeWriter = true) where TMeta : class, IFrameMetadata =>
            (pipe.Input.AsFrameDecoder(decoder, synchronizeReader), pipe.Output.AsFrameEncoder(encoder, synchronizeWriter));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static (IFrameDecoder, IFrameEncoder) AsFrameDecoderEncoderPair<TMeta>(this IDuplexPipe pipe, IMetadataParser parser,
            bool synchronizeReader = false, bool synchronizeWriter = true) where TMeta : class, IFrameMetadata =>
            (pipe.Input.AsFrameDecoder<TMeta>(parser, synchronizeReader), pipe.Output.AsFrameEncoder<TMeta>(parser, synchronizeWriter));
    }
}
