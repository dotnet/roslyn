// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests
{
    internal sealed class TestableDiagnosticListener : IDiagnosticListener
    {
        public TimeSpan? KeepAlive { get; set; }
        public bool KeepAliveHit { get; set; }
        public List<CompletionData> CompletionDataList { get; set; } = new List<CompletionData>();
        public int ConnectionReceivedCount { get; set; }

        public void ConnectionListening()
        {
        }

        public void ConnectionReceived() => ConnectionReceivedCount++;

        public void ConnectionCompleted(CompletionData completionData) => CompletionDataList.Add(completionData);

        public void UpdateKeepAlive(TimeSpan keepAlive) => KeepAlive = keepAlive;

        public void KeepAliveReached() => KeepAliveHit = true;
    }
}
