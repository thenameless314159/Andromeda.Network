using System;
using System.Reflection;

namespace Protocols
{
    [Message(1)]
    public class HandshakeMessage
    {
        public char[] SupportedOperators { get; set; } = Array.Empty<char>();
    }

    [Message(2)]
    public class ArithmeticOperation
    {
        public int Left { get; set; }
        public int Right { get; set; }
        public char Operator { get; set; }
    }

    [Message(3)]
    public class ArithmeticOperationResult
    {
        public int Result { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class MessageAttribute : Attribute
    {
        public MessageAttribute(int id) => Id = id;
        public int Id { get; init; }
    }

    public static class MetadataProvider
    {
        public static int? GetIdOf<TMessage>() => MessageIdStore<TMessage>.Value;

        private static class MessageIdStore<T>
        {
            public static int? Value { get; }

            static MessageIdStore() => Value = typeof(T).GetCustomAttribute<MessageAttribute>()?.Id;
        }
    }
}
