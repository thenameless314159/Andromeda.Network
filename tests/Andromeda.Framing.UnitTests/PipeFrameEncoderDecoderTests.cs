using Xunit;
using System.Buffers;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Andromeda.Framing.UnitTests.Helpers;
using Andromeda.Framing.UnitTests.Metadata;

using Xunit.Abstractions;

namespace Andromeda.Framing.UnitTests
{
    public class PipeFrameEncoderDecoderTests
    {
        protected record DuplexPipe(Pipe Pipe) : IDuplexPipe
        {
            public DuplexPipe() : this(new Pipe()) { }
            public PipeReader Input => Pipe.Reader;
            public PipeWriter Output => Pipe.Writer;
        }

        public PipeFrameEncoderDecoderTests(ITestOutputHelper logger) => _logger = logger;
        private readonly IMetadataParser _parser = new IdPrefixedMetadataParser();
        private readonly ITestOutputHelper _logger;

        protected virtual (IFrameEncoder, IFrameDecoder, IDuplexPipe) CreateEncoderDecoderPair(IMetadataParser parser) {
            var pipe = new DuplexPipe(); var (decoder, encoder) = pipe.AsFrameDecoderEncoderPair(parser);
            return (encoder, decoder, pipe);
        }

        // TODO: fix for length > 8192
        // also the fast path seems to disable with the possibility to assign the task to a var and await later
        // (like after writing the second part of the payload)
        [Theory, InlineData(1, 8), InlineData(2, 2048), InlineData(3, 4096)]
        public async Task ShouldParseWrittenFrameWithIncompletePayload(short messageId, int length)
        {
            var (encoder, decoder, pipe) = CreateEncoderDecoderPair(_parser);
            await using var __ = encoder; await using var _ = decoder;
            
            try
            {
                var metadata = new IdPrefixedMetadata(messageId, length);
                FrameProvider.TryGetFrameWithRandomPayload(metadata, out var frame);
                var incomplete = new Frame(frame.Payload.Slice(0, length / 2), frame.Metadata);
                await encoder.WriteAsync(in incomplete);
                await pipe.Output.WriteAsync(frame.Payload.Slice(length / 2).ToArray());

                var readFrame = await decoder.ReadFrameAsync();
                Assert.Equal(frame.Payload.ToArray(), readFrame.Payload.ToArray());
                Assert.Equal(metadata, readFrame.Metadata);
                Assert.Equal(1, encoder.FramesWritten);
                Assert.Equal(1, decoder.FramesRead);
            }
            finally { pipe.Output.Complete(); pipe.Input.Complete(); }
        }

        [Theory, InlineData(1, 0), InlineData(2, 8), InlineData(3, 2048), InlineData(4, 8192)]
        public async Task ShouldParseWrittenFrame(short messageId, int length)
        {
            var (encoder, decoder, pipe) = CreateEncoderDecoderPair(_parser);
            await using var __ = encoder; await using var _ = decoder;

            try
            {
                var metadata = new IdPrefixedMetadata(messageId, length);
                FrameProvider.TryGetFrameWithRandomPayload(metadata, out var frame);
                await encoder.WriteAsync(in frame);

                var readFrame = await decoder.ReadFrameAsync();
                _logger.WriteLine("Successfully read " + readFrame.Metadata);
                Assert.Equal(frame.Payload.ToArray(), readFrame.Payload.ToArray());
                Assert.Equal(metadata, readFrame.Metadata);
                Assert.Equal(1, encoder.FramesWritten);
                Assert.Equal(1, decoder.FramesRead);
            }
            finally { pipe.Output.Complete(); pipe.Input.Complete(); }
        }

        [Theory, InlineData(0, 128, 1024, 2048, 8192)]
        public async Task ShouldParseWrittenFrames(params int[] framesLength)
        {
            var (encoder, decoder, pipe) = CreateEncoderDecoderPair(_parser);
            await using var __ = encoder; await using var _ = decoder;
            
            try
            {
                await using var enumerator = decoder.ReadFramesAsync().GetAsyncEnumerator();
                for(var i = 0; i < framesLength.Length; i++)
                {
                    var metadata = new IdPrefixedMetadata(i, framesLength[i]);
                    FrameProvider.TryGetFrameWithRandomPayload(metadata, out var frame);
                    await encoder.WriteAsync(in frame);

                    Assert.True(await enumerator.MoveNextAsync());
                    var readFrame = enumerator.Current;

                    _logger.WriteLine("Successfully read " + readFrame.Metadata);
                    Assert.Equal(frame.Payload.ToArray(), readFrame.Payload.ToArray());
                    Assert.Equal(metadata, readFrame.Metadata);
                }

                Assert.Equal(framesLength.Length, encoder.FramesWritten);
                Assert.Equal(framesLength.Length, decoder.FramesRead);
            }
            finally { pipe.Output.Complete(); pipe.Input.Complete(); }
        }
    }
}
