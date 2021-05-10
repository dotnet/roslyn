// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class RecordDeclarationSyntax
    {
        internal PrimaryConstructorBaseTypeSyntax? PrimaryConstructorBaseTypeIfClass
        {
            get
            {
                if (Kind() == SyntaxKind.RecordStructDeclaration)
                {
                    return null;
                }

                return BaseList?.Types.FirstOrDefault() as PrimaryConstructorBaseTypeSyntax;
            }
        }

        public RecordDeclarationSyntax Update(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, SyntaxToken keyword, SyntaxToken identifier,
            TypeParameterListSyntax? typeParameterList, ParameterListSyntax? parameterList, BaseListSyntax? baseList, SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses,
            SyntaxToken openBraceToken, SyntaxList<MemberDeclarationSyntax> members, SyntaxToken closeBraceToken, SyntaxToken semicolonToken)
        {
            return Update(attributeLists, modifiers, keyword, this.ClassOrStructKeyword, identifier,
                typeParameterList, parameterList, baseList, constraintClauses, openBraceToken, members, closeBraceToken, semicolonToken);
        }
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        public static RecordDeclarationSyntax RecordDeclaration(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, SyntaxToken keyword, SyntaxToken identifier,
            TypeParameterListSyntax? typeParameterList, ParameterListSyntax? parameterList, BaseListSyntax? baseList, SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses,
            SyntaxToken openBraceToken, SyntaxList<MemberDeclarationSyntax> members, SyntaxToken closeBraceToken, SyntaxToken semicolonToken)
        {
            return RecordDeclaration(SyntaxKind.RecordDeclaration, attributeLists, modifiers, keyword, classOrStructKeyword: default, identifier,
                typeParameterList, parameterList, baseList, constraintClauses, openBraceToken, members, closeBraceToken, semicolonToken);
        }

        public static RecordDeclarationSyntax RecordDeclaration(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, SyntaxToken keyword, SyntaxToken identifier,
            TypeParameterListSyntax typeParameterList, ParameterListSyntax parameterList, BaseListSyntax baseList, SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses, SyntaxList<MemberDeclarationSyntax> members)
        {
            return RecordDeclaration(SyntaxKind.RecordDeclaration, attributeLists, modifiers, keyword, classOrStructKeyword: default, identifier,
                typeParameterList, parameterList, baseList, constraintClauses, openBraceToken: default, members, closeBraceToken: default, semicolonToken: default);
        }

        public static RecordDeclarationSyntax RecordDeclaration(SyntaxToken keyword, string identifier)
        {
            return RecordDeclaration(keyword, SyntaxFactory.Identifier(identifier));
        }

        public static RecordDeclarationSyntax RecordDeclaration(SyntaxToken keyword, SyntaxToken identifier)
        {
            return RecordDeclaration(SyntaxKind.RecordDeclaration, attributeLists: default, modifiers: default, keyword, classOrStructKeyword: default, identifier,
                typeParameterList: null, parameterList: null, baseList: null, constraintClauses: default, openBraceToken: default, members: default, closeBraceToken: default, semicolonToken: default);
        }
    }
}
