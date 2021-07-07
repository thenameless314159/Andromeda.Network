using System.Threading.Tasks;
using Andromeda.Framing;

namespace Andromeda.Dispatcher
{
    public interface IFrameDispatcher<TMeta> : IFrameDispatcher where TMeta : class, IFrameMetadata
    {
        Task<DispatchResult> OnFrameReceivedAsync(in Frame<TMeta> frame, SenderContext context);
    }
}
