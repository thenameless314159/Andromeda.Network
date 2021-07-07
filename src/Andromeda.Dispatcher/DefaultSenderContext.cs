using Microsoft.AspNetCore.Connections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Andromeda.Framing;
using System.Threading;

namespace Andromeda.Dispatcher
{
    public class DefaultSenderContext : SenderContextConnection
    {
        public DefaultSenderContext(ConnectionContext connection, IMetadataEncoder encoder, IMessageWriter writer, SemaphoreSlim? singleWriter = default) 
            : this(connection, connection.Transport.Output.AsFrameMessageEncoder(encoder, writer, singleWriter))
        {
        }
        
        public DefaultSenderContext(ConnectionContext connection, IFrameMessageEncoder encoder) : base(connection) => _encoder = encoder;

        private readonly IFrameMessageEncoder _encoder;

        public override ValueTask SendAsync(in Frame frame) => _encoder.WriteAsync(in frame, _context.ConnectionClosed);
        public override ValueTask SendAsync(IEnumerable<Frame> frames) => _encoder.WriteAsync(frames, _context.ConnectionClosed);
        public override ValueTask SendAsync(IAsyncEnumerable<Frame> frames) => _encoder.WriteAsync(frames, _context.ConnectionClosed);
        public override ValueTask SendAsync<T>(in T message) => _encoder.WriteAsync(in message, _context.ConnectionClosed);
    }
}
