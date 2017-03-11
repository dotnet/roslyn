// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.AddDefine
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.AddDefine), Shared]
    internal partial class AddDefineCodeRefactoringProvider : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var textSpan = context.Span;
            var cancellationToken = context.CancellationToken;

            if (!textSpan.IsEmpty ||
                document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
            {
                return;
            }

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var trivia = root.FindTrivia(textSpan.Start);
            if (!trivia.HasStructure)
            {
                return;
            }

            var structure = trivia.GetStructure();
            if (structure.Kind() != SyntaxKind.IfDirectiveTrivia)
            {
                return;
            }

            var ifPragma = (IfDirectiveTriviaSyntax)structure;
            if (ifPragma == null ||
                ifPragma.ConditionValue ||
                ifPragma.Condition.Kind() != SyntaxKind.IdentifierName)
            {
                return;
            }

            context.RegisterRefactoring(
                new AddDefineCodeAction(
                    CSharpFeaturesResources.Add_define_pragma,
                    (c) => AddDefineAsync(document, ifPragma, c)));
        }

        private async Task<Document> AddDefineAsync(Document document, IfDirectiveTriviaSyntax ifPragma,
            CancellationToken cancellationToken)
        {
            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var root = tree.GetRoot();
            var firstUsing = root.ChildNodes().FirstOrDefault();

            var define = SyntaxFactory.Trivia(SyntaxFactory.DefineDirectiveTrivia(
                ((IdentifierNameSyntax)ifPragma.Condition).Identifier
                    .WithLeadingTrivia(SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, " "))
                    .WithTrailingTrivia(SyntaxFactory.SyntaxTrivia(SyntaxKind.EndOfLineTrivia, "\r\n")), false));
            var newLeadingTrivia = new SyntaxTriviaList();
            newLeadingTrivia = newLeadingTrivia.AddRange(firstUsing.GetLeadingTrivia());
            newLeadingTrivia = newLeadingTrivia.Add(define);

            var result = root.ReplaceNode(firstUsing, firstUsing.WithLeadingTrivia(newLeadingTrivia));
            return document.WithSyntaxRoot(result);
        }

        private class AddDefineCodeAction : CodeAction.DocumentChangeAction
        {
            public AddDefineCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument) :
                base(title, createChangedDocument)
            {
            }
        }
    }
}
