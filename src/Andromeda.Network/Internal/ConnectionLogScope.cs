// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Andromeda.Network.Internal
{
    internal class ConnectionLogScope : IReadOnlyList<KeyValuePair<string, object?>>
    {
        // Name chosen so as not to collide with Kestrel's "ConnectionId"
        private const string ClientConnectionIdKey = "ClientConnectionId";

        public ConnectionLogScope(string connectionId) => _connectionId = connectionId;
        private string? _cachedToString;
        private readonly string _connectionId;

        public KeyValuePair<string, object?> this[int index]
        {
            get
            {
                if (Count == 1 && index == 0) return new KeyValuePair<string, object?>(ClientConnectionIdKey, _connectionId);
                throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public int Count => string.IsNullOrEmpty(_connectionId) ? 0 : 1;

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() { for (var i = 0; i < Count; ++i) yield return this[i]; }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        public override string ToString() => _cachedToString ??= FormattableString.Invariant($"{ClientConnectionIdKey}:{_connectionId}");
    }
}
