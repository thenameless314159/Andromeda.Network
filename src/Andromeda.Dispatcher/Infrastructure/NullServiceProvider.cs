using System;

namespace Andromeda.Dispatcher.Infrastructure
{
    internal class NullServiceProvider : IServiceProvider
    {
        public static IServiceProvider Instance { get; } = new NullServiceProvider();
        public object? GetService(Type serviceType) => null;
    }
}
