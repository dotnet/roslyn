// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.CodeFixes.FixAllOccurrences
{
    /// <summary>
    /// A simple implementation of <see cref="FixAllProvider"/> that takes care of collecting
    /// all the diagnostics and fixes all documents in parallel.  The only functionality a 
    /// subclass needs to provide is how each document will apply all the fixes to all the 
    /// diagnostics in that document.
    /// </summary>
    internal abstract class DocumentBasedFixAllProvider : FixAllProvider
    {
        public sealed override async Task<CodeAction> GetFixAsync(FixAllContext fixAllContext)
        {
            var documentsAndDiagnosticsToFixMap = await fixAllContext.GetDocumentDiagnosticsToFixAsync().ConfigureAwait(false);
            return await GetFixAsync(documentsAndDiagnosticsToFixMap, fixAllContext.State, fixAllContext.CancellationToken).ConfigureAwait(false);
        }

        internal sealed override async Task<CodeAction> GetFixAsync(
            ImmutableDictionary<Document, ImmutableArray<Diagnostic>> documentsAndDiagnosticsToFixMap, 
            FixAllState fixAllState, 
            CancellationToken cancellationToken)
        {
            // Process all documents in parallel.
            var updatedDocumentTasks = documentsAndDiagnosticsToFixMap.Select(
                kvp => FixDocumentAsync(kvp.Key, kvp.Value, cancellationToken));

            await Task.WhenAll(updatedDocumentTasks).ConfigureAwait(false);

            var currentSolution = fixAllState.Solution;
            foreach (var task in updatedDocumentTasks)
            {
                // 'await' the tasks so that if any completed in a cancelled manner then we'll
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

        protected abstract Task<Document> FixDocumentAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken);
    }
}
