using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Andromeda.Network.Middleware;
using System.Threading.Tasks;
using Andromeda.Network;
using System.Net;
using System.IO;
using System;

var services = new ServiceCollection()
    .AddLogging(builder => builder
        .SetMinimumLevel(LogLevel.Debug)
        .AddConsole());

var sp = services.BuildServiceProvider();

Console.WriteLine("Samples: ");
Console.WriteLine("1. Echo Server");

while (true)
{
    var keyInfo = Console.ReadKey();

    if (keyInfo.Key == ConsoleKey.D1)
    {
        Console.WriteLine("Running echo server example");
        await echoServer(sp);
    }
}

static async Task echoServer(IServiceProvider serviceProvider)
{
    var client = new ClientBuilder(serviceProvider).UseSockets()
        .UseConnectionLogging()
        .Build();

    var connection = await client.ConnectAsync(new IPEndPoint(IPAddress.Loopback, 5000));
    Console.WriteLine($"Connected to {connection.LocalEndPoint}");

    Console.WriteLine("Echo server running, type into the console");
    var reads = Console.OpenStandardInput().CopyToAsync(connection.Transport.Output.AsStream());
    var writes = connection.Transport.Input.CopyToAsync(Stream.Null);

    await reads;
    await writes;
}
