using System;
using System.Buffers;

namespace Andromeda.Framing
{
    public interface IMessageWriter
    {
        void Encode<T>(T message, IBufferWriter<byte> writer);
        void Encode<T>(T message, ref Span<byte> writer, out long bytesWritten);
    }
}
