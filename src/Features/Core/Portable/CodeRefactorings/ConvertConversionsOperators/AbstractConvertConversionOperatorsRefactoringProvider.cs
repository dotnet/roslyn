// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeRefactorings.ConvertConversionOperators
{
    /// <summary>
    /// Refactor:
    ///     var o = (object)1;
    ///
    /// Into:
    ///     var o = 1 as object;
    ///
    /// Or:
    ///     visa versa
    /// </summary>
    internal abstract class AbstractConvertConversionOperatorsRefactoringProvider<TCastExpressionSyntax, TAsExpressionSyntax> : CodeRefactoringProvider
        where TCastExpressionSyntax : SyntaxNode
        where TAsExpressionSyntax : SyntaxNode
    {
        protected abstract string GetTitle();
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {

            var castExpressions = await context.GetRelevantNodesAsync<TCastExpressionSyntax>().ConfigureAwait(false);
            var asExpressions = await context.GetRelevantNodesAsync<TAsExpressionSyntax>().ConfigureAwait(false);

            castExpressions = FilterCastExpressionCandidates(castExpressions);
            asExpressions = FilterAsExpressionCandidates(asExpressions);

            if (castExpressions.IsEmpty && asExpressions.IsEmpty)
            {
                return;
            }

            var nodesToRegister = castExpressions.Cast<SyntaxNode>().Union(asExpressions).ToImmutableArray();

            foreach (var node in nodesToRegister)
            {
                Func<CancellationToken, Task<Document>> createChangedDocument = node switch
                {
                    TCastExpressionSyntax castExpression => cancellationToken => ConvertFromCastToAsAsync(context.Document, castExpression, cancellationToken),
                    TAsExpressionSyntax asExpression => cancellationToken => ConvertFromAsToCastAsync(context.Document, asExpression, cancellationToken),
                    _ => throw new InvalidOperationException("Unsupported node type"),
                };
                context.RegisterRefactoring(
                    new MyCodeAction(
                        GetTitle(),
                        createChangedDocument
                    ), node.Span);
            }
        }

        protected virtual ImmutableArray<TCastExpressionSyntax> FilterCastExpressionCandidates(ImmutableArray<TCastExpressionSyntax> castExpressions)
            => castExpressions;

        protected virtual ImmutableArray<TAsExpressionSyntax> FilterAsExpressionCandidates(ImmutableArray<TAsExpressionSyntax> asExpressions)
            => asExpressions;

        protected abstract Task<Document> ConvertFromCastToAsAsync(
            Document document,
            TCastExpressionSyntax castExpression,
            CancellationToken cancellationToken);

        protected abstract Task<Document> ConvertFromAsToCastAsync(
            Document document,
            TAsExpressionSyntax asExpression,
            CancellationToken cancellationToken);

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
            }
        }
    }
}
