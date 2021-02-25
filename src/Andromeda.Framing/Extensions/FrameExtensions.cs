using System;
using System.Runtime.CompilerServices;

namespace Andromeda.Framing
{
    public static class FrameExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Frame<T> AsTyped<T>(this Frame frame) where T : class, IFrameMetadata => new(frame.Payload,
            frame.Metadata as T ?? throw new ArgumentException("Invalid frame " + nameof(IFrameMetadata) + " type !", nameof(frame)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Frame AsUntyped<T>(this Frame<T> frame) where T : class, IFrameMetadata => new(frame.Payload, frame.Metadata);
    }
}
