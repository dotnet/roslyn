// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.ConvertLinq
{
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

        protected abstract TQueryExpression FindNodeToRefactor(SyntaxNode root, TextSpan span);

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var cancellationToken = context.CancellationToken;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var queryExpression = FindNodeToRefactor(root, context.Span);
            if (queryExpression == null)
            {
                return;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var semanticFacts = document.GetLanguageService<ISemanticFactsService>();
            if (TryConvert(queryExpression, semanticModel, semanticFacts, cancellationToken, out var documentUpdateInfo))
            {
                context.RegisterRefactoring(
                    new MyCodeAction(
                        Title,
                        c => Task.FromResult(document.WithSyntaxRoot(documentUpdateInfo.UpdateRoot(root)))));
            }
        }

        /// <summary>
        /// Handles information about updating the document with the refactoring.
        /// </summary>
        internal sealed class DocumentUpdateInfo
        {
            public readonly TStatement Source;
            public readonly ImmutableArray<TStatement> Destinations;

            public DocumentUpdateInfo(TStatement source, TStatement destination) : this(source, new[] { destination })
            {
            }

            public DocumentUpdateInfo(TStatement source, IEnumerable<TStatement> destinations)
            {
                Source = source;
                Destinations = ImmutableArray.CreateRange(destinations);
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

        protected sealed class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
            }
        }
    }
}
