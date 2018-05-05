// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.DeclareAsNullable
{
    /// <summary>
    /// If you apply a null test on a symbol that isn't nullable, then we'll help you make that symbol nullable.
    /// For example: `nonNull == null`, `nonNull?.Property`
    /// </summary>
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.DeclareAsNullable), Shared]
    internal class DeclareAsNullableCodeRefactoringProvider : CodeRefactoringProvider
    {
        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var document = context.Document;
            var textSpan = context.Span;
            var cancellationToken = context.CancellationToken;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var symbolToFix = await TryGetSymbolToFix(root, textSpan, document, cancellationToken).ConfigureAwait(false);
            if (symbolToFix == null ||
                symbolToFix.Locations.Length != 1 ||
                !symbolToFix.IsNonImplicitAndFromSource())
            {
                return;
            }

            if (!IsFixableType(symbolToFix))
            {
                return;
            }

            var declarationLocation = symbolToFix.Locations[0];
            var node = declarationLocation.FindNode(getInnermostNodeForTie: true, cancellationToken);

            var typeToFix = TryGetTypeToFix(node);
            if (typeToFix == null || typeToFix is NullableTypeSyntax)
            {
                return;
            }

            context.RegisterRefactoring(
                new MyCodeAction(
                    CSharpFeaturesResources.Declare_as_nullable,
                    c => UpdateDocumentAsync(document, typeToFix, c)));
        }

        private bool IsFixableType(ISymbol symbolToFix)
        {
            ITypeSymbol type = null;
            switch (symbolToFix)
            {
                case IParameterSymbol parameter:
                    type = parameter.Type;
                    break;
                case ILocalSymbol local:
                    type = local.Type;
                    break;
                case IPropertySymbol property:
                    type = property.Type;
                    break;
                case IMethodSymbol method when method.IsDefinition:
                    type = method.ReturnType;
                    break;
                case IFieldSymbol field:
                    type = field.Type;
                    break;
                default:
                    return false;
            }

            return type?.IsReferenceType == true;
        }

        private TypeSyntax TryGetTypeToFix(SyntaxNode node)
        {
            switch (node)
            {
                case ParameterSyntax parameter:
                    return parameter.Type;

                case VariableDeclaratorSyntax declarator:
                    if (declarator.IsParentKind(SyntaxKind.VariableDeclaration))
                    {
                        var declaration = (VariableDeclarationSyntax)declarator.Parent;
                        return declaration.Variables.Count == 1 ? declaration.Type : null;
                    }
                    return null;

                case PropertyDeclarationSyntax property:
                    return property.Type;

                case MethodDeclarationSyntax method:
                    if (method.Modifiers.Any(SyntaxKind.PartialKeyword))
                    {
                        return null;
                    }
                    return method.ReturnType;
            }

            return null;
        }

        private async Task<ISymbol> TryGetSymbolToFix(SyntaxNode root, TextSpan textSpan, Document document, CancellationToken cancellationToken)
        {
            var token = root.FindToken(textSpan.Start);

            if (!token.IsKind(SyntaxKind.EqualsEqualsToken, SyntaxKind.ExclamationEqualsToken, SyntaxKind.NullKeyword))
            {
                return null;
            }

            BinaryExpressionSyntax equals;
            if (token.Parent.IsKind(SyntaxKind.EqualsExpression, SyntaxKind.NotEqualsExpression))
            {
                equals = (BinaryExpressionSyntax)token.Parent;
            }
            else if (token.Parent.IsKind(SyntaxKind.NullLiteralExpression) && token.Parent.IsParentKind(SyntaxKind.EqualsExpression, SyntaxKind.NotEqualsExpression))
            {
                equals = (BinaryExpressionSyntax)token.Parent.Parent;
            }
            else
            {
                return null;
            }

            ExpressionSyntax value;
            if (equals.Right.IsKind(SyntaxKind.NullLiteralExpression))
            {
                value = equals.Left;
            }
            else if (equals.Left.IsKind(SyntaxKind.NullLiteralExpression))
            {
                value = equals.Right;
            }
            else
            {
                return null;
            }

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            return semanticModel.GetSymbolInfo(value).Symbol;
        }

        private static async Task<Document> UpdateDocumentAsync(Document document, TypeSyntax typeToFix, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var editor = new SyntaxEditor(root, document.Project.Solution.Workspace);

            var fixedType = SyntaxFactory.NullableType(typeToFix.WithoutTrivia()).WithTriviaFrom(typeToFix);
            editor.ReplaceNode(typeToFix, fixedType);

            var newRoot = editor.GetChangedRoot();
            return document.WithSyntaxRoot(newRoot);
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
