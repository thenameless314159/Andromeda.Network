using System.Buffers;

namespace Andromeda.Framing
{
    public interface IMessageWriter
    {
        void Encode<T>(in T message, IBufferWriter<byte> writer);
    }
}
