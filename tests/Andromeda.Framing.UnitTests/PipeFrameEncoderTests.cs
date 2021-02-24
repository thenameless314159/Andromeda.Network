using System;
using System.IO.Pipelines;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Andromeda.Framing.Extensions;
using Andromeda.Framing.UnitTests.Metadata;
using Xunit;

namespace Andromeda.Framing.UnitTests
{
    public class PipeFrameEncoderTests
    {
        private readonly IMetadataParser _parser = new IdPrefixedMetadataParser();
        private static (IFrameEncoder, Pipe) CreateEncoder(IMetadataEncoder encoder) { var pipe = new Pipe();
            return (pipe.Writer.AsFrameEncoder(encoder), pipe);
        }

        [Fact]
        public async Task ShouldThrow_WhenDecoderHasDisposed()
        {
            static async Task writeEmptyAsync(IFrameEncoder encoder)
            {
                await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
                    { await encoder.WriteAsync(Frame.Empty); }).ConfigureAwait(false);
                await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
                    { await encoder.WriteAsync(Array.Empty<Frame>()); }).ConfigureAwait(false);
                await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
                    { await encoder.WriteAsync(AsyncEnumerable.Empty<Frame>()); }).ConfigureAwait(false);
            }
            var (encoder, _) = CreateEncoder(_parser); encoder.Dispose();
            await writeEmptyAsync(encoder).ConfigureAwait(false);
        }

        [Fact]
        public async Task ShouldThrow_WhenPipeIsNull()
        {
            static async Task writeEmptyAsync(IFrameEncoder encoder)
            {
                await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
                    { await encoder.WriteAsync(Frame.Empty); }).ConfigureAwait(false);
                await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
                    { await encoder.WriteAsync(Array.Empty<Frame>()); }).ConfigureAwait(false);
                await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
                    { await encoder.WriteAsync(AsyncEnumerable.Empty<Frame>()); }).ConfigureAwait(false);
            }
            var encoder = default(PipeWriter)!.AsFrameEncoder(_parser);
            await writeEmptyAsync(encoder).ConfigureAwait(false);
        }

        [Fact]
        public async Task ShouldThrow_WhenEncoderReturnInvalidLength()
        {
            var (encoder, _) = CreateEncoder(new InvalidMetadataEncoder());
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                { await encoder.WriteAsync(Frame.Empty); }).ConfigureAwait(false);
        }

        [Fact]
        public async Task ShouldReturn_WhenPipeHasAlreadyBeenCompleted()
        {
            static async Task writeEmptyAsync(IFrameEncoder encoder)
            {
                await encoder.WriteAsync(Frame.Empty);
                await encoder.WriteAsync(Array.Empty<Frame>());
                await encoder.WriteAsync(AsyncEnumerable.Empty<Frame>());
            }
            var (encoder, pipe) = CreateEncoder(_parser); pipe.Writer.Complete();
            await writeEmptyAsync(encoder).ConfigureAwait(false);
        }
    }
}
