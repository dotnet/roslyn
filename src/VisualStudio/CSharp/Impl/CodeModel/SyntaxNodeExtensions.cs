// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.CodeModel
{
    internal static class SyntaxNodeExtensions
    {
        public static bool TryGetAttributeLists(this SyntaxNode node, out SyntaxList<AttributeListSyntax> attributeLists)
        {
            if (node is CompilationUnitSyntax)
            {
                attributeLists = ((CompilationUnitSyntax)node).AttributeLists;
                return true;
            }
            else if (node is BaseTypeDeclarationSyntax)
            {
                attributeLists = ((BaseTypeDeclarationSyntax)node).AttributeLists;
                return true;
            }
            else if (node is BaseMethodDeclarationSyntax)
            {
                attributeLists = ((BaseMethodDeclarationSyntax)node).AttributeLists;
                return true;
            }
            else if (node is BasePropertyDeclarationSyntax)
            {
                attributeLists = ((BasePropertyDeclarationSyntax)node).AttributeLists;
                return true;
            }
            else if (node is BaseFieldDeclarationSyntax)
            {
                attributeLists = ((BaseFieldDeclarationSyntax)node).AttributeLists;
                return true;
            }
            else if (node is DelegateDeclarationSyntax)
            {
                attributeLists = ((DelegateDeclarationSyntax)node).AttributeLists;
                return true;
            }
            else if (node is EnumMemberDeclarationSyntax)
            {
                attributeLists = ((EnumMemberDeclarationSyntax)node).AttributeLists;
                return true;
            }
            else if (node is ParameterSyntax)
            {
                attributeLists = ((ParameterSyntax)node).AttributeLists;
                return true;
            }

            attributeLists = default(SyntaxList<AttributeListSyntax>);
            return false;
        }

        public static SyntaxToken GetFirstTokenAfterAttributes(this SyntaxNode node)
        {
            SyntaxList<AttributeListSyntax> attributeLists;
            if (node.TryGetAttributeLists(out attributeLists) && attributeLists.Count > 0)
            {
                return attributeLists.Last().GetLastToken().GetNextToken();
            }

            return node.GetFirstToken();
        }
    }
}