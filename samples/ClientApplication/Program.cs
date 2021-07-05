using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Andromeda.Network.Middleware;
using System.Threading.Tasks;
using Andromeda.Network;
using System.Net;
using System.IO;
using System;
using System.Text;
using Andromeda.Framing;
using Protocols;

var services = new ServiceCollection()
    .AddLogging(builder => builder
        .SetMinimumLevel(LogLevel.Debug)
        .AddConsole());

var sp = services.BuildServiceProvider();

Console.WriteLine("Samples: ");
Console.WriteLine("1. Echo Server");
Console.WriteLine("2. Length Prefixed Protocol Server");

while (true)
{
    var keyInfo = Console.ReadKey();

    if (keyInfo.Key == ConsoleKey.D1)
    {
        Console.WriteLine("Running echo server example");
        await echoServer(sp);
    }
    if (keyInfo.Key == ConsoleKey.D2)
    {
        Console.WriteLine("Running length prefixed protocol server example");
        await lengthPrefixedProtocolServer(sp);
    }
}

static async Task echoServer(IServiceProvider serviceProvider)
{
    var client = new ClientBuilder(serviceProvider).UseSockets()
        .UseConnectionLogging()
        .Build();

    await using var connection = await client.ConnectAsync(new IPEndPoint(IPAddress.Loopback, 5000));
    Console.WriteLine($"Connected to {connection.LocalEndPoint}");

    Console.WriteLine("Echo server running, type into the console");
    var reads = Console.OpenStandardInput().CopyToAsync(connection.Transport.Output.AsStream());
    var writes = connection.Transport.Input.CopyToAsync(Stream.Null);

    await reads;
    await writes;
}

static async Task lengthPrefixedProtocolServer(IServiceProvider serviceProvider)
{
    var client = new ClientBuilder(serviceProvider).UseSockets()
        .UseConnectionLogging()
        .Build();

    await using var connection = await client.ConnectAsync(new IPEndPoint(IPAddress.Loopback, 5001));
    Console.WriteLine($"Connected to {connection.LocalEndPoint}");

    var parser = new LengthPrefixedMetadataParser();
    await using var encoder = connection.Transport.Output.AsFrameEncoder(parser);
    await using var decoder = connection.Transport.Input.AsFrameDecoder(parser);
    
    while (true)
    {
        var line = Console.ReadLine();
        var payload = !string.IsNullOrEmpty(line)
            ? Encoding.UTF8.GetBytes(line)
            : Array.Empty<byte>();

        await encoder.WriteAsync(new Frame(payload, new DefaultFrameMetadata(payload.Length)), connection.ConnectionClosed);
        var result = await decoder.ReadFrameAsync(connection.ConnectionClosed);
    }
}
