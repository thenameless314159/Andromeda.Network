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
            LoggerMessage.Define<string>(LogLevel.Trace, new EventId(4, "ConnectionPause"), @"Connection id ""{ConnectionId}"" paused.");

        private static readonly Action<ILogger, string, Exception?> _connectionResume =
            LoggerMessage.Define<string>(LogLevel.Trace, new EventId(5, "ConnectionResume"), @"Connection id ""{ConnectionId}"" resumed.");

        private static readonly Action<ILogger, string, Exception?> _connectionReadFin =
            LoggerMessage.Define<string>(LogLevel.Trace, new EventId(6, "ConnectionReadFin"), @"Connection id ""{ConnectionId}"" received FIN.");

        private static readonly Action<ILogger, string, string, Exception?> _connectionWriteFin =
            LoggerMessage.Define<string, string>(LogLevel.Trace, new EventId(7, "ConnectionWriteFin"), @"Connection id ""{ConnectionId}"" sending FIN because: ""{Reason}""");

        // ConnectionWrite: Reserved: 11

        // ConnectionWriteCallback: Reserved: 12

        private static readonly Action<ILogger, string, Exception?> _connectionError =
            LoggerMessage.Define<string>(LogLevel.Trace, new EventId(14, "ConnectionError"), @"Connection id ""{ConnectionId}"" communication error.");

        private static readonly Action<ILogger, string, Exception?> _connectionReset =
            LoggerMessage.Define<string>(LogLevel.Trace, new EventId(19, "ConnectionReset"), @"Connection id ""{ConnectionId}"" reset.");

        private readonly ILogger _logger;

        public SocketsTrace(ILogger logger)
        {
            _logger = logger;
        }

        /*public void ConnectionRead(SocketConnection connection, int count)
        {
            // Don't log for now since this could be *too* verbose.
            // Reserved: Event ID 3
        }*/

        public void ConnectionReadFin(SocketConnection connection)
        {
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _connectionReadFin(_logger, connection.ConnectionId, null);
            }
        }

        public void ConnectionWriteFin(SocketConnection connection, string reason)
        {
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _connectionWriteFin(_logger, connection.ConnectionId, reason, null);
            }
        }

        /*public void ConnectionWrite(SocketConnection connection, int count)
        {
            // Don't log for now since this could be *too* verbose.
            // Reserved: Event ID 11
        }*/

        /*public void ConnectionWriteCallback(SocketConnection connection, int status)
        {
            // Don't log for now since this could be *too* verbose.
            // Reserved: Event ID 12
        }*/

        public void ConnectionError(SocketConnection connection, Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _connectionError(_logger, connection.ConnectionId, ex);
            }
        }

        public void ConnectionReset(string connectionId)
        {
            _connectionReset(_logger, connectionId, null);
        }

        public void ConnectionReset(SocketConnection connection)
        {
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _connectionReset(_logger, connection.ConnectionId, null);
            }
        }

        public void ConnectionPause(SocketConnection connection)
        {
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _connectionPause(_logger, connection.ConnectionId, null);
            }
        }

        public void ConnectionResume(SocketConnection connection)
        {
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _connectionResume(_logger, connection.ConnectionId, null);
            }
        }

        public IDisposable BeginScope<TState>(TState state) => _logger.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel) => _logger.IsEnabled(logLevel);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => _logger.Log(logLevel, eventId, state, exception, formatter);
    }
}
