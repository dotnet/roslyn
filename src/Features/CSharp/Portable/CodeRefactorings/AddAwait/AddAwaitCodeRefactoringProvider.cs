// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.AddAwait
{
    /// <summary>
    /// This refactoring complements the AddAwait fixer. It allows adding `await` even there is no compiler error to trigger the fixer.
    /// </summary>
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.AddAwait), Shared]
    internal partial class CSharpAddAwaitCodeRefactoringProvider : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
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
                    CSharpFeaturesResources.Add_await,
                    c => AddAwaitAsync(document, awaitable, c)));
        }

        private ExpressionSyntax GetAwaitableExpression(TextSpan textSpan, SyntaxToken token, SemanticModel model, CancellationToken cancellationToken)
        {
            var invocation = token.GetAncestor<InvocationExpressionSyntax>();
            if (invocation is null)
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
            ExpressionSyntax invocation,
            CancellationToken cancellationToken)
        {
            var awaitExpression = SyntaxFactory.AwaitExpression(invocation)
                .Parenthesize()
                .WithTriviaFrom(invocation);
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
