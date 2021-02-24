using System;

namespace Andromeda.Framing
{
    public static class FrameExtensions
    {
        public static Frame<T> AsTyped<T>(this Frame frame) where T : class, IFrameMetadata => new(frame.Payload,
            frame.Metadata as T ?? throw new ArgumentException("Invalid frame " + nameof(IFrameMetadata) + " type !", nameof(frame)));

        public static Frame AsUntyped<T>(this Frame<T> frame) where T : class, IFrameMetadata => new(frame.Payload, frame.Metadata);
    }
}
