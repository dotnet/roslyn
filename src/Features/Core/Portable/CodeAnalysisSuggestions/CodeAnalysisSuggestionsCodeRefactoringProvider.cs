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

    protected override CodeActionRequestPriority ComputeRequestPriority() => CodeActionRequestPriority.Low;

    public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        var (document, span, cancellationToken) = context;

        var configService = document.Project.Solution.Services.GetRequiredService<ICodeAnalysisSuggestionsConfigService>();
        var ruleConfigData = await configService.TryGetCodeAnalysisSuggestionsConfigDataAsync(document.Project, cancellationToken).ConfigureAwait(false);
        if (ruleConfigData.IsEmpty)
            return;

        var codeFixService = document.Project.Solution.Services.ExportProvider.GetExports<ICodeFixService>().FirstOrDefault()?.Value;
        if (codeFixService == null)
            return;

        var (actions, totalViolations) = await GetCodeAnalysisSuggestionActionsAsync(ruleConfigData, document, codeFixService, cancellationToken).ConfigureAwait(false);
        if (actions.Length > 0)
        {
            Debug.Assert(totalViolations > 0);

            context.RegisterRefactoring(
                CodeAction.Create(
                    string.Format(FeaturesResources.Code_analysis_improvements_0, totalViolations),
                    actions,
                    isInlinable: false,
                    CodeActionPriority.Low),
                span);
        }
    }

    private static async Task<(ImmutableArray<CodeAction> Actions, int TotalViolations)> GetCodeAnalysisSuggestionActionsAsync(
        ImmutableArray<(string, ImmutableDictionary<string, ImmutableArray<DiagnosticData>>)> configData,
        Document document,
        ICodeFixService codeFixService,
        CancellationToken cancellationToken)
    {
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var location = root.GetLocation();

        using var _1 = ArrayBuilder<CodeAction>.GetInstance(out var actionsBuilder);
        using var _2 = ArrayBuilder<CodeAction>.GetInstance(out var nestedActionsBuilder);
        using var _3 = ArrayBuilder<CodeAction>.GetInstance(out var nestedNestedActionsBuilder);
        var totalViolations = 0;
        foreach (var (category, diagnosticsById) in configData)
        {
            var totalViolationsForCategory = 0;
            foreach (var (id, diagnostics) in diagnosticsById)
            {
                Debug.Assert(diagnostics.All(d => string.Equals(d.Category, category, StringComparison.OrdinalIgnoreCase)));

                var (diagnosticData, documentForFix) = GetPreferredDiagnosticAndDocument(diagnostics, document);
                var diagnostic = await diagnosticData.ToDiagnosticAsync(document.Project, cancellationToken).ConfigureAwait(false);
                if (SuppressionHelpers.IsNotConfigurableDiagnostic(diagnostic))
                    continue;

                if (documentForFix != null)
                {
                    var codeFixCollection = await codeFixService.GetDocumentFixAllForIdInSpanAsync(documentForFix, diagnostic.Location.SourceSpan, id, CodeActionOptions.DefaultProvider, cancellationToken).ConfigureAwait(false);
                    if (codeFixCollection != null)
                    {
                        nestedNestedActionsBuilder.AddRange(codeFixCollection.Fixes.Select(f => f.Action));
                    }
                    else
                    {
                        // This diagnostic has no code fix, or fix application failed.
                        // TODO: Can we add some code action such that it shows a preview of the diagnostic span with squiggle?
                    }
                }
                else
                {
                    // This is either a project diagnostic with no location OR a dummy diagnostic created from descriptor without background analysis.
                    // Append this document's span as it location so we can show configure severity code fix for it.
                    diagnostic = Diagnostic.Create(diagnostic.Descriptor, location);
                }

                var nestedNestedAction = ConfigureSeverityLevelCodeFixProvider.CreateSeverityConfigurationCodeAction(diagnostic, document.Project);
                nestedNestedActionsBuilder.Add(nestedNestedAction);

                var totalInCurrentProject = diagnostics.Where(dd => dd.ProjectId == document.Project.Id).Count();
                if (totalInCurrentProject > 0)
                {
                    // {0} ({1}): {2}
                    var title = string.Format(FeaturesResources.Code_analysis_improvements_diagnostic_id_based_title,
                        diagnostic.Id, totalInCurrentProject, diagnostic.Descriptor.Title);
                    var nestedAction = CodeAction.Create(title, nestedNestedActionsBuilder.ToImmutableAndClear(), isInlinable: false);
                    nestedActionsBuilder.Add(nestedAction);
                    totalViolationsForCategory += totalInCurrentProject;
                }
            }

            if (nestedActionsBuilder.Count == 0)
                continue;

            Debug.Assert(totalViolationsForCategory > 0);
            totalViolations += totalViolationsForCategory;

            // Add code action to Configure severity for the entire 'Category'
            var categoryConfigurationAction = ConfigureSeverityLevelCodeFixProvider.CreateBulkSeverityConfigurationCodeAction(category, document.Project);
            nestedActionsBuilder.Add(categoryConfigurationAction);

            // {0} ({1})
            var categoryBasedTitle = string.Format(FeaturesResources.Code_analysis_improvements_category_based_title,
                category, totalViolationsForCategory);
            var action = CodeAction.Create(categoryBasedTitle, nestedActionsBuilder.ToImmutableAndClear(), isInlinable: false);
            actionsBuilder.Add(action);
        }

        if (actionsBuilder.Count > 0)
        {
            var disablingAction = CodeAction.Create(FeaturesResources.Do_not_show_Code_analysis_improvements,
                createChangedSolution: cancellationToken =>
                {
                    var globalOptions = document.Project.Solution.Services.ExportProvider.GetExports<IGlobalOptionService>().Single().Value;
                    globalOptions.SetGlobalOption(CodeAnalysisSuggestionsOptionsStorage.ShowCodeAnalysisSuggestionsInLightbulb, false);
                    return Task.FromResult(document.Project.Solution);
                },
                equivalenceKey: nameof(FeaturesResources.Do_not_show_Code_analysis_improvements));
            actionsBuilder.Add(disablingAction);
        }

        return (actionsBuilder.ToImmutable(), totalViolations);

        static (DiagnosticData, Document?) GetPreferredDiagnosticAndDocument(ImmutableArray<DiagnosticData> diagnostics, Document document)
        {
            (DiagnosticData diagnostic, DocumentId? documentId)? preferredDiagnosticAndDocumentId = null;
            foreach (var diagnostic in diagnostics)
            {
                if (diagnostic.DocumentId == document.Id)
                {
                    return (diagnostic, document);
                }
                else if (!preferredDiagnosticAndDocumentId.HasValue &&
                    diagnostic.DocumentId?.ProjectId == document.Project.Id)
                {
                    preferredDiagnosticAndDocumentId = (diagnostic, diagnostic.DocumentId);
                }
            }

            if (preferredDiagnosticAndDocumentId.HasValue)
            {
                return (preferredDiagnosticAndDocumentId.Value.diagnostic,
                    document.Project.GetDocument(preferredDiagnosticAndDocumentId.Value.documentId!));
            }

            return (diagnostics.First(), null);
        }
    }
}
