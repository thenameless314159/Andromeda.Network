using System.Collections.Generic;
using System.Net;

namespace Andromeda.Network.Features
{
    public interface IServerEndpointsFeature
    {
        ICollection<EndPoint> EndPoints { get; }
    }
}
