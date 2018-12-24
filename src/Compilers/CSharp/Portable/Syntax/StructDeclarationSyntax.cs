// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class StructDeclarationSyntax
    {
        public StructDeclarationSyntax Update(
            SyntaxList<AttributeListSyntax> attributeLists,
            SyntaxTokenList modifiers,
            SyntaxToken keyword,
            SyntaxToken identifier,
            TypeParameterListSyntax typeParameterList,
            BaseListSyntax baseList,
            SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses,
            SyntaxToken openBraceToken,
            SyntaxList<MemberDeclarationSyntax> members,
            SyntaxToken closeBraceToken,
            SyntaxToken semicolonToken)
            => Update(attributeLists,
                modifiers,
                keyword,
                identifier,
                typeParameterList,
                default(ParameterListSyntax),
                baseList,
                constraintClauses,
                openBraceToken,
                members,
                closeBraceToken,
                semicolonToken);
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public static partial class SyntaxFactory
    {
        public static StructDeclarationSyntax StructDeclaration(
            SyntaxList<AttributeListSyntax> attributeLists,
            SyntaxTokenList modifiers,
            SyntaxToken identifier,
            TypeParameterListSyntax typeParameterList,
            BaseListSyntax baseList,
            SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses,
            SyntaxList<MemberDeclarationSyntax> members)
            => StructDeclaration(
                attributeLists,
                modifiers,
                SyntaxFactory.Token(SyntaxKind.StructKeyword),
                identifier,
                typeParameterList,
                default(ParameterListSyntax),
                baseList,
                constraintClauses,
                SyntaxFactory.Token(SyntaxKind.OpenBraceToken),
                members,
                SyntaxFactory.Token(SyntaxKind.CloseBraceToken),
                semicolonToken: default);

        public static StructDeclarationSyntax StructDeclaration(
            SyntaxList<AttributeListSyntax> attributeLists,
            SyntaxTokenList modifiers,
            SyntaxToken keyword,
            SyntaxToken identifier,
            TypeParameterListSyntax typeParameterList,
            BaseListSyntax baseList,
            SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses,
            SyntaxToken openBraceToken,
            SyntaxList<MemberDeclarationSyntax> members,
            SyntaxToken closeBraceToken,
            SyntaxToken semicolonToken)
            => StructDeclaration(
                attributeLists,
                modifiers,
                keyword,
                identifier,
                typeParameterList,
                default(ParameterListSyntax),
                baseList,
                constraintClauses,
                openBraceToken,
                members,
                closeBraceToken,
                semicolonToken);

    }
}
