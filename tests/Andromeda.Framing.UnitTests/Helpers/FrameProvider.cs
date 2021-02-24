using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Linq;

namespace Andromeda.Framing.UnitTests.Helpers
{
    internal static class FrameProvider
    {
        public static Memory<byte> GetMultiplesRandomFramesAsBuffer(int messageId, params int[] framesLength)
        {
            var random = new Random();
            Memory<byte> buffer = new byte[6 * framesLength.Length + framesLength.Sum()];
            var offset = 0;
            foreach (var length in framesLength)
            {
                GetRandomFrameAsBuffer((short)messageId, length, random)
                    .CopyTo(buffer[offset..]);

                offset += 6 + length;
            }

            return buffer;
        }
        
        public static Memory<byte> GetRandomFrameAsBuffer(short id, int length, Random? random = default)
        {
            random ??= new Random();
            Memory<byte> buffer = new byte[length + 6];
            BinaryPrimitives.WriteInt16BigEndian(buffer.Span, id);
            BinaryPrimitives.WriteInt32BigEndian(buffer.Span[2..], length);
            if (length > 0) random.NextBytes(buffer.Span[6..]);
            return buffer;
        }

        public static ReadOnlySequence<byte> GetMultiplesRandomFramesAsSequence(int messageId, params int[] framesLength) => new(GetMultiplesRandomFramesAsBuffer(messageId, framesLength));
        public static ReadOnlySequence<byte> GetRandomFrame(short id, int length) => new(GetRandomFrameAsBuffer(id, length));
    }
}
