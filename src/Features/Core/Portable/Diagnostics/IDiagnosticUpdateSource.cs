// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;

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
        event EventHandler<DiagnosticsUpdatedArgs> DiagnosticsUpdated;

        /// <summary>
        /// Raise this when all diagnostics reported from this update source has cleared
        /// </summary>
        event EventHandler DiagnosticsCleared;

        /// <summary>
        /// Return true if the source supports GetDiagnostics API otherwise, return false so that the engine can cache data from DiagnosticsUpdated in memory
        /// </summary>
        bool SupportGetDiagnostics { get; }

        /// <summary>
        /// Get diagnostics stored in the source
        /// </summary>
        ImmutableArray<DiagnosticData> GetDiagnostics(Workspace workspace, ProjectId projectId, DocumentId documentId, object id, bool includeSuppressedDiagnostics, CancellationToken cancellationToken);
    }
}
