// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Analyzers.MatchFolderAndNamespace;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.MatchFolderAndNamespace;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.SyncNamespaces;

internal abstract class AbstractSyncNamespacesService<TSyntaxKind, TNamespaceSyntax>
    : ISyncNamespacesService
    where TSyntaxKind : struct
    where TNamespaceSyntax : SyntaxNode
{
    public abstract AbstractMatchFolderAndNamespaceDiagnosticAnalyzer<TSyntaxKind, TNamespaceSyntax> DiagnosticAnalyzer { get; }
    public abstract AbstractChangeNamespaceToMatchFolderCodeFixProvider CodeFixProvider { get; }

    /// <inheritdoc/>
    public async Task<Solution> SyncNamespacesAsync(
        ImmutableArray<Project> projects,
        CodeActionOptionsProvider options,
        IProgress<CodeAnalysisProgress> progressTracker,
        CancellationToken cancellationToken)
    {
        // all projects must be of the same language
        Debug.Assert(projects.All(project => project.Language == projects[0].Language));

        var solution = projects[0].Solution;
        var diagnosticAnalyzers = ImmutableArray.Create<DiagnosticAnalyzer>(DiagnosticAnalyzer);
        var diagnosticsByProject = await GetDiagnosticsByProjectAsync(projects, diagnosticAnalyzers, cancellationToken).ConfigureAwait(false);

        // If no diagnostics are reported, then there is nothing to fix.
        if (diagnosticsByProject.Values.All(diagnostics => diagnostics.IsEmpty))
        {
            return solution;
        }

        var fixAllContext = await GetFixAllContextAsync(
            solution, CodeFixProvider, diagnosticsByProject, options, progressTracker, cancellationToken).ConfigureAwait(false);
        var fixAllProvider = CodeFixProvider.GetFixAllProvider();
        RoslynDebug.AssertNotNull(fixAllProvider);

        return await ApplyCodeFixAsync(fixAllProvider, fixAllContext, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<ImmutableDictionary<Project, ImmutableArray<Diagnostic>>> GetDiagnosticsByProjectAsync(
        ImmutableArray<Project> projects,
        ImmutableArray<DiagnosticAnalyzer> diagnosticAnalyzers,
        CancellationToken cancellationToken)
    {
        var builder = ImmutableDictionary.CreateBuilder<Project, ImmutableArray<Diagnostic>>();

        foreach (var project in projects)
        {
            var diagnostics = await GetDiagnosticsAsync(project, diagnosticAnalyzers, cancellationToken).ConfigureAwait(false);
            builder.Add(project, diagnostics);
        }

        return builder.ToImmutable();
    }

    private static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(
        Project project,
        ImmutableArray<DiagnosticAnalyzer> diagnosticAnalyzers,
        CancellationToken cancellationToken)
    {
        var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
        RoslynDebug.AssertNotNull(compilation);

        var analyzerOptions = new CompilationWithAnalyzersOptions(
            project.AnalyzerOptions,
            onAnalyzerException: null,
            concurrentAnalysis: true,
            logAnalyzerExecutionTime: false,
            reportSuppressedDiagnostics: false);
        var analyzerCompilation = compilation.WithAnalyzers(diagnosticAnalyzers, analyzerOptions);

        return await analyzerCompilation.GetAnalyzerDiagnosticsAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<FixAllContext> GetFixAllContextAsync(
        Solution solution,
        CodeFixProvider codeFixProvider,
        ImmutableDictionary<Project, ImmutableArray<Diagnostic>> diagnosticsByProject,
        CodeActionOptionsProvider options,
        IProgress<CodeAnalysisProgress> progressTracker,
        CancellationToken cancellationToken)
    {
        var diagnosticProvider = new DiagnosticProvider(diagnosticsByProject);

        var firstDiagnostic = diagnosticsByProject
            .SelectMany(kvp => kvp.Value)
            .FirstOrDefault();
        RoslynDebug.AssertNotNull(firstDiagnostic?.Location?.SourceTree);

        var document = solution.GetRequiredDocument(firstDiagnostic.Location.SourceTree);

        // This will allow us access to the equivalence key
        CodeAction? action = null;
        var context = new CodeFixContext(
            document,
            firstDiagnostic.Location.SourceSpan,
            [firstDiagnostic],
            (a, _) => action ??= a,
            options,
            cancellationToken);
        await codeFixProvider.RegisterCodeFixesAsync(context).ConfigureAwait(false);

        return new FixAllContext(
            new FixAllState(
                fixAllProvider: NoOpFixAllProvider.Instance,
                diagnosticSpan: firstDiagnostic.Location.SourceSpan,
                document,
                document.Project,
                codeFixProvider,
                FixAllScope.Solution,
                codeActionEquivalenceKey: action?.EquivalenceKey!, // FixAllState supports null equivalence key. This should still be supported.
                diagnosticIds: codeFixProvider.FixableDiagnosticIds,
                fixAllDiagnosticProvider: diagnosticProvider,
                options),
            progressTracker,
            cancellationToken);
    }

    private static async Task<Solution> ApplyCodeFixAsync(
        FixAllProvider fixAllProvider,
        FixAllContext fixAllContext,
        CancellationToken cancellationToken)
    {
        var fixAllAction = await fixAllProvider.GetFixAsync(fixAllContext).ConfigureAwait(false);
        RoslynDebug.AssertNotNull(fixAllAction);

        var operations = await fixAllAction.GetOperationsAsync(
            fixAllContext.Solution, fixAllContext.Progress, cancellationToken).ConfigureAwait(false);
        var applyChangesOperation = operations.OfType<ApplyChangesOperation>().SingleOrDefault();
        RoslynDebug.AssertNotNull(applyChangesOperation);

        return applyChangesOperation.ChangedSolution;
    }

    private class DiagnosticProvider : FixAllContext.DiagnosticProvider
    {
        private static readonly Task<IEnumerable<Diagnostic>> EmptyDiagnosticResult = Task.FromResult(Enumerable.Empty<Diagnostic>());

        private readonly ImmutableDictionary<Project, ImmutableArray<Diagnostic>> _diagnosticsByProject;

        internal DiagnosticProvider(ImmutableDictionary<Project, ImmutableArray<Diagnostic>> diagnosticsByProject)
        {
            _diagnosticsByProject = diagnosticsByProject;
        }

        public override Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(Project project, CancellationToken cancellationToken)
        {
            return GetProjectDiagnosticsAsync(project, cancellationToken);
        }

        public override async Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, CancellationToken cancellationToken)
        {
            var projectDiagnostics = await GetProjectDiagnosticsAsync(document.Project, cancellationToken).ConfigureAwait(false);
            return projectDiagnostics
                .Where(diagnostic => diagnostic.Location.SourceTree?.FilePath == document.FilePath)
                .ToImmutableArray();
        }

        public override Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(Project project, CancellationToken cancellationToken)
        {
            return _diagnosticsByProject.TryGetValue(project, out var diagnostics)
                ? Task.FromResult<IEnumerable<Diagnostic>>(diagnostics)
                : EmptyDiagnosticResult;
        }
    }
}
