using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;

namespace Andromeda.Network.Internal
{
    internal class TimeoutControl : IConnectionTimeoutFeature
    {
        private static readonly long _heartbeatInterval = TimeSpan.FromSeconds(1).Ticks;
        private readonly ITimeoutHandler _timeoutHandler;
        private long _timeoutTimestamp = long.MaxValue;
        private long _lastTimestamp;

        public TimeoutControl(ITimeoutHandler timeoutHandler) => _timeoutHandler = timeoutHandler;

        internal void Initialize(long nowTicks) => _lastTimestamp = nowTicks;

        public void Tick(DateTimeOffset now)
        {
            var timestamp = now.Ticks;

            CheckForTimeout(timestamp);
            Interlocked.Exchange(ref _lastTimestamp, timestamp);
        }

        private void CheckForTimeout(long timestamp)
        {
            if (Debugger.IsAttached) return;
            if (timestamp <= Interlocked.Read(ref _timeoutTimestamp)) return;

            CancelTimeout();
            const string message = "The connection was aborted by the application via an IConnectionTimeoutFeature.";
            _timeoutHandler.OnTimeout(message);
        }

        public void SetTimeout(long ticks)
        {
            Debug.Assert(_timeoutTimestamp == long.MaxValue, "Concurrent timeouts are not supported.");
            AssignTimeout(ticks);
        }

        public void ResetTimeout(long ticks)
        {
            AssignTimeout(ticks);
        }

        public void CancelTimeout()
        {
            Interlocked.Exchange(ref _timeoutTimestamp, long.MaxValue);
        }

        private void AssignTimeout(long ticks)
        {
            // Add Heartbeat.Interval since this can be called right before the next heartbeat.
            Interlocked.Exchange(ref _timeoutTimestamp, Interlocked.Read(ref _lastTimestamp) + ticks + _heartbeatInterval);
        }

        void IConnectionTimeoutFeature.SetTimeout(TimeSpan timeSpan)
        {
            if (timeSpan < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(timeSpan));

            if (_timeoutTimestamp != long.MaxValue)
                throw new InvalidOperationException("Concurrent timeout are not supported");

            SetTimeout(timeSpan.Ticks);
        }

        void IConnectionTimeoutFeature.ResetTimeout(TimeSpan timeSpan)
        {
            if (timeSpan < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(timeSpan));

            ResetTimeout(timeSpan.Ticks);
        }
    }
}
