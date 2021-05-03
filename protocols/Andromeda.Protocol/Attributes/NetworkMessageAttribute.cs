using System;

namespace Andromeda.Protocol.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class NetworkMessageAttribute : Attribute
    {
        public NetworkMessageAttribute(int id) => Id = id;
        public int Id { get; }
    }
}
