// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Aggregates events from various diagnostic sources.
    /// </summary>
    internal interface IDiagnosticService
    {
        IGlobalOptionService GlobalOptions { get; }

        /// <summary>
        /// Event to get notified as new diagnostics are discovered by IDiagnosticUpdateSource
        /// 
        /// Notifications for this event are serialized to preserve order.
        /// However, individual event notifications may occur on any thread.
        /// </summary>
        event EventHandler<DiagnosticsUpdatedArgs> DiagnosticsUpdated;

        /// <summary>
        /// This call is equivalent to <see cref="GetPullDiagnosticsAsync"/> and immediately blocking on the result.
        /// </summary>
        [Obsolete("Legacy overload for TypeScript.  Use GetPullDiagnosticsAsync instead.", error: false)]
        ImmutableArray<DiagnosticData> GetDiagnostics(
            Workspace workspace, ProjectId? projectId, DocumentId? documentId, object? id, bool includeSuppressedDiagnostics, CancellationToken cancellationToken);

        /// <summary>
        /// Get current diagnostics stored in IDiagnosticUpdateSource.
        /// </summary>
        ValueTask<ImmutableArray<DiagnosticData>> GetPullDiagnosticsAsync(
            Workspace workspace, ProjectId? projectId, DocumentId? documentId, object? id, bool includeSuppressedDiagnostics, CancellationToken cancellationToken);

        /// <summary>
        /// Get current buckets storing our grouped diagnostics.  Specific buckets can be retrieved by calling <see
        /// cref="IDiagnosticServiceExtensions.GetPullDiagnosticsAsync(IDiagnosticService, DiagnosticBucket, bool, CancellationToken)"/>.
        /// </summary>
        ImmutableArray<DiagnosticBucket> GetPullDiagnosticBuckets(
            Workspace workspace, ProjectId? projectId, DocumentId? documentId, CancellationToken cancellationToken);
    }
}
