using System;
using System.Buffers;

namespace Andromeda.Framing
{
    public readonly struct Frame
    {
        public static readonly Frame Empty = new(ReadOnlySequence<byte>.Empty, default!);
        public Frame(ReadOnlyMemory<byte> payload, IFrameMetadata metadata) =>
            (Payload, Metadata) = (new ReadOnlySequence<byte>(payload), metadata);

        public Frame(ReadOnlySequence<byte> payload, IFrameMetadata metadata) =>
            (Payload, Metadata) = (payload, metadata);

        public ReadOnlySequence<byte> Payload { get; }
        public IFrameMetadata Metadata { get; }

        public bool IsEmpty() => Metadata != null! && Metadata.Length == 0 && Payload.IsEmpty;
    }

    public readonly struct Frame<TMetadata> where TMetadata : class, IFrameMetadata
    {
        public static readonly Frame<TMetadata> Empty = new(ReadOnlySequence<byte>.Empty, default!);

        public Frame(ReadOnlyMemory<byte> payload, TMetadata metadata) =>
            (Payload, Metadata) = (new ReadOnlySequence<byte>(payload), metadata);

        public Frame(ReadOnlySequence<byte> payload, TMetadata metadata) =>
            (Payload, Metadata) = (payload, metadata);

        public ReadOnlySequence<byte> Payload { get; }
        public TMetadata Metadata { get; }

        public bool IsEmpty() => Metadata == null! || Metadata.Length == 0 && Payload.IsEmpty;
    }
}
