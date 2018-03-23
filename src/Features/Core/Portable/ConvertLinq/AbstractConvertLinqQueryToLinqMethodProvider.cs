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
    internal abstract class AbstractConvertLinqQueryToLinqMethodProvider<TQueryExpression> : CodeRefactoringProvider
        where TQueryExpression : SyntaxNode
    {
        protected abstract string Title { get; }

        protected abstract bool TryConvert(
            TQueryExpression queryExpression,
            SemanticModel semanticModel,
            CancellationToken cancellationToken,
            out DocumentUpdateInfo documentUpdate);

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var cancellationToken = context.CancellationToken;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var queryExpression = root.FindNode(context.Span) as TQueryExpression;
            if (queryExpression == null || queryExpression.ContainsDiagnostics)
            {
                return;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            if (TryConvert(queryExpression, semanticModel, cancellationToken, out DocumentUpdateInfo documentUpdateInfo))
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
            private readonly SyntaxNode _source;
            private readonly ImmutableArray<SyntaxNode> _destinations;

            public DocumentUpdateInfo(SyntaxNode source, SyntaxNode destination) : this(source, new[] { destination })
            {
            }

            public DocumentUpdateInfo(SyntaxNode source, IEnumerable<SyntaxNode> destinations)
            {
                _source = source;
                _destinations = ImmutableArray.CreateRange(destinations);
            }

            /// <summary>
            /// Updates the root of the docuemtn with the document update.
            /// </summary>
            public SyntaxNode UpdateRoot(SyntaxNode root)
            {
                return root.ReplaceNode(_source, _destinations.Select(node => node.WithAdditionalAnnotations(Simplifier.Annotation)));
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
