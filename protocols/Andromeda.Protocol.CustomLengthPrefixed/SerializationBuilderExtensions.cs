using Andromeda.Protocol.CustomLengthPrefixed;
using Andromeda.Serialization;
using System.Buffers.Binary;
using Andromeda.Sizing;
using System.Buffers;
using System.Text;
using System;

namespace Andromeda.Protocol
{
    // this should be automatically generated using ET or source generators
    internal static class SerializationBuilderExtensions
    {
        public static SerializationBuilder ConfigureCustomProtocol(this SerializationBuilder builder) => builder
            .Configure<LoginFailedMessage>(TryDeserializeLoginFailed)
            .Configure<HandshakeMessage>(TryDeserializeHandshake)
            .Configure<CommandMessage>(TryDeserializeCommand)
            .Configure<LoginMessage>(TryDeserializeLogin)
            .Configure<LoginFailedMessage>(WriteLoginFailed)
            .Configure<HandshakeMessage>(WriteHandshake)
            .Configure<CommandMessage>(WriteCommand)
            .Configure<LoginMessage>(WriteLogin);

        public static SizingBuilder ConfigureCustomProtocol(this SizingBuilder builder) => builder
            .Configure<LoginSuccessMessage>(SizeOfLoginSuccess)
            .Configure<LoginFailedMessage>(SizeOfLoginFailed)
            .Configure<HandshakeMessage>(SizeOfHandshake)
            .Configure<CommandMessage>(SizeOfCommand)
            .Configure<LoginMessage>(SizeOfLogin);

        private static bool TryDeserializeHandshake(IDeserializer _, in ReadOnlySequence<byte> seq, HandshakeMessage value,
            out long read)
        {
            var reader = new SequenceReader<byte>(seq);
            if (!reader.TryReadUtf8(out var protocolRequired)) {
                read = reader.Consumed;
                return false;
            }

            value.ProtocolRequired = protocolRequired;
            read = reader.Consumed;
            return true;
        }

        private static void WriteHandshake(ISerializer _, ref Span<byte> buf, in HandshakeMessage value,
            out long written)
        {
            if (string.IsNullOrEmpty(value.ProtocolRequired)) {
                BinaryPrimitives.WriteInt16LittleEndian(buf, 0);
                written = sizeof(short);
                return;
            }

            var length = Encoding.UTF8.GetBytes(value.ProtocolRequired, buf[sizeof(short)..]);
            BinaryPrimitives.WriteInt16LittleEndian(buf, (short)length);
            written = sizeof(short) + length;
        }

        private static bool TryDeserializeCommand(IDeserializer _, in ReadOnlySequence<byte> seq, CommandMessage value,
            out long read)
        {
            var reader = new SequenceReader<byte>(seq);
            if (!reader.TryReadUtf8(out var command)) {
                read = reader.Consumed;
                return false;
            }

            value.Command = command;
            read = reader.Consumed;
            return true;
        }

        private static void WriteCommand(ISerializer _, ref Span<byte> buf, in CommandMessage value,
            out long written)
        {
            if (string.IsNullOrEmpty(value.Command)) {
                BinaryPrimitives.WriteInt16LittleEndian(buf, 0);
                written = sizeof(short);
                return;
            }

            var length = Encoding.UTF8.GetBytes(value.Command, buf[sizeof(short)..]);
            BinaryPrimitives.WriteInt16LittleEndian(buf, (short)length);
            written = sizeof(short) + length;
        }

        private static bool TryDeserializeLogin(IDeserializer _, in ReadOnlySequence<byte> seq, LoginMessage value,
            out long read)
        {
            var reader = new SequenceReader<byte>(seq);
            if (!reader.TryReadUtf8(out var username)) {
                read = reader.Consumed;
                return false;
            }

            if (!reader.TryReadUtf8(out var password)) {
                read = reader.Consumed;
                return false;
            }

            value.Username = username;
            value.Password = password;
            read = reader.Consumed;
            return true;
        }

        private static void WriteLogin(ISerializer _, ref Span<byte> buf, in LoginMessage value,
            out long written)
        {
            var usernameLength = Encoding.UTF8.GetBytes(value.Username, buf[sizeof(short)..]);
            BinaryPrimitives.WriteInt16LittleEndian(buf, (short)usernameLength);

            var passwordLength = Encoding.UTF8.GetBytes(value.Username, buf[(sizeof(short) * 2 + usernameLength)..]);
            BinaryPrimitives.WriteInt16LittleEndian(buf[(sizeof(short) + usernameLength)..], (short)usernameLength);
            written = sizeof(short) * 2 + usernameLength + passwordLength;
        }

        private static int SizeOfLogin(ISizing sizing, in LoginMessage value) => sizeof(short) * 2 + 
            Encoding.UTF8.GetByteCount(value.Username) + Encoding.UTF8.GetByteCount(value.Password);

        private static bool TryDeserializeLoginFailed(IDeserializer _, in ReadOnlySequence<byte> seq, LoginFailedMessage value,
            out long read)
        {
            var reader = new SequenceReader<byte>(seq);
            if (!reader.TryReadUtf8(out var reason)) {
                read = reader.Consumed;
                return false;
            }

            value.Reason = reason;
            read = reader.Consumed;
            return true;
        }

        private static void WriteLoginFailed(ISerializer _, ref Span<byte> buf, in LoginFailedMessage value,
            out long written)
        {
            if (string.IsNullOrEmpty(value.Reason)) {
                BinaryPrimitives.WriteInt16LittleEndian(buf, 0);
                written = sizeof(short);
                return;
            }

            var length = Encoding.UTF8.GetBytes(value.Reason, buf[sizeof(short)..]);
            BinaryPrimitives.WriteInt16LittleEndian(buf, (short)length);
            written = sizeof(short) + length;
        }

        private static int SizeOfLoginSuccess(ISizing sizing, in LoginSuccessMessage value) => 0;
        private static int SizeOfLoginFailed(ISizing sizing, in LoginFailedMessage value) =>
            sizeof(short) + Encoding.UTF8.GetByteCount(value.Reason);
        private static int SizeOfHandshake(ISizing sizing, in HandshakeMessage value) =>
            sizeof(short) + Encoding.UTF8.GetByteCount(value.ProtocolRequired);
        private static int SizeOfCommand(ISizing sizing, in CommandMessage value) =>
            sizeof(short) + Encoding.UTF8.GetByteCount(value.Command);

        private static bool TryReadUtf8(this ref SequenceReader<byte> reader, out string value)
        {
            value = string.Empty;
            if (!reader.TryReadLittleEndian(out short len)) 
                return false;

            var strLen = (ushort)len;
            if (reader.Remaining < strLen) return false;

            var strSequence = reader.Sequence.Slice(reader.Position, strLen);
            value = strSequence.AsString(Encoding.UTF8);
            reader.Advance(strLen);
            return true;
        }

        private static string AsString(this ref ReadOnlySequence<byte> buffer, Encoding? useEncoding = default)
        {
            var encoding = useEncoding ?? Encoding.UTF8;
            if (buffer.IsSingleSegment) return encoding.GetString(buffer.FirstSpan);
            return string.Create((int)buffer.Length, buffer, (span, sequence) =>
            {
                foreach (var segment in sequence) {
                    encoding.GetChars(segment.Span, span);
                    span = span[segment.Length..];
                }
            });
        }
    }
}
