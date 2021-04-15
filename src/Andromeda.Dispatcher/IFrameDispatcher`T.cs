using System.Threading.Tasks;
using Andromeda.Framing;

namespace Andromeda.Dispatcher
{
    public interface IFrameDispatcher<TMeta> : IFrameDispatcher where TMeta : class, IFrameMetadata
    {
        new Task<DispatchResult> OnFrameReceivedAsync(in Frame frame, SenderContext context) => OnFrameReceivedAsync(frame.AsTyped<TMeta>(), context);
        Task<DispatchResult> OnFrameReceivedAsync(in Frame<TMeta> frame, SenderContext context);
    }
}
