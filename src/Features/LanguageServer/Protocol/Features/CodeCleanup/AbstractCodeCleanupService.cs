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
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeCleanup
{
    internal abstract class AbstractCodeCleanupService : ICodeCleanupService
    {
        private readonly ICodeFixService _codeFixService;
        private readonly IDiagnosticAnalyzerService _diagnosticService;

        protected AbstractCodeCleanupService(ICodeFixService codeFixService, IDiagnosticAnalyzerService diagnosticAnalyzerService)
        {
            _codeFixService = codeFixService;
            _diagnosticService = diagnosticAnalyzerService;
        }

        protected abstract string OrganizeImportsDescription { get; }
        protected abstract ImmutableArray<DiagnosticSet> GetDiagnosticSets();

        public async Task<Document> CleanupAsync(
            Document document,
            EnabledDiagnosticOptions enabledDiagnostics,
            IProgressTracker progressTracker,
            CodeActionOptionsProvider fallbackOptions,
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
                document, enabledDiagnostics.Diagnostics, progressTracker, fallbackOptions, cancellationToken).ConfigureAwait(false);

            if (enabledDiagnostics.RunThirdPartyFixers)
            {
                document = await ApplyThirdPartyCodeFixesAsync(
                    document, thirdPartyDiagnosticIdsAndTitles, progressTracker, fallbackOptions, cancellationToken).ConfigureAwait(false);
            }

            // do the remove usings after code fix, as code fix might remove some code which can results in unused usings.
            if (organizeUsings)
            {
                progressTracker.Description = this.OrganizeImportsDescription;
                document = await RemoveSortUsingsAsync(
                    document, enabledDiagnostics.OrganizeUsings, fallbackOptions, cancellationToken).ConfigureAwait(false);
                progressTracker.ItemCompleted();
            }

            if (enabledDiagnostics.FormatDocument)
            {
                var formattingOptions = await document.GetSyntaxFormattingOptionsAsync(fallbackOptions, cancellationToken).ConfigureAwait(false);

                progressTracker.Description = FeaturesResources.Formatting_document;
                using (Logger.LogBlock(FunctionId.CodeCleanup_Format, cancellationToken))
                {
                    document = await Formatter.FormatAsync(document, formattingOptions, cancellationToken).ConfigureAwait(false);
                    progressTracker.ItemCompleted();
                }
            }

            if (enabledDiagnostics.RunThirdPartyFixers)
            {
                document = await ApplyThirdPartyCodeFixesAsync(
                    document, thirdPartyDiagnosticIdsAndTitles, progressTracker, fallbackOptions, cancellationToken).ConfigureAwait(false);
            }

            return document;
        }

        private static async Task<Document> RemoveSortUsingsAsync(
            Document document, OrganizeUsingsSet organizeUsingsSet, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            if (organizeUsingsSet.IsRemoveUnusedImportEnabled &&
                document.GetLanguageService<IRemoveUnnecessaryImportsService>() is { } removeUsingsService)
            {
                using (Logger.LogBlock(FunctionId.CodeCleanup_RemoveUnusedImports, cancellationToken))
                {
                    var formattingOptions = await document.GetSyntaxFormattingOptionsAsync(fallbackOptions, cancellationToken).ConfigureAwait(false);
                    document = await removeUsingsService.RemoveUnnecessaryImportsAsync(document, formattingOptions, cancellationToken).ConfigureAwait(false);
                }
            }

            if (organizeUsingsSet.IsSortImportsEnabled &&
                document.GetLanguageService<IOrganizeImportsService>() is { } organizeImportsService)
            {
                using (Logger.LogBlock(FunctionId.CodeCleanup_SortImports, cancellationToken))
                {
                    var organizeOptions = await document.GetOrganizeImportsOptionsAsync(fallbackOptions, cancellationToken).ConfigureAwait(false);
                    document = await organizeImportsService.OrganizeImportsAsync(document, organizeOptions, cancellationToken).ConfigureAwait(false);
                }
            }

            return document;
        }

        private async Task<Document> ApplyCodeFixesAsync(
            Document document, ImmutableArray<DiagnosticSet> enabledDiagnosticSets,
            IProgressTracker progressTracker, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            // Add a progress item for each enabled option we're going to fixup.
            foreach (var diagnosticSet in enabledDiagnosticSets)
            {
                cancellationToken.ThrowIfCancellationRequested();

                progressTracker.Description = diagnosticSet.Description;
                document = await ApplyCodeFixesForSpecificDiagnosticIdsAsync(
                    document, diagnosticSet.DiagnosticIds, progressTracker, fallbackOptions, cancellationToken).ConfigureAwait(false);

                // Mark this option as being completed.
                progressTracker.ItemCompleted();
            }

            return document;
        }

        private async Task<Document> ApplyCodeFixesForSpecificDiagnosticIdsAsync(
            Document document, ImmutableArray<string> diagnosticIds, IProgressTracker progressTracker, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            foreach (var diagnosticId in diagnosticIds)
            {
                using (Logger.LogBlock(FunctionId.CodeCleanup_ApplyCodeFixesAsync, diagnosticId, cancellationToken))
                {
                    document = await ApplyCodeFixesForSpecificDiagnosticIdAsync(
                        document, diagnosticId, progressTracker, fallbackOptions, cancellationToken).ConfigureAwait(false);
                }
            }

            return document;
        }

        private async Task<Document> ApplyCodeFixesForSpecificDiagnosticIdAsync(Document document, string diagnosticId, IProgressTracker progressTracker, CodeActionOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var textSpan = new TextSpan(0, tree.Length);

            var fixCollection = await _codeFixService.GetDocumentFixAllForIdInSpanAsync(
                document, textSpan, diagnosticId, fallbackOptions, cancellationToken).ConfigureAwait(false);
            if (fixCollection == null)
            {
                return document;
            }

            var fixAllService = document.Project.Solution.Services.GetRequiredService<IFixAllGetFixesService>();

            var solution = await fixAllService.GetFixAllChangedSolutionAsync(
                new FixAllContext(fixCollection.FixAllState, progressTracker, cancellationToken)).ConfigureAwait(false);
            Contract.ThrowIfNull(solution);

            return solution.GetDocument(document.Id) ?? throw new NotSupportedException(FeaturesResources.Removal_of_document_not_supported);
        }

        private async Task<ImmutableArray<(string diagnosticId, string? title)>> GetThirdPartyDiagnosticIdsAndTitlesAsync(Document document, CancellationToken cancellationToken)
        {
            var tree = await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var range = new TextSpan(0, tree.Length);

            // Compute diagnostics for everything that is not an IDE analyzer
            var diagnostics = (await _diagnosticService.GetDiagnosticsForSpanAsync(document, range,
                shouldIncludeDiagnostic: static diagnosticId => !(IDEDiagnosticIdToOptionMappingHelper.IsKnownIDEDiagnosticId(diagnosticId)),
                includeCompilerDiagnostics: true, includeSuppressedDiagnostics: false,
                priorityProvider: new DefaultCodeActionRequestPriorityProvider(),
                addOperationScope: null, DiagnosticKind.All, isExplicit: false,
                cancellationToken).ConfigureAwait(false));

            // ensure more than just known diagnostics were returned
            if (!diagnostics.Any())
            {
                return ImmutableArray<(string diagnosticId, string? title)>.Empty;
            }

            return diagnostics.SelectAsArray(static d => (d.Id, d.Title)).Distinct();
        }

        private async Task<Document> ApplyThirdPartyCodeFixesAsync(
            Document document,
            ImmutableArray<(string diagnosticId, string? title)> diagnosticIds,
            IProgressTracker progressTracker,
            CodeActionOptionsProvider fallbackOptions,
            CancellationToken cancellationToken)
        {
            foreach (var (diagnosticId, title) in diagnosticIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                progressTracker.Description = string.Format(FeaturesResources.Fixing_0, title ?? diagnosticId);
                // Apply codefixes for diagnostics with a severity of warning or higher
                var updatedDocument = await _codeFixService.ApplyCodeFixesForSpecificDiagnosticIdAsync(
                    document, diagnosticId, DiagnosticSeverity.Warning, progressTracker, fallbackOptions, cancellationToken).ConfigureAwait(false);

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
}
