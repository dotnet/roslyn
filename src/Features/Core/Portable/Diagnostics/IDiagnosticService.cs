// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Threading;

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
        event EventHandler<DiagnosticsUpdatedArgs> DiagnosticsUpdated;

        /// <summary>
        /// Get current diagnostics stored in IDiagnosticUpdateSource
        /// </summary>
        ImmutableArray<DiagnosticData> GetDiagnostics(Workspace workspace, ProjectId? projectId, DocumentId? documentId, object? id, bool includeSuppressedDiagnostics, CancellationToken cancellationToken);

        /// <summary>
        /// Get current buckets stored our diagnostics are grouped into.  Specific buckets can be retrieved by calling
        /// <see cref="IDiagnosticServiceExtensions.GetDiagnostics(IDiagnosticService, DiagnosticBucket, bool, CancellationToken)"/>.
        /// </summary>
        ImmutableArray<DiagnosticBucket> GetDiagnosticBuckets(Workspace workspace, ProjectId? projectId, DocumentId? documentId, CancellationToken cancellationToken);
    }
}
