using System;
using System.Net;
using System.Threading.Tasks;
using Andromeda.Dispatcher;
using Andromeda.Network;
using Andromeda.Protocol;
using Andromeda.Protocol.CustomLengthPrefixed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var services = new ServiceCollection();
services.AddLogging(builder => {
    builder.SetMinimumLevel(LogLevel.Debug);
    builder.AddConsole();
});

var sp = services.BuildServiceProvider();

Console.WriteLine("Samples: ");
Console.WriteLine("1. Length prefixed custom binary protocol");

try
{
    while (true)
    {
        var keyInfo = Console.ReadKey();
        if (keyInfo.Key == ConsoleKey.D1)
        {
            Console.WriteLine("Custom length prefixed protocol.");
            await CustomProtocol(sp);
        }
    }
}
catch (Exception e) {
    Console.WriteLine(e);
}

static async Task CustomProtocol(IServiceProvider services)
{
    var clientFactory = new ClientBuilder(services).UseSockets().Build();
    await using var connection = await clientFactory.ConnectAsync(new IPEndPoint(IPAddress.Loopback, 8000));
    Console.WriteLine($"Connected to {connection.LocalEndPoint}");

    var msgParser = new CustomProtocolMessageParser(); var metaParser = new CustomProtocolParser();
    await using var client = new DefaultClient(connection, metaParser, msgParser, msgParser);

    var handshakeMsg = await client.ReceiveAsync<HandshakeMessage>();
    if (handshakeMsg == default) {
        Console.WriteLine("Handshake failed !");
        return;
    }

    Console.WriteLine($"Received {handshakeMsg}");
    await client.SendAsync(new LoginMessage("Nameless", "password"));
    var success = await client.ReceiveAsync<LoginSuccessMessage>();
    if (success == default) {
        Console.WriteLine("Login failed !");
        return;
    }

    Console.WriteLine("Login success !");
    var random = new Random().Next(1, 3);
    var sendCommandAsync = random switch {
        2 => client.SendAsync(new CommandMessage("handshake")),
        3 => client.SendAsync(new CommandMessage("handshakeAndAbort")),
        _ => client.SendAsync(new CommandMessage("abort"))
    };

    if (!sendCommandAsync.IsCompletedSuccessfully) await sendCommandAsync;
    if (random == 2 || random == 3) {
        handshakeMsg = await client.ReceiveAsync<HandshakeMessage>();
        if (handshakeMsg == default) {
            Console.WriteLine("Command failed !");
            return;
        }

        Console.WriteLine($"Received {handshakeMsg}");
    }

    Console.WriteLine("Connection ended");
}