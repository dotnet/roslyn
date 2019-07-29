// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
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
    internal abstract class AbstractAddAwaitCodeRefactoringProvider<TExpressionSyntax, TInvocationExpressionSyntax> : CodeRefactoringProvider
        where TExpressionSyntax : SyntaxNode
        where TInvocationExpressionSyntax : TExpressionSyntax
    {
        protected abstract string GetTitle();
        protected abstract string GetTitleWithConfigureAwait();

        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, textSpan, cancellationToken) = context;
            if (!textSpan.IsEmpty)
            {
                return;
            }

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindTokenOnLeftOfPosition(textSpan.Start);

            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var awaitable = GetAwaitableExpression(token, model, syntaxFacts);
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

        private TExpressionSyntax GetAwaitableExpression(SyntaxToken token, SemanticModel model, ISyntaxFactsService syntaxFacts)
        {
            var invocation = token.GetAncestor<TInvocationExpressionSyntax>();
            if (invocation is null)
            {
                return null;
            }

            if (syntaxFacts.IsExpressionOfInvocationExpression(invocation.Parent))
            {
                // Do not offer fix on `MethodAsync()$$.ConfigureAwait()`
                // Do offer fix on `MethodAsync()$$.Invalid()`
                if (!model.GetTypeInfo(invocation.Parent.Parent).Type.IsErrorType())
                {
                    return null;
                }
            }

            if (syntaxFacts.IsExpressionOfAwaitExpression(invocation))
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
            var syntaxGenerator = SyntaxGenerator.GetGenerator(document);
            SyntaxNode withoutTrivia = invocation.WithoutTrivia();
            if (withConfigureAwait)
            {
                withoutTrivia = syntaxGenerator.InvocationExpression(
                    syntaxGenerator.MemberAccessExpression(withoutTrivia, "ConfigureAwait"),
                    syntaxGenerator.FalseLiteralExpression());
            }

            var awaitExpression = syntaxGenerator
                .AddParentheses(syntaxGenerator.AwaitExpression(withoutTrivia))
                .WithTriviaFrom(invocation);

            return await document.ReplaceNodeAsync(invocation, awaitExpression, cancellationToken).ConfigureAwait(false); ;
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
