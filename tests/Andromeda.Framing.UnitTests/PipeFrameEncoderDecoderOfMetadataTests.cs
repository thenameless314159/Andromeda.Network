using System.IO.Pipelines;
using Andromeda.Framing.UnitTests.Metadata;
using Xunit.Abstractions;

namespace Andromeda.Framing.UnitTests
{
    public class PipeFrameEncoderDecoderOfMetadataTests : PipeFrameEncoderDecoderTests
    {
        public PipeFrameEncoderDecoderOfMetadataTests(ITestOutputHelper logger) : base(logger)
        {
        }

        protected override (IFrameEncoder, IFrameDecoder, IDuplexPipe) CreateEncoderDecoderPair(IMetadataParser parser)
        {
            var pipe = new DuplexPipe(); var (decoder, encoder) = pipe.AsFrameDecoderEncoderPair<IdPrefixedMetadata>(parser);
            return (encoder, decoder, pipe);
        }
    }
}
