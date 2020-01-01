// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class ClassDeclarationSyntax
    {
        public ClassDeclarationSyntax Update(
            SyntaxList<AttributeListSyntax> attributeLists,
            SyntaxTokenList modifiers,
            SyntaxToken keyword,
            SyntaxToken identifier,
            TypeParameterListSyntax? typeParameterList,
            BaseListSyntax? baseList,
            SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses,
            SyntaxToken openBraceToken,
            SyntaxList<MemberDeclarationSyntax> members,
            SyntaxToken closeBraceToken,
            SyntaxToken semicolonToken)
        {
            if (attributeLists != this.AttributeLists || modifiers != this.Modifiers || keyword != this.Keyword || identifier != this.Identifier || typeParameterList != this.TypeParameterList || baseList != this.BaseList || constraintClauses != this.ConstraintClauses || openBraceToken != this.OpenBraceToken || members != this.Members || closeBraceToken != this.CloseBraceToken || semicolonToken != this.SemicolonToken)
            {
                var newNode = SyntaxFactory.ClassDeclaration(attributeLists, modifiers, keyword, identifier, typeParameterList, this.ParameterList, baseList, constraintClauses, openBraceToken, members, closeBraceToken, semicolonToken);
                var annotations = GetAnnotations();
                return annotations?.Length > 0 ? newNode.WithAnnotations(annotations) : newNode;
            }

            return this;
        }
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        /// <summary>Creates a new ClassDeclarationSyntax instance.</summary>
        public static ClassDeclarationSyntax ClassDeclaration(
            SyntaxList<AttributeListSyntax> attributeLists,
            SyntaxTokenList modifiers,
            SyntaxToken keyword,
            SyntaxToken identifier,
            TypeParameterListSyntax? typeParameterList,
            BaseListSyntax? baseList,
            SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses,
            SyntaxToken openBraceToken,
            SyntaxList<MemberDeclarationSyntax> members,
            SyntaxToken closeBraceToken,
            SyntaxToken semicolonToken)
            => SyntaxFactory.ClassDeclaration(
                attributeLists,
                modifiers,
                keyword,
                identifier,
                typeParameterList,
                parameterList: null,
                baseList,
                constraintClauses,
                openBraceToken,
                members,
                closeBraceToken,
                semicolonToken);

        /// <summary>Creates a new ClassDeclarationSyntax instance.</summary>
        public static ClassDeclarationSyntax ClassDeclaration(
            SyntaxList<AttributeListSyntax> attributeLists,
            SyntaxTokenList modifiers,
            SyntaxToken identifier,
            TypeParameterListSyntax typeParameterList,
            BaseListSyntax baseList,
            SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses,
            SyntaxList<MemberDeclarationSyntax> members)
            => SyntaxFactory.ClassDeclaration(
                attributeLists,
                modifiers,
                SyntaxFactory.Token(SyntaxKind.ClassKeyword),
                identifier,
                typeParameterList,
                baseList,
                constraintClauses,
                SyntaxFactory.Token(SyntaxKind.OpenBraceToken),
                members,
                SyntaxFactory.Token(SyntaxKind.CloseBraceToken),
                semicolonToken: default);

        /// <summary>Creates a new ClassDeclarationSyntax instance.</summary>
        public static ClassDeclarationSyntax ClassDeclaration(
            SyntaxList<AttributeListSyntax> attributeLists,
            SyntaxTokenList modifiers,
            SyntaxToken identifier,
            TypeParameterListSyntax? typeParameterList,
            ParameterListSyntax? parameterList,
            BaseListSyntax? baseList,
            SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses,
            SyntaxList<MemberDeclarationSyntax> members)
            => SyntaxFactory.ClassDeclaration(
                attributeLists,
                modifiers,
                SyntaxFactory.Token(SyntaxKind.ClassKeyword),
                identifier,
                typeParameterList,
                parameterList,
                baseList,
                constraintClauses,
                SyntaxFactory.Token(SyntaxKind.OpenBraceToken),
                members,
                SyntaxFactory.Token(SyntaxKind.CloseBraceToken),
                semicolonToken: default);

        /// <summary>Creates a new ClassDeclarationSyntax instance.</summary>
        public static ClassDeclarationSyntax ClassDeclaration(SyntaxToken identifier)
            => SyntaxFactory.ClassDeclaration(default,
                default(SyntaxTokenList),
                SyntaxFactory.Token(SyntaxKind.ClassKeyword),
                identifier,
                default,
                default,
                default,
                SyntaxFactory.Token(SyntaxKind.OpenBraceToken),
                default,
                SyntaxFactory.Token(SyntaxKind.CloseBraceToken),
                default);

        /// <summary>Creates a new ClassDeclarationSyntax instance.</summary>
        public static ClassDeclarationSyntax ClassDeclaration(string identifier)
            => SyntaxFactory.ClassDeclaration(default,
                default(SyntaxTokenList),
                SyntaxFactory.Token(SyntaxKind.ClassKeyword),
                SyntaxFactory.Identifier(identifier),
                default,
                default,
                default,
                SyntaxFactory.Token(SyntaxKind.OpenBraceToken),
                default,
                SyntaxFactory.Token(SyntaxKind.CloseBraceToken),
                default);
    }
}
