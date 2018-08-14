// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeRefactorings.InvertIf
{
    internal abstract partial class AbstractInvertIfCodeRefactoringProvider : CodeRefactoringProvider
    {
        protected abstract SyntaxNode GetIfStatement(TextSpan textSpan, SyntaxToken token, CancellationToken cancellationToken);
        protected abstract SyntaxNode GetRootWithInvertIfStatement(Document document, SemanticModel model, SyntaxNode ifStatement, CancellationToken cancellationToken);
        protected abstract ISyntaxFactsService GetSyntaxFactsService();
        protected abstract string GetTitle();

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
            var token = root.FindToken(textSpan.Start);

            var ifStatement = GetIfStatement(textSpan, token, cancellationToken);
            if (ifStatement == null)
            {
                return;
            }

            if (ifStatement.OverlapsHiddenPosition(cancellationToken))
            {
                return;
            }

            context.RegisterRefactoring(
                new MyCodeAction(
                    GetTitle(),
                    c => InvertIfAsync(document, ifStatement, c)));
        }

        private async Task<Document> InvertIfAsync(
            Document document, 
            SyntaxNode ifStatement, 
            CancellationToken cancellationToken)
        {
            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            return document.WithSyntaxRoot(
                // this returns the root because VB requires changing context around if statement
                GetRootWithInvertIfStatement(
                    document, model, ifStatement, cancellationToken));
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
