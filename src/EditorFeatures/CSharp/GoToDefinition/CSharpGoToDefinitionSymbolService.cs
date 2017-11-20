// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Composition;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editor.GoToDefinition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.CSharp.GoToDefinition
{
    [ExportLanguageService(typeof(IGoToDefinitionSymbolService), LanguageNames.CSharp), Shared]
    internal class CSharpGoToDefinitionSymbolService : AbstractGoToDefinitionSymbolService
    {
        protected override ISymbol FindRelatedExplicitlyDeclaredSymbol(ISymbol symbol, Compilation compilation)
        {
            return symbol;
        }

        protected override bool TokenIsPartOfDeclaringSyntax(SyntaxToken token)
        {
            var parentNode = token.Parent;
            switch (parentNode.Kind())
            {
                case SyntaxKind.ClassDeclaration:
                case SyntaxKind.ConstructorDeclaration:
                case SyntaxKind.MethodDeclaration:
                case SyntaxKind.PropertyDeclaration:
                case SyntaxKind.EventDeclaration:
                case SyntaxKind.FieldDeclaration:
                case SyntaxKind.GetAccessorDeclaration:
                case SyntaxKind.SetAccessorDeclaration:
                case SyntaxKind.AddAccessorDeclaration:
                case SyntaxKind.RemoveAccessorDeclaration:
                case SyntaxKind.VariableDeclaration:
                case SyntaxKind.DelegateDeclaration:
                case SyntaxKind.EnumDeclaration:
                case SyntaxKind.StructDeclaration:
                case SyntaxKind.Parameter:
                case SyntaxKind.EnumMemberDeclaration:
                case SyntaxKind.VariableDeclarator:
                case SyntaxKind.LocalFunctionStatement:
                case SyntaxKind.TypeParameter:
                case SyntaxKind.AnonymousObjectCreationExpression:
                    return true;
                case SyntaxKind.IdentifierName:
                    return parentNode?.Parent.IsKind(SyntaxKind.NameColon, SyntaxKind.NameEquals) ?? false;
            }

            return false;
        }
    }
}
