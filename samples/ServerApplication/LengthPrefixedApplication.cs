using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Andromeda.Framing;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using Protocols;

namespace ServerApplication
{
    public class LengthPrefixedApplication : ConnectionHandler
    {
        public LengthPrefixedApplication(ILogger<LengthPrefixedApplication> logger) => _logger = logger;
        private readonly IMetadataParser _parser = new LengthPrefixedMetadataParser();
        private readonly ILogger<LengthPrefixedApplication> _logger;

        public override async Task OnConnectedAsync(ConnectionContext connection)
        {
            var (decoder, encoder) = connection.Transport.AsFrameDecoderEncoderPair(_parser);
            try {
                await foreach (var frame in decoder.ReadFramesAsync(connection.ConnectionClosed)) 
                {
                    _logger.LogInformation("Received a frame with {Length} bytes, payload : {Content}", frame.Metadata.Length, 
                        Encoding.UTF8.GetString(frame.Payload));

                    await encoder.WriteAsync(in frame, connection.ConnectionClosed);
                }
            }
            finally
            {
                await decoder.DisposeAsync();
                await encoder.DisposeAsync();
            }
        }
    }
}
