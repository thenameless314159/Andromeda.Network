using System.Threading.Tasks;

namespace Andromeda.Dispatcher.Infrastructure
{
    internal static class DispatchResultTasks
    {
        public static Task<DispatchResult> Success => Task.FromResult(DispatchResult.Success);
        public static Task<DispatchResult> NotFound => Task.FromResult(DispatchResult.NotFound);
        public static Task<DispatchResult> Unauthorized => Task.FromResult(DispatchResult.Unauthorized);
        public static Task<DispatchResult> UnexpectedFrame => Task.FromResult(DispatchResult.UnexpectedFrame);
        public static Task<DispatchResult> InvalidFramePayload => Task.FromResult(DispatchResult.InvalidFramePayload);
        public static Task<DispatchResult> InvalidFrameMetadata => Task.FromResult(DispatchResult.InvalidFrameMetadata);
    }
}
