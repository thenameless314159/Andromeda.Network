using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Andromeda.Network.Internal;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Andromeda.Network
{
    internal sealed class ServerListener
    {
        private readonly ConcurrentDictionary<long, (ServerConnection Connection, Task ExecutionTask)> _connections;
        private readonly ConnectionDelegate _application;
        private readonly ServerLogPolicy _logPolicy;
        private readonly Task _shutdownTask;
        private readonly ILogger _logger;

        public IConnectionListener Listener { get; }
        public Task? ExecutionTask { get; private set; }

        public ServerListener(IConnectionListener listener, ConnectionDelegate application, Task? shutdownTask = default, 
            ILogger? logger = default, ServerLogPolicy? logPolicy = default)
        {
            _connections = new ConcurrentDictionary<long, (ServerConnection Context, Task ExecutionTask)>();
            _shutdownTask = shutdownTask ?? Task.CompletedTask;
            _logPolicy = logPolicy ?? new ServerLogPolicy();
            _logger = logger ?? NullLogger.Instance;
            _application = application;
            Listener = listener;
        }

        public void Start() => ExecutionTask = RunListenerAsync();
        public void TickHeartbeat() { foreach (var (_, (connection, _)) in _connections) connection.TickHeartbeat(); }

        private async Task RunListenerAsync()
        {
            await using var listener = Listener;
            var app = _application;

            if(_logPolicy.LogStartedListening) _logger.Log(_logPolicy.MessageLogLevel, 
                "Now listening on: {EndPoint}", listener.EndPoint);

            for (long id = 0; true ;id++)
            {
                try
                {
                    var connection = await listener.AcceptAsync().ConfigureAwait(false);
                    // Null means we don't have anymore connections
                    if (connection == default) break;

                    var networkConnection = new ServerConnection(id, connection, _logger);
                    _connections[id] = (networkConnection, StartConnectionAsync(networkConnection, app));
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _logger.LogCritical(ex, "Stopped accepting connections on network server at '{endpoint}' !", listener.EndPoint);
                    break;
                }
            }

            if (_logPolicy.LogStoppedListening) _logger.Log(_logPolicy.MessageLogLevel, 
                "Stopped listening on: {EndPoint}", listener.EndPoint);

            // Don't shut down connections until entire server is shutting down
            await _shutdownTask.ConfigureAwait(false);

            // Give connections a chance to close gracefully
            var tasks = new List<Task>(_connections.Count);
            foreach (var (_, (connection, task)) in _connections) {
                connection.RequestClose(); 
                tasks.Add(task);
            }

            if (await Task.WhenAll(tasks).TimeoutAfter(TimeSpan.FromSeconds(5)).ConfigureAwait(false))
                return;

            // If they didn't, abort try via Abort()
            const string serverStopped = "The connection was aborted because the server was stopped.";
            foreach (var (_, (connection, _)) in _connections) 
                connection.Transport.Abort(new ConnectionAbortedException(serverStopped));

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private async Task StartConnectionAsync(ServerConnection connection, ConnectionDelegate execute)
        {
            await Task.Yield();
            var transport = connection.Transport;

            using var scope = BeginConnectionScope(connection.Transport);
            const string acceptedMessage = "Connection with Id={ConnectionId} has successfully been accepted on network server at '{EndPoint}'.";
            if (_logPolicy.LogConnectionAccepted) _logger.Log(_logPolicy.MessageLogLevel, acceptedMessage, 
                transport.ConnectionId, Listener.EndPoint);

            try { await execute(transport).ConfigureAwait(false); }
            catch (ConnectionAbortedException e) when (!string.IsNullOrWhiteSpace(e.Message))
            {
                if (!_logPolicy.LogConnectionAborted) return;
                const string abortedMessage = "Connection with Id={ConnectionId} was aborted on network server at '{EndPoint}' with reason : {Message}";
                if (e.InnerException is not null) _logger.LogError(e.InnerException, abortedMessage, transport.ConnectionId, Listener.EndPoint, e.Message);
                else _logger.Log(_logPolicy.MessageLogLevel, abortedMessage, transport.ConnectionId, Listener.EndPoint, e.Message);
            }
            catch (ConnectionAbortedException) { /* Don't let connection aborted exceptions out */ }
            catch (ConnectionResetException) { /* Don't let connection reset exceptions out */ }
            catch (Exception e) 
            {
                const string errorMessage =
                    "Connection with Id={ConnectionId} was stopped on network server at '{EndPoint}' because an unexpected exception has been caught.";
                _logger.LogError(e, errorMessage, transport.ConnectionId, Listener.EndPoint);
            }
            finally
            {
                await connection.FireOnCompletedAsync().ConfigureAwait(false);
                await transport.DisposeAsync().ConfigureAwait(false);

                // Remove the connection from tracking
                _connections.TryRemove(connection.Id, out _);

                const string completedMessage = "Connection with Id={ConnectionId} has successfully been completed on network server at '{EndPoint}'.";
                if (_logPolicy.LogConnectionCompleted) _logger.Log(_logPolicy.MessageLogLevel, completedMessage, transport.ConnectionId, Listener.EndPoint);
            }
        }

        private IDisposable? BeginConnectionScope(ConnectionContext transport) => _logger.IsEnabled(LogLevel.Critical)
            ? _logger.BeginScope(new ConnectionLogScope(transport.ConnectionId))
            : null;
    }
}
