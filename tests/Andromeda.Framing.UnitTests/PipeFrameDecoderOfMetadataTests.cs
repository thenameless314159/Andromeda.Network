using System.IO.Pipelines;
using Andromeda.Framing.Extensions;
using Andromeda.Framing.UnitTests.Metadata;

namespace Andromeda.Framing.UnitTests
{
    public class PipeFrameDecoderOfMetadataTests : PipeFrameDecoderTests
    {
        protected override (IFrameDecoder, Pipe) CreateDecoder(IMetadataDecoder decoder) {
            var pipe = new Pipe(); return (pipe.Reader.AsFrameDecoder<IdPrefixedMetadata>(decoder), pipe);
        }
    }
}
