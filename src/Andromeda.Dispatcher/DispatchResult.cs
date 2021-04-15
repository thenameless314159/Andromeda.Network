namespace Andromeda.Dispatcher
{
    public enum DispatchResult
    {
        Success,
        NotFound,
        Unauthorized,
        UnexpectedFrame,
        InvalidFramePayload,
        InvalidFrameMetadata
    }
}
