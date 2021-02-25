using System;
using System.Runtime.CompilerServices;

namespace Andromeda.Framing
{
    /// <summary>
    /// Extensions methods of <see cref="Frame"/> and <see cref="Frame{TMetadata}"/> to allow
    /// conversion between them.
    /// </summary>
    public static class FrameExtensions
    {
        /// <summary>
        /// Convert an untyped <see cref="Frame"/> to a typed <see cref="Frame{TMetadata}"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="IFrameMetadata"/> type.</typeparam>
        /// <param name="frame">The untyped <see cref="Frame"/> to convert.</param>
        /// <returns>A typed copy of the <see cref="Frame{TMetadata}"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Frame<T> AsTyped<T>(this Frame frame) where T : class, IFrameMetadata => new(frame.Payload,
            frame.Metadata as T ?? throw new ArgumentException("Invalid frame " + nameof(IFrameMetadata) + " type !", nameof(frame)));

        /// <summary>
        /// Convert a typed <see cref="Frame{TMetadata}"/> to an untyped <see cref="Frame"/>.
        /// </summary>
        /// <typeparam name="T">The <see cref="IFrameMetadata"/> type.</typeparam>
        /// <param name="frame">The typed <see cref="Frame{TMetadata}"/> to convert.</param>
        /// <returns>An untyped copy pf <see cref="Frame"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Frame AsUntyped<T>(this Frame<T> frame) where T : class, IFrameMetadata => new(frame.Payload, frame.Metadata);
    }
}
