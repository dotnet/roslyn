// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;

internal abstract class AbstractProjectDiagnosticSource(Project project)
    : IDiagnosticSource
{
    protected Project Project => project;

    public static AbstractProjectDiagnosticSource CreateForFullSolutionAnalysisDiagnostics(Project project, IDiagnosticAnalyzerService diagnosticAnalyzerService, Func<DiagnosticAnalyzer, bool>? shouldIncludeAnalyzer)
        => new FullSolutionAnalysisDiagnosticSource(project, diagnosticAnalyzerService, shouldIncludeAnalyzer);

    public static AbstractProjectDiagnosticSource CreateForCodeAnalysisDiagnostics(Project project, ICodeAnalysisDiagnosticAnalyzerService codeAnalysisService)
        => new CodeAnalysisDiagnosticSource(project, codeAnalysisService);

    public abstract bool IsLiveSource();
    public abstract Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(RequestContext context, CancellationToken cancellationToken);

    public ProjectOrDocumentId GetId() => new(Project.Id);
    public Project GetProject() => Project;
    public TextDocumentIdentifier? GetDocumentIdentifier()
        => !string.IsNullOrEmpty(Project.FilePath)
            ? new VSTextDocumentIdentifier { ProjectContext = ProtocolConversions.ProjectToProjectContext(Project), Uri = ProtocolConversions.CreateAbsoluteUri(Project.FilePath) }
            : null;
    public string ToDisplayString() => Project.Name;

    private sealed class FullSolutionAnalysisDiagnosticSource(Project project, IDiagnosticAnalyzerService diagnosticAnalyzerService, Func<DiagnosticAnalyzer, bool>? shouldIncludeAnalyzer)
        : AbstractProjectDiagnosticSource(project)
    {
        /// <summary>
        /// This is a normal project source that represents live/fresh diagnostics that should supersede everything else.
        /// </summary>
        public override bool IsLiveSource()
            => true;

        public override async Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(
            RequestContext context,
            CancellationToken cancellationToken)
        {
            // Directly use the IDiagnosticAnalyzerService.  This will use the actual snapshots
            // we're passing in.  If information is already cached for that snapshot, it will be returned.  Otherwise,
            // it will be computed on demand.  Because it is always accurate as per this snapshot, all spans are correct
            // and do not need to be adjusted.
            return await diagnosticAnalyzerService.GetProjectDiagnosticsForIdsAsync(Project.Solution, Project.Id,
                diagnosticIds: null, shouldIncludeAnalyzer, includeSuppressedDiagnostics: false, includeNonLocalDocumentDiagnostics: false, cancellationToken).ConfigureAwait(false);
        }
    }

    private sealed class CodeAnalysisDiagnosticSource(Project project, ICodeAnalysisDiagnosticAnalyzerService codeAnalysisService)
        : AbstractProjectDiagnosticSource(project)
    {
        /// <summary>
        /// This source provides the results of the *last* explicitly kicked off "run code analysis" command from the
        /// user.  As such, it is definitely not "live" data, and it should be overridden by any subsequent fresh data
        /// that has been produced.
        /// </summary>
        public override bool IsLiveSource()
            => false;

        public override Task<ImmutableArray<DiagnosticData>> GetDiagnosticsAsync(
            RequestContext context,
            CancellationToken cancellationToken)
        {
            return codeAnalysisService.GetLastComputedProjectDiagnosticsAsync(Project.Id, cancellationToken);
        }
    }
}
