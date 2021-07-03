using System.Buffers;
using Andromeda.Framing.Extensions.UnitTests.Infrastructure;
using Andromeda.Framing.Extensions.UnitTests.Models;
using Xunit;

namespace Andromeda.Framing.Extensions.UnitTests
{
    public class MessageWriterTests
    {
        private readonly IdPrefixedMessageWriter _messageWriter = new();
        private readonly IdPrefixedMetadataParser _parser = new();

        [Fact]
        public void Encode_ShouldWriteMessageWithCorrectLength_AccordingToBytesWritten()
        {
            var writer = new ArrayBufferWriter<byte>();
            var msg = new SmallerBytesWrittenMessage {Number = byte.MaxValue};
            _messageWriter.Encode(msg, writer);

            Assert.Equal(8, writer.WrittenCount);

            var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(writer.WrittenMemory));
            Assert.True(_parser.TryParse(ref reader, out var metadata));
            var meta = Assert.IsType<IdPrefixedMetadata>(metadata);

            Assert.Equal(4, meta.MessageId);
            Assert.Equal(2, meta.Length);
        }
    }
}
