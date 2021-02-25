using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Andromeda.Framing.Extensions;
using Xunit;

namespace Andromeda.Framing.UnitTests
{
    public class PipeWriterExtensionsTests
    {
        private const int _chunkMaxSize = 1024 * 8;

        [Theory, InlineData(1024), InlineData(2048), InlineData(4096), InlineData(_chunkMaxSize)]
        public async Task WriteSequenceAsync_ShouldWriteSingleChunk(int onLength)
        {
            var buffer = new byte[onLength];
            new Random().NextBytes(buffer);
            var pipe = new Pipe();

            var writeResult = await pipe.Writer.WriteSequenceAsync(new ReadOnlySequence<byte>(buffer));
            Assert.True(!writeResult.IsCanceled && !writeResult.IsCompleted);

            var result = await pipe.Reader.ReadAsync();
            Assert.Equal(buffer, result.Buffer.ToArray());
        }

        [Theory, InlineData(1024), InlineData(2048), InlineData(4096), InlineData(_chunkMaxSize)]
        public async Task WriteMemoryAsync_ShouldWriteSingleChunk(int onLength)
        {
            var buffer = new byte[onLength];
            new Random().NextBytes(buffer);
            var pipe = new Pipe();

            var writeResult = await pipe.Writer.WriteMemoryAsync(buffer);
            Assert.True(!writeResult.IsCanceled && !writeResult.IsCompleted);

            var result = await pipe.Reader.ReadAsync();
            Assert.Equal(buffer, result.Buffer.ToArray());
        }

        [Theory, InlineData(2), InlineData(4), InlineData(6)]
        public async Task WriteSequenceAsync_ShouldWriteChunks(int nb)
        {
            var buffer = new byte[nb * _chunkMaxSize];
            new Random().NextBytes(buffer);
            var pipe = new Pipe();

            var writeResult = await pipe.Writer.WriteSequenceAsync(new ReadOnlySequence<byte>(buffer));
            Assert.True(!writeResult.IsCanceled && !writeResult.IsCompleted);

            var result = await pipe.Reader.ReadAsync();
            Assert.Equal(buffer, result.Buffer.ToArray());
        }

        [Theory, InlineData(2), InlineData(4), InlineData(6)]
        public async Task WriteMemoryAsync_ShouldWriteChunks(int nb)
        {
            var buffer = new byte[nb * _chunkMaxSize];
            new Random().NextBytes(buffer);
            var pipe = new Pipe();

            var writeResult = await pipe.Writer.WriteMemoryAsync(buffer);
            Assert.True(!writeResult.IsCanceled && !writeResult.IsCompleted);

            var result = await pipe.Reader.ReadAsync();
            Assert.Equal(buffer, result.Buffer.ToArray());
        }
    }
}
