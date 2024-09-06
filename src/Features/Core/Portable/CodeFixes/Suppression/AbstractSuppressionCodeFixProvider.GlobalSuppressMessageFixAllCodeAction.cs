// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.AddImport;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression;

internal abstract partial class AbstractSuppressionCodeFixProvider : IConfigurationFixProvider
{
    internal sealed class GlobalSuppressMessageFixAllCodeAction : AbstractGlobalSuppressMessageCodeAction
    {
        private readonly INamedTypeSymbol _suppressMessageAttribute;
        private readonly IEnumerable<KeyValuePair<ISymbol, ImmutableArray<Diagnostic>>> _diagnosticsBySymbol;
        private readonly CodeActionOptionsProvider _fallbackOptions;

        private GlobalSuppressMessageFixAllCodeAction(
            AbstractSuppressionCodeFixProvider fixer,
            INamedTypeSymbol suppressMessageAttribute,
            IEnumerable<KeyValuePair<ISymbol, ImmutableArray<Diagnostic>>> diagnosticsBySymbol,
            Project project,
            CodeActionOptionsProvider fallbackOptions)
            : base(fixer, project)
        {
            _suppressMessageAttribute = suppressMessageAttribute;
            _diagnosticsBySymbol = diagnosticsBySymbol;
            _fallbackOptions = fallbackOptions;
        }

        internal static CodeAction Create(string title, AbstractSuppressionCodeFixProvider fixer, Document triggerDocument, ImmutableDictionary<Document, ImmutableArray<Diagnostic>> diagnosticsByDocument, CodeActionOptionsProvider fallbackOptions)
        {
            return new GlobalSuppressionSolutionChangeAction(title,
                (_, ct) => CreateChangedSolutionAsync(fixer, triggerDocument, diagnosticsByDocument, fallbackOptions, ct),
                equivalenceKey: title);
        }

        internal static CodeAction Create(string title, AbstractSuppressionCodeFixProvider fixer, Project triggerProject, ImmutableDictionary<Project, ImmutableArray<Diagnostic>> diagnosticsByProject, CodeActionOptionsProvider fallbackOptions)
        {
            return new GlobalSuppressionSolutionChangeAction(title,
                (_, ct) => CreateChangedSolutionAsync(fixer, triggerProject, diagnosticsByProject, fallbackOptions, ct),
                equivalenceKey: title);
        }

        private sealed class GlobalSuppressionSolutionChangeAction(
            string title,
            Func<IProgress<CodeAnalysisProgress>, CancellationToken, Task<Solution>> createChangedSolution,
            string equivalenceKey) : SolutionChangeAction(title, createChangedSolution, equivalenceKey)
        {
            protected override Task<Document> PostProcessChangesAsync(Document document, CancellationToken cancellationToken)
            {
                // PERF: We don't to formatting on the entire global suppressions document, but instead do it for each attribute individual in the fixer.
                return Task.FromResult(document);
            }
        }

        private static async Task<Solution> CreateChangedSolutionAsync(
            AbstractSuppressionCodeFixProvider fixer,
            Document triggerDocument,
            ImmutableDictionary<Document, ImmutableArray<Diagnostic>> diagnosticsByDocument,
            CodeActionOptionsProvider fallbackOptions,
            CancellationToken cancellationToken)
        {
            var currentSolution = triggerDocument.Project.Solution;
            foreach (var grouping in diagnosticsByDocument.GroupBy(d => d.Key.Project))
            {
                var oldProject = grouping.Key;
                var currentProject = currentSolution.GetProject(oldProject.Id);
                var compilation = await currentProject.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                var supressMessageAttribute = compilation.SuppressMessageAttributeType();

                if (supressMessageAttribute != null)
                {
                    var diagnosticsBySymbol = await CreateDiagnosticsBySymbolAsync(fixer, grouping, cancellationToken).ConfigureAwait(false);
                    if (diagnosticsBySymbol.Any())
                    {
                        var projectCodeAction = new GlobalSuppressMessageFixAllCodeAction(fixer, supressMessageAttribute, diagnosticsBySymbol, currentProject, fallbackOptions);
                        var newDocument = await projectCodeAction.GetChangedSuppressionDocumentAsync(cancellationToken).ConfigureAwait(false);
                        currentSolution = newDocument.Project.Solution;
                    }
                }
            }

            return currentSolution;
        }

        private static async Task<Solution> CreateChangedSolutionAsync(
            AbstractSuppressionCodeFixProvider fixer,
            Project triggerProject,
            ImmutableDictionary<Project, ImmutableArray<Diagnostic>> diagnosticsByProject,
            CodeActionOptionsProvider fallbackOptions,
            CancellationToken cancellationToken)
        {
            var currentSolution = triggerProject.Solution;
            foreach (var (oldProject, diagnostics) in diagnosticsByProject)
            {
                var currentProject = currentSolution.GetProject(oldProject.Id);
                var compilation = await currentProject.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                var suppressMessageAttribute = compilation.SuppressMessageAttributeType();

                if (suppressMessageAttribute != null)
                {
                    var diagnosticsBySymbol = await CreateDiagnosticsBySymbolAsync(oldProject, diagnostics, cancellationToken).ConfigureAwait(false);
                    if (diagnosticsBySymbol.Any())
                    {
                        var projectCodeAction = new GlobalSuppressMessageFixAllCodeAction(
                            fixer, suppressMessageAttribute, diagnosticsBySymbol, currentProject, fallbackOptions);
                        var newDocument = await projectCodeAction.GetChangedSuppressionDocumentAsync(cancellationToken).ConfigureAwait(false);
                        currentSolution = newDocument.Project.Solution;
                    }
                }
            }

            return currentSolution;
        }

        // Equivalence key is not meaningful for FixAll code action.
        protected override string DiagnosticIdForEquivalenceKey => string.Empty;

        protected override async Task<Document> GetChangedSuppressionDocumentAsync(CancellationToken cancellationToken)
        {
            var suppressionsDoc = await GetOrCreateSuppressionsDocumentAsync(cancellationToken).ConfigureAwait(false);
            var services = suppressionsDoc.Project.Solution.Services;
            var suppressionsRoot = await suppressionsDoc.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var addImportsService = suppressionsDoc.GetRequiredLanguageService<IAddImportsService>();
            var cleanupOptions = await suppressionsDoc.GetCodeCleanupOptionsAsync(_fallbackOptions, cancellationToken).ConfigureAwait(false);

            foreach (var (targetSymbol, diagnostics) in _diagnosticsBySymbol)
            {
                foreach (var diagnostic in diagnostics)
                {
                    Contract.ThrowIfFalse(!diagnostic.IsSuppressed);
                    suppressionsRoot = Fixer.AddGlobalSuppressMessageAttribute(
                        suppressionsRoot, targetSymbol, _suppressMessageAttribute, diagnostic,
                        services, cleanupOptions.FormattingOptions, addImportsService, cancellationToken);
                }
            }

            var result = suppressionsDoc.WithSyntaxRoot(suppressionsRoot);
            var final = await CleanupDocumentAsync(result, cleanupOptions, cancellationToken).ConfigureAwait(false);
            return final;
        }

        private static async Task<IEnumerable<KeyValuePair<ISymbol, ImmutableArray<Diagnostic>>>> CreateDiagnosticsBySymbolAsync(AbstractSuppressionCodeFixProvider fixer, IEnumerable<KeyValuePair<Document, ImmutableArray<Diagnostic>>> diagnosticsByDocument, CancellationToken cancellationToken)
        {
            var diagnosticsMapBuilder = ImmutableDictionary.CreateBuilder<ISymbol, List<Diagnostic>>();
            foreach (var (document, diagnostics) in diagnosticsByDocument)
            {
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

        private static async Task<IEnumerable<KeyValuePair<ISymbol, ImmutableArray<Diagnostic>>>> CreateDiagnosticsBySymbolAsync(Project project, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
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

            if (!diagnosticsMapBuilder.TryGetValue(targetSymbol, out var diagnosticsForSymbol))
            {
                diagnosticsForSymbol = [];
                diagnosticsMapBuilder.Add(targetSymbol, diagnosticsForSymbol);
            }

            diagnosticsForSymbol.Add(diagnostic);
        }

        private static IEnumerable<KeyValuePair<ISymbol, ImmutableArray<Diagnostic>>> CreateDiagnosticsBySymbol(ImmutableDictionary<ISymbol, List<Diagnostic>>.Builder diagnosticsMapBuilder)
        {
            if (diagnosticsMapBuilder.Count == 0)
                return [];

            var builder = new List<KeyValuePair<ISymbol, ImmutableArray<Diagnostic>>>();
            foreach (var (symbol, diagnostics) in diagnosticsMapBuilder)
                builder.Add(KeyValuePairUtil.Create(symbol, GetUniqueDiagnostics(diagnostics)));

            return builder.OrderBy(kvp => kvp.Key.GetDocumentationCommentId() ?? string.Empty);
        }

        private static ImmutableArray<Diagnostic> GetUniqueDiagnostics(List<Diagnostic> diagnostics)
        {
            var uniqueIds = new HashSet<string>();
            var uniqueDiagnostics = ArrayBuilder<Diagnostic>.GetInstance();
            foreach (var diagnostic in diagnostics)
            {
                if (uniqueIds.Add(diagnostic.Id))
                {
                    uniqueDiagnostics.Add(diagnostic);
                }
            }

            return uniqueDiagnostics.ToImmutableAndFree();
        }
    }
}
