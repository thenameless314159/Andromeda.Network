// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal
{
    internal sealed partial class SocketConnection : TransportConnection
    {
        private static readonly int MinAllocBufferSize = PinnedBlockMemoryPool.BlockSize / 2;

        private readonly Socket _socket;
        private readonly ISocketsTrace _trace;
        private readonly SocketReceiver _receiver;
        private SocketSender? _sender;
        private readonly SocketSenderPool _socketSenderPool;
        private readonly IDuplexPipe _originalTransport;
        private readonly CancellationTokenSource _connectionClosedTokenSource = new();

        private readonly object _shutdownLock = new();
        private volatile bool _socketDisposed;
        private volatile Exception? _shutdownReason;
        private Task? _sendingTask;
        private Task? _receivingTask;
        private readonly TaskCompletionSource _waitForConnectionClosedTcs = new();
        private bool _connectionClosed;
        private readonly bool _waitForData;

        internal SocketConnection(Socket socket,
                                  MemoryPool<byte> memoryPool,
                                  PipeScheduler transportScheduler,
                                  ISocketsTrace trace,
                                  SocketSenderPool socketSenderPool,
                                  PipeOptions inputOptions,
                                  PipeOptions outputOptions,
                                  bool waitForData = true)
        {
            Debug.Assert(socket != null);
            Debug.Assert(memoryPool != null);
            Debug.Assert(trace != null);

            _socket = socket;
            MemoryPool = memoryPool;
            _trace = trace;
            _waitForData = waitForData;
            _socketSenderPool = socketSenderPool;

            LocalEndPoint = _socket.LocalEndPoint;
            RemoteEndPoint = _socket.RemoteEndPoint;

            ConnectionClosed = _connectionClosedTokenSource.Token;

            // On *nix platforms, Sockets already dispatches to the ThreadPool.
            // Yes, the IOQueues are still used for the PipeSchedulers. This is intentional.
            // https://github.com/aspnet/KestrelHttpServer/issues/2573
            var awaiterScheduler = OperatingSystem.IsWindows() ? transportScheduler : PipeScheduler.Inline;

            _receiver = new SocketReceiver(awaiterScheduler);

            var pair = DuplexPipe.CreateConnectionPair(inputOptions, outputOptions);

            // Set the transport and connection id
            Transport = _originalTransport = pair.Transport;
            Application = pair.Application;

            InitializeFeatures();
        }

        public PipeWriter Input => Application.Output;

        public PipeReader Output => Application.Input;

        public override MemoryPool<byte> MemoryPool { get; }

        public void Start()
        {
            try
            {
                // Spawn send and receive logic
                _receivingTask = DoReceive();
                _sendingTask = DoSend();
            }
            catch (Exception ex)
            {
                _trace.LogError(0, ex, $"Unexpected exception in {nameof(SocketConnection)}.{nameof(Start)}.");
            }
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
            _originalTransport.Input.Complete();
            _originalTransport.Output.Complete();

            try
            {
                // Now wait for both to complete
                if (_receivingTask != null)
                {
                    await _receivingTask;
                }

                if (_sendingTask != null)
                {
                    await _sendingTask;
                }

            }
            catch (Exception ex)
            {
                _trace.LogError(0, ex, $"Unexpected exception in {nameof(SocketConnection)}.{nameof(Start)}.");
            }
            finally
            {
                _receiver.Dispose();
                _sender?.Dispose();
            }

            _connectionClosedTokenSource.Dispose();
        }

        private async Task DoReceive()
        {
            Exception? error = null;

            try
            {
                while (true)
                {
                    if (_waitForData)
                    {
                        // Wait for data before allocating a buffer.
                        await _receiver.WaitForDataAsync(_socket);
                    }

                    // Ensure we have some reasonable amount of buffer space
                    var buffer = Input.GetMemory(MinAllocBufferSize);

                    var bytesReceived = await _receiver.ReceiveAsync(_socket, buffer);

                    if (bytesReceived == 0)
                    {
                        // FIN
                        _trace.ConnectionReadFin(this);
                        break;
                    }

                    Input.Advance(bytesReceived);

                    var flushTask = Input.FlushAsync();

                    var paused = !flushTask.IsCompleted;

                    if (paused)
                    {
                        _trace.ConnectionPause(this);
                    }

                    var result = await flushTask;

                    if (paused)
                    {
                        _trace.ConnectionResume(this);
                    }

                    if (result.IsCompleted || result.IsCanceled)
                    {
                        // Pipe consumer is shut down, do we stop writing
                        break;
                    }
                }
            }
            catch (SocketException ex) when (IsConnectionResetError(ex.SocketErrorCode))
            {
                // This could be ignored if _shutdownReason is already set.
                error = new ConnectionResetException(ex.Message, ex);

                // There's still a small chance that both DoReceive() and DoSend() can log the same connection reset.
                // Both logs will have the same ConnectionId. I don't think it's worthwhile to lock just to avoid this.
                if (!_socketDisposed)
                {
                    _trace.ConnectionReset(this);
                }
            }
            catch (Exception ex)
                when ((ex is SocketException socketEx && IsConnectionAbortError(socketEx.SocketErrorCode)) ||
                       ex is ObjectDisposedException)
            {
                // This exception should always be ignored because _shutdownReason should be set.
                error = ex;

                if (!_socketDisposed)
                {
                    // This is unexpected if the socket hasn't been disposed yet.
                    _trace.ConnectionError(this, error);
                }
            }
            catch (Exception ex)
            {
                // This is unexpected.
                error = ex;
                _trace.ConnectionError(this, error);
            }
            finally
            {
                // If Shutdown() has already bee called, assume that was the reason ProcessReceives() exited.
                Input.Complete(_shutdownReason ?? error);

                FireConnectionClosed();

                await _waitForConnectionClosedTcs.Task;
            }
        }

        private async Task DoSend()
        {
            Exception? shutdownReason = null;
            Exception? unexpectedError = null;

            try
            {
                while (true)
                {
                    var result = await Output.ReadAsync();

                    if (result.IsCanceled)
                    {
                        break;
                    }
                    var buffer = result.Buffer;

                    if (!buffer.IsEmpty)
                    {
                        _sender = _socketSenderPool.Rent();
                        await _sender.SendAsync(_socket, buffer);
                        // We don't return to the pool if there was an exception, and
                        // we keep the _sender assigned so that we can dispose it in StartAsync.
                        _socketSenderPool.Return(_sender);
                        _sender = null;
                    }

                    Output.AdvanceTo(buffer.End);

                    if (result.IsCompleted)
                    {
                        break;
                    }
                }
            }
            catch (SocketException ex) when (IsConnectionResetError(ex.SocketErrorCode))
            {
                shutdownReason = new ConnectionResetException(ex.Message, ex);
                _trace.ConnectionReset(this);
            }
            catch (Exception ex)
                when ((ex is SocketException socketEx && IsConnectionAbortError(socketEx.SocketErrorCode)) ||
                       ex is ObjectDisposedException)
            {
                // This should always be ignored since Shutdown() must have already been called by Abort().
                shutdownReason = ex;
            }
            catch (Exception ex)
            {
                shutdownReason = ex;
                unexpectedError = ex;
                _trace.ConnectionError(this, unexpectedError);
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

        private void FireConnectionClosed()
        {
            // Guard against scheduling this multiple times
            if (_connectionClosed)
            {
                return;
            }

            _connectionClosed = true;

            ThreadPool.UnsafeQueueUserWorkItem(state =>
            {
                state.CancelConnectionClosedToken();

                state._waitForConnectionClosedTcs.TrySetResult();
            },
            this,
            preferLocal: false);
        }

        private void Shutdown(Exception? shutdownReason)
        {
            lock (_shutdownLock)
            {
                if (_socketDisposed)
                {
                    return;
                }

                // Make sure to close the connection only after the _aborted flag is set.
                // Without this, the RequestsCanBeAbortedMidRead test will sometimes fail when
                // a BadHttpRequestException is thrown instead of a TaskCanceledException.
                _socketDisposed = true;

                // shutdownReason should only be null if the output was completed gracefully, so no one should ever
                // ever observe the nondescript ConnectionAbortedException except for connection middleware attempting
                // to half close the connection which is currently unsupported.
                _shutdownReason = shutdownReason ?? new ConnectionAbortedException("The Socket transport's send loop completed gracefully.");
                _trace.ConnectionWriteFin(this, _shutdownReason.Message);

                try
                {
                    // Try to gracefully close the socket even for aborts to match libuv behavior.
                    _socket.Shutdown(SocketShutdown.Both);
                }
                catch
                {
                    // Ignore any errors from Socket.Shutdown() since we're tearing down the connection anyway.
                }

                _socket.Dispose();
            }
        }

        private void CancelConnectionClosedToken()
        {
            try
            {
                _connectionClosedTokenSource.Cancel();
            }
            catch (Exception ex)
            {
                _trace.LogError(0, ex, $"Unexpected exception in {nameof(SocketConnection)}.{nameof(CancelConnectionClosedToken)}.");
            }
        }

        private static bool IsConnectionResetError(SocketError errorCode)
        {
            return errorCode == SocketError.ConnectionReset ||
                   errorCode == SocketError.Shutdown ||
                   (errorCode == SocketError.ConnectionAborted && OperatingSystem.IsWindows());
        }

        private static bool IsConnectionAbortError(SocketError errorCode)
        {
            // Calling Dispose after ReceiveAsync can cause an "InvalidArgument" error on *nix.
            return errorCode == SocketError.OperationAborted ||
                   errorCode == SocketError.Interrupted ||
                   (errorCode == SocketError.InvalidArgument && !OperatingSystem.IsWindows());
        }
    }
}
