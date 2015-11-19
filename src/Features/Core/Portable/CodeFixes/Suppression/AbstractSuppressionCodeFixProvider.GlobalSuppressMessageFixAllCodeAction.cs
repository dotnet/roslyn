// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression
{
    internal abstract partial class AbstractSuppressionCodeFixProvider : ISuppressionFixProvider
    {
        internal sealed class GlobalSuppressMessageFixAllCodeAction : AbstractGlobalSuppressMessageCodeAction
        {
            private readonly IEnumerable<KeyValuePair<ISymbol, ImmutableArray<Diagnostic>>> _diagnosticsBySymbol;

            private GlobalSuppressMessageFixAllCodeAction(AbstractSuppressionCodeFixProvider fixer, IEnumerable<KeyValuePair<ISymbol, ImmutableArray<Diagnostic>>> diagnosticsBySymbol, Project project)
                : base(fixer, project)
            {
                _diagnosticsBySymbol = diagnosticsBySymbol;
            }

            internal static CodeAction Create(string title, AbstractSuppressionCodeFixProvider fixer, Document triggerDocument, ImmutableDictionary<Document, ImmutableArray<Diagnostic>> diagnosticsByDocument)
            {
                return new GlobalSuppressionSolutionChangeAction(title,
                    ct => CreateChangedSolutionAsync(fixer, triggerDocument, diagnosticsByDocument, ct),
                    equivalenceKey: title);
            }

            internal static CodeAction Create(string title, AbstractSuppressionCodeFixProvider fixer, Project triggerProject, ImmutableDictionary<Project, ImmutableArray<Diagnostic>> diagnosticsByProject)
            {
                return new GlobalSuppressionSolutionChangeAction(title,
                    ct => CreateChangedSolutionAsync(fixer, triggerProject, diagnosticsByProject, ct),
                    equivalenceKey: title);
            }

            private class GlobalSuppressionSolutionChangeAction : SolutionChangeAction
            {
                public GlobalSuppressionSolutionChangeAction(string title, Func<CancellationToken, Task<Solution>> createChangedSolution, string equivalenceKey)
                    : base(title, createChangedSolution, equivalenceKey)
                {
                }

                protected override Task<Document> PostProcessChangesAsync(Document document, CancellationToken cancellationToken)
                {
                    // PERF: We don't to formatting on the entire global suppressions document, but instead do it for each attribute individual in the fixer.
                    return Task.FromResult(document);
                }
            }

            private static async Task<Solution> CreateChangedSolutionAsync(AbstractSuppressionCodeFixProvider fixer, Document triggerDocument, ImmutableDictionary<Document, ImmutableArray<Diagnostic>> diagnosticsByDocument, CancellationToken cancellationToken)
            {
                var currentSolution = triggerDocument.Project.Solution;
                foreach (var grouping in diagnosticsByDocument.GroupBy(d => d.Key.Project))
                {
                    var oldProject = grouping.Key;
                    var currentProject = currentSolution.GetProject(oldProject.Id);
                    var diagnosticsBySymbol = await CreateDiagnosticsBySymbolAsync(fixer, grouping, cancellationToken).ConfigureAwait(false);
                    if (diagnosticsBySymbol.Any())
                    {
                        var projectCodeAction = new GlobalSuppressMessageFixAllCodeAction(fixer, diagnosticsBySymbol, currentProject);
                        var newDocument = await projectCodeAction.GetChangedSuppressionDocumentAsync(cancellationToken).ConfigureAwait(false);
                        currentSolution = newDocument.Project.Solution;
                    }
                }

                return currentSolution;
            }

            private static async Task<Solution> CreateChangedSolutionAsync(AbstractSuppressionCodeFixProvider fixer, Project triggerProject, ImmutableDictionary<Project, ImmutableArray<Diagnostic>> diagnosticsByProject, CancellationToken cancellationToken)
            {
                var currentSolution = triggerProject.Solution;
                foreach (var kvp in diagnosticsByProject)
                {
                    var oldProject = kvp.Key;
                    var currentProject = currentSolution.GetProject(oldProject.Id);
                    var diagnosticsBySymbol = await CreateDiagnosticsBySymbolAsync(fixer, oldProject, kvp.Value, cancellationToken).ConfigureAwait(false);
                    if (diagnosticsBySymbol.Any())
                    {
                        var projectCodeAction = new GlobalSuppressMessageFixAllCodeAction(fixer, diagnosticsBySymbol, currentProject);
                        var newDocument = await projectCodeAction.GetChangedSuppressionDocumentAsync(cancellationToken).ConfigureAwait(false);
                        currentSolution = newDocument.Project.Solution;
                    }
                }

                return currentSolution;
            }

            // Equivalence key is not meaningful for FixAll code action.
            protected override string DiagnosticIdForEquivalenceKey => string.Empty;

            protected override async Task<Document> GetChangedSuppressionDocumentAsync(CancellationToken cancellationToken)
            {
                var suppressionsDoc = await GetOrCreateSuppressionsDocumentAsync(cancellationToken).ConfigureAwait(false);
                var workspace = suppressionsDoc.Project.Solution.Workspace;
                var suppressionsRoot = await suppressionsDoc.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                foreach (var kvp in _diagnosticsBySymbol)
                {
                    var targetSymbol = kvp.Key;
                    var diagnostics = kvp.Value;

                    foreach (var diagnostic in diagnostics)
                    {
                        Contract.ThrowIfFalse(!diagnostic.IsSuppressed);
                        suppressionsRoot = Fixer.AddGlobalSuppressMessageAttribute(suppressionsRoot, targetSymbol, diagnostic, workspace, cancellationToken);
                    }
                }

                return suppressionsDoc.WithSyntaxRoot(suppressionsRoot);
            }

            private static async Task<IEnumerable<KeyValuePair<ISymbol, ImmutableArray<Diagnostic>>>> CreateDiagnosticsBySymbolAsync(AbstractSuppressionCodeFixProvider fixer, IEnumerable<KeyValuePair<Document, ImmutableArray<Diagnostic>>> diagnosticsByDocument, CancellationToken cancellationToken)
            {
                var diagnosticsMapBuilder = ImmutableDictionary.CreateBuilder<ISymbol, List<Diagnostic>>();
                foreach (var kvp in diagnosticsByDocument)
                {
                    var document = kvp.Key;
                    var diagnostics = kvp.Value;
                    foreach (var diagnostic in diagnostics)
                    {
                        Contract.ThrowIfFalse(diagnostic.Location.IsInSource);
                        var suppressionTargetInfo = await fixer.GetSuppressionTargetInfoAsync(document, diagnostic.Location.SourceSpan, cancellationToken).ConfigureAwait(false);
                        if (suppressionTargetInfo != null)
                        {
                            var targetSymbol = suppressionTargetInfo.TargetSymbol;
                            Contract.ThrowIfNull(targetSymbol);
                            AddDiagnosticForSymbolIfNeeded(targetSymbol, diagnostic, diagnosticsMapBuilder);
                        }
                    }
                }

                return CreateDiagnosticsBySymbol(diagnosticsMapBuilder);
            }

            private static async Task<IEnumerable<KeyValuePair<ISymbol, ImmutableArray<Diagnostic>>>> CreateDiagnosticsBySymbolAsync(AbstractSuppressionCodeFixProvider fixer, Project project, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
            {
                var diagnosticsMapBuilder = ImmutableDictionary.CreateBuilder<ISymbol, List<Diagnostic>>();
                var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                if (compilation != null)
                {
                    foreach (var diagnostic in diagnostics)
                    {
                        Contract.ThrowIfFalse(!diagnostic.Location.IsInSource);
                        var targetSymbol = compilation.Assembly;
                        AddDiagnosticForSymbolIfNeeded(targetSymbol, diagnostic, diagnosticsMapBuilder);
                    }
                }

                return CreateDiagnosticsBySymbol(diagnosticsMapBuilder);
            }

            private static void AddDiagnosticForSymbolIfNeeded(ISymbol targetSymbol, Diagnostic diagnostic, ImmutableDictionary<ISymbol, List<Diagnostic>>.Builder diagnosticsMapBuilder)
            {
                if (diagnostic.IsSuppressed)
                {
                    return;
                }

                List<Diagnostic> diagnosticsForSymbol;
                if (!diagnosticsMapBuilder.TryGetValue(targetSymbol, out diagnosticsForSymbol))
                {
                    diagnosticsForSymbol = new List<Diagnostic>();
                    diagnosticsMapBuilder.Add(targetSymbol, diagnosticsForSymbol);
                }

                diagnosticsForSymbol.Add(diagnostic);
            }

            private static IEnumerable<KeyValuePair<ISymbol, ImmutableArray<Diagnostic>>> CreateDiagnosticsBySymbol(ImmutableDictionary<ISymbol, List<Diagnostic>>.Builder diagnosticsMapBuilder)
            {
                if (diagnosticsMapBuilder.Count == 0)
                {
                    return SpecializedCollections.EmptyEnumerable<KeyValuePair<ISymbol, ImmutableArray<Diagnostic>>>();
                }

                var builder = new List<KeyValuePair<ISymbol, ImmutableArray<Diagnostic>>>();
                foreach (var kvp in diagnosticsMapBuilder)
                {
                    builder.Add(KeyValuePair.Create(kvp.Key, GetUniqueDiagnostics(kvp.Value)));
                }

                return builder.OrderBy(kvp => kvp.Key.GetDocumentationCommentId() ?? string.Empty);
            }

            private static ImmutableArray<Diagnostic> GetUniqueDiagnostics(List<Diagnostic> diagnostics)
            {
                var uniqueIds = new HashSet<string>();
                var uniqueDiagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
                foreach (var diagnostic in diagnostics)
                {
                    if (uniqueIds.Add(diagnostic.Id))
                    {
                        uniqueDiagnostics.Add(diagnostic);
                    }
                }

                return uniqueDiagnostics.ToImmutable();
            }
        }
    }
}
