using System.Threading.Tasks;
using Andromeda.Framing;
using Microsoft.AspNetCore.Connections;

namespace Andromeda.Dispatcher
{
    public interface IFrameDispatcher<TMeta> : IFrameDispatcher where TMeta : class, IFrameMetadata
    {
        Task<DispatchResult> OnFrameReceivedAsync(in Frame<TMeta> frame, ConnectionContext context);
    }
}
