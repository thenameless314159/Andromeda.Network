using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Andromeda.Protocol.Attributes;

namespace Andromeda.Protocol
{
    public static class AssemblyExtensions
    {
        public static IEnumerable<(Type Type, int Id)> GetAllNetworkMessages(this Assembly[] protocolAssemblies) =>
            from assembly in protocolAssemblies
            from type in assembly.GetExportedTypes()
            where !type.IsAbstract && type.IsClass
                  && Attribute.IsDefined(type, typeof(NetworkMessageAttribute))
            select (type, type.GetCustomAttribute<NetworkMessageAttribute>()!.Id);
        
        public static IEnumerable<(Type Type, int Id)> GetAllNetworkMessages(this Assembly protocolAssembly) =>
            new[] {protocolAssembly}.GetAllNetworkMessages();
    }
}
