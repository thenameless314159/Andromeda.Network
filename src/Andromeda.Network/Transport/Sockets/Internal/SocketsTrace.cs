// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Microsoft.Extensions.Logging;

namespace Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets.Internal
{
    internal class SocketsTrace : ISocketsTrace
    {
        // ConnectionRead: Reserved: 3

        private static readonly Action<ILogger, string, Exception?> _connectionPause =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId(4, "ConnectionPause"), @"Connection id ""{ConnectionId}"" paused.");

        private static readonly Action<ILogger, string, Exception?> _connectionResume =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId(5, "ConnectionResume"), @"Connection id ""{ConnectionId}"" resumed.");

        private static readonly Action<ILogger, string, Exception?> _connectionReadFin =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId(6, "ConnectionReadFin"), @"Connection id ""{ConnectionId}"" received FIN.");

        private static readonly Action<ILogger, string, string, Exception?> _connectionWriteFin =
            LoggerMessage.Define<string, string>(LogLevel.Debug, new EventId(7, "ConnectionWriteFin"), @"Connection id ""{ConnectionId}"" sending FIN because: ""{Reason}""");

        // ConnectionWrite: Reserved: 11

        // ConnectionWriteCallback: Reserved: 12

        private static readonly Action<ILogger, string, Exception?> _connectionError =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId(14, "ConnectionError"), @"Connection id ""{ConnectionId}"" communication error.");

        private static readonly Action<ILogger, string, Exception?> _connectionReset =
            LoggerMessage.Define<string>(LogLevel.Debug, new EventId(19, "ConnectionReset"), @"Connection id ""{ConnectionId}"" reset.");

        private readonly ILogger _logger;

        public SocketsTrace(ILogger logger)
        {
            _logger = logger;
        }

        public void ConnectionReadFin(string connectionId)
        {
            _connectionReadFin(_logger, connectionId, null);
        }

        public void ConnectionWriteFin(string connectionId, string reason)
        {
            _connectionWriteFin(_logger, connectionId, reason, null);
        }


        public void ConnectionError(string connectionId, Exception ex)
        {
            _connectionError(_logger, connectionId, ex);
        }

        public void ConnectionReset(string connectionId)
        {
            _connectionReset(_logger, connectionId, null);
        }

        public void ConnectionPause(string connectionId)
        {
            _connectionPause(_logger, connectionId, null);
        }

        public void ConnectionResume(string connectionId)
        {
            _connectionResume(_logger, connectionId, null);
        }

        public IDisposable BeginScope<TState>(TState state) => _logger.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel) => _logger.IsEnabled(logLevel);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => _logger.Log(logLevel, eventId, state, exception, formatter);
    }
}
