using Xunit;
using System;
using System.IO.Pipelines;
using System.Threading.Tasks;
using Andromeda.Framing.Extensions;
using Andromeda.Framing.UnitTests.Metadata;

namespace Andromeda.Framing.UnitTests
{
    public class PipeFrameDecoderTests
    {
        private readonly IMetadataParser _parser = new IdPrefixedMetadataParser();
        protected virtual (IFrameDecoder, Pipe) CreateDecoder(IMetadataDecoder decoder) {
            var pipe = new Pipe(); return (pipe.Reader.AsFrameDecoder(decoder), pipe);
        }

        [Fact]
        public async Task ReadFrames_ShouldNotMoveNext_WhenDecoderHasBeenDisposed()
        {
            var (decoder, _) = CreateDecoder(_parser); decoder.Dispose();
            await using var enumerator = decoder.ReadFramesAsync().GetAsyncEnumerator();
            Assert.False(await enumerator.MoveNextAsync());
        }

        [Fact]
        public async Task ReadFrames_ShouldNotMoveNext_WhenPipeHasAlreadyBeenCompleted()
        {
            var (decoder, pipe) = CreateDecoder(_parser); pipe.Reader.Complete();
            await using var enumerator = decoder.ReadFramesAsync().GetAsyncEnumerator();
            Assert.False(await enumerator.MoveNextAsync());
        }

        [Fact]
        public async Task ReadFrames_ShouldNotMoveNext_WhenPipeIsNull()
        {
            var decoder = default(PipeReader)!.AsFrameDecoder(_parser);
            await using var enumerator = decoder.ReadFramesAsync().GetAsyncEnumerator();
            Assert.False(await enumerator.MoveNextAsync());
        }

        [Fact]
        public async Task ReadFrames_ShouldThrow_WhenDecoderReturnInvalidMetadataLength()
        {
            var (decoder, pipe) = CreateDecoder(new InvalidMetadataDecoder());

            // write dummy value so the decoder can read
            await pipe.Writer.WriteAsync(new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3 }));
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await using var enumerator = decoder.ReadFramesAsync().GetAsyncEnumerator();
                await enumerator.MoveNextAsync();
            }).ConfigureAwait(false);
        }

        [Fact]
        public async Task ReadFrame_ShouldThrow_WhenDecoderHasBeenDisposed()
        {
            var (decoder, _) = CreateDecoder(_parser); 
            decoder.Dispose();

            await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
                { await decoder.ReadFrameAsync(); }).ConfigureAwait(false);
        }

        [Fact]
        public async Task ReadFrame_ShouldThrow_WhenPipeHasAlreadyBeenCompleted()
        {
           var (decoder, pipe) = CreateDecoder(_parser); 
           pipe.Reader.Complete();

            await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
                { await decoder.ReadFrameAsync(); }).ConfigureAwait(false);
        }

        [Fact]
        public async Task ReadFrame_ShouldThrow_WhenPipeIsNull()
        {
            var decoder = default(PipeReader)!.AsFrameDecoder(_parser);

            await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
                { await decoder.ReadFrameAsync(); }).ConfigureAwait(false);
        }

        [Fact]
        public async Task ReadFrame_ShouldThrow_WhenDecoderReturnInvalidMetadataLength()
        {
            var (decoder, pipe) = CreateDecoder(new InvalidMetadataDecoder());

            // write dummy value so the decoder can read
            await pipe.Writer.WriteAsync(new ReadOnlyMemory<byte>(new byte[] {1, 2, 3}));
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            { await decoder.ReadFrameAsync(); }).ConfigureAwait(false);
        }
    }
}
