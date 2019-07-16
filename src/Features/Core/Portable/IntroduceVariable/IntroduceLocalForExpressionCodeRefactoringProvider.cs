// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.IntroduceVariable
{
    internal abstract class AbstractIntroduceLocalForExpressionCodeRefactoringProvider<
        TStatementSyntax,
        TExpressionStatementSyntax,
        TLocalDeclarationStatementSyntax> : CodeRefactoringProvider
        where TStatementSyntax : SyntaxNode
        where TExpressionStatementSyntax : TStatementSyntax
        where TLocalDeclarationStatementSyntax : TStatementSyntax
    {
        protected abstract bool IsValid(TExpressionStatementSyntax expressionStatement, TextSpan span);
        protected abstract Task<TLocalDeclarationStatementSyntax> CreateLocalDeclarationAsync(Document document, TExpressionStatementSyntax expressionStatement, CancellationToken cancellationToken);

        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var span = context.Span;
            var cancellationToken = context.CancellationToken;

            var expressionStatement = await GetExpressionStatementAsync(document, span, cancellationToken).ConfigureAwait(false);
            if (expressionStatement == null)
            {
                return;
            }

            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var expression = syntaxFacts.GetExpressionOfExpressionStatement(expressionStatement);

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var type = semanticModel.GetTypeInfo(expression).Type;
            if (type == null ||
                type.SpecialType == SpecialType.System_Void)
            {
                return;
            }

            var singleLineExpression = syntaxFacts.ConvertToSingleLine(expression);
            var nodeString = singleLineExpression.ToString();

            context.RegisterRefactoring(new MyCodeAction(
                string.Format(FeaturesResources.Introduce_local_for_0, nodeString),
                c => IntroduceLocalAsync(document, span, c)));
        }

        protected async Task<TExpressionStatementSyntax> GetExpressionStatementAsync(Document document, TextSpan span, CancellationToken cancellationToken)
        {
            var helpers = document.GetLanguageService<IRefactoringHelpersService>();
            var expressionStatement = await helpers.TryGetSelectedNodeAsync<TExpressionStatementSyntax>(document, span, cancellationToken).ConfigureAwait(false);
            return expressionStatement != null && IsValid(expressionStatement, span)
                ? expressionStatement
                : null;
        }

        private async Task<Document> IntroduceLocalAsync(Document document, TextSpan span, CancellationToken cancellationToken)
        {
            var expressionStatement = await GetExpressionStatementAsync(document, span, cancellationToken).ConfigureAwait(false);
            var localDeclaration = await CreateLocalDeclarationAsync(document, expressionStatement, cancellationToken).ConfigureAwait(false);

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = root.ReplaceNode(expressionStatement, localDeclaration);

            return document.WithSyntaxRoot(newRoot);
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
