using System;
using System.Buffers;
using System.Threading.Tasks;

namespace Andromeda.Network.Client
{
    public interface IClientPipeProxy
    {
        ValueTask SendAsync(in ReadOnlySpan<byte> buffer);
        ValueTask SendAsync(in ReadOnlyMemory<byte> buffer);
        ValueTask SendAsync(in ReadOnlySequence<byte> buffer);
    }
}
