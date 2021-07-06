using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using Andromeda.Network.Middleware;
using Andromeda.Serialization;
using System.Threading.Tasks;
using Andromeda.Network;
using Applications;
using Protocols;
using System;

var services = new ServiceCollection().AddLogging(builder => builder
    .SetMinimumLevel(LogLevel.Debug)
    .AddConsole());
    
services.AddDefaultSerialization(sb => sb.UseIdPrefixedProtocolSerialization());
services.AddDefaultSizing(sb => sb.UseIdPrefixedProtocolSizing());
var sp = services.BuildServiceProvider();

var server = new ServerBuilder(sp)
    .UseSockets(sockets => {
        sockets.ListenLocalhost(5000, c => c.UseConnectionLogging().UseConnectionHandler<EchoServerApplication>(), defaultPolicy);
        sockets.ListenLocalhost(5001, c => c.UseConnectionHandler<LengthPrefixedProtocolApplication>(), defaultPolicy);
        sockets.ListenLocalhost(5002, c => c.UseConnectionHandler<IdPrefixedProtocolApplication>(), defaultPolicy);
    })
    .Build();

await server.StartAsync();

var tcs = new TaskCompletionSource<object>();
Console.CancelKeyPress += (_, _) => tcs.TrySetResult(null);
await tcs.Task;

await server.StopAsync();

static void defaultPolicy(ServerLogPolicy policy) {
    policy.LogConnectionAccepted = true;
    policy.LogConnectionCompleted = true;
    policy.LogStartedListening = true;
    policy.LogStoppedListening = true;
    policy.MessageLogLevel = LogLevel.Information;
}