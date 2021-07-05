// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Threading;
using Microsoft.AspNetCore.Connections.Features;

#nullable enable

namespace Microsoft.AspNetCore.Connections
{
    internal partial class TransportConnection
    {
        MemoryPool<byte> IMemoryPoolFeature.MemoryPool => MemoryPool;

        IDuplexPipe IConnectionTransportFeature.Transport
        {
            get => Transport;
            set => Transport = value;
        }

        IDictionary<object, object?> IConnectionItemsFeature.Items
        {
            get => Items;
            set => Items = value;
        }

        CancellationToken IConnectionLifetimeFeature.ConnectionClosed
        {
            get => ConnectionClosed;
            set => ConnectionClosed = value;
        }

        void IConnectionLifetimeFeature.Abort() => Abort(new ConnectionAbortedException("The connection was aborted by the application via IConnectionLifetimeFeature.Abort()."));
    }
}