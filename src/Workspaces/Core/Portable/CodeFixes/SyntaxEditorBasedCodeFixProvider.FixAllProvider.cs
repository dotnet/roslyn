// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;

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
            {
                _codeFixProvider = codeFixProvider;
            }

            public sealed override async Task<CodeAction> GetFixAsync(FixAllContext fixAllContext)
            {
                var documentsAndDiagnosticsToFixMap = await fixAllContext.GetDocumentDiagnosticsToFixAsync().ConfigureAwait(false);
                return await GetFixAsync(documentsAndDiagnosticsToFixMap, fixAllContext.State, fixAllContext.CancellationToken).ConfigureAwait(false);
            }

            internal sealed override async Task<CodeAction> GetFixAsync(
                ImmutableDictionary<Document, ImmutableArray<Diagnostic>> documentsAndDiagnosticsToFixMap,
                FixAllState fixAllState, CancellationToken cancellationToken)
            {
                // Process all documents in parallel.
                var updatedDocumentTasks = documentsAndDiagnosticsToFixMap.Select(
                    kvp => FixDocumentAsync(fixAllState, kvp.Key, kvp.Value, cancellationToken));

                await Task.WhenAll(updatedDocumentTasks).ConfigureAwait(false);

                var currentSolution = fixAllState.Solution;
                foreach (var task in updatedDocumentTasks)
                {
                    // 'await' the tasks so that if any completed in a canceled manner then we'll
                    // throw the right exception here.  Calling .Result on the tasks might end up
                    // with AggregateExceptions being thrown instead.
                    var updatedDocument = await task.ConfigureAwait(false);
                    currentSolution = currentSolution.WithDocumentSyntaxRoot(
                        updatedDocument.Id,
                        await updatedDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false));
                }

                var title = fixAllState.GetDefaultFixAllTitle();
                return new CodeAction.SolutionChangeAction(title, _ => Task.FromResult(currentSolution));
            }

            private async Task<Document> FixDocumentAsync(
                FixAllState fixAllState, Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
            {
                // Ensure that diagnostics for this document are always in document location
                // order.  This provides a consistent and deterministic order for fixers
                // that want to update a document.
                // Also ensure that we do not pass in duplicates by invoking Distinct.
                // See https://github.com/dotnet/roslyn/issues/31381, that seems to be causing duplicate diagnostics.
                var filteredDiagnostics = diagnostics.Distinct()
                                                     .WhereAsArray(d => _codeFixProvider.IncludeDiagnosticDuringFixAll(fixAllState, d, cancellationToken))
                                                     .Sort((d1, d2) => d1.Location.SourceSpan.Start - d2.Location.SourceSpan.Start);

                // PERF: Do not invoke FixAllAsync on the code fix provider if there are no diagnostics to be fixed.
                if (filteredDiagnostics.Length == 0)
                {
                    return document;
                }

                return await _codeFixProvider.FixAllAsync(document, filteredDiagnostics, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
