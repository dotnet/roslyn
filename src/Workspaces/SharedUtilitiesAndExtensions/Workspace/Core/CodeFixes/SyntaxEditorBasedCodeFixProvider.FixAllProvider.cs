// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CodeFixes
{
    internal partial class SyntaxEditorBasedCodeFixProvider : CodeFixProvider
    {
        /// <summary>
        /// A simple implementation of <see cref="FixAllProvider"/> that takes care of collecting
        /// all the diagnostics and fixes all documents in parallel.  The only functionality a 
        /// subclass needs to provide is how each document will apply all the fixes to all the 
        /// diagnostics in that document.
        /// </summary>
        internal sealed class SyntaxEditorBasedFixAllProvider : FixAllProvider
        {
            private readonly SyntaxEditorBasedCodeFixProvider _codeFixProvider;

            public SyntaxEditorBasedFixAllProvider(SyntaxEditorBasedCodeFixProvider codeFixProvider)
                => _codeFixProvider = codeFixProvider;

            public sealed override async Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
            {
                var documentsAndDiagnosticsToFixMap = await GetDocumentDiagnosticsToFixAsync(fixAllContext).ConfigureAwait(false);
                return await GetFixAsync(documentsAndDiagnosticsToFixMap, fixAllContext).ConfigureAwait(false);
            }

            private static async Task<ImmutableDictionary<Document, ImmutableArray<Diagnostic>>> GetDocumentDiagnosticsToFixAsync(FixAllContext fixAllContext)
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
                        await updatedDocument.GetRequiredSyntaxRootAsync(fixAllContext.CancellationToken).ConfigureAwait(false));
                }

                var title = FixAllContextHelper.GetDefaultFixAllTitle(fixAllContext);
                return new CustomCodeActions.SolutionChangeAction(title, _ => Task.FromResult(currentSolution));
            }

            private async Task<Document> FixDocumentAsync(
                Document document, ImmutableArray<Diagnostic> diagnostics, FixAllContext fixAllContext)
            {
                var model = await document.GetRequiredSemanticModelAsync(fixAllContext.CancellationToken).ConfigureAwait(false);

                // Ensure that diagnostics for this document are always in document location
                // order.  This provides a consistent and deterministic order for fixers
                // that want to update a document.
                // Also ensure that we do not pass in duplicates by invoking Distinct.
                // See https://github.com/dotnet/roslyn/issues/31381, that seems to be causing duplicate diagnostics.
                var filteredDiagnostics = diagnostics.Distinct()
                                                     .WhereAsArray(d => _codeFixProvider.IncludeDiagnosticDuringFixAll(d, document, model, fixAllContext.CodeActionEquivalenceKey, fixAllContext.CancellationToken))
                                                     .Sort((d1, d2) => d1.Location.SourceSpan.Start - d2.Location.SourceSpan.Start);

                // PERF: Do not invoke FixAllAsync on the code fix provider if there are no diagnostics to be fixed.
                if (filteredDiagnostics.Length == 0)
                {
                    return document;
                }

                return await _codeFixProvider.FixAllAsync(document, filteredDiagnostics, fixAllContext.CancellationToken).ConfigureAwait(false);
            }
        }
    }
}
