// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.RemoveUnnecessaryImports;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeCleanup
{
    internal abstract class AbstractCodeCleanupService : ICodeCleanupService
    {
        private readonly ICodeFixService _codeFixService;

        protected AbstractCodeCleanupService(ICodeFixService codeFixService)
        {
            _codeFixService = codeFixService;
        }

        protected abstract string OrganizeImportsDescription { get; }
        protected abstract ImmutableArray<DiagnosticSet> GetDiagnosticSets();

        public async Task<Document> CleanupAsync(
            Document document,
            EnabledDiagnosticOptions enabledDiagnostics,
            IProgressTracker progressTracker,
            CancellationToken cancellationToken)
        {
            // add one item for the 'format' action we'll do last
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

            document = await ApplyCodeFixesAsync(
                document, enabledDiagnostics.Diagnostics, progressTracker, cancellationToken).ConfigureAwait(false);

            // do the remove usings after code fix, as code fix might remove some code which can results in unused usings.
            if (organizeUsings)
            {
                progressTracker.Description = this.OrganizeImportsDescription;
                document = await RemoveSortUsingsAsync(
                    document, enabledDiagnostics.OrganizeUsings, cancellationToken).ConfigureAwait(false);
                progressTracker.ItemCompleted();
            }

            if (enabledDiagnostics.FormatDocument)
            {
                progressTracker.Description = FeaturesResources.Formatting_document;
                using (Logger.LogBlock(FunctionId.CodeCleanup_Format, cancellationToken))
                {
                    document = await Formatter.FormatAsync(document, cancellationToken: cancellationToken).ConfigureAwait(false);
                    progressTracker.ItemCompleted();
                }
            }

            return document;
        }

        private static async Task<Document> RemoveSortUsingsAsync(
            Document document, OrganizeUsingsSet organizeUsingsSet, CancellationToken cancellationToken)
        {
            if (organizeUsingsSet.IsRemoveUnusedImportEnabled)
            {
                var removeUsingsService = document.GetLanguageService<IRemoveUnnecessaryImportsService>();
                if (removeUsingsService != null)
                {
                    using (Logger.LogBlock(FunctionId.CodeCleanup_RemoveUnusedImports, cancellationToken))
                    {
                        document = await removeUsingsService.RemoveUnnecessaryImportsAsync(document, cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            if (organizeUsingsSet.IsSortImportsEnabled)
            {
                using (Logger.LogBlock(FunctionId.CodeCleanup_SortImports, cancellationToken))
                {
                    document = await Formatter.OrganizeImportsAsync(document, cancellationToken).ConfigureAwait(false);
                }
            }

            return document;
        }

        private async Task<Document> ApplyCodeFixesAsync(
            Document document, ImmutableArray<DiagnosticSet> enabledDiagnosticSets,
            IProgressTracker progressTracker, CancellationToken cancellationToken)
        {
            // Add a progress item for each enabled option we're going to fixup.
            progressTracker.AddItems(enabledDiagnosticSets.Length);

            foreach (var diagnosticSet in enabledDiagnosticSets)
            {
                cancellationToken.ThrowIfCancellationRequested();

                progressTracker.Description = diagnosticSet.Description;
                document = await ApplyCodeFixesForSpecificDiagnosticIdsAsync(
                    document, diagnosticSet.DiagnosticIds, progressTracker, cancellationToken).ConfigureAwait(false);

                // Mark this option as being completed.
                progressTracker.ItemCompleted();
            }

            return document;
        }

        private async Task<Document> ApplyCodeFixesForSpecificDiagnosticIdsAsync(
            Document document, ImmutableArray<string> diagnosticIds, IProgressTracker progressTracker, CancellationToken cancellationToken)
        {
            foreach (var diagnosticId in diagnosticIds)
            {
                using (Logger.LogBlock(FunctionId.CodeCleanup_ApplyCodeFixesAsync, diagnosticId, cancellationToken))
                {
                    document = await _codeFixService.ApplyCodeFixesForSpecificDiagnosticIdAsync(
                        document, diagnosticId, progressTracker, cancellationToken).ConfigureAwait(false);
                }
            }

            return document;
        }

        public EnabledDiagnosticOptions GetAllDiagnostics()
            => new EnabledDiagnosticOptions(formatDocument: true, GetDiagnosticSets(), new OrganizeUsingsSet(isRemoveUnusedImportEnabled: true, isSortImportsEnabled: true));
    }
}
