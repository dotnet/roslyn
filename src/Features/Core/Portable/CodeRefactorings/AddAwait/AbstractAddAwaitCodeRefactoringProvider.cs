// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CodeRefactorings.AddAwait
{
    /// <summary>
    /// Refactor:
    ///     var x = GetAsync();
    ///
    /// Into:
    ///     var x = await GetAsync();
    ///
    /// Or:
    ///     var x = await GetAsync().ConfigureAwait(false);
    /// </summary>
    internal abstract class AbstractAddAwaitCodeRefactoringProvider<TExpressionSyntax> : CodeRefactoringProvider
        where TExpressionSyntax : SyntaxNode
    {
        protected abstract string GetTitle();
        protected abstract string GetTitleWithConfigureAwait();

        protected abstract bool IsInAsyncContext(SyntaxNode node);

        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, span, cancellationToken) = context;

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var node = root.FindNode(span);
            if (!IsInAsyncContext(node))
                return;

            var model = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

            var expressions = await context.GetRelevantNodesAsync<TExpressionSyntax>().ConfigureAwait(false);
            for (var i = expressions.Length - 1; i >= 0; i--)
            {
                var expression = expressions[i];
                if (IsValidAwaitableExpression(model, syntaxFacts, expression, cancellationToken))
                {
                    var title = GetTitle();
                    context.RegisterRefactoring(
                        CodeAction.Create(
                            title,
                            c => AddAwaitAsync(document, expression, withConfigureAwait: false, c),
                            title),
                        expression.Span);

                    var titleWithConfigureAwait = GetTitleWithConfigureAwait();
                    context.RegisterRefactoring(
                        CodeAction.Create(
                            titleWithConfigureAwait,
                            c => AddAwaitAsync(document, expression, withConfigureAwait: true, c),
                            titleWithConfigureAwait),
                        expression.Span);
                }
            }
        }

        private static bool IsValidAwaitableExpression(
            SemanticModel model, ISyntaxFactsService syntaxFacts, SyntaxNode node, CancellationToken cancellationToken)
        {
            if (syntaxFacts.IsExpressionOfInvocationExpression(node.Parent))
            {
                // Do not offer fix on `MethodAsync()$$.ConfigureAwait()`
                // Do offer fix on `MethodAsync()$$.Invalid()`
                if (!model.GetTypeInfo(node.GetRequiredParent().GetRequiredParent(), cancellationToken).Type.IsErrorType())
                    return false;
            }

            if (syntaxFacts.IsExpressionOfAwaitExpression(node))
                return false;

            // if we're on an actual type symbol itself (like literally `Task`) we don't want to offer to add await.
            // we only want to add for actual expressions whose type is awaitable, not on the awaitable type itself.
            var symbol = model.GetSymbolInfo(node, cancellationToken).GetAnySymbol();
            if (symbol is ITypeSymbol)
                return false;

            var type = model.GetTypeInfo(node, cancellationToken).Type;
            return type?.IsAwaitableNonDynamic(model, node.SpanStart) == true;
        }

        private static Task<Document> AddAwaitAsync(
            Document document,
            TExpressionSyntax expression,
            bool withConfigureAwait,
            CancellationToken cancellationToken)
        {
            var generator = SyntaxGenerator.GetGenerator(document);
            var withoutTrivia = expression.WithoutTrivia();
            withoutTrivia = (TExpressionSyntax)generator.AddParentheses(withoutTrivia);
            if (withConfigureAwait)
            {
                withoutTrivia = (TExpressionSyntax)generator.InvocationExpression(
                    generator.MemberAccessExpression(withoutTrivia, nameof(Task.ConfigureAwait)),
                    generator.FalseLiteralExpression());
            }

            var awaitExpression = generator
                .AddParentheses(generator.AwaitExpression(withoutTrivia))
                .WithTriviaFrom(expression);

            return document.ReplaceNodeAsync(expression, awaitExpression, cancellationToken);
        }
    }
}
