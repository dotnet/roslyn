// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Simplification;

namespace Microsoft.CodeAnalysis.ConvertLinq
{
    /// <summary>
    /// This is a base class for LINQ related conversions between LINQ queries, LINQ methods and foreach loops.
    /// </summary>
    internal abstract class AbstractConvertLinqProvider : CodeRefactoringProvider
    {
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var semanticModel = await document.GetSemanticModelAsync().ConfigureAwait(false);

            await CreateAnalyzer(semanticModel, context.CancellationToken)
                .ComputeRefactoringsAsync(context).ConfigureAwait(false);
        }

        protected abstract IAnalyzer CreateAnalyzer(SemanticModel semanticModel, CancellationToken cancellationToken);

        protected interface IAnalyzer
        {
            Task ComputeRefactoringsAsync(CodeRefactoringContext context);
        }

        protected abstract class AnalyzerBase<TRefactor, TSource, TDestination> : IAnalyzer
            where TRefactor : SyntaxNode
            where TSource : SyntaxNode
            where TDestination : SyntaxNode
        {
            protected readonly SemanticModel _semanticModel;
            protected readonly CancellationToken _cancellationToken;

            public AnalyzerBase(SemanticModel semanticModel, CancellationToken cancellationToken)
            {
                _semanticModel = semanticModel;
                _cancellationToken = cancellationToken;
            }

            public async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
            {
                var document = context.Document;
                var cancellationToken = context.CancellationToken;
                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                var nodeToRefactor = FindNodeToRefactor(root, context);
                if (nodeToRefactor == null)
                {
                    return;
                }

                if (nodeToRefactor.ContainsDiagnostics)
                {
                    return;
                }

                if (TryConvert(nodeToRefactor, out var result))
                {
                    context.RegisterRefactoring(new MyCodeAction(Title, c => UpdateDocumentAsync(root, document, result)));
                }
            }

            protected abstract bool TryConvert(TRefactor refactor, out DocumentUpdate documentUpdate);

            protected virtual TRefactor FindNodeToRefactor(SyntaxNode root, CodeRefactoringContext context) =>
                root.FindNode(context.Span) as TRefactor ?? default;

            protected abstract string Title { get; }

            private Task<Document> UpdateDocumentAsync(
                SyntaxNode root,
                Document document,
                DocumentUpdate documentUpdate)
            {
                root = documentUpdate.UpdateRoot(root);
                return Task.FromResult(document.WithSyntaxRoot(root));
            }
        }

        protected sealed class DocumentUpdate
        {
            private ImmutableArray<(SyntaxNode source, ImmutableArray<SyntaxNode> destinations)> _updates;

            public DocumentUpdate(SyntaxNode source, SyntaxNode destination)
                => _updates = ImmutableArray.Create((source, ImmutableArray.Create(destination)));

            public DocumentUpdate(SyntaxNode source, IEnumerable<SyntaxNode> destination)
                => _updates = ImmutableArray.Create((source, ImmutableArray.CreateRange(destination)));

            public SyntaxNode UpdateRoot(SyntaxNode root)
            {
                foreach (var updateItem in _updates)
                {
                    if (updateItem.destinations.Any())
                    {
                        root = root.ReplaceNode(updateItem.source, updateItem.destinations.Select(node => node.WithAdditionalAnnotations(Simplifier.Annotation)));
                    }
                    else
                    {
                        root = root.RemoveNode(updateItem.source, SyntaxRemoveOptions.AddElasticMarker);
                    }
                }

                return root;
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
