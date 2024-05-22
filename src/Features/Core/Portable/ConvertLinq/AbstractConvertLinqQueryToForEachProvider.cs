// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.ConvertLinq;

internal abstract class AbstractConvertLinqQueryToForEachProvider<TQueryExpression, TStatement> : CodeRefactoringProvider
    where TQueryExpression : SyntaxNode
    where TStatement : SyntaxNode
{
    protected abstract string Title { get; }

    protected abstract bool TryConvert(
        TQueryExpression queryExpression,
        SemanticModel semanticModel,
        ISemanticFactsService semanticFacts,
        CancellationToken cancellationToken,
        out DocumentUpdateInfo documentUpdate);

    protected abstract Task<TQueryExpression> FindNodeToRefactorAsync(CodeRefactoringContext context);

    public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        var (document, _, cancellationToken) = context;
        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var queryExpression = await FindNodeToRefactorAsync(context).ConfigureAwait(false);
        if (queryExpression == null)
        {
            return;
        }

        var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var semanticFacts = document.GetRequiredLanguageService<ISemanticFactsService>();
        if (TryConvert(queryExpression, semanticModel, semanticFacts, cancellationToken, out var documentUpdateInfo))
        {
            context.RegisterRefactoring(
                CodeAction.Create(
                    Title,
                    c => Task.FromResult(document.WithSyntaxRoot(documentUpdateInfo.UpdateRoot(root))),
                    Title),
                queryExpression.Span);
        }
    }

    /// <summary>
    /// Handles information about updating the document with the refactoring.
    /// </summary>
    internal sealed class DocumentUpdateInfo(TStatement source, IEnumerable<TStatement> destinations)
    {
        public readonly TStatement Source = source;
        public readonly ImmutableArray<TStatement> Destinations = ImmutableArray.CreateRange(destinations);

        public DocumentUpdateInfo(TStatement source, TStatement destination) : this(source, [destination])
        {
        }

        /// <summary>
        /// Updates the root of the document with the document update.
        /// </summary>
        public SyntaxNode UpdateRoot(SyntaxNode root)
        {
            // There are two overloads of ReplaceNode: one accepts a collection of nodes and another a single node.
            // If we replace a node, e.g. in statement of an if-statement(without block),
            // it cannot replace it with collection even if there is a just 1 element in it.
            if (Destinations.Length == 1)
            {
                return root.ReplaceNode(Source, Destinations[0]);
            }
            else
            {
                return root.ReplaceNode(Source, Destinations);
            }
        }
    }
}
