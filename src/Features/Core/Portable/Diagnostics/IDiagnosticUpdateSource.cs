// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Implement this to participate in diagnostic service framework as one of diagnostic update source
    /// </summary>
    internal interface IDiagnosticUpdateSource
    {
        /// <summary>
        /// Raise this when new diagnostics are found
        /// </summary>
        event EventHandler<ImmutableArray<DiagnosticsUpdatedArgs>> DiagnosticsUpdated;

        /// <summary>
        /// Raise this when all diagnostics reported from this update source has cleared
        /// </summary>
        event EventHandler DiagnosticsCleared;

        /// <summary>
        /// Return <see langword="true"/> if the source supports <see cref="GetDiagnosticsAsync"/> API otherwise, return
        /// <see langword="false"/> so that the engine can cache data from <see cref="DiagnosticsUpdated"/> in memory.
        /// </summary>
        bool SupportGetDiagnostics { get; }

        /// <summary>
        /// Get diagnostics stored in the source.
        /// </summary>
        ValueTask<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(Workspace workspace, ProjectId? projectId, DocumentId? documentId, object? id, bool includeSuppressedDiagnostics, CancellationToken cancellationToken);
    }
}
