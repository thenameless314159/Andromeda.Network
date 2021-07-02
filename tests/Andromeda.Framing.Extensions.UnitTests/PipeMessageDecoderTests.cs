using System;
using System.IO.Pipelines;
using Andromeda.Framing.Extensions.UnitTests.Infrastructure;
using Xunit;

namespace Andromeda.Framing.Extensions.UnitTests
{
    public class PipeMessageDecoderTests
    {
        private readonly IdPrefixedMetadataParser _parser = new();
        private readonly IdPrefixedMessageReader _reader = new();

        protected virtual (IFrameMessageDecoder, Pipe) CreateDecoder(IMetadataDecoder decoder) {
            var pipe = new Pipe(); return (new PipeMessageDecoder(pipe.Reader, _parser, _reader), pipe);
        }
    }
}
