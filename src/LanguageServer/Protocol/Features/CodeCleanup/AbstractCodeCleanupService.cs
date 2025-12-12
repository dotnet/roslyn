// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.OrganizeImports;
using Microsoft.CodeAnalysis.RemoveUnnecessaryImports;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeCleanup;

internal abstract class AbstractCodeCleanupService(ICodeFixService codeFixService) : ICodeCleanupService
{
    private readonly ICodeFixService _codeFixService = codeFixService;

    protected abstract string OrganizeImportsDescription { get; }
    protected abstract ImmutableArray<DiagnosticSet> GetDiagnosticSets();

    public async Task<Document> CleanupAsync(
        Document document,
        EnabledDiagnosticOptions enabledDiagnostics,
        IProgress<CodeAnalysisProgress> progressTracker,
        CancellationToken cancellationToken)
    {
        // add one item for the code fixers we get from nuget, we'll do last
        var thirdPartyDiagnosticIdsAndTitles = ImmutableArray<(string diagnosticId, string? title)>.Empty;
        if (enabledDiagnostics.RunThirdPartyFixers)
        {
            thirdPartyDiagnosticIdsAndTitles = await GetThirdPartyDiagnosticIdsAndTitlesAsync(document, cancellationToken).ConfigureAwait(false);
            progressTracker.AddItems(thirdPartyDiagnosticIdsAndTitles.Length);
        }

        // add one item for the 'format' action
        if (enabledDiagnostics.FormatDocument)
        {
            progressTracker.AddItems(1);
        }

        // and one for 'remove/sort usings' if we're going to run that.
        var organizeUsings = enabledDiagnostics.OrganizeUsings.IsRemoveUnusedImportEnabled ||
            enabledDiagnostics.OrganizeUsings.IsSortImportsEnabled;
        if (organizeUsings)
        {
            progressTracker.AddItems(1);
        }

        if (enabledDiagnostics.Diagnostics.Any())
        {
            progressTracker.AddItems(enabledDiagnostics.Diagnostics.Length);
        }

        document = await ApplyCodeFixesAsync(
            document, enabledDiagnostics.Diagnostics, progressTracker, cancellationToken).ConfigureAwait(false);

        if (enabledDiagnostics.RunThirdPartyFixers)
        {
            document = await ApplyThirdPartyCodeFixesAsync(
                document, thirdPartyDiagnosticIdsAndTitles, progressTracker, cancellationToken).ConfigureAwait(false);
        }

        // do the remove usings after code fix, as code fix might remove some code which can results in unused usings.
        if (organizeUsings)
        {
            progressTracker.Report(CodeAnalysisProgress.Description(this.OrganizeImportsDescription));
            document = await RemoveSortUsingsAsync(
                document, enabledDiagnostics.OrganizeUsings, cancellationToken).ConfigureAwait(false);
            progressTracker.ItemCompleted();
        }

        if (enabledDiagnostics.FormatDocument)
        {
            var formattingOptions = await document.GetSyntaxFormattingOptionsAsync(cancellationToken).ConfigureAwait(false);

            progressTracker.Report(CodeAnalysisProgress.Description(FeaturesResources.Formatting_document));
            using (Logger.LogBlock(FunctionId.CodeCleanup_Format, cancellationToken))
            {
                document = await Formatter.FormatAsync(document, formattingOptions, cancellationToken).ConfigureAwait(false);
                progressTracker.ItemCompleted();
            }
        }

        if (enabledDiagnostics.RunThirdPartyFixers)
        {
            document = await ApplyThirdPartyCodeFixesAsync(
                document, thirdPartyDiagnosticIdsAndTitles, progressTracker, cancellationToken).ConfigureAwait(false);
        }

        return document;
    }

    private static async Task<Document> RemoveSortUsingsAsync(
        Document document, OrganizeUsingsSet organizeUsingsSet, CancellationToken cancellationToken)
    {
        if (organizeUsingsSet.IsRemoveUnusedImportEnabled)
        {
            using (Logger.LogBlock(FunctionId.CodeCleanup_RemoveUnusedImports, cancellationToken))
            {
                // The compiler reports any usings/imports it didn't think were used.  Regardless of the state of
                // the code in the file.  For example, if there are major parse errors, it can end up causing
                // many usings to seem unused simply because the compiler isn't actually able to determine the
                // meaning of all the code.  Similarly, in scenarios where there may be a bunch of disabled code
                // (like when merge markers are introduced) this can happen as well.
                //
                // For the normal editing experience, this is not a huge deal.  The usings/imports may fade,
                // but they'll stay around unless the user goes out of the way to remove them.  That's not the case
                // for code-cleanup, which may run automatically on actions like 'save'.  As such, we don't 
                // remove usings in that case if we see that there are major issues in the file (like syntactic
                // diagnostics).
                var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                if (!root.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error))
                {
                    var formattingOptions = await document.GetSyntaxFormattingOptionsAsync(cancellationToken).ConfigureAwait(false);

                    var removeUsingsService = document.GetRequiredLanguageService<IRemoveUnnecessaryImportsService>();
                    document = await removeUsingsService.RemoveUnnecessaryImportsAsync(document, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        if (organizeUsingsSet.IsSortImportsEnabled)
        {
            using (Logger.LogBlock(FunctionId.CodeCleanup_SortImports, cancellationToken))
            {
                var organizeOptions = await document.GetOrganizeImportsOptionsAsync(cancellationToken).ConfigureAwait(false);

                var organizeImportsService = document.GetRequiredLanguageService<IOrganizeImportsService>();
                document = await organizeImportsService.OrganizeImportsAsync(document, organizeOptions, cancellationToken).ConfigureAwait(false);
            }
        }

        return document;
    }

    private async Task<Document> ApplyCodeFixesAsync(
        Document document, ImmutableArray<DiagnosticSet> enabledDiagnosticSets,
        IProgress<CodeAnalysisProgress> progressTracker, CancellationToken cancellationToken)
    {
        // Add a progressTracker item for each enabled option we're going to fixup.
        foreach (var diagnosticSet in enabledDiagnosticSets)
        {
            cancellationToken.ThrowIfCancellationRequested();

            progressTracker.Report(CodeAnalysisProgress.Description(diagnosticSet.Description));
            document = await ApplyCodeFixesForSpecificDiagnosticIdsAsync(
                document, diagnosticSet.DiagnosticIds, diagnosticSet.IsAnyDiagnosticIdExplicitlyEnabled, progressTracker, cancellationToken).ConfigureAwait(false);

            // Mark this option as being completed.
            progressTracker.ItemCompleted();
        }

        return document;
    }

    private async Task<Document> ApplyCodeFixesForSpecificDiagnosticIdsAsync(
        Document document, ImmutableArray<string> diagnosticIds, bool isAnyDiagnosticIdExplicitlyEnabled, IProgress<CodeAnalysisProgress> progressTracker, CancellationToken cancellationToken)
    {
        // Enable fixes for all diagnostic severities if any of the diagnostic IDs has been explicitly enabled in Code Cleanup.
        // Otherwise, only enable fixes for Warning and Error severity diagnostics.
        var minimumSeverity = isAnyDiagnosticIdExplicitlyEnabled ? DiagnosticSeverity.Hidden : DiagnosticSeverity.Warning;

        foreach (var diagnosticId in diagnosticIds)
        {
            using (Logger.LogBlock(FunctionId.CodeCleanup_ApplyCodeFixesAsync, diagnosticId, cancellationToken))
            {
                document = await ApplyCodeFixesForSpecificDiagnosticIdAsync(
                    document, diagnosticId, minimumSeverity, progressTracker, cancellationToken).ConfigureAwait(false);
            }
        }

        return document;
    }

    private async Task<Document> ApplyCodeFixesForSpecificDiagnosticIdAsync(
        Document document, string diagnosticId, DiagnosticSeverity minimumSeverity, IProgress<CodeAnalysisProgress> progressTracker, CancellationToken cancellationToken)
    {
        var fixCollection = await _codeFixService.GetDocumentFixAllForIdInSpanAsync(
            document, textSpan: null, diagnosticId, minimumSeverity, cancellationToken).ConfigureAwait(false);
        if (fixCollection == null)
        {
            return document;
        }

        var fixAllService = document.Project.Solution.Services.GetRequiredService<IFixAllGetFixesService>();

        var solution = await fixAllService.GetFixAllChangedSolutionAsync(
            new FixAllContext(fixCollection.FixAllState!, progressTracker, cancellationToken)).ConfigureAwait(false);
        Contract.ThrowIfNull(solution);

        return solution.GetDocument(document.Id) ?? throw new NotSupportedException(FeaturesResources.Removal_of_document_not_supported);
    }

    private async Task<ImmutableArray<(string diagnosticId, string? title)>> GetThirdPartyDiagnosticIdsAndTitlesAsync(
        Document document, CancellationToken cancellationToken)
    {
        var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
        var range = new TextSpan(0, tree.Length);

        var diagnosticService = document.Project.Solution.Services.GetRequiredService<IDiagnosticAnalyzerService>();
        var diagnostics = await diagnosticService.GetDiagnosticsForSpanAsync(
            document, range,
            // Compute diagnostics for everything that is *NOT* an IDE analyzer
            DiagnosticIdFilter.Exclude(IDEDiagnosticIdToOptionMappingHelper.KnownIDEDiagnosticIds),
            priority: null,
            DiagnosticKind.All,
            cancellationToken).ConfigureAwait(false);

        // We don't want code cleanup automatically cleaning suppressed diagnostics.
        diagnostics = diagnostics.WhereAsArray(d => !d.IsSuppressed);

        // ensure more than just known diagnostics were returned
        if (!diagnostics.Any())
        {
            return [];
        }

        return diagnostics.SelectAsArray(static d => (d.Id, d.Title)).Distinct();
    }

    private async Task<Document> ApplyThirdPartyCodeFixesAsync(
        Document document,
        ImmutableArray<(string diagnosticId, string? title)> diagnosticIds,
        IProgress<CodeAnalysisProgress> progressTracker,
        CancellationToken cancellationToken)
    {
        foreach (var (diagnosticId, title) in diagnosticIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            progressTracker.Report(CodeAnalysisProgress.Description(string.Format(FeaturesResources.Fixing_0, title ?? diagnosticId)));
            // Apply codefixes for diagnostics with a severity of warning or higher
            var updatedDocument = await _codeFixService.ApplyCodeFixesForSpecificDiagnosticIdAsync(
                document, textSpan: null, diagnosticId, DiagnosticSeverity.Warning, progressTracker, cancellationToken).ConfigureAwait(false);

            // If changes were made to the solution snap shot outside the current document discard the changes.
            // The assumption here is that if we are applying a third party code fix to a document it only affects the document.
            // Symbol renames and other complex refactorings we do not want to include in code cleanup.
            // We can revisit this if we get feedback to the contrary
            if (!ChangesMadeOutsideDocument(document, updatedDocument))
            {
                document = updatedDocument;
            }

            progressTracker.ItemCompleted();
        }

        return document;

        static bool ChangesMadeOutsideDocument(Document currentDocument, Document updatedDocument)
        {
            var solutionChanges = updatedDocument.Project.Solution.GetChanges(currentDocument.Project.Solution);
            return
                solutionChanges.GetAddedProjects().Any() ||
                solutionChanges.GetRemovedProjects().Any() ||
                solutionChanges.GetAddedAnalyzerReferences().Any() ||
                solutionChanges.GetRemovedAnalyzerReferences().Any() ||
                solutionChanges.GetProjectChanges().Any(
                    projectChanges => projectChanges.GetAddedProjectReferences().Any() ||
                                      projectChanges.GetRemovedProjectReferences().Any() ||
                                      projectChanges.GetAddedMetadataReferences().Any() ||
                                      projectChanges.GetRemovedMetadataReferences().Any() ||
                                      projectChanges.GetAddedAnalyzerReferences().Any() ||
                                      projectChanges.GetRemovedAnalyzerReferences().Any() ||
                                      projectChanges.GetAddedDocuments().Any() ||
                                      projectChanges.GetAddedAdditionalDocuments().Any() ||
                                      projectChanges.GetAddedAnalyzerConfigDocuments().Any() ||
                                      projectChanges.GetChangedDocuments().Any(documentId => documentId != updatedDocument.Id) ||
                                      projectChanges.GetChangedAdditionalDocuments().Any(documentId => documentId != updatedDocument.Id) ||
                                      projectChanges.GetChangedAnalyzerConfigDocuments().Any(documentId => documentId != updatedDocument.Id));
        }
    }
    public EnabledDiagnosticOptions GetAllDiagnostics()
        => new(FormatDocument: true, RunThirdPartyFixers: true, Diagnostics: GetDiagnosticSets(), OrganizeUsings: new OrganizeUsingsSet(isRemoveUnusedImportEnabled: true, isSortImportsEnabled: true));
}
