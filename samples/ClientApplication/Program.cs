using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Andromeda.Network.Middleware;
using Andromeda.Serialization;
using System.Threading.Tasks;
using Andromeda.Framing;
using Andromeda.Network;
using Andromeda.Sizing;
using System.Text;
using System.Net;
using System.IO;
using Protocols;
using System;

var services = new ServiceCollection().AddLogging(builder => builder
    .SetMinimumLevel(LogLevel.Debug)
    .AddConsole());

var sp = services.BuildServiceProvider();

Console.WriteLine("Samples: ");
Console.WriteLine("1. Echo Server");
Console.WriteLine("2. Length Prefixed Protocol Server");
Console.WriteLine("3. Id Prefixed Protocol Server");
Console.WriteLine("4. Id Prefixed Protocol With Message Dispatcher Server");

while (true)
{
    var keyInfo = Console.ReadKey();
    var application = keyInfo.Key switch {
        ConsoleKey.D1 => echoServer(sp),
        ConsoleKey.D2 => lengthPrefixedProtocolServer(sp),
        ConsoleKey.D3 => idPrefixedProtocolServer(sp),
        ConsoleKey.D4 => idPrefixedProtocolServer(sp, 5003),
        _ => Task.CompletedTask
    };

    await application;
}

static async Task echoServer(IServiceProvider serviceProvider)
{
    Console.WriteLine("Running echo server example");
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
    Console.WriteLine("Running length prefixed protocol server example");
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
        await decoder.ReadFrameAsync(connection.ConnectionClosed);
    }
}

static async Task idPrefixedProtocolServer(IServiceProvider serviceProvider, int port = 5002)
{
    Console.WriteLine("Running id prefixed protocol server example");
    var client = new ClientBuilder(serviceProvider).UseSockets().Build();
    await using var connection = await client.ConnectAsync(new IPEndPoint(IPAddress.Loopback, port));
    Console.WriteLine($"Connected to {connection.LocalEndPoint}");

    var parser = new IdPrefixedMetadataParser();
    var sizing = SizingBuilder.CreateDefault()
        .UseIdPrefixedProtocolSizing()
        .Build();

    var serializer = SerializationBuilder.CreateDefault()
        .UseIdPrefixedProtocolSerialization()
        .Build();

    var reader = new IdPrefixedMessageReader(serializer);
    var writer = new IdPrefixedMessageWriter(parser, serializer, sizing);

    await using var encoder = connection.Transport.Output.AsFrameMessageEncoder(parser, writer);
    await using var decoder = connection.Transport.Input.AsFrameMessageDecoder(parser, reader);

    var handshake = await decoder.ReadAsync<HandshakeMessage>(connection.ConnectionClosed);
    if (handshake is null) throw new InvalidOperationException();

    var supportedOperators = string.Join(", ", handshake.SupportedOperators);
    while (true)
    {
        Console.WriteLine($"Type an arithmetic operation using the following supported operator : {supportedOperators}");
        var line = Console.ReadLine();

        if (!TryParseArithmeticOperation(line, handshake.SupportedOperators, out var operation)) {
            Console.WriteLine("Invalid operation supplied !");
            continue;
        }

        await encoder.WriteAsync(in operation);
        var result = await decoder.ReadAsync<ArithmeticOperationResult>(connection.ConnectionClosed);
        Console.WriteLine($"Result : {result!.Result}");
    }

    static bool TryParseArithmeticOperation(string line, ReadOnlySpan<char> operators, out ArithmeticOperation operation)
    {
        operation = null;
        if (string.IsNullOrWhiteSpace(line)) return false;

        ReadOnlySpan<char> trimmed = line.Trim();
        foreach (var op in operators) 
        {
            var indexOf = trimmed.IndexOf(op);
            if (indexOf == -1) continue;

            if (!int.TryParse(trimmed[..indexOf], out var left)) return false;
            if (!int.TryParse(trimmed[(indexOf + 1)..], out var right)) return false;

            operation = new ArithmeticOperation {Left = left, Operator = op, Right = right};
            return true;
        }

        return false;
    }
}
