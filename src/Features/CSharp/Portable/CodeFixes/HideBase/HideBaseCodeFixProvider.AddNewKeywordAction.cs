// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using System.Threading;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.HideBase
{
    internal partial class HideBaseCodeFixProvider
    {
        private class AddNewKeywordAction : CodeActions.CodeAction
        {
            private Document _document;
            private SyntaxNode _node;

            public override string Title
            {
                get
                {
                    return CSharpFeaturesResources.HideBase;
                }
            }

            public AddNewKeywordAction(Document document, SyntaxNode node)
            {
                _document = document;
                _node = node;
            }

            protected override async Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                var root = await _document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                var newNode = GetNewNode(_document, _node, cancellationToken);
                var newRoot = root.ReplaceNode(_node, newNode);

                return _document.WithSyntaxRoot(newRoot);
            }

            private SyntaxNode GetNewNode(Document document, SyntaxNode node, CancellationToken cancellationToken)
            {
                SyntaxNode newNode = null;

                var propertyStatement = node as PropertyDeclarationSyntax;
                if (propertyStatement != null)
                {
                    newNode = propertyStatement.AddModifiers(SyntaxFactory.Token(SyntaxKind.NewKeyword)) as SyntaxNode;
                }

                var methodStatement = node as MethodDeclarationSyntax;
                if (methodStatement != null)
                {
                    newNode = methodStatement.AddModifiers(SyntaxFactory.Token(SyntaxKind.NewKeyword));
                }

                var fieldDeclaration = node as FieldDeclarationSyntax;
                if (fieldDeclaration != null)
                {
                    newNode = fieldDeclaration.AddModifiers(SyntaxFactory.Token(SyntaxKind.NewKeyword));
                }

                //Make sure we preserve any trivia from the original node
                newNode = newNode.WithTriviaFrom(node);

                return newNode.WithAdditionalAnnotations(Formatter.Annotation);
            }
        }
    }
}
