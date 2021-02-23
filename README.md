# <p align="center"> <img src="https://raw.githubusercontent.com/thenameless314159/Andromeda.Network/master/andromeda_icon3.png" width="48" height="48" style="margin-bottom:-15"> Andromeda.Network  [![.NET](https://github.com/thenameless314159/Andromeda.Network/actions/workflows/dotnet.yml/badge.svg)](https://github.com/thenameless314159/Andromeda.Network/actions/workflows/dotnet.yml)</p>

<div style="text-align:center"><p align="center"><img src="https://raw.githubusercontent.com/thenameless314159/Andromeda.ServiceRegistration/master/andromeda_icon2.png?token=AFMTCCLAUUAALOP5UR4TWWC6JQ6Y6" width="140" height="158"><img src="https://raw.githubusercontent.com/thenameless314159/Andromeda.ServiceRegistration/master/ASP.NET-Core-Logo_2colors_Square_RGB.png?token=AFMTCCNPNVM6MBG7AF6E75K6JQTHI" width="180" height="168"><img src="https://raw.githubusercontent.com/thenameless314159/Andromeda.ServiceRegistration/master/NET-Core-Logo_2colors_Square_RGB.png?token=AFMTCCNORD45RRHKSS456HK6JQTJU" width="180" height="168"></p></div>

This project is based on the [*Project Bedrock*](https://github.com/aspnet/AspNetCore/issues/4772). At this point, the project was only made to suits my needs, therefore it has some differences with the original :

- No  protocol logic, only the base socket layers with the same APIs
- Added an `INetworkServer` interface that expose an `IFeatureCollection` containing an `IServerEndPointsFeature` with the bound endpoints of the configured servers by default. This interface is registered in the relative host service collection when configured using `.ConfigureServer()`.
- Added an `IConnectionTimeoutFeature` in the server `ConnectionContext`
- Use latest [`Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets`](https://github.com/dotnet/aspnetcore/tree/main/src/Servers/Kestrel/Transport.Sockets/src) APIs
- Added configurable logging to server listeners

For further infos about the original project, see the presentation [here](https://speakerdeck.com/davidfowl/project-bedrock).

## Packages

[![NuGet Badge](https://buildstats.info/nuget/Andromeda.Network)](https://www.nuget.org/packages/Andromeda.Network/) - **Andromeda.Network**