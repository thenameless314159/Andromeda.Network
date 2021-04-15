using Andromeda.Framing;
using System.IO.Pipelines;
using System.Security.Claims;
using System.Security.Principal;
using Microsoft.AspNetCore.Connections;

namespace Andromeda.Dispatcher
{
    public record SenderContext
    {
        public virtual IPrincipal Identity { get; init; }
        public virtual IClientProxy Proxy { get; init; }
        public virtual IClient Client { get; init; }

        public SenderContext(DefaultClient client, IPrincipal? identity = default) =>
            (Client, Proxy, Identity) = (client, client, identity ?? new ClaimsPrincipal());

        public SenderContext(IClient client, IClientProxy proxy, IPrincipal? identity = default) =>
            (Client, Proxy, Identity) = (client, proxy, identity ?? new ClaimsPrincipal());

        /// <summary>
        /// Creates a new <see cref="SenderContext"/>.
        /// </summary>
        /// <remarks>
        /// The default constructor is provided for unit test purposes only.
        /// </remarks>
        public SenderContext(IDuplexPipe? pipe = default, IPrincipal? identity = default, IMetadataParser parser = default!,
            IMessageReader? reader = default, IMessageWriter? writer = default)
        {
            var context = pipe is not null
                ? new DefaultConnectionContext { Transport = pipe }
                : new DefaultConnectionContext();

            var client = new DefaultClient(context, parser, reader, writer);
            Identity = identity ?? new ClaimsPrincipal();
            Client = client;
            Proxy = client;
        }
    }
}
