using System;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Andromeda.Framing.Extensions;
using Andromeda.Framing.UnitTests.Metadata;
using Xunit;

namespace Andromeda.Framing.UnitTests
{
    public class PipeFrameDecoderTests
    {
        private readonly IMetadataParser _parser = new IdPrefixedMetadataParser();
        private static (IFrameDecoder, Pipe) CreateDecoder(IMetadataDecoder decoder)
        {
            var pipe = new Pipe();
            return (pipe.Reader.AsFrameDecoder(decoder), pipe);
        }

        [Fact]
        public async Task ShouldThrow_WhenDecoderReturnInvalidMetadataLength()
        {
            var (decoder, pipe) = CreateDecoder(new InvalidMetadataDecoder());

            // write dummy value so the decoder can read
            await pipe.Writer.WriteAsync(new ReadOnlyMemory<byte>(new byte[] {1, 2, 3}));
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            { await decoder.ReadFrameAsync(); }).ConfigureAwait(false);
        }
    }
}
