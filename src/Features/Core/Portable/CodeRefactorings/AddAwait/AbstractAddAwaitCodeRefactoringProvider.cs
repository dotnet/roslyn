﻿// Licensed to the .NET Foundation under one or more agreements.
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
                if (IsValidAwaitableExpression(expression, model, syntaxFacts))
                {
                    context.RegisterRefactoring(
                        new MyCodeAction(
                            GetTitle(),
                            c => AddAwaitAsync(document, expression, withConfigureAwait: false, c)),
                        expression.Span);

                    context.RegisterRefactoring(
                        new MyCodeAction(
                            GetTitleWithConfigureAwait(),
                            c => AddAwaitAsync(document, expression, withConfigureAwait: true, c)),
                        expression.Span);
                }
            }
        }

        private static bool IsValidAwaitableExpression(SyntaxNode invocation, SemanticModel model, ISyntaxFactsService syntaxFacts)
        {
            if (syntaxFacts.IsExpressionOfInvocationExpression(invocation.Parent))
            {
                // Do not offer fix on `MethodAsync()$$.ConfigureAwait()`
                // Do offer fix on `MethodAsync()$$.Invalid()`
                if (!model.GetTypeInfo(invocation.GetRequiredParent().GetRequiredParent()).Type.IsErrorType())
                    return false;
            }

            if (syntaxFacts.IsExpressionOfAwaitExpression(invocation))
                return false;

            var type = model.GetTypeInfo(invocation).Type;
            if (type?.IsAwaitableNonDynamic(model, invocation.SpanStart) == true)
                return true;

            return false;
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

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
            }
        }
    }
}
