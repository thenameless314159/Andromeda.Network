// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;


namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal
{
    internal sealed class SocketConnection : ConnectionContext, IConnectionInherentKeepAliveFeature, IMemoryPoolFeature
    {
        private static readonly int MinAllocBufferSize = SlabMemoryPool.BlockSize / 2;
        private readonly CancellationTokenSource _connectionClosedTokenSource = new();
        private readonly TaskCompletionSource _waitForConnectionClosedTcs = new();
        private readonly object _shutdownLock = new();
        private volatile Exception? _shutdownReason;
        private readonly SocketReceiver _receiver;
        private readonly SocketSender _sender;
        private readonly ISocketsTrace _trace;
        private volatile bool _socketDisposed;
        private readonly bool _waitForData;
        private readonly Socket _socket;
        private bool _connectionClosed;
        private Task? _processingTask;

        internal SocketConnection(
            Socket socket, 
            MemoryPool<byte> memoryPool, 
            PipeScheduler transportScheduler,
            ISocketsTrace trace,
            long? maxReadBufferSize = null,
            long? maxWriteBufferSize = null,
            bool waitForData = true,
            bool useInlineSchedulers = false)
        {
            _waitForData = waitForData;
            MemoryPool = memoryPool;
            _socket = socket;
            _trace = trace;

            maxReadBufferSize ??= 0;
            maxWriteBufferSize ??= 0;
            LocalEndPoint = _socket.LocalEndPoint;
            RemoteEndPoint = _socket.RemoteEndPoint;
            ConnectionClosed = _connectionClosedTokenSource.Token;

            // On *nix platforms, Sockets already dispatches to the ThreadPool.
            // Yes, the IOQueues are still used for the PipeSchedulers. This is intentional.
            // https://github.com/aspnet/KestrelHttpServer/issues/2573
            var awaiterScheduler = OperatingSystem.IsWindows() ? transportScheduler : PipeScheduler.Inline;

            var applicationScheduler = PipeScheduler.ThreadPool;
            if (useInlineSchedulers)
            {
                transportScheduler = PipeScheduler.Inline;
                awaiterScheduler = PipeScheduler.Inline;
                applicationScheduler = PipeScheduler.Inline;
            }

            _receiver = new SocketReceiver(_socket, awaiterScheduler);
            _sender = new SocketSender(_socket, awaiterScheduler);

            var outputOptions = new PipeOptions(MemoryPool, transportScheduler, applicationScheduler, maxWriteBufferSize.Value, maxWriteBufferSize.Value / 2, useSynchronizationContext: false);
            var inputOptions = new PipeOptions(MemoryPool, applicationScheduler, transportScheduler, maxReadBufferSize.Value, maxReadBufferSize.Value / 2, useSynchronizationContext: false);
            var pair = DuplexPipe.CreateConnectionPair(inputOptions, outputOptions);
           
            // Set the transport, connection id and features
            ConnectionId = Guid.NewGuid().ToString();
            Application = pair.Application;
            Transport = pair.Transport;

            Features = new FeatureCollection();
            Features.Set<IMemoryPoolFeature>(this);
            Features.Set<IConnectionInherentKeepAliveFeature>(this);
        }

        public override IDictionary<object, object?> Items { get; set; } = default!;
        public override IFeatureCollection Features { get; }
        public override IDuplexPipe Transport { get; set; }
        public override string ConnectionId { get; set; }
        public IDuplexPipe Application { get; set; }
        
        // We claim to have inherent keep-alive so the client doesn't kill the connection when it hasn't seen ping frames.
        public bool HasInherentKeepAlive { get; } = true;
        public PipeWriter Input => Application.Output;
        public PipeReader Output => Application.Input;
        public MemoryPool<byte> MemoryPool { get; }

        public void Start() => _processingTask = StartAsync();

        private async Task StartAsync()
        {
            try
            {
                // Spawn send and receive logic
                var receiveTask = DoReceive();
                var sendTask = DoSend();

                // Now wait for both to complete
                await receiveTask; await sendTask;
                _receiver.Dispose(); _sender.Dispose();
            }
            catch (Exception ex) { _trace.LogError(0, ex, $"Unexpected exception in {nameof(SocketConnection)}.{nameof(StartAsync)}."); }
        }

        public override void Abort(ConnectionAbortedException abortReason)
        {
            // Try to gracefully close the socket to match libuv behavior.
            Shutdown(abortReason);

            // Cancel ProcessSends loop after calling shutdown to ensure the correct _shutdownReason gets set.
            Output.CancelPendingRead();
        }

        // Only called after connection middleware is complete which means the ConnectionClosed token has fired.
        public override async ValueTask DisposeAsync()
        {
            try { // Complete the transport pipes
                await Transport.Output.CompleteAsync().ConfigureAwait(false);
                await Transport.Input.CompleteAsync().ConfigureAwait(false);
            }
            catch { /* discard any unexpected exception at this point */ }

            if (_processingTask != null) await _processingTask;
            _connectionClosedTokenSource.Dispose();
        }

        private async Task DoReceive()
        {
            Exception? error = null;

            try
            {
                await ProcessReceives();
            }
            catch (SocketException ex) when (IsConnectionResetError(ex.SocketErrorCode))
            {
                // This could be ignored if _shutdownReason is already set.
                error = new ConnectionResetException(ex.Message, ex);

                // There's still a small chance that both DoReceive() and DoSend() can log the same connection reset.
                // Both logs will have the same ConnectionId. I don't think it's worthwhile to lock just to avoid this.
                if (!_socketDisposed)
                {
                    _trace.ConnectionReset(ConnectionId);
                }
            }
            catch (Exception ex)
                when (ex is SocketException socketEx && IsConnectionAbortError(socketEx.SocketErrorCode) ||
                       ex is ObjectDisposedException)
            {
                // This exception should always be ignored because _shutdownReason should be set.
                error = ex;

                if (!_socketDisposed)
                {
                    // This is unexpected if the socket hasn't been disposed yet.
                    _trace.ConnectionError(ConnectionId, error);
                }
            }
            catch (Exception ex)
            {
                // This is unexpected.
                error = ex;
                _trace.ConnectionError(ConnectionId, error);
            }
            finally
            {
                // If Shutdown() has already bee called, assume that was the reason ProcessReceives() exited.
                Input.Complete(_shutdownReason ?? error);
                FireConnectionClosed();

                await _waitForConnectionClosedTcs.Task;
            }
        }

        private async Task ProcessReceives()
        {
            // Resolve `input` PipeWriter via the IDuplexPipe interface prior to loop start for performance.
            var input = Input;
            while (true)
            {
                if (_waitForData)
                {
                    // Wait for data before allocating a buffer.
                    await _receiver.WaitForDataAsync();
                }

                // Ensure we have some reasonable amount of buffer space
                var buffer = input.GetMemory(MinAllocBufferSize);

                var bytesReceived = await _receiver.ReceiveAsync(buffer);

                if (bytesReceived == 0)
                {
                    // FIN
                    _trace.ConnectionReadFin(ConnectionId);
                    break;
                }

                input.Advance(bytesReceived);

                var flushTask = input.FlushAsync();
                var paused = !flushTask.IsCompleted;

                if (paused) _trace.ConnectionPause(ConnectionId);

                var result = await flushTask;

                if (paused) _trace.ConnectionResume(ConnectionId);

                if (result.IsCompleted || result.IsCanceled)
                {
                    // Pipe consumer is shut down, do we stop writing
                    break;
                }
            }
        }

        private async Task DoSend()
        {
            Exception? unexpectedError = null;
            Exception? shutdownReason = null;
            
            try { await ProcessSends(); }
            catch (SocketException ex) when (IsConnectionResetError(ex.SocketErrorCode))
            {
                shutdownReason = new ConnectionResetException(ex.Message, ex);
                _trace.ConnectionReset(ConnectionId);
            }
            catch (Exception ex)
                when (ex is SocketException socketEx && IsConnectionAbortError(socketEx.SocketErrorCode) ||
                       ex is ObjectDisposedException)
            {
                // This should always be ignored since Shutdown() must have already been called by Abort().
                shutdownReason = ex;
            }
            catch (Exception ex)
            {
                shutdownReason = ex;
                unexpectedError = ex;
                _trace.ConnectionError(ConnectionId, unexpectedError);
            }
            finally
            {
                Shutdown(shutdownReason);

                // Complete the output after disposing the socket
                Output.Complete(unexpectedError);

                // Cancel any pending flushes so that the input loop is un-paused
                Input.CancelPendingFlush();
            }
        }

        private async Task ProcessSends()
        {
            // Resolve `output` PipeReader via the IDuplexPipe interface prior to loop start for performance.
            var output = Output;
            while (true)
            {
                var result = await output.ReadAsync();
                if (result.IsCanceled) break;

                var buffer = result.Buffer;
                var end = buffer.End;

                var isCompleted = result.IsCompleted;
                if (!buffer.IsEmpty) await _sender.SendAsync(buffer);

                output.AdvanceTo(end);
                if (isCompleted) break;
            }
        }

        private void FireConnectionClosed()
        {
            // Guard against scheduling this multiple times
            if (_connectionClosed) return;
            _connectionClosed = true;

            ThreadPool.UnsafeQueueUserWorkItem(state => {
                state.CancelConnectionClosedToken();
                state._waitForConnectionClosedTcs.TrySetResult();
            },
            this, preferLocal: false);
        }

        private void Shutdown(Exception? shutdownReason)
        {
            lock (_shutdownLock)
            {
                if (_socketDisposed) return;

                // Make sure to close the connection only after the _aborted flag is set.
                // Without this, the RequestsCanBeAbortedMidRead test will sometimes fail when
                // a BadHttpRequestException is thrown instead of a TaskCanceledException.
                _socketDisposed = true;

                // shutdownReason should only be null if the output was completed gracefully, so no one should ever
                // ever observe the nondescript ConnectionAbortedException except for connection middleware attempting
                // to half close the connection which is currently unsupported.
                _shutdownReason = shutdownReason ?? new ConnectionAbortedException("The Socket transport's send loop completed gracefully.");
                _trace.ConnectionWriteFin(ConnectionId, _shutdownReason.Message);

                try
                {
                    // Try to gracefully close the socket even for aborts to match libuv behavior.
                    _socket.Shutdown(SocketShutdown.Both);
                }
                catch { /* Ignore any errors from Socket.Shutdown() since we're tearing down the connection anyway. */ }

                _socket.Dispose();
            }
        }

        private void CancelConnectionClosedToken()
        {
            try { _connectionClosedTokenSource.Cancel(); }
            catch (Exception ex) { _trace.LogError(0, ex, $"Unexpected exception in {nameof(SocketConnection)}.{nameof(CancelConnectionClosedToken)}."); }
        }

        private static bool IsConnectionResetError(SocketError errorCode) =>
            errorCode == SocketError.ConnectionReset ||
            errorCode == SocketError.Shutdown ||
            errorCode == SocketError.ConnectionAborted && OperatingSystem.IsWindows();

        // Calling Dispose after ReceiveAsync can cause an "InvalidArgument" error on *nix.
        private static bool IsConnectionAbortError(SocketError errorCode) =>
            errorCode == SocketError.OperationAborted ||
            errorCode == SocketError.Interrupted ||
            errorCode == SocketError.InvalidArgument && !OperatingSystem.IsWindows();
    }
}
