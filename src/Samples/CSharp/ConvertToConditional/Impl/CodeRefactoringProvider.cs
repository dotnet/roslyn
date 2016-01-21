// *********************************************************
//
// Copyright © Microsoft Corporation
//
// Licensed under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in
// compliance with the License. You may obtain a copy of
// the License at
//
// http://www.apache.org/licenses/LICENSE-2.0 
//
// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES
// OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED,
// INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES
// OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY OR NON-INFRINGEMENT.
//
// See the Apache 2 License for the specific language
// governing permissions and limitations under the License.
//
// *********************************************************

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace ConvertToConditionalCS
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = "ConvertToConditionalCS"), Shared]
    internal class ConvertToConditionalCodeRefactoringProvider : CodeRefactoringProvider
    {
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var textSpan = context.Span;
            var cancellationToken = context.CancellationToken;

            var root = (SyntaxNode)await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(textSpan.Start);

            // Only trigger if the text span is within the 'if' keyword token of an if-else statement.

            if (token.Kind() != SyntaxKind.IfKeyword ||
                !token.Span.IntersectsWith(textSpan.Start) ||
                !token.Span.IntersectsWith(textSpan.End))
            {
                return;
            }

            var ifStatement = token.Parent as IfStatementSyntax;
            if (ifStatement == null || ifStatement.Else == null)
            {
                return;
            }

            var semanticModel = (SemanticModel)await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            ReturnStatementSyntax returnStatement;
            if (ReturnConditionalAnalyzer.TryGetNewReturnStatement(ifStatement, semanticModel, out returnStatement))
            {
                var action = new ConvertToConditionalCodeAction("Convert to conditional expression", (c) => Task.FromResult(ConvertToConditional(document, semanticModel, ifStatement, returnStatement, c)));
                context.RegisterRefactoring(action);
            }
        }

        private Document ConvertToConditional(Document document, SemanticModel semanticModel, IfStatementSyntax ifStatement, StatementSyntax replacementStatement, CancellationToken cancellationToken)
        {
            var oldRoot = semanticModel.SyntaxTree.GetRoot();
            var newRoot = oldRoot.ReplaceNode(
                oldNode: ifStatement,
                newNode: replacementStatement.WithAdditionalAnnotations(Formatter.Annotation));

            return document.WithSyntaxRoot(newRoot);
        }

        private class ConvertToConditionalCodeAction : CodeAction
        {
            private readonly string title;
            private readonly Func<CancellationToken, Task<Document>> createChangedDocument;

            public ConvertToConditionalCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
            {
                this.title = title;
                this.createChangedDocument = createChangedDocument;
            }

            public override string Title { get { return title; } }

            protected override Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                return this.createChangedDocument(cancellationToken);
            }
        }
    }
}