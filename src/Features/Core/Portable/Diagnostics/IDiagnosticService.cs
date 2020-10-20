// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        /// Get current diagnostics stored in IDiagnosticUpdateSource.
        /// </summary>
        /// <param name="forPullDiagnostics">If the caller of this method will be using the diagnostics for pull or push
        /// diagnostic purposes.  The <see cref="IDiagnosticService"/> only provides diagnostics for either push or pull
        /// purposes (but not both).  If the caller's desired purpose doesn't match the service's setting, then this
        /// will return nothing, otherwise it will return the requested diagnostics.</param>
        ImmutableArray<DiagnosticData> GetDiagnostics(Workspace workspace, ProjectId? projectId, DocumentId? documentId, object? id, bool includeSuppressedDiagnostics, bool forPullDiagnostics, CancellationToken cancellationToken);

        /// <summary>
        /// Get current buckets stored our diagnostics are grouped into.  Specific buckets can be retrieved by calling
        /// <see cref="IDiagnosticServiceExtensions.GetDiagnostics(IDiagnosticService, DiagnosticBucket, bool, bool, CancellationToken)"/>.
        /// </summary>
        /// <param name="forPullDiagnostics">If the caller of this method will be using the diagnostics for pull or push
        /// diagnostic purposes.  The <see cref="IDiagnosticService"/> only provides diagnostics for either push or pull
        /// purposes (but not both).  If the caller's desired purpose doesn't match the service's setting, then this
        /// will return nothing, otherwise it will return the requested buckets.</param>
        ImmutableArray<DiagnosticBucket> GetDiagnosticBuckets(Workspace workspace, ProjectId? projectId, DocumentId? documentId, bool forPullDiagnostics, CancellationToken cancellationToken);
    }
}
