// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

internal sealed record class ProjectDiagnosticSource(Project Project, Func<DiagnosticAnalyzer, bool>? ShouldIncludeAnalyzer, bool CachedDiagnosticsOnly) : IDiagnosticSource
{
    public ProjectOrDocumentId GetId() => new(Project.Id);
    public Project GetProject() => Project;
    public TextDocumentIdentifier? GetDocumentIdentifier()
        => !string.IsNullOrEmpty(Project.FilePath)
            ? new VSTextDocumentIdentifier { ProjectContext = ProtocolConversions.ProjectToProjectContext(Project), Uri = ProtocolConversions.CreateAbsoluteUri(Project.FilePath) }
            : null;

    public async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(
        IDiagnosticAnalyzerService diagnosticAnalyzerService,
        RequestContext context,
        CancellationToken cancellationToken)
    {
        if (CachedDiagnosticsOnly)
        {
            Debug.Assert(diagnosticAnalyzerService.WasForceAnalyzed(Project.Id));
            return await diagnosticAnalyzerService.GetCachedDiagnosticsAsync(Project.Solution.Workspace, Project.Id,
                documentId: null, includeSuppressedDiagnostics: false, includeLocalDocumentDiagnostics: false, includeNonLocalDocumentDiagnostics: false, cancellationToken).ConfigureAwait(false);
        }

        // Directly use the IDiagnosticAnalyzerService.  This will use the actual snapshots
        // we're passing in.  If information is already cached for that snapshot, it will be returned.  Otherwise,
        // it will be computed on demand.  Because it is always accurate as per this snapshot, all spans are correct
        // and do not need to be adjusted.
        return await diagnosticAnalyzerService.GetProjectDiagnosticsForIdsAsync(Project.Solution, Project.Id,
            diagnosticIds: null, ShouldIncludeAnalyzer, includeSuppressedDiagnostics: false, includeNonLocalDocumentDiagnostics: false, cancellationToken).ConfigureAwait(false);
    }

    public string ToDisplayString() => Project.Name;
}
