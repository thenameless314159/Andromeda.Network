using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Andromeda.Framing.Extensions.UnitTests.Infrastructure;
using Andromeda.Framing.Extensions.UnitTests.Models;
using Xunit;

namespace Andromeda.Framing.Extensions.UnitTests
{
    public class PipeMessageDecoderTests
    {
        private readonly IdPrefixedMetadataParser _parser = new();
        private readonly IdPrefixedMessageReader _reader = new();

        protected virtual (IFrameMessageDecoder, Pipe) CreateDecoder(IMetadataDecoder decoder, IMessageReader reader) {
            var pipe = new Pipe(); return (new PipeMessageDecoder(pipe.Reader, decoder, reader), pipe);
        }

        [Fact]
        public async Task ReadAsync_ShouldTryDeserializeFromUnreadFrame_OnInvalidMessage()
        {
            var (decoder, pipe) = CreateDecoder(_parser, _reader);
            await pipe.Writer.WriteFrameAsync(_parser, new Frame(ReadOnlySequence<byte>.Empty, 
                new IdPrefixedMetadata(3, 0)));

            Assert.Null(await decoder.ReadAsync<TestMessage>());
            Assert.NotNull(await decoder.ReadAsync<EmptyMessage>());
            Assert.Equal(1, decoder.FramesRead);
        }

        [Fact]
        public async Task TryReadAsync_ShouldTryDeserializeFromUnreadFrame_OnInvalidMessage()
        {
            var (decoder, pipe) = CreateDecoder(_parser, _reader);
            await pipe.Writer.WriteFrameAsync(_parser, new Frame(ReadOnlySequence<byte>.Empty,
                new IdPrefixedMetadata(3, 0)));

            Assert.False(await decoder.TryReadAsync(new TestMessage()));
            Assert.True(await decoder.TryReadAsync(new EmptyMessage()));
            Assert.Equal(1, decoder.FramesRead);
        }
    }
}
