// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixesAndRefactorings;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Threading;

namespace Microsoft.CodeAnalysis.UseAutoProperty;

internal abstract partial class AbstractUseAutoPropertyCodeFixProvider<
    TProvider,
    TTypeDeclarationSyntax,
    TPropertyDeclaration,
    TVariableDeclarator,
    TConstructorDeclaration,
    TExpression>
{
    private sealed class UseAutoPropertyFixAllProvider(TProvider provider) : FixAllProvider
    {
#if CODE_STYLE

        public override async Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
        {
            return CodeAction.Create(
                fixAllContext.GetDefaultFixAllTitle(),
                cancellationToken => FixAllAsync(fixAllContext, cancellationToken));
        }

        private async Task<Solution> FixAllAsync(FixAllContext fixAllContext, CancellationToken cancellationToken)
        {
            return await GetUpdatedSolutionAsync(
                provider, fixAllContext, fixAllContext.Solution, cancellationToken).ConfigureAwait(false);
        }

#else

        public override Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
            => DefaultFixAllProviderHelpers.GetFixAsync(
                fixAllContext.GetDefaultFixAllTitle(), fixAllContext, FixAllContextsHelperAsync);

        private async Task<Solution?> FixAllContextsHelperAsync(FixAllContext originalContext, ImmutableArray<FixAllContext> contexts)
        {
            // Very slow approach, but the only way we know how to do this correctly and without colliding edits. We
            // effectively apply each fix one at a time, moving the solution forward each time.  As we process each
            // diagnostic, we attempt to re-recover the field/property it was referring to in the original solution to
            // the current solution.
            //
            // Note: we can process each project in parallel.  That's because all changes to a field/prop only impact
            // the project they are in, and nothing beyond that.

            // Add a progress item for each context we need to process.
            originalContext.Progress.AddItems(contexts.Length);

            var documentsIdsAndNewRoots = await ProducerConsumer<(DocumentId documentId, SyntaxNode newRoot)>.RunParallelAsync(
                contexts,
                produceItems: async static (currentContext, callback, args, cancellationToken) =>
                {
                    // Within a single context (a project) get all diagnostics, and then handle each diagnostic, one at
                    // a time, to get the final state of the project.
                    var (originalContext, provider) = args;

                    // Complete a progress item as we finish each project.
                    using var _ = originalContext.Progress.ItemCompletedScope();

                    var originalSolution = originalContext.Solution;

                    var currentSolution = await GetUpdatedSolutionAsync(
                        provider, currentContext, originalSolution, cancellationToken).ConfigureAwait(false);

                    // After we finish this context, report the changed documents to the consumeItems callback to process.
                    // This also lets us release all the forked solution info we created above.
                    foreach (var changedDocumentId in originalSolution.GetChangedDocuments(currentSolution))
                    {
                        var changedDocument = currentSolution.GetRequiredDocument(changedDocumentId);
                        var changedRoot = await changedDocument.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                        callback((changedDocumentId, changedRoot));
                    }
                },
                args: (originalContext, provider),
                originalContext.CancellationToken).ConfigureAwait(false);

            return originalContext.Solution.WithDocumentSyntaxRoots(documentsIdsAndNewRoots);
        }

#endif

        private static async Task<Solution> GetUpdatedSolutionAsync(
            TProvider provider, FixAllContext currentContext, Solution originalSolution, CancellationToken cancellationToken)
        {
            var currentSolution = originalSolution;

            var documentToDiagnostics = await FixAllContextHelper.GetDocumentDiagnosticsToFixAsync(currentContext).ConfigureAwait(false);
            foreach (var (_, diagnostics) in documentToDiagnostics)
            {
                foreach (var diagnostic in diagnostics)
                {
                    currentSolution = await provider.ProcessResultAsync(
                        originalSolution, currentSolution, diagnostic, cancellationToken).ConfigureAwait(false);
                }
            }

            return currentSolution;
        }
    }
}
