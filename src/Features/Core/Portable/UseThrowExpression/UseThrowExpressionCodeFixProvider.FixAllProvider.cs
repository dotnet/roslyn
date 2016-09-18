// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;

namespace Microsoft.CodeAnalysis.UseThrowExpression
{
    internal partial class UseThrowExpressionCodeFixProvider
    {
        /// <summary>
        /// A specialized <see cref="FixAllProvider"/> for this CodeFixProvider.
        /// SimplifyNullCheck needs a specialized <see cref="FixAllProvider"/> because
        /// the normal <see cref="BatchFixAllProvider"/> is insufficient for our needs.
        /// Specifically, when doing a bulk fix-all, it's very common for many edits to
        /// be near each other. The simplest example of this is just the following code:
        /// 
        /// <code>
        ///     if (s == null) throw ...
        ///     if (t == null) throw ...
        ///     _s = s;
        ///     _t = t;
        /// </code>
        /// 
        /// If we use the normal batch-fixer then the underlying merge algorithm gets
        /// throw off by hte sequence of edits.  specifically, the removal of 
        /// "if (s == null) throw ..." actually gets seen by it as the removal of 
        /// "(s == null) throw ... \r\n if".  That text change then intersects the
        /// removal of "if (t == null) throw ...".  because of the intersection, one
        /// of the edits is ignored.
        /// 
        /// This FixAllProvider avoids this entirely by not doing any textual merging.
        /// Instead, we just take all the fixes to apply in the document, as we use
        /// the core <see cref="UseThrowExpressionCodeFixProvider.FixAllAsync"/> to do
        /// all the editing at once on the SyntaxTree.  Because we're doing real tree
        /// edits with actual nodes, there is no issue with anything getting messed up.
        /// </summary>
        private class UseThrowExpressionFixAllProvider : BatchFixAllProvider
        {
            private readonly UseThrowExpressionCodeFixProvider _provider;

            public UseThrowExpressionFixAllProvider(UseThrowExpressionCodeFixProvider provider)
            {
                _provider = provider;
            }

            internal override async Task<CodeAction> GetFixAsync(
                ImmutableDictionary<Document, ImmutableArray<Diagnostic>> documentsAndDiagnosticsToFixMap,
                FixAllState fixAllState,
                CancellationToken cancellationToken)
            {
                // Process all documents in parallel.
                // Defer to the actual SimplifyNullCheckCodeFixProvider to process htis
                // document.  It can process all the diagnostics and apply them properly.
                var updatedDocumentTasks = documentsAndDiagnosticsToFixMap.Select(
                    kvp => FixAllAsync(kvp.Key, kvp.Value, cancellationToken));

                await Task.WhenAll(updatedDocumentTasks).ConfigureAwait(false);

                var currentSolution = fixAllState.Solution;
                foreach (var task in updatedDocumentTasks)
                {
                    var updatedDocument = await task.ConfigureAwait(false);
                    currentSolution = currentSolution.WithDocumentSyntaxRoot(
                        updatedDocument.Id,
                        await updatedDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false));
                }

                var title = GetFixAllTitle(fixAllState);
                return new CodeAction.SolutionChangeAction(title, _ => Task.FromResult(currentSolution));
            }

            private Task<Document> FixAllAsync(
                Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
            {
                var filteredDiagnostics = diagnostics.WhereAsArray(
                    d => !d.Descriptor.CustomTags.Contains(WellKnownDiagnosticTags.Unnecessary));
                return _provider.FixAllAsync(document, filteredDiagnostics, cancellationToken);
            }
        }
    }
}