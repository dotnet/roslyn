// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Aggregates events from various diagnostic sources.
    /// </summary>
    internal interface IDiagnosticService
    {
        /// <summary>
        /// Event to get notified as new diagnostics are discovered by IDiagnosticUpdateSource
        /// 
        /// Notifications for this event are serialized to preserve order.
        /// However, individual event notifications may occur on any thread.
        /// </summary>
        event EventHandler<ImmutableArray<DiagnosticsUpdatedArgs>> DiagnosticsUpdated;
    }
}
