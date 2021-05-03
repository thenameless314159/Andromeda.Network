using System;
using ServerApplication;
using Andromeda.Network;
using System.Threading.Tasks;
using Andromeda.Network.Features;
using Andromeda.Protocol;
using Andromeda.Protocol.CustomLengthPrefixed;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

services.AddScoped<IMessageHandler<CommandMessage>, CommandMessageHandler>();
services.AddLogging(builder => {
    builder.AddFilter("Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets", LogLevel.Warning);
    builder.SetMinimumLevel(LogLevel.Debug);
    builder.AddConsole();
});

var sp = services.BuildServiceProvider();
var server = new ServerBuilder(sp)
    .UseSockets(sockets => sockets.ListenLocalhost(8000, 
        builder => builder.UseConnectionHandler<CustomProtocolApplication>(),
        logPolicy => {
            logPolicy.LogStoppedListening = true;
            logPolicy.LogStartedListening = true;
            logPolicy.LogConnectionAborted = true;
            logPolicy.LogConnectionAccepted = true;
            logPolicy.LogConnectionCompleted = true;
            logPolicy.MessageLogLevel = LogLevel.Debug;
        }))
    .Build();

await server.StartAsync();

/*var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger<NetworkServer>();
var endpointsFeature = server.Features.Get<IServerEndpointsFeature>();

foreach (var ep in endpointsFeature.EndPoints) 
    logger.LogInformation("Listening on {EndPoint}", ep);*/

var tcs = new TaskCompletionSource<object>();
Console.CancelKeyPress += (_, _) => tcs.TrySetResult(null);
await tcs.Task;

await server.StopAsync();

