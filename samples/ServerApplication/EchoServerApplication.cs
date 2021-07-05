using Microsoft.AspNetCore.Connections;
using System.Threading.Tasks;

namespace Applications
{
    public class EchoServerApplication : ConnectionHandler
    {
        public override Task OnConnectedAsync(ConnectionContext connection) =>
            connection.Transport.Input.CopyToAsync(connection.Transport.Output);
    }
}
