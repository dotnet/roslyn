// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServer;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

internal abstract class AbstractProjectDiagnosticSource(Project project)
    : IDiagnosticSource
{
    protected Project Project => project;
    protected Solution Solution => this.Project.Solution;

    public static AbstractProjectDiagnosticSource CreateForFullSolutionAnalysisDiagnostics(Project project, AnalyzerFilter analyzerFilter)
        => new FullSolutionAnalysisDiagnosticSource(project, analyzerFilter);

    public static AbstractProjectDiagnosticSource CreateForCodeAnalysisDiagnostics(Project project, ICodeAnalysisDiagnosticAnalyzerService codeAnalysisService)
        => new CodeAnalysisDiagnosticSource(project, codeAnalysisService);

    public abstract Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(RequestContext context, CancellationToken cancellationToken);

    public ProjectOrDocumentId GetId() => new(Project.Id);
    public Project GetProject() => Project;
    public TextDocumentIdentifier? GetDocumentIdentifier()
        => !string.IsNullOrEmpty(Project.FilePath)
            ? new VSTextDocumentIdentifier { ProjectContext = ProtocolConversions.ProjectToProjectContext(Project), DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(Project.FilePath) }
            : null;
    public string ToDisplayString() => Project.Name;

    private sealed class FullSolutionAnalysisDiagnosticSource(
        Project project, AnalyzerFilter analyzerFilter)
        : AbstractProjectDiagnosticSource(project)
    {
        public override async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(
            RequestContext context,
            CancellationToken cancellationToken)
        {
            // Directly use the IDiagnosticAnalyzerService.  This will use the actual snapshots
            // we're passing in.  If information is already cached for that snapshot, it will be returned.  Otherwise,
            // it will be computed on demand.  Because it is always accurate as per this snapshot, all spans are correct
            // and do not need to be adjusted.
            var service = this.Solution.Services.GetRequiredService<IDiagnosticAnalyzerService>();
            var diagnostics = await service.GetProjectDiagnosticsForIdsAsync(
                Project, diagnosticIds: null, analyzerFilter, cancellationToken).ConfigureAwait(false);

            // TODO(cyrusn): In the future we could consider reporting these, but with a flag on the diagnostic mentioning
            // that it is suppressed and should be hidden from the task list by default.
            return diagnostics.WhereAsArray(d => !d.IsSuppressed);
        }
    }

    private sealed class CodeAnalysisDiagnosticSource(Project project, ICodeAnalysisDiagnosticAnalyzerService codeAnalysisService)
        : AbstractProjectDiagnosticSource(project)
    {
        public override async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(
            RequestContext context,
            CancellationToken cancellationToken)
        {
            var diagnostics = codeAnalysisService.GetLastComputedProjectDiagnostics(Project.Id);

            // This source provides the results of the *last* explicitly kicked off "run code analysis" command from the
            // user.  As such, it is definitely not "live" data, and it should be overridden by any subsequent fresh data
            // that has been produced.
            diagnostics = ProtocolConversions.AddBuildTagIfNotPresent(diagnostics);
            return diagnostics;
        }
    }
}
