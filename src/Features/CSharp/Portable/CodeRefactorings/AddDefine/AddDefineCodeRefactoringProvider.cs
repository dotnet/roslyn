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
    internal class AddDefineCodeRefactoringProvider : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var textSpan = context.Span;
            var cancellationToken = context.CancellationToken;

            if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
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
            if (structure.Kind() != SyntaxKind.IfDirectiveTrivia &&
                structure.Kind() != SyntaxKind.ElifDirectiveTrivia)
            {
                return;
            }

            var conditionalPragma = (ConditionalDirectiveTriviaSyntax)structure;
            if (conditionalPragma.ConditionValue ||
                conditionalPragma.Condition.Kind() != SyntaxKind.IdentifierName)
            {
                return;
            }

            var label = ((IdentifierNameSyntax)conditionalPragma.Condition).Identifier;
            context.RegisterRefactoring(
                new AddDefineCodeAction(
                    string.Format(CSharpFeaturesResources.Add_define_0, label.ValueText),
                    c => AddDefineAsync(document, label, c)));
        }

        private async Task<Document> AddDefineAsync(Document document, SyntaxToken identifier,
            CancellationToken cancellationToken)
        {
            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var root = tree.GetRoot(cancellationToken);
            var firstNode = root.ChildNodes().FirstOrDefault();

            var define = SyntaxFactory.Trivia(SyntaxFactory.DefineDirectiveTrivia(
                identifier
                    .WithLeadingTrivia(SyntaxFactory.Whitespace(" "))
                    .WithTrailingTrivia(SyntaxFactory.EndOfLine("\r\n")),
                isActive: false));
            var newLeadingTrivia = new SyntaxTriviaList();
            newLeadingTrivia = newLeadingTrivia.AddRange(firstNode.GetLeadingTrivia());
            newLeadingTrivia = newLeadingTrivia.Add(define);

            var result = root.ReplaceNode(firstNode, firstNode.WithLeadingTrivia(newLeadingTrivia));
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
