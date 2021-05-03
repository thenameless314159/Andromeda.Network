using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Andromeda.Protocol
{
    public interface IMessageContainer : IEnumerable<KeyValuePair<int, Type>>
    {
        Type this[int messageId] { get; }

        bool IsRegistered(int messageId);
        bool IsRegistered<T>(T? _ = default);

        int GetId(Type type);
        int GetId<T>(T? _ = default);
        string GetName(int messageId);
    }

    public class MessageContainer : IMessageContainer
    {
        public static IMessageContainer? Instance { get; private set; }

        private MessageContainer(IEnumerable<(Type, int)> messages) {
            _messagesByType = messages.ToDictionary(m => m.Item1, m => m.Item2);
            _messagesById = messages.ToDictionary(m => m.Item2, m => m.Item1);
        }
        private readonly Dictionary<Type, int> _messagesByType;
        private readonly Dictionary<int, Type> _messagesById;
        private static bool _isSingletonAlreadySetup;

        public static IMessageContainer CreateFrom(bool setupSingleton, params Assembly[] protocolAssemblies) {
            if (setupSingleton && _isSingletonAlreadySetup) throw new InvalidOperationException(
                "Singleton has already been setup previously !");

            var container = new MessageContainer(protocolAssemblies.GetAllNetworkMessages());
            if (!setupSingleton) return container;
            _isSingletonAlreadySetup = true;
            Instance = container;
            return container;
        }

        public Type this[int messageId] => !_messagesById.TryGetValue(messageId, out var message)
            ? throw new KeyNotFoundException($"Couldn't find message with Id={messageId} in the current IMessageContainer !")
            : message;

        public bool IsRegistered(int messageId) => _messagesById.ContainsKey(messageId);
        public bool IsRegistered<T>(T? _ = default) => _messagesByType.ContainsKey(typeof(T));

        public string GetName(int messageId) => _messagesById[messageId].Name;

        public int GetId(Type type) => _messagesByType[type];
        public int GetId<T>(T? _ = default) => _messagesByType[typeof(T)];

        public IEnumerator<KeyValuePair<int, Type>> GetEnumerator() => _messagesById.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
