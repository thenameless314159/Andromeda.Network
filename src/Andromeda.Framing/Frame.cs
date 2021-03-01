using System;
using System.Buffers;

namespace Andromeda.Framing
{
    /// <summary>
    /// Represent a frame of any kind of length prefixed protocol.
    /// </summary>
    public readonly struct Frame
    {
        /// <summary>
        /// An empty <see cref="Frame"/> with and empty payload and <see cref="IFrameMetadata"/> set to null.
        /// </summary>
        public static readonly Frame Empty = new(ReadOnlySequence<byte>.Empty, default!);

        /// <summary>ctor</summary>
        /// <param name="payload">The payload of the <see cref="Frame"/>.</param>
        /// <param name="metadata">The <see cref="IFrameMetadata"/> of the <see cref="Frame"/>.</param>
        public Frame(ReadOnlyMemory<byte> payload, IFrameMetadata metadata) =>
            (Payload, Metadata) = (new ReadOnlySequence<byte>(payload), metadata);


        /// <summary>ctor</summary>
        /// <param name="payload">The payload of the <see cref="Frame"/>.</param>
        /// <param name="metadata">The <see cref="IFrameMetadata"/> of the <see cref="Frame"/>.</param>
        public Frame(ReadOnlySequence<byte> payload, IFrameMetadata metadata) =>
            (Payload, Metadata) = (payload, metadata);

        /// <summary>
        /// Get the payload of the current <see cref="Frame"/>.
        /// </summary>
        public ReadOnlySequence<byte> Payload { get; }

        /// <summary>
        /// Get the <see cref="IFrameMetadata"/> of the current <see cref="Frame"/>.
        /// </summary>
        public IFrameMetadata Metadata { get; }

        /// <returns>Whether the <see cref="IFrameMetadata"/> and payload length of the current <see cref="Frame"/> are lesser than 1 or not.</returns>
        public bool IsPayloadEmpty() =>  Metadata.Length == 0 && Payload.IsEmpty;

        /// <returns>Whether or not the current <see cref="Frame"/> is a Frame.Empty.</returns>
        public bool IsEmptyFrame() => Metadata == default! && Payload.IsEmpty;
    }

    /// <summary>
    /// Represent a frame of any kind of length prefixed protocol with specific <see cref="IFrameMetadata"/>.
    /// </summary>
    /// <typeparam name="TMetadata">The specific <see cref="IFrameMetadata"/>.</typeparam>
    public readonly struct Frame<TMetadata> where TMetadata : class, IFrameMetadata
    {
        /// <summary>
        /// An empty <see cref="Frame"/> with and empty payload and <see cref="IFrameMetadata"/> set to null.
        /// </summary>
        public static readonly Frame<TMetadata> Empty = new(ReadOnlySequence<byte>.Empty, default!);

        /// <summary>ctor</summary>
        /// <param name="payload">The payload of the <see cref="Frame"/>.</param>
        /// <param name="metadata">The <see cref="IFrameMetadata"/> of the <see cref="Frame"/>.</param>
        public Frame(ReadOnlyMemory<byte> payload, TMetadata metadata) =>
            (Payload, Metadata) = (new ReadOnlySequence<byte>(payload), metadata);

        /// <summary>ctor</summary>
        /// <param name="payload">The payload of the <see cref="Frame"/>.</param>
        /// <param name="metadata">The <see cref="IFrameMetadata"/> of the <see cref="Frame"/>.</param>
        public Frame(ReadOnlySequence<byte> payload, TMetadata metadata) =>
            (Payload, Metadata) = (payload, metadata);

        /// <summary>
        /// Get the payload of the current <see cref="Frame"/>.
        /// </summary>
        public ReadOnlySequence<byte> Payload { get; }

        /// <summary>
        /// Get the specific <see cref="IFrameMetadata"/> of the current <see cref="Frame{TMetadata}"/>.
        /// </summary>
        public TMetadata Metadata { get; }

        /// <returns>Whether the <see cref="IFrameMetadata"/> and payload length of the current <see cref="Frame{TMetadata}"/> are lesser than 1 or not.</returns>
        public bool IsPayloadEmpty() => Metadata.Length == 0 && Payload.IsEmpty;

        /// <returns>Whether or not the current <see cref="Frame"/> is a Frame.Empty.</returns>
        public bool IsEmptyFrame() => Metadata == default! && Payload.IsEmpty;
    }
}
