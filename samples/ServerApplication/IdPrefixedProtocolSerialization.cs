using System;
using System.Text;
using System.Buffers;
using Andromeda.Sizing;
using System.Buffers.Binary;
using Andromeda.Serialization;

namespace Protocols
{
    // Would be generated using ET or source generator in real-life
    public static class IdPrefixedProtocolSerialization
    {
        public static SerializationBuilder UseIdPrefixedProtocolSerialization(this SerializationBuilder b) => b
            .Configure<HandshakeMessage>(TryDeserialize)
            .Configure<HandshakeMessage>(Serialize)
            .Configure<ArithmeticOperation>(TryDeserialize)
            .Configure<ArithmeticOperation>(Serialize)
            .Configure<ArithmeticOperationResult>(TryDeserialize)
            .Configure<ArithmeticOperationResult>(Serialize);

        public static SizingBuilder UseIdPrefixedProtocolSizing(this SizingBuilder b) => b
            .Configure<ArithmeticOperationResult>(SizeOf)
            .Configure<ArithmeticOperation>(SizeOf)
            .Configure<HandshakeMessage>(SizeOf);

        private static int SizeOf(ISizing _, in HandshakeMessage value) => sizeof(short) + value.SupportedOperators.Length;
        private static int SizeOf(ISizing _, in ArithmeticOperation value) => sizeof(int) + sizeof(int) + sizeof(byte);
        private static int SizeOf(ISizing _, in ArithmeticOperationResult value) => sizeof(int);

        private static bool TryDeserialize(IDeserializer _, in ReadOnlySequence<byte> seq, HandshakeMessage value,
            out long bytesRead)
        {
            var reader = new SequenceReader<byte>(seq);
            if (!reader.TryReadLittleEndian(out short arrayLen) || reader.Remaining < arrayLen) {
                bytesRead = reader.Consumed;
                return false;
            }

            value.SupportedOperators = new char[arrayLen];
            for (var i = 0; i < arrayLen; i++)
            {
                if(!reader.TryRead(out var b)) 
                { 
                    bytesRead = reader.Consumed;
                    return false;
                }

                value.SupportedOperators[i] = (char) b;
            }

            bytesRead = reader.Consumed;
            return true;
        }

        private static void Serialize(ISerializer _, in Span<byte> buf, HandshakeMessage value, out int bytesWritten)
        {
            bytesWritten = 0;
            BinaryPrimitives.WriteInt16LittleEndian(buf, (short)value.SupportedOperators.Length);

            var arraySpan = buf[2..];
            for (var i = 0; i < value.SupportedOperators.Length; i++) 
                arraySpan[i] = (byte) value.SupportedOperators[i];

            bytesWritten += sizeof(short) + value.SupportedOperators.Length;
        }

        private static bool TryDeserialize(IDeserializer _, in ReadOnlySequence<byte> seq, ArithmeticOperation value,
            out long bytesRead)
        {
            var reader = new SequenceReader<byte>(seq);
            if (!reader.TryReadLittleEndian(out int left)) 
            {
                bytesRead = reader.Consumed;
                return false;
            }

            value.Left = left;

            if (!reader.TryReadLittleEndian(out int right)) 
            {
                bytesRead = reader.Consumed;
                return false;
            }

            value.Right = right;

            if(!reader.TryRead(out var @operator))
            {
                bytesRead = reader.Consumed;
                return false;
            }

            value.Operator = (char)@operator;
            bytesRead = reader.Consumed;
            return true;
        }

        private static void Serialize(ISerializer _, in Span<byte> buf, ArithmeticOperation value, out int bytesWritten)
        {
            bytesWritten = 0;
            buf[8] = (byte) value.Operator;
            BinaryPrimitives.WriteInt32LittleEndian(buf, value.Left);
            BinaryPrimitives.WriteInt32LittleEndian(buf[4..], value.Right);
            bytesWritten += sizeof(int) + sizeof(int) + sizeof(byte);
        }

        private static bool TryDeserialize(IDeserializer _, in ReadOnlySequence<byte> seq, ArithmeticOperationResult value,
            out long bytesRead)
        {
            var reader = new SequenceReader<byte>(seq);
            if (!reader.TryReadLittleEndian(out int result))
            {
                bytesRead = reader.Consumed;
                return false;
            }

            value.Result = result;
            bytesRead = reader.Consumed;
            return true;
        }

        private static void Serialize(ISerializer _, in Span<byte> buf, ArithmeticOperationResult value, out int bytesWritten)
        {
            bytesWritten = 0;
            BinaryPrimitives.WriteInt32LittleEndian(buf, value.Result);
            bytesWritten += sizeof(int);
        }
    }
}
