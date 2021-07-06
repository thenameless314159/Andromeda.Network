using Microsoft.AspNetCore.Connections;
using System.Threading.Tasks;
using Andromeda.Framing;

namespace Andromeda.Dispatcher
{
    public interface IFrameDispatcher
    {
        Task<DispatchResult> OnFrameReceivedAsync(in Frame frame, ConnectionContext context);
    }
}
