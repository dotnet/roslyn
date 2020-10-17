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
    internal abstract class AbstractConvertConversionOperatorsRefactoringProvider<TFromExpression> : CodeRefactoringProvider
        where TFromExpression : SyntaxNode
    {
        protected abstract string GetTitle();

        protected abstract SyntaxNode ConvertExpression(TFromExpression fromExpression);

        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var fromExpressions = await context.GetRelevantNodesAsync<TFromExpression>().ConfigureAwait(false);

            if (fromExpressions.IsEmpty)
            {
                return;
            }

            var (document, cancellationToken) = (context.Document, context.CancellationToken);

            fromExpressions = await FilterFromExpressionCandidatesAsync(fromExpressions, document, cancellationToken).ConfigureAwait(false);

            foreach (var node in fromExpressions.Distinct())
            {
                context.RegisterRefactoring(
                    new MyCodeAction(
                        GetTitle(),
                        c => ConvertAsync(document, node, cancellationToken)
                    ), node.Span);
            }
        }

        protected virtual Task<ImmutableArray<TFromExpression>> FilterFromExpressionCandidatesAsync(
            ImmutableArray<TFromExpression> fromExpressions,
            Document document,
            CancellationToken cancellationToken)
            => Task.FromResult(fromExpressions);

        protected async Task<Document> ConvertAsync(
            Document document,
            TFromExpression fromExpression,
            CancellationToken cancellationToken)
        {
            var converted = ConvertExpression(fromExpression);
            return await document.ReplaceNodeAsync(fromExpression, converted, cancellationToken).ConfigureAwait(false);
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
            }
        }
    }
}
