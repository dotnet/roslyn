﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
    internal abstract class AbstractAddAwaitCodeRefactoringProvider<TInvocationExpressionSyntax> : CodeRefactoringProvider
        where TInvocationExpressionSyntax : SyntaxNode
    {
        protected abstract string GetTitle();
        protected abstract string GetTitleWithConfigureAwait();

        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, _, cancellationToken) = context;

            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

            var awaitable = await context.TryGetRelevantNodeAsync<TInvocationExpressionSyntax>().ConfigureAwait(false);
            if (awaitable == null || !IsValidAwaitableExpression(awaitable, model, syntaxFacts))
            {
                return;
            }

            context.RegisterRefactoring(
                new MyCodeAction(
                    GetTitle(),
                    c => AddAwaitAsync(document, awaitable, withConfigureAwait: false, c)),
                awaitable.Span);

            context.RegisterRefactoring(
                new MyCodeAction(
                    GetTitleWithConfigureAwait(),
                    c => AddAwaitAsync(document, awaitable, withConfigureAwait: true, c)),
                awaitable.Span);
        }

        private static bool IsValidAwaitableExpression(SyntaxNode invocation, SemanticModel model, ISyntaxFactsService syntaxFacts)
        {
            if (syntaxFacts.IsExpressionOfInvocationExpression(invocation.Parent))
            {
                // Do not offer fix on `MethodAsync()$$.ConfigureAwait()`
                // Do offer fix on `MethodAsync()$$.Invalid()`
                if (!model.GetTypeInfo(invocation.Parent.Parent).Type.IsErrorType())
                {
                    return false;
                }
            }

            if (syntaxFacts.IsExpressionOfAwaitExpression(invocation))
            {
                return false;
            }

            var type = model.GetTypeInfo(invocation).Type;
            if (type?.IsAwaitableNonDynamic(model, invocation.SpanStart) == true)
            {
                return true;
            }

            return false;
        }

        private static async Task<Document> AddAwaitAsync(
            Document document,
            TInvocationExpressionSyntax invocation,
            bool withConfigureAwait,
            CancellationToken cancellationToken)
        {
            var syntaxGenerator = SyntaxGenerator.GetGenerator(document);
            SyntaxNode withoutTrivia = invocation.WithoutTrivia();
            if (withConfigureAwait)
            {
                withoutTrivia = syntaxGenerator.InvocationExpression(
                    syntaxGenerator.MemberAccessExpression(withoutTrivia, nameof(Task.ConfigureAwait)),
                    syntaxGenerator.FalseLiteralExpression());
            }

            var awaitExpression = syntaxGenerator
                .AddParentheses(syntaxGenerator.AwaitExpression(withoutTrivia))
                .WithTriviaFrom(invocation);

            return await document.ReplaceNodeAsync(invocation, awaitExpression, cancellationToken).ConfigureAwait(false);
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
