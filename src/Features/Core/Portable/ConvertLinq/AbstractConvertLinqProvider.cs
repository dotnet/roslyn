// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;

namespace Microsoft.CodeAnalysis.ConvertLinq
{
    internal abstract class AbstractConvertLinqProvider : CodeRefactoringProvider
    {
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var semanticModel = await document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);

            await CreateAnalyzer(semanticModel, context.CancellationToken)
                .ComputeRefactoringsAsync(context).ConfigureAwait(false);
        }

        protected abstract IAnalyzer CreateAnalyzer(SemanticModel semanticModel, CancellationToken cancellationToken);

        protected interface IAnalyzer
        {
            Task ComputeRefactoringsAsync(CodeRefactoringContext context);
        }

        protected abstract class AnalyzerBase<TSource, TDestination> : IAnalyzer
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

                var sourceNode = FindNodeToRefactor(root, context);
                if (sourceNode == null)
                {
                    return;
                }

                if (sourceNode.ContainsDiagnostics)
                {
                    return;
                }

                if (TryConvert(sourceNode, out var destinationNode) &&
                    Validate(sourceNode, destinationNode))
                {
                    context.RegisterRefactoring(new MyCodeAction(Title, c =>
                        UpdateDocumentAsync(root, document, sourceNode, destinationNode)));
                }
            }

            protected abstract bool TryConvert(TSource source, out TDestination destination);

            protected virtual TSource FindNodeToRefactor(SyntaxNode root, CodeRefactoringContext context) =>
                root.FindNode(context.Span).FirstAncestorOrSelf<TSource>();

            protected virtual bool Validate(TSource source, TDestination destination)
                => true;

            protected abstract string Title { get; }

            private Task<Document> UpdateDocumentAsync(
                SyntaxNode root,
                Document document,
                TSource sourceNode,
                TDestination destinationNode)
            {
                root = root.ReplaceNode(root.FindNode(sourceNode.Span), destinationNode);
                return Task.FromResult(document.WithSyntaxRoot(root));
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
