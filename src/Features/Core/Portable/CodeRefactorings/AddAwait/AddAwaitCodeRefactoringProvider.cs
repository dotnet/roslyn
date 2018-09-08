// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

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
    internal abstract class AddAwaitCodeRefactoringProvider<TExpressionSyntax, TInvocationExpressionSyntax> : CodeRefactoringProvider
        where TExpressionSyntax : SyntaxNode
        where TInvocationExpressionSyntax : TExpressionSyntax
    {
        protected abstract string GetTitle();
        protected abstract string GetTitleWithConfigureAwait();
        protected abstract bool IsAlreadyAwaited(TInvocationExpressionSyntax invocation);

        /// <summary>
        /// Add `.ConfigureAwait(false)`
        /// </summary>
        protected abstract TExpressionSyntax WithConfigureAwait(TExpressionSyntax expression);

        /// <summary>
        /// Add `await` and trivia
        /// </summary>
        protected abstract TExpressionSyntax WithAwait(TExpressionSyntax expression, TExpressionSyntax originalExpression);

        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var textSpan = context.Span;
            var cancellationToken = context.CancellationToken;

            if (!textSpan.IsEmpty)
            {
                return;
            }

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindTokenOnLeftOfPosition(textSpan.Start);

            var model = await document.GetSemanticModelAsync(cancellationToken);
            var awaitable = GetAwaitableExpression(textSpan, token, model, cancellationToken);
            if (awaitable == null)
            {
                return;
            }

            if (awaitable.OverlapsHiddenPosition(cancellationToken))
            {
                return;
            }

            context.RegisterRefactoring(
                new MyCodeAction(
                    GetTitle(),
                    c => AddAwaitAsync(document, awaitable, withConfigureAwait: false, c)));


            context.RegisterRefactoring(
                new MyCodeAction(
                    GetTitleWithConfigureAwait(),
                    c => AddAwaitAsync(document, awaitable, withConfigureAwait: true, c)));
        }

        private TExpressionSyntax GetAwaitableExpression(TextSpan textSpan, SyntaxToken token, SemanticModel model, CancellationToken cancellationToken)
        {
            var invocation = token.GetAncestor<TInvocationExpressionSyntax>();
            if (invocation is null)
            {
                return null;
            }

            if (IsAlreadyAwaited(invocation))
            {
                return null;
            }

            var type = model.GetTypeInfo(invocation).Type;
            if (type?.IsAwaitableNonDynamic(model, token.SpanStart) == true)
            {
                return invocation;
            }

            return null;
        }

        private async Task<Document> AddAwaitAsync(
            Document document,
            TExpressionSyntax invocation,
            bool withConfigureAwait,
            CancellationToken cancellationToken)
        {
            var withoutTrivia = invocation.WithoutTrivia();
            if (withConfigureAwait)
            {
                withoutTrivia = WithConfigureAwait(withoutTrivia);
            }

            var awaitExpression = WithAwait(withoutTrivia, invocation);

            return await document.ReplaceNodeAsync(invocation, awaitExpression, cancellationToken);
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument) :
                base(title, createChangedDocument)
            {
            }
        }
    }
}
