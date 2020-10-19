// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
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

            var (document, _, cancellationToken) = context;

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

        protected async Task<ImmutableArray<TFromExpression>> FilterCastExpressionsOfReferenceTypesAsync(
            ImmutableArray<TFromExpression> fromExpressions,
            Document document,
            CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            using var _ = ArrayBuilder<TFromExpression>.GetInstance(out var builder);
            foreach (var expression in fromExpressions)
            {
                syntaxFacts.GetPartsOfCastExpression(expression, out var typeNode, out var _);
                if (typeNode is not null)
                {
                    var type = semanticModel.GetTypeInfo(typeNode, cancellationToken).Type;
                    if (IsReferenceTypeOrTypeParameter(type))
                    {
                        builder.Add(expression);
                    }
                }
            }

            return builder.ToImmutable();
        }

        private static bool IsReferenceTypeOrTypeParameter(ITypeSymbol? type)
            => type switch
            {
                null => false,
                { Kind: SymbolKind.ErrorType } => false,
                { IsReferenceType: true } => true,
                _ => false,
            };

        protected Task<Document> ConvertAsync(
                Document document,
                TFromExpression fromExpression,
                CancellationToken cancellationToken)
        {
            var converted = ConvertExpression(fromExpression);
            return document.ReplaceNodeAsync(fromExpression, converted, cancellationToken);
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
