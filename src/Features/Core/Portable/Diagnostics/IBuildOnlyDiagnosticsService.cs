// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Service to keep track of build-only diagnostics reported from explicit Build/Rebuild commands.
    /// Note that this service only keeps track of those diagnostics that can never be reported from live analysis.
    /// </summary>
    internal interface IBuildOnlyDiagnosticsService : IWorkspaceService
    {
        void AddBuildOnlyDiagnostics(Solution solution, ProjectId? projectId, DocumentId? documentId, ImmutableArray<DiagnosticData> diagnostics);

        void ClearBuildOnlyDiagnostics(Solution solution, ProjectId? projectId, DocumentId? documentId);

        ImmutableArray<DiagnosticData> GetBuildOnlyDiagnostics(DocumentId documentId);

        ImmutableArray<DiagnosticData> GetBuildOnlyDiagnostics(ProjectId projectId);
    }
}
