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
        /// <param name="diagnosticMode">Option controlling if pull diagnostics are allowed for the client.  The
        /// <see cref="IDiagnosticService"/> only provides diagnostics for either push or pull purposes (but not both).
        /// If the caller's desired purpose doesn't match the option value, then this will return nothing, otherwise it
        /// will return the requested diagnostics.</param>
        ValueTask<ImmutableArray<DiagnosticData>> GetPushDiagnosticsAsync(
            Workspace workspace, ProjectId? projectId, DocumentId? documentId, object? id, bool includeSuppressedDiagnostics,
            DiagnosticMode diagnosticMode, CancellationToken cancellationToken);

        /// <summary>
        /// Get current buckets storing our grouped diagnostics.
        /// </summary>
        /// <param name="diagnosticMode">Option controlling if pull diagnostics are allowed for the client.  The <see
        /// cref="IDiagnosticService"/> only provides diagnostics for either push or pull purposes (but not both).  If
        /// the caller's desired purpose doesn't match the option value, then this will return nothing, otherwise it
        /// will return the requested buckets.</param>
        ImmutableArray<DiagnosticBucket> GetPushDiagnosticBuckets(
            Workspace workspace, ProjectId? projectId, DocumentId? documentId,
            DiagnosticMode diagnosticMode, CancellationToken cancellationToken);
    }
}
