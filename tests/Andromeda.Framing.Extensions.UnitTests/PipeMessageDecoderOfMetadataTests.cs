using System.IO.Pipelines;
using Andromeda.Framing.Extensions.UnitTests.Infrastructure;

namespace Andromeda.Framing.Extensions.UnitTests
{
    public class PipeMessageDecoderOfMetadataTests : PipeMessageDecoderTests
    {
        protected override (IFrameMessageDecoder, Pipe) CreateDecoder(IMetadataDecoder decoder, IMessageReader reader) { var pipe = new Pipe(); 
            return (new PipeMessageDecoder<IdPrefixedMetadata>(pipe.Reader, decoder, (IMessageReader<IdPrefixedMetadata>)reader), pipe);
        }
    }
}
