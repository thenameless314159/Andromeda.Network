using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System;

namespace Andromeda.Network
{
    internal sealed class ServerConnection : IConnectionEndPointFeature, IConnectionCompleteFeature, IConnectionHeartbeatFeature, IConnectionLifetimeNotificationFeature
    {
        private List<(Action<object> handler, object state)>? _heartbeatHandlers;
        private readonly object _heartbeatLock = new();

        private Stack<KeyValuePair<Func<object, Task>, object>>? _onCompleted;
        private bool _completed;

        private readonly CancellationTokenSource _connectionClosingCts = new();
        private readonly ILogger _logger;

        public CancellationToken ConnectionClosedRequested { get; set; }
        public ConnectionContext Transport { get; }
        public long Id { get; }

        public ServerConnection(long id, ConnectionContext transport, ILogger logger)
        {
            ConnectionClosedRequested = _connectionClosingCts.Token;
            Transport = transport;
            _logger = logger;
            Id = id;
            
            transport.Features.Set<IConnectionCompleteFeature>(this);
            transport.Features.Set<IConnectionHeartbeatFeature>(this);
            transport.Features.Set<IConnectionLifetimeNotificationFeature>(this);

            var endpointFeature = transport.Features[typeof(IConnectionEndPointFeature)];
            if (endpointFeature is null) transport.Features.Set<IConnectionEndPointFeature>(this);
        }

        public EndPoint? LocalEndPoint
        {
            get => Transport.LocalEndPoint;
            set => Transport.LocalEndPoint = value;
        }

        public EndPoint? RemoteEndPoint
        {
            get => Transport.RemoteEndPoint;
            set => Transport.RemoteEndPoint = value;
        }

        public void RequestClose()
        {
            try { _connectionClosingCts.Cancel(); }
            catch (ObjectDisposedException) {
                // There's a race where the token could be disposed
                // swallow the exception and no-op
            }
        }

        public void TickHeartbeat()
        {
            lock (_heartbeatLock) {
                if (_heartbeatHandlers is null) return;

                foreach (var (handler, state) in _heartbeatHandlers) handler(state);
            }
        }

        public void OnTimeout(string reason) => Transport.Abort(new ConnectionAbortedException(reason));

        public void OnHeartbeat(Action<object> action, object state)
        {
            lock (_heartbeatLock) {
                _heartbeatHandlers ??= new List<(Action<object> handler, object state)>();
                _heartbeatHandlers.Add((action, state));
            }
        }

        void IConnectionCompleteFeature.OnCompleted(Func<object, Task> callback, object state)
        {
            if (_completed) throw new InvalidOperationException("The connection is already complete.");

            _onCompleted ??= new Stack<KeyValuePair<Func<object, Task>, object>>();
            _onCompleted.Push(new KeyValuePair<Func<object, Task>, object>(callback, state));
        }

        public Task FireOnCompletedAsync()
        {
            if (_completed) throw new InvalidOperationException("The connection is already complete.");
            _completed = true;

            var onCompleted = _onCompleted;
            if (onCompleted == null || onCompleted.Count == 0) return Task.CompletedTask;
            return CompleteAsyncMayAwait(onCompleted);
        }

        private Task CompleteAsyncMayAwait(Stack<KeyValuePair<Func<object, Task>, object>> onCompleted)
        {
            async Task completeAsyncAwaited(Task currentTask, Stack<KeyValuePair<Func<object, Task>, object>> remaining)
            {
                try { await currentTask.ConfigureAwait(false); }
                catch (Exception ex) { _logger.LogError(ex, "An error occurred running an IConnectionCompleteFeature.OnCompleted callback."); }

                while (remaining.TryPop(out var entry)) {
                    try { await entry.Key.Invoke(entry.Value).ConfigureAwait(false); }
                    catch (Exception ex) { _logger.LogError(ex, "An error occurred running an IConnectionCompleteFeature.OnCompleted callback."); }
                }
            }

            while (onCompleted.TryPop(out var entry))
            {
                try {
                    var task = entry.Key.Invoke(entry.Value);
                    if (!task.IsCompletedSuccessfully) return completeAsyncAwaited(task, onCompleted);
                }
                catch (Exception ex) { _logger.LogError(ex, "An error occurred running an IConnectionCompleteFeature.OnCompleted callback."); }
            }

            return Task.CompletedTask;
        }

        public override string ToString() => Transport.ConnectionId;
    }
}
