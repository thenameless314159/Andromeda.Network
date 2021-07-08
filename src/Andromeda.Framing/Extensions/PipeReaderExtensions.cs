using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Andromeda.Framing
{
    /// <summary>
    /// Extension methods to read frames of any <see cref="PipeReader"/> and to construct <see cref="IFrameDecoder"/>.
    /// </summary>
    public static class PipeReaderExtensions
    {
        /// <summary>
        /// Create an <see cref="IFrameDecoder{TMetadata}"/> from the specified <see cref="PipeReader"/> using the provided <see cref="MetadataDecoder{TMetadata}"/>.
        /// </summary>
        /// <typeparam name="TMetadata">The specific <see cref="IFrameMetadata"/>.</typeparam>
        /// <param name="r">The pipe reader.</param>
        /// <param name="decoder">The metadata decoder.</param>
        /// <returns>An <see cref="IFrameDecoder{TMetadata}"/> instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IFrameDecoder<TMetadata> AsFrameDecoder<TMetadata>(this PipeReader r, MetadataDecoder<TMetadata> decoder)
            where TMetadata : class, IFrameMetadata => new PipeFrameDecoder<TMetadata>(r, decoder);

        /// <summary>
        /// Create an <see cref="IFrameDecoder{TMetadata}"/> from the specified <see cref="PipeReader"/> using the provided <see cref="MetadataDecoder{TMetadata}"/>.
        /// </summary>
        /// <typeparam name="TMetadata">The specific <see cref="IFrameMetadata"/>.</typeparam>
        /// <param name="r">The pipe reader.</param>
        /// <param name="decoder">The metadata decoder.</param>
        /// <returns>An <see cref="IFrameDecoder{TMetadata}"/> instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IFrameDecoder<TMetadata> AsFrameDecoder<TMetadata>(this PipeReader r, MetadataParser<TMetadata> decoder)
            where TMetadata : class, IFrameMetadata => new PipeFrameDecoder<TMetadata>(r, decoder);

        /// <summary>
        /// Create an <see cref="IFrameDecoder"/> from the specified <see cref="PipeReader"/> using the provided <see cref="IMetadataDecoder"/>.
        /// </summary>
        /// <param name="r">The pipe reader.</param>
        /// <param name="decoder">The metadata decoder.</param>
        /// <returns>An <see cref="IFrameDecoder"/> instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IFrameDecoder AsFrameDecoder(this PipeReader r, IMetadataDecoder decoder) => new PipeFrameDecoder(r, decoder);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static IFrameDecoder<TMetadata> AsFrameDecoder<TMetadata>(this PipeReader r, IMetadataDecoder decoder)
            where TMetadata : class, IFrameMetadata => new PipeFrameDecoder<TMetadata>(r, decoder);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryAdvanceTo(this PipeReader r, SequencePosition consumed)
        {
            // Suppress exceptions if the pipe has already been completed
            try { r.AdvanceTo(consumed, consumed); return true; }
            catch (OperationCanceledException) { return false; }
            catch (InvalidOperationException) { return false; }
        }
    }
}
