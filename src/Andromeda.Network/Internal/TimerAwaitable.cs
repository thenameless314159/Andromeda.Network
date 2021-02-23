// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Andromeda.Network.Internal
{
    internal class TimerAwaitable : IDisposable, ICriticalNotifyCompletion
    {
        private static readonly Action _callbackCompleted = () => { };
        private readonly object _lockObj = new();

        private Action? _callback;
        private Timer? _timer;

        private bool _running = true;
        private bool _disposed;

        public TimerAwaitable(TimeSpan dueTime, TimeSpan period) => 
            (_dueTime, _period) = (dueTime, period);
        
        private readonly TimeSpan _dueTime;
        private readonly TimeSpan _period;

        public void Start()
        {
            if (_timer is not null) return;
            lock (_lockObj)
            {
                if (_disposed) return;
                _timer ??= new Timer(state => ((TimerAwaitable?)state)!.Tick(), this, _dueTime, _period);
            }
        }

        public TimerAwaitable GetAwaiter() => this;
        public bool IsCompleted => ReferenceEquals(_callback, _callbackCompleted);

        public bool GetResult()
        {
            _callback = null;
            return _running;
        }

        private void Tick()
        {
            var continuation = Interlocked.Exchange(ref _callback, _callbackCompleted);
            continuation?.Invoke();
        }

        public void OnCompleted(Action continuation)
        {
            if (ReferenceEquals(_callback, _callbackCompleted) ||
                ReferenceEquals(Interlocked.CompareExchange(ref _callback, continuation, null), _callbackCompleted))
                Task.Run(continuation);
        }

        public void UnsafeOnCompleted(Action continuation)
        {
            OnCompleted(continuation);
        }

        public void Stop()
        {
            lock (_lockObj) {
                // Stop should be used to trigger the call to end the loop which disposes
                if (_disposed) throw new ObjectDisposedException(GetType().FullName);
                _running = false;
            }

            // Call tick here to make sure that we yield the callback,
            // if it's currently waiting, we don't need to wait for the next period
            Tick();
        }

        void IDisposable.Dispose()
        {
            lock (_lockObj)
            {
                _disposed = true;
                _timer?.Dispose();
                _timer = null;
            }
        }
    }
}