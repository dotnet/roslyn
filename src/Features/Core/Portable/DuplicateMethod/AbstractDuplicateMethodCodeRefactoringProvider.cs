// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.CodeRefactorings;
using System.Text.RegularExpressions;

namespace Microsoft.CodeAnalysis.DuplicateMethod
{
    internal abstract class AbstractDuplicateMethodCodeRefactoringProvider<TMethodDeclarationSyntax> : CodeRefactoringProvider
        where TMethodDeclarationSyntax : SyntaxNode
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            if (document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles)
            {
                return;
            }

            var cancellationToken = context.CancellationToken;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var method = root.FindNode(context.Span).GetAncestorOrThis<TMethodDeclarationSyntax>();
            if (method == null)
            {
                return;
            }

            if (method.ContainsDiagnostics)
            {
                return;
            }

            var identifier = GetIdentifier(method);
            if (context.Span.Length > 0 &&
                context.Span != identifier.Span)
            {
                return;
            }

            context.RegisterRefactoring(new MyCodeAction("TODO",
                c => CloneMethodAsync(method, document, root, identifier)));
        }

        protected abstract SyntaxToken GetIdentifier(TMethodDeclarationSyntax method);
        protected abstract TMethodDeclarationSyntax WithName(TMethodDeclarationSyntax method, string name);

        private Task<Document> CloneMethodAsync(
            TMethodDeclarationSyntax method, Document document, SyntaxNode root, SyntaxToken identifier)
        {
            var name = GenerateName(identifier.ValueText);
            var generator = SyntaxGenerator.GetGenerator(document);
            var currentDeclaration = method.Parent;
            var updatedDeclaration = generator.InsertNodesAfter(currentDeclaration, method, new[] { WithName(method, name) });
            var updatedRoot = root.ReplaceNode(currentDeclaration, updatedDeclaration);
            return Task.FromResult(document.WithSyntaxRoot(updatedRoot));
        }

        private static string GenerateName(string name)
        {
            // Increase the trailing number, if any
            var match = Regex.Match(name, @"0*\d+$");
            if (match.Success)
            {
                var number = match.Value;
                var newNumber = (int.Parse(number) + 1).ToString("D" + number.Length);
                return name.Replace(number, newNumber);
            }
            else
            {
                return name + "01";
            }
        }

        private sealed class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument)
            {
            }
        }
    }
}
