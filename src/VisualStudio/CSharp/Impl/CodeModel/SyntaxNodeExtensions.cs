// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.CodeModel
{
    internal static class SyntaxNodeExtensions
    {
        public static bool TryGetAttributeLists(this SyntaxNode node, out SyntaxList<AttributeListSyntax> attributeLists)
        {
            if (node is CompilationUnitSyntax compilationUnit)
            {
                attributeLists = compilationUnit.AttributeLists;
                return true;
            }
            else if (node is BaseTypeDeclarationSyntax baseType)
            {
                attributeLists = baseType.AttributeLists;
                return true;
            }
            else if (node is BaseMethodDeclarationSyntax baseMethod)
            {
                attributeLists = baseMethod.AttributeLists;
                return true;
            }
            else if (node is BasePropertyDeclarationSyntax baseProperty)
            {
                attributeLists = baseProperty.AttributeLists;
                return true;
            }
            else if (node is BaseFieldDeclarationSyntax baseField)
            {
                attributeLists = baseField.AttributeLists;
                return true;
            }
            else if (node is DelegateDeclarationSyntax delegateDeclaration)
            {
                attributeLists = delegateDeclaration.AttributeLists;
                return true;
            }
            else if (node is EnumMemberDeclarationSyntax enumMember)
            {
                attributeLists = enumMember.AttributeLists;
                return true;
            }
            else if (node is ParameterSyntax parameter)
            {
                attributeLists = parameter.AttributeLists;
                return true;
            }

            attributeLists = default;
            return false;
        }

        public static SyntaxToken GetFirstTokenAfterAttributes(this SyntaxNode node)
        {
            if (node.TryGetAttributeLists(out var attributeLists) && attributeLists.Count > 0)
            {
                return attributeLists.Last().GetLastToken().GetNextToken();
            }

            return node.GetFirstToken();
        }
    }
}
