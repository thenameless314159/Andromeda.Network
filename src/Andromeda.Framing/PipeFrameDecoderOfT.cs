using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Andromeda.Framing
{
    /// <inheritdoc cref="IFrameDecoder{TMetadata}"/>
    public class PipeFrameDecoder<TMeta> : PipeFrameDecoder, IFrameDecoder<TMeta>
        where TMeta : class, IFrameMetadata
    {
        /// <inheritdoc />
        public PipeFrameDecoder(PipeReader pipe, IMetadataDecoder decoder) : base(pipe, decoder) { }

        /// <inheritdoc />
        public PipeFrameDecoder(Stream stream, IMetadataDecoder decoder) : base(stream, decoder) { }

        /// <inheritdoc />
        public new ValueTask<Frame<TMeta>> ReadFrameAsync(CancellationToken token = default)
        {
            static async ValueTask<Frame<TMeta>> awaitAndReturn(ValueTask<Frame> readTask) {
                var r = await readTask.ConfigureAwait(false);
                return r.AsTyped<TMeta>();
            }

            var readAsync = base.ReadFrameAsync(token);
            return readAsync.IsCompletedSuccessfully 
                ? ValueTask.FromResult(readAsync.Result.AsTyped<TMeta>())
                : awaitAndReturn(readAsync);
        }

        /// <inheritdoc />
        public new async IAsyncEnumerable<Frame<TMeta>> ReadFramesAsync([EnumeratorCancellation]CancellationToken token = default) {
            await foreach (var frame in base.ReadFramesAsync(token)) yield return frame.AsTyped<TMeta>();
        }
    }
}
