using System.Collections.Generic;
using System.Net;

namespace Andromeda.Network.Features
{
    internal class ReadOnlyServerEndpointsFeature : IServerEndpointsFeature
    {
        public ReadOnlyServerEndpointsFeature(List<EndPoint> endPoints) => EndPoints = endPoints.AsReadOnly();
        public ICollection<EndPoint> EndPoints { get; }
    }
}
