namespace Andromeda.Network.Internal
{
    internal interface ITimeoutHandler
    {
        void OnTimeout(string reason);
    }
}
