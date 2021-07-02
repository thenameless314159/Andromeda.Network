namespace Andromeda.Framing
{
    public interface IMessageReader
    {
        bool TryDecode<T>(in Frame frame, T message);
    }
}
