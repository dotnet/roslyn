// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ConvertToAutoPropertyCS
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = "ConvertToAutoPropertyCS"), Shared]
    internal class ConvertToAutoPropertyCodeRefactoringProvider : CodeRefactoringProvider
    {
        public sealed override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var textSpan = context.Span;
            var cancellationToken = context.CancellationToken;

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(textSpan.Start);
            if (token.Parent == null)
            {
                return;
            }

            var propertyDeclaration = token.Parent.FirstAncestorOrSelf<PropertyDeclarationSyntax>();

            // Refactor only properties with both a getter and a setter.
            if (propertyDeclaration == null ||
                !HasBothAccessors(propertyDeclaration) ||
                !propertyDeclaration.Identifier.Span.IntersectsWith(textSpan.Start))
            {
                return;
            }

            context.RegisterRefactoring(
                new ConvertToAutoPropertyCodeAction("Convert to auto property",
                                                    (c) => ConvertToAutoPropertyAsync(document, propertyDeclaration, c)));
        }

        /// <summary>
        /// Returns true if both get and set accessors exist on the given property; otherwise false.
        /// </summary>
        private static bool HasBothAccessors(BasePropertyDeclarationSyntax property)
        {
            var accessors = property.AccessorList.Accessors;
            var getter = accessors.FirstOrDefault(ad => ad.Kind() == SyntaxKind.GetAccessorDeclaration);
            var setter = accessors.FirstOrDefault(ad => ad.Kind() == SyntaxKind.SetAccessorDeclaration);

            if (getter != null && setter != null)
            {
                // The getter and setter should have a body.
                return getter.Body != null && setter.Body != null;
            }

            return false;
        }

        private async Task<Document> ConvertToAutoPropertyAsync(Document document, PropertyDeclarationSyntax property, CancellationToken cancellationToken)
        {
            var tree = (SyntaxTree)await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = (SemanticModel)await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            // Retrieves the get accessor declarations of the specified property.
            var getter = property.AccessorList.Accessors.FirstOrDefault(ad => ad.Kind() == SyntaxKind.GetAccessorDeclaration);

            // Retrieves the type that contains the specified property
            var containingType = semanticModel.GetDeclaredSymbol(property).ContainingType;

            // Find the backing field of the property
            var backingField = await GetBackingFieldAsync(document, getter, containingType, cancellationToken).ConfigureAwait(false);

            // Rewrite property
            var propertyRewriter = new PropertyRewriter(semanticModel, backingField, property);
            var root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = propertyRewriter.Visit(root);

            return document.WithSyntaxRoot(newRoot);
        }

        private async Task<ISymbol> GetBackingFieldAsync(Document document, AccessorDeclarationSyntax getter, INamedTypeSymbol containingType, CancellationToken cancellationToken)
        {
            var statements = getter.Body.Statements;
            if (statements.Count == 1)
            {
                var returnStatement = statements.FirstOrDefault() as ReturnStatementSyntax;
                if (returnStatement != null && returnStatement.Expression != null)
                {
                    var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                    var symbolInfo = semanticModel.GetSymbolInfo(returnStatement.Expression);
                    var fieldSymbol = symbolInfo.Symbol as IFieldSymbol;

                    if (fieldSymbol != null && Equals(fieldSymbol.OriginalDefinition.ContainingType, containingType))
                    {
                        return fieldSymbol;
                    }
                }
            }

            return null;
        }

        private class ConvertToAutoPropertyCodeAction : CodeAction
        {
            private Func<CancellationToken, Task<Document>> generateDocument;
            private string title;

            public ConvertToAutoPropertyCodeAction(string title, Func<CancellationToken, Task<Document>> generateDocument)
            {
                this.title = title;
                this.generateDocument = generateDocument;
            }

            public override string Title { get { return title; } }

            protected override Task<Document> GetChangedDocumentAsync(CancellationToken cancellationToken)
            {
                return this.generateDocument(cancellationToken);
            }
        }
    }
}
