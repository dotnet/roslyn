// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeStyle;

namespace Microsoft.CodeAnalysis.Shared.CodeFixes
{
    /// <summary>
    /// A simple implementation of a <see cref="FixAllProvider"/> that handles concurrent
    /// execution over all the <see cref="Document"/>s in a <see cref="Project"/>.
    /// <para/>
    /// Subclasses need only concern them selves with processing all the diagnostics for a specific
    /// <see cref="Document"/>.  This class will then merge all the disparate changes to all
    /// those documents into a full solution change.
    /// </summary>
    internal abstract class AbstractConcurrentFixAllProvider : FixAllProvider
    {
        /// <summary>
        /// Overridden by subclasses to fix all the <see cref="Diagnostic"/>s provided in the given
        /// <see cref="Document"/>.  The diagnostics will be ordered by their position in the
        /// source, and will only contains values that returned <see langword="true"/> for
        /// <see cref="IncludeDiagnosticDuringFixAll"/>.
        /// </summary>
        protected abstract Task<Document> FixAllAsync(FixAllContext fixAllContext, Document document, ImmutableArray<Diagnostic> filteredDiagnostics);

        /// <summary>
        /// Whether or not this diagnostic should be included when performing a FixAll.  This is
        /// useful for providers that create multiple diagnostics for the same issue (For example,
        /// one main diagnostic and multiple 'faded out code' diagnostics).  FixAll can be invoked
        /// from any of those, but we'll only want perform an edit for only one diagnostic for each
        /// of those sets of diagnostics.
        /// </summary>
        protected virtual bool IncludeDiagnosticDuringFixAll(FixAllContext fixAllContext, Diagnostic diagnostic)
            => true;

        public sealed override async Task<CodeAction> GetFixAsync(FixAllContext fixAllContext)
        {
            var documentsAndDiagnosticsToFixMap = await GetDocumentDiagnosticsToFixAsync(fixAllContext).ConfigureAwait(false);
            return await GetFixAsync(documentsAndDiagnosticsToFixMap, fixAllContext).ConfigureAwait(false);
        }

        private async Task<ImmutableDictionary<Document, ImmutableArray<Diagnostic>>> GetDocumentDiagnosticsToFixAsync(FixAllContext fixAllContext)
        {
            var result = await GetDocumentDiagnosticsToFixWorkerAsync(fixAllContext).ConfigureAwait(false);

            // Filter out any documents that we don't have any diagnostics for.
            return result.Where(kvp => !kvp.Value.IsDefaultOrEmpty).ToImmutableDictionary();

            static async Task<ImmutableDictionary<Document, ImmutableArray<Diagnostic>>> GetDocumentDiagnosticsToFixWorkerAsync(FixAllContext fixAllContext)
            {
                return await FixAllContextHelper.GetDocumentDiagnosticsToFixAsync(
                    fixAllContext,
                    progressTrackerOpt: null).ConfigureAwait(false);
            }
        }

        private async Task<CodeAction> GetFixAsync(
            ImmutableDictionary<Document, ImmutableArray<Diagnostic>> documentsAndDiagnosticsToFixMap,
            FixAllContext fixAllContext)
        {
            // Process all documents in parallel.
            var updatedDocumentTasks = documentsAndDiagnosticsToFixMap.Select(
                kvp => FixDocumentAsync(kvp.Key, kvp.Value, fixAllContext));

            await Task.WhenAll(updatedDocumentTasks).ConfigureAwait(false);

            var currentSolution = fixAllContext.Solution;
            foreach (var task in updatedDocumentTasks)
            {
                // 'await' the tasks so that if any completed in a canceled manner then we'll
                // throw the right exception here.  Calling .Result on the tasks might end up
                // with AggregateExceptions being thrown instead.
                var updatedDocument = await task.ConfigureAwait(false);
                currentSolution = currentSolution.WithDocumentSyntaxRoot(
                    updatedDocument.Id,
                    await updatedDocument.GetSyntaxRootAsync(fixAllContext.CancellationToken).ConfigureAwait(false));
            }

            var title = FixAllContextHelper.GetDefaultFixAllTitle(fixAllContext);
            return new CustomCodeActions.SolutionChangeAction(title, _ => Task.FromResult(currentSolution));
        }

        private Task<Document> FixDocumentAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics, FixAllContext fixAllContext)
        {
            var cancellationToken = fixAllContext.CancellationToken;
            var equivalenceKey = fixAllContext.CodeActionEquivalenceKey;

            // Ensure that diagnostics for this document are always in document location
            // order.  This provides a consistent and deterministic order for fixers
            // that want to update a document.
            // Also ensure that we do not pass in duplicates by invoking Distinct.
            // See https://github.com/dotnet/roslyn/issues/31381, that seems to be causing duplicate diagnostics.
            var filteredDiagnostics = diagnostics.Distinct()
                                                 .WhereAsArray(d => this.IncludeDiagnosticDuringFixAll(fixAllContext, d))
                                                 .Sort((d1, d2) => d1.Location.SourceSpan.Start - d2.Location.SourceSpan.Start);

            // PERF: Do not invoke FixAllAsync on the code fix provider if there are no diagnostics to be fixed.
            if (filteredDiagnostics.Length == 0)
                return Task.FromResult(document);

            return this.FixAllAsync(fixAllContext, document, filteredDiagnostics);
        }
    }
}
