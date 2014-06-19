// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public abstract partial class TypeDeclarationSyntax
    {
        public int Arity
        {
            get
            {
                return this.TypeParameterList == null ? 0 : this.TypeParameterList.Parameters.Count;
            }
        }
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public static partial class SyntaxFactory
    {
        internal static SyntaxKind GetTypeDeclarationKeywordKind(DeclarationKind kind)
        {
            switch (kind)
            {
                case DeclarationKind.Class:
                    return SyntaxKind.ClassKeyword;
                case DeclarationKind.Struct:
                    return SyntaxKind.StructKeyword;
                case DeclarationKind.Interface:
                    return SyntaxKind.InterfaceKeyword;
                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }

        private static SyntaxKind GetTypeDeclarationKeywordKind(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.ClassDeclaration:
                    return SyntaxKind.ClassKeyword;
                case SyntaxKind.StructDeclaration:
                    return SyntaxKind.StructKeyword;
                case SyntaxKind.InterfaceDeclaration:
                    return SyntaxKind.InterfaceKeyword;
                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }

        public static TypeDeclarationSyntax TypeDeclaration(SyntaxKind kind, SyntaxToken identifier)
        {
            return TypeDeclaration(
                kind,
                default(SyntaxList<AttributeListSyntax>),
                default(SyntaxTokenList),
                SyntaxFactory.Token(GetTypeDeclarationKeywordKind(kind)),
                identifier,
                default(TypeParameterListSyntax),
                default(BaseListSyntax),
                default(SyntaxList<TypeParameterConstraintClauseSyntax>),
                SyntaxFactory.Token(SyntaxKind.OpenBraceToken),
                default(SyntaxList<MemberDeclarationSyntax>),
                SyntaxFactory.Token(SyntaxKind.CloseBraceToken),
                default(SyntaxToken));
        }

        public static TypeDeclarationSyntax TypeDeclaration(SyntaxKind kind, string identifier)
        {
            return SyntaxFactory.TypeDeclaration(kind, SyntaxFactory.Identifier(identifier));
        }

        /// <summary>Creates a new ClassDeclarationSyntax instance.</summary>
        public static ClassDeclarationSyntax ClassDeclaration(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, SyntaxToken keyword, SyntaxToken identifier, TypeParameterListSyntax typeParameterList, BaseListSyntax baseList, SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses, SyntaxToken openBraceToken, SyntaxList<MemberDeclarationSyntax> members, SyntaxToken closeBraceToken, SyntaxToken semicolonToken)
        {
            return SyntaxFactory.ClassDeclaration(attributeLists, modifiers, keyword, identifier, typeParameterList, null, baseList, constraintClauses, openBraceToken, members, closeBraceToken, semicolonToken);
        }

        /// <summary>Creates a new ClassDeclarationSyntax instance.</summary>
        public static ClassDeclarationSyntax ClassDeclaration(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, SyntaxToken identifier, TypeParameterListSyntax typeParameterList, BaseListSyntax baseList, SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses, SyntaxList<MemberDeclarationSyntax> members)
        {
            return SyntaxFactory.ClassDeclaration(attributeLists, modifiers, identifier, typeParameterList, null, baseList, constraintClauses, members);
        }

        /// <summary>Creates a new StructDeclarationSyntax instance.</summary>
        public static StructDeclarationSyntax StructDeclaration(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, SyntaxToken keyword, SyntaxToken identifier, TypeParameterListSyntax typeParameterList, BaseListSyntax baseList, SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses, SyntaxToken openBraceToken, SyntaxList<MemberDeclarationSyntax> members, SyntaxToken closeBraceToken, SyntaxToken semicolonToken)
        {
            return SyntaxFactory.StructDeclaration(attributeLists, modifiers, keyword, identifier, typeParameterList, null, baseList, constraintClauses, openBraceToken, members, closeBraceToken, semicolonToken);
        }

        /// <summary>Creates a new StructDeclarationSyntax instance.</summary>
        public static StructDeclarationSyntax StructDeclaration(SyntaxList<AttributeListSyntax> attributeLists, SyntaxTokenList modifiers, SyntaxToken identifier, TypeParameterListSyntax typeParameterList, BaseListSyntax baseList, SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses, SyntaxList<MemberDeclarationSyntax> members)
        {
            return SyntaxFactory.StructDeclaration(attributeLists, modifiers, identifier, typeParameterList, null, baseList, constraintClauses, members);
        }

        public static TypeDeclarationSyntax TypeDeclaration(SyntaxKind kind, SyntaxList<AttributeListSyntax> attributes, SyntaxTokenList modifiers, SyntaxToken keyword, SyntaxToken identifier, TypeParameterListSyntax typeParameterList, BaseListSyntax baseList, SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses, SyntaxToken openBraceToken, SyntaxList<MemberDeclarationSyntax> members, SyntaxToken closeBraceToken, SyntaxToken semicolonToken)
        {
            switch (kind)
            {
                case SyntaxKind.ClassDeclaration:
                    return SyntaxFactory.ClassDeclaration(attributes, modifiers, keyword, identifier, typeParameterList, baseList, constraintClauses, openBraceToken, members, closeBraceToken, semicolonToken);
                case SyntaxKind.StructDeclaration:
                    return SyntaxFactory.StructDeclaration(attributes, modifiers, keyword, identifier, typeParameterList, baseList, constraintClauses, openBraceToken, members, closeBraceToken, semicolonToken);
                case SyntaxKind.InterfaceDeclaration:
                    return SyntaxFactory.InterfaceDeclaration(attributes, modifiers, keyword, identifier, typeParameterList, baseList, constraintClauses, openBraceToken, members, closeBraceToken, semicolonToken);
                default:
                    throw new ArgumentException("kind");
            }
        }
    }
}