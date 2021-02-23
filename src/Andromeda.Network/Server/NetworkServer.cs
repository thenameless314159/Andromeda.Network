using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Andromeda.Network.Features;
using System.Collections.Generic;
using Andromeda.Network.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http.Features;

namespace Andromeda.Network
{
    public class NetworkServer : INetworkServer
    {
        public IFeatureCollection Features { get; } = new FeatureCollection();
        private readonly List<ServerListener> _runningListeners = new();
        private readonly TaskCompletionSource<object?> _shutdownTcs;
        private readonly TimerAwaitable _timerAwaitable;
        private readonly ServerBuilder _builder;
        private readonly ILogger _logger;

        internal NetworkServer(ServerBuilder builder)
        {
            _timerAwaitable = new TimerAwaitable(builder.HeartBeatInterval, builder.HeartBeatInterval);
            _shutdownTcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
            _logger = builder.ApplicationServices.GetLoggerFactory().CreateLogger<NetworkServer>();
            _builder = builder;
        }

        private Task _timerTask = Task.CompletedTask;

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            var listeningEndpoints = new List<EndPoint>(_builder.Bindings.Count);
            try
            {
                foreach (var (endPoint, connectionDelegate, logPolicy, listenerFactory) in _builder.Bindings)
                {
                    var connectionListener = await listenerFactory.BindAsync(endPoint, cancellationToken).ConfigureAwait(false);
                    var serverListener = new ServerListener(connectionListener, connectionDelegate, _shutdownTcs.Task, _logger, logPolicy);
                    listeningEndpoints.Add(connectionListener.EndPoint);
                    _runningListeners.Add(serverListener);
                    serverListener.Start();
                }
            }
            catch {
                await StopAsync(default).ConfigureAwait(false);
                throw; // rethrow unexpected exception
            }

            Features.Set<IServerEndpointsFeature>(new ReadOnlyServerEndpointsFeature(
                listeningEndpoints));

            _timerAwaitable.Start();
            _timerTask = StartTimerAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            var tasks = new Task[_runningListeners.Count];
            for (var i = 0; i < _runningListeners.Count; i++)
                tasks[i] = _runningListeners[i].Listener.UnbindAsync(cancellationToken)
                    .AsTask();

            await Task.WhenAll(tasks).ConfigureAwait(false);

            // Signal to all of the listeners that it's time to start the shutdown process
            // We call this after unbind so that we're not touching the listener anymore (each loop will dispose the listener)
            _shutdownTcs.TrySetResult(null);
            for (var i = 0; i < _runningListeners.Count; i++)
                tasks[i] = _runningListeners[i].ExecutionTask 
                           ?? Task.CompletedTask;

            var shutdownTask = Task.WhenAll(tasks);
            if (cancellationToken.CanBeCanceled) await shutdownTask.WithCancellation(cancellationToken).ConfigureAwait(false);
            else await shutdownTask.ConfigureAwait(false);

            try { _timerAwaitable.Stop(); await _timerTask.ConfigureAwait(false); }
            catch { /* discard any timer exception in case it's already completed */ }
        }

        private async Task StartTimerAsync()
        {
            using (_timerAwaitable)
            {
                while (await _timerAwaitable)
                    foreach (var listener in _runningListeners)
                        listener.TickHeartbeat();
            }
        }
    }
}
