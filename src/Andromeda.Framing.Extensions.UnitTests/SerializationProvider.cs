using Andromeda.Framing.Extensions.UnitTests.Models;
using Andromeda.Serialization;
using System.Buffers;
using System;
using System.Buffers.Binary;
using System.Text;

namespace Andromeda.Framing.Extensions.UnitTests
{
    internal static class SerializationProvider
    {
        public static readonly ISerDes Serializer = SerializationBuilder.CreateDefault()
            .Configure<HelloMessage>(TryDeserialize)
            .Configure<TestMessage>(TryDeserialize)
            .Configure<HelloMessage>(Serialize)
            .Configure<TestMessage>(Serialize)
            .Build();

        public static bool TryDeserialize(IDeserializer ser, in ReadOnlySequence<byte> seq, HelloMessage value,
            out long bytesRead)
        {
            var reader = new SequenceReader<byte>(seq);
            if (!reader.TryReadLittleEndian(out int strLen) || reader.Remaining < strLen) {
                bytesRead = reader.Consumed;
                return false;
            }

            value.Message = Encoding.ASCII.GetString(reader.UnreadSequence.Slice(0, strLen));
            reader.Advance(strLen);

            if(!reader.TryReadLittleEndian(out int versionRequired)) {
                bytesRead = reader.Consumed;
                return false;
            }

            value.VersionRequired = versionRequired;
            bytesRead = reader.Consumed;
            return true;
        }

        public static void Serialize(ISerializer ser, in Span<byte> buf, HelloMessage value, out int bytesWritten)
        {
            bytesWritten = 0;
            var strLen = Encoding.ASCII.GetBytes(value.Message, buf[2..]);
            BinaryPrimitives.WriteInt32LittleEndian(buf, strLen);
            bytesWritten += sizeof(int) + strLen;

            BinaryPrimitives.WriteInt32LittleEndian(buf[bytesWritten..], value.VersionRequired);
            bytesWritten +=sizeof(long);
        }

        public static bool TryDeserialize(IDeserializer ser, in ReadOnlySequence<byte> seq, TestMessage value,
            out long bytesRead)
        {
            var reader = new SequenceReader<byte>(seq);
            if (!reader.TryRead(out var flag)) {
                bytesRead = reader.Consumed;
                return false;
            }

            value.Flag = flag;
            if (!reader.TryReadLittleEndian(out long id)) {
                bytesRead = reader.Consumed;
                return false;
            }

            value.Id = id;
            bytesRead = reader.Consumed;
            return true;
        }

        public static void Serialize(ISerializer ser, in Span<byte> buf, TestMessage value, out int bytesWritten)
        {
            buf[0] = value.Flag;
            BinaryPrimitives.WriteInt64LittleEndian(buf[1..], value.Id);
            bytesWritten = sizeof(byte) + sizeof(long);
        }
    }
}
