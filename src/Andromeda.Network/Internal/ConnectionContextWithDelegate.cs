using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Features;

namespace Andromeda.Network
{
    internal class ConnectionContextWithDelegate : ConnectionContext
    {
        private readonly TaskCompletionSource<object?> _executionTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private Task _middlewareTask = Task.CompletedTask;

        public ConnectionContextWithDelegate(ConnectionContext connection, ConnectionDelegate connectionDelegate) =>
            (_connectionDelegate, _connection) = (connectionDelegate, connection);
        
        private readonly ConnectionDelegate _connectionDelegate;
        private readonly ConnectionContext _connection;

        // Execute the middleware pipeline
        public void Start() => _middlewareTask = RunMiddleware();

        private async Task RunMiddleware()
        {
            try { await _connectionDelegate(this).ConfigureAwait(false); }
            catch (Exception ex) {
                // If we failed to initialize, bubble that exception to the caller.
                // If we've already initialized then this will noop
                Initialized.TrySetException(ex);
            }
        }

        public TaskCompletionSource<ConnectionContext> Initialized { get; set; } = new();

        public Task ExecutionTask => _executionTcs.Task;

        public override string ConnectionId
        {
            get => _connection.ConnectionId;
            set => _connection.ConnectionId = value;
        }

        public override IFeatureCollection Features => _connection.Features;

        public override IDictionary<object, object?> Items
        {
            get => _connection.Items;
            set => _connection.Items = value;
        }

        public override IDuplexPipe Transport
        {
            get => _connection.Transport;
            set => _connection.Transport = value;
        }

        public override EndPoint? LocalEndPoint
        {
            get => _connection.LocalEndPoint;
            set => _connection.LocalEndPoint = value;
        }

        public override EndPoint? RemoteEndPoint
        {
            get => _connection.RemoteEndPoint;
            set => _connection.RemoteEndPoint = value;
        }

        public override CancellationToken ConnectionClosed
        {
            get => _connection.ConnectionClosed;
            set => _connection.ConnectionClosed = value;
        }

        public override void Abort() => _connection.Abort();
        public override void Abort(ConnectionAbortedException abortReason) => _connection.Abort(abortReason);

        public override async ValueTask DisposeAsync()
        {
            await _connection.DisposeAsync().ConfigureAwait(false);

            _executionTcs.TrySetResult(null);

            await _middlewareTask.ConfigureAwait(false);
        }
    }
}
