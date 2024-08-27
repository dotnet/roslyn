// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.RelatedDocuments;

internal abstract class AbstractRelatedDocumentsService<
    TExpressionSyntax,
    TNameSyntax> : IRelatedDocumentsService
    where TExpressionSyntax : SyntaxNode
{
    private static class ConcurrentSetPool<T> where T : notnull
    {
        private static readonly ObjectPool<ConcurrentSet<T>> s_pool = new(() => []);

        public static PooledObject<ConcurrentSet<T>> GetInstance(out ConcurrentSet<T> set)
        {
            var obj = s_pool.GetPooledObject();
            set = obj.Object;
            return obj;
        }

        public static void ClearAndFree(ConcurrentSet<T> set)
        {
            set.Clear();
            s_pool.Free(set);
        }
    }

    protected abstract IEnumerable<(TExpressionSyntax expression, SyntaxToken nameToken)> IteratePotentialTypeNodes(SyntaxNode root);

    public async ValueTask GetRelatedDocumentIdsAsync(
        Document document, int position, Func<ImmutableArray<DocumentId>, CancellationToken, ValueTask> callbackAsync, CancellationToken cancellationToken)
    {
        var project = document.Project;
        var client = await RemoteHostClient.TryGetClientAsync(project, cancellationToken).ConfigureAwait(false);
        if (client != null)
        {
            var remoteCallback = new RelatedDocumentsServiceCallback(callbackAsync, cancellationToken);

            var result = await client.TryInvokeAsync<IRemoteRelatedDocumentsService>(
                // We don't need to sync the entire solution (only the project) to ask for the related files for a
                // particular document.
                document.Project,
                (service, solutionChecksum, callbackId, cancellationToken) => service.GetRelatedDocumentIdsAsync(
                    solutionChecksum, document.Id, position, callbackId, cancellationToken),
                remoteCallback,
                cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await GetRelatedDocumentIdsInCurrentProcessAsync(
                document, position, callbackAsync, cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask GetRelatedDocumentIdsInCurrentProcessAsync(
        Document document,
        int position,
        Func<ImmutableArray<DocumentId>, CancellationToken, ValueTask> callbackAsync,
        CancellationToken cancellationToken)
    {
        var solution = document.Project.Solution;

        // Don't need nullable analysis, and we're going to walk a lot of the tree, so speed things up by not doing
        // excess semantic work.
        var semanticModel = await document.GetRequiredNullableDisabledSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        using var _1 = ConcurrentSetPool<DocumentId>.GetInstance(out var seenDocumentIds);
        using var _2 = ConcurrentSetPool<string>.GetInstance(out var seenTypeNames);

        var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
        var syntaxKinds = syntaxFacts.SyntaxKinds;
        var identifierTokenKind = syntaxKinds.IdentifierToken;

        // Bind as much as we can in parallel to get the results as quickly as possible.  We call into the
        // ProducerConsumer overload that will batch up values into arrays, and call into the callback with them.  While
        // that callback is executing, the ProducerConsumer continues to run, computing more results and getting them
        // ready for when that call returns.  This approach ensures that we're not pausing while we're reporting the
        // results to whatever client is calling into us.
        await ProducerConsumer<DocumentId>.RunParallelAsync(
            // Order the nodes by the distance from the requested position.
            IteratePotentialTypeNodes(root).OrderBy(t => t.expression.SpanStart - position),
            produceItems: (tuple, callback, _, cancellationToken) =>
            {
                ProduceItems(
                    tuple.expression, tuple.nameToken, callback, cancellationToken);

                return Task.CompletedTask;
            },
            consumeItems: static async (array, callbackAsync, cancellationToken) =>
            {
                await callbackAsync(array, cancellationToken).ConfigureAwait(false);
            },
            args: callbackAsync,
            cancellationToken).ConfigureAwait(false);

        return;

        void ProduceItems(
            TExpressionSyntax expression,
            SyntaxToken nameToken,
            Action<DocumentId> callback,
            CancellationToken cancellationToken)
        {
            if (nameToken.RawKind != identifierTokenKind)
                return;

            if (nameToken.ValueText == "")
                return;

            // Don't rebind a type name we've already seen.  Note: this is a conservative/inaccurate check.
            // Specifically, there could be different types with the same last name portion (from different
            // namespaces). In that case, we'll miss the one that is further away.  We can revisit this in the
            // future if we think it's necessary.
            if (!seenTypeNames.Add(nameToken.ValueText))
                return;

            var symbol = semanticModel.GetSymbolInfo(expression, cancellationToken).GetAnySymbol();
            if (symbol is not ITypeSymbol)
                return;

            foreach (var syntaxReference in symbol.DeclaringSyntaxReferences)
            {
                var documentId = solution.GetDocument(syntaxReference.SyntaxTree)?.Id;
                if (documentId != null && seenDocumentIds.Add(documentId))
                    callback(documentId);
            }
        }
    }
}
