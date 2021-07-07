namespace Andromeda.Dispatcher
{
    public interface IMessageDispatcherBuilder 
    {
        void Map<TMessage>(int withMessageId) where TMessage : new();
    }
}
