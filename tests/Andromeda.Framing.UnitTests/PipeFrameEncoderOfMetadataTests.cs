using System.IO.Pipelines;
using Andromeda.Framing.UnitTests.Metadata;

namespace Andromeda.Framing.UnitTests
{
    public class PipeFrameEncoderOfMetadataTests : PipeFrameEncoderTests
    {
        protected override (IFrameEncoder, Pipe) CreateEncoder(IMetadataEncoder encoder) {
            var pipe = new Pipe(); return (pipe.Writer.AsFrameEncoder<IdPrefixedMetadata>(encoder), pipe);
        }
    }
}
