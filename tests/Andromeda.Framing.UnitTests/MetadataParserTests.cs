using Xunit;
using System;
using System.Buffers;
using Andromeda.Framing.UnitTests.Helpers;
using Andromeda.Framing.UnitTests.Metadata;

namespace Andromeda.Framing.UnitTests
{
    public class MetadataParserTests
    {
        private readonly IMetadataParser _parser = new IdPrefixedMetadataParser();
        
        [Theory, InlineData(1,8), InlineData(2, 64), InlineData(3, 4096)]
        public void TryParse_ShouldReturnTrue_OnValidFrame(short messageId, int length)
        {
            var frame = FrameProvider.GetRandomFrame(messageId, length);
            var reader = new SequenceReader<byte>(frame);
            var couldParse = _parser.TryParse(ref reader, out var metadata);

            Assert.True(couldParse);
            Assert.NotNull(metadata);
            Assert.Equal(metadata!.Length, length);
            Assert.Equal(((IdPrefixedMetadata)metadata).MessageId, messageId);
        }
        
        [Theory, InlineData(1, 8), InlineData(2, 64), InlineData(3, 4096)]
        public void Write_ShouldSerializeCorrectly(short messageId, int length)
        {
            var metadata = FrameProvider.GetRandomFrame(messageId, length)
                .Slice(0, 6).ToArray();

            var meta = new IdPrefixedMetadata(messageId, length);
            var writtenMetadata = new byte[_parser.GetLength(meta)];
            var writtenMetadataSpan = writtenMetadata.AsSpan();
            _parser.Write(ref writtenMetadataSpan, meta);

            Assert.Equal(metadata, writtenMetadata);
        }

        [Fact]
        public void TryParse_ShouldReturnFalse_OnIncompleteMetadata()
        {
            var frame = FrameProvider.GetRandomFrame(1, 5);
            var reader = new SequenceReader<byte>(frame.Slice(0, 5));
            var couldParse = _parser.TryParse(ref reader, out _);
            Assert.False(couldParse);
        }
    }
}
