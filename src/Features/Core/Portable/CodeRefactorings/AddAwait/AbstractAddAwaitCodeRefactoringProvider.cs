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
    internal abstract class AbstractAddAwaitCodeRefactoringProvider<TInvocationExpressionSyntax> : CodeRefactoringProvider
        where TInvocationExpressionSyntax : SyntaxNode
    {
        protected abstract string GetTitle();
        protected abstract string GetTitleWithConfigureAwait();

        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var (document, textSpan, cancellationToken) = context;

            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();

            var awaitable = await context.TryGetRelevantNodeAsync<TInvocationExpressionSyntax>().ConfigureAwait(false);
            if (awaitable == null || !IsValidAwaitableExpression(awaitable, model, syntaxFacts))
            {
                return;
            }

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindTokenOnLeftOfPosition(textSpan.Start);

            context.RegisterRefactoring(
                new MyCodeAction(
                    GetTitle(),
                    c => AddAwaitAsync(document, awaitable, withConfigureAwait: false, c)));


            context.RegisterRefactoring(
                new MyCodeAction(
                    GetTitleWithConfigureAwait(),
                    c => AddAwaitAsync(document, awaitable, withConfigureAwait: true, c)));
        }

        private bool IsValidAwaitableExpression(SyntaxNode invocation, SemanticModel model, ISyntaxFactsService syntaxFacts)
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

        private async Task<Document> AddAwaitAsync(
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
