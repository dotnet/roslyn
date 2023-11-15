// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.Configuration.ConfigureSeverity;
using Microsoft.CodeAnalysis.CodeFixes.Suppression;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeAnalysisSuggestions;

[ExportCodeRefactoringProvider(LanguageNames.CSharp, LanguageNames.VisualBasic), Shared]
internal sealed partial class CodeAnalysisSuggestionsCodeRefactoringProvider
    : CodeRefactoringProvider
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public CodeAnalysisSuggestionsCodeRefactoringProvider()
    {
    }

    protected override CodeActionRequestPriority ComputeRequestPriority()
        => CodeActionRequestPriority.Low;

    public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        var (document, span, cancellationToken) = context;

        var configService = document.Project.Solution.Services.GetRequiredService<ICodeAnalysisSuggestionsConfigService>();
        var diagnosticsByCategory = await configService.TryGetCodeAnalysisSuggestionsConfigDataAsync(document.Project, isExplicitlyInvoked: false, cancellationToken).ConfigureAwait(false);
        if (diagnosticsByCategory.IsEmpty)
            return;

        // Compute the code analysis suggestions along with the count of total diagnostics/improvements.
        var (actions, totalDiagnostics) = await GetCodeAnalysisSuggestionActionsAsync(diagnosticsByCategory, document, span, cancellationToken).ConfigureAwait(false);
        if (actions.Length > 0)
        {
            Debug.Assert(totalDiagnostics > 0);

            context.RegisterRefactoring(
                CodeAction.Create(
                    string.Format(FeaturesResources.Code_analysis_improvements_0, totalDiagnostics),
                    actions,
                    isInlinable: false,
                    CodeActionPriority.Low),
                span);
        }
    }

    private static async Task<(ImmutableArray<CodeAction> Actions, int TotalDiagnostics)> GetCodeAnalysisSuggestionActionsAsync(
        ImmutableArray<(string Category, ImmutableArray<DiagnosticData> Diagnostics)> diagnosticsByCategory,
        Document document,
        TextSpan span,
        CancellationToken cancellationToken)
    {
        var codeFixService = document.Project.Solution.Services.ExportProvider.GetExports<ICodeFixService>().First().Value;

        using var _1 = ArrayBuilder<CodeAction>.GetInstance(out var actions);
        using var _2 = ArrayBuilder<CodeAction>.GetInstance(out var nestedActions);
        using var _3 = ArrayBuilder<CodeAction>.GetInstance(out var nestedNestedActions);
        var totalDiagnostics = 0;
        foreach (var (category, diagnosticsForCategory) in diagnosticsByCategory)
        {
            // Group diagnostics by ID and sort in descending order of diagnostic count for each ID.
            // We want to prioritize showing the suggestions for diagnostic IDs that have maximum violations.
            var diagnosticsById = diagnosticsForCategory.GroupBy(d => d.Id)
                .Select(group => (group.Key, group.AsImmutable()))
                .OrderByDescending(idAndDiagnostics => idAndDiagnostics.Item2.Length);

            var totalDiagnosticsForCategory = 0;
            foreach (var (id, diagnostics) in diagnosticsById)
            {
                Debug.Assert(diagnostics.All(d => d.Category == category));
                Debug.Assert(diagnostics.All(d => d.DataLocation.DocumentId != null));
                Debug.Assert(diagnostics.All(d => d.DataLocation.DocumentId?.ProjectId == document.Project.Id));

                var diagnosticAndDocument = await GetPreferredDiagnosticAndDocumentAsync(diagnostics, document, span, cancellationToken).ConfigureAwait(false);
                if (!diagnosticAndDocument.HasValue)
                    continue;

                var (diagnostic, documentForDiagnostic) = diagnosticAndDocument.Value;

                // We only show code fix if the preferred diagnostic is in the current document.
                if (documentForDiagnostic == document)
                {
                    var codeFixCollection = await codeFixService.GetDocumentFixAllForIdInSpanAsync(documentForDiagnostic, diagnostic.Location.SourceSpan, id, diagnostic.Severity, CodeActionOptions.DefaultProvider, cancellationToken).ConfigureAwait(false);
                    if (codeFixCollection != null)
                    {
                        nestedNestedActions.AddRange(codeFixCollection.Fixes.Select(f => f.Action));
                    }
                    else
                    {
                        // This diagnostic has no code fix, or fix application failed.
                        // TODO: Can we add some code action such that it shows a preview of the diagnostic span with squiggle?
                    }
                }

                // Add configure severity fix if diagnostic has configurable severity.
                if (!SuppressionHelpers.IsNotConfigurableDiagnostic(diagnostic))
                {
                    var nestedNestedAction = ConfigureSeverityLevelCodeFixProvider.CreateSeverityConfigurationCodeAction(diagnostic, document.Project);
                    nestedNestedActions.Add(nestedNestedAction);
                }

                // {0} ({1}): {2}
                var title = string.Format(FeaturesResources.Code_analysis_improvements_diagnostic_id_based_title,
                    diagnostic.Id, diagnostics.Length, diagnostic.Descriptor.Title);
                var nestedAction = CodeAction.Create(title, nestedNestedActions.ToImmutableAndClear(), isInlinable: false);
                nestedActions.Add(nestedAction);
                totalDiagnosticsForCategory += diagnostics.Length;
            }

            if (nestedActions.Count == 0)
                continue;

            Debug.Assert(totalDiagnosticsForCategory > 0);
            totalDiagnostics += totalDiagnosticsForCategory;

            // Add code action to Configure severity for the entire 'Category'
            var categoryConfigurationAction = ConfigureSeverityLevelCodeFixProvider.CreateBulkSeverityConfigurationCodeAction(category, document.Project);
            nestedActions.Add(categoryConfigurationAction);

            // {0} ({1})
            var categoryBasedTitle = string.Format(FeaturesResources.Code_analysis_improvements_category_based_title,
                category, totalDiagnosticsForCategory);
            var action = CodeAction.Create(categoryBasedTitle, nestedActions.ToImmutableAndClear(), isInlinable: false);
            actions.Add(action);
        }

        // If we have non-zero actions, then also add a nested item to disable showing these code analysis suggestions.
        if (actions.Count > 0)
        {
            var disablingAction = CodeAction.Create(FeaturesResources.Do_not_show_Code_analysis_improvements,
                createChangedSolution: cancellationToken =>
                {
                    var globalOptions = document.Project.Solution.Services.ExportProvider.GetExports<IGlobalOptionService>().Single().Value;
                    globalOptions.SetGlobalOption(CodeAnalysisSuggestionsOptionsStorage.ShowCodeAnalysisSuggestionsInLightbulb, false);
                    return Task.FromResult(document.Project.Solution);
                },
                equivalenceKey: nameof(FeaturesResources.Do_not_show_Code_analysis_improvements));
            actions.Add(disablingAction);
        }

        return (actions.ToImmutable(), totalDiagnostics);

        static async Task<(Diagnostic Diagnostic, Document Document)?> GetPreferredDiagnosticAndDocumentAsync(ImmutableArray<DiagnosticData> diagnostics, Document document, TextSpan span, CancellationToken cancellationToken)
        {
            Debug.Assert(diagnostics.Length > 0);

            // We prefer diagnostic in the given document that is closest to the lightbulb span.
            Diagnostic? preferredDiagnostic = null;
            var minDistance = int.MaxValue;

            foreach (var diagnosticData in diagnostics)
            {
                // We expect all diagnostics to have a source location in some document in the given project.
                Debug.Assert(diagnosticData.DocumentId != null);
                Debug.Assert(diagnosticData.ProjectId == document.Project.Id);

                var diagnostic = await diagnosticData.ToDiagnosticAsync(document.Project, cancellationToken).ConfigureAwait(false);
                Debug.Assert(diagnostic.Location.IsInSource);

                if (diagnosticData.DocumentId == document.Id)
                {
                    var distance = GetDistance(span, diagnostic.Location.SourceSpan);
                    if (distance < minDistance)
                    {
                        preferredDiagnostic = diagnostic;
                        minDistance = distance;
                    }
                }
            }

            if (preferredDiagnostic != null)
            {
                return (preferredDiagnostic, document);
            }

            // All the computed diagnostics are in other documents in this project, we have no specific preference amongst those.
            // Return the valid diagnostic/document pair in the project.
            foreach (var diagnosticData in diagnostics)
            {
                Debug.Assert(diagnosticData.DocumentId != document.Id);

                if (document.Project.GetDocument(diagnosticData.DocumentId!) is not { } otherDocument)
                    continue;

                preferredDiagnostic = await diagnosticData.ToDiagnosticAsync(otherDocument.Project, cancellationToken).ConfigureAwait(false);
                return (preferredDiagnostic, otherDocument);
            }

            return null;

            static int GetDistance(TextSpan span1, TextSpan span2)
            {
                if (span1.IntersectsWith(span2))
                    return 0;

                var diff = span2.Start - span1.End;
                if (diff > 0)
                    return diff;

                return span1.Start - span2.End;
            }
        }
    }
}
