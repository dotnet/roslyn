// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using CoreSyntax = Microsoft.CodeAnalysis.Syntax.InternalSyntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal abstract partial class TypeDeclarationSyntax
    {
        public abstract TypeDeclarationSyntax UpdateCore(
            CoreSyntax.SyntaxList<AttributeListSyntax> attributeLists,
            CoreSyntax.SyntaxList<SyntaxToken> modifiers,
            SyntaxToken keyword,
            SyntaxToken identifier,
            TypeParameterListSyntax typeParameterList,
            ParameterListSyntax parameterList,
            BaseListSyntax baseList,
            CoreSyntax.SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses,
            SyntaxToken openBraceToken,
            CoreSyntax.SyntaxList<MemberDeclarationSyntax> members,
            SyntaxToken closeBraceToken,
            SyntaxToken semicolonToken);
    }

    internal partial class ClassDeclarationSyntax
    {
        public override TypeDeclarationSyntax UpdateCore(
            CoreSyntax.SyntaxList<AttributeListSyntax> attributeLists,
            CoreSyntax.SyntaxList<SyntaxToken> modifiers,
            SyntaxToken keyword,
            SyntaxToken identifier,
            TypeParameterListSyntax typeParameterList,
            ParameterListSyntax parameterList,
            BaseListSyntax baseList,
            CoreSyntax.SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses,
            SyntaxToken openBraceToken,
            CoreSyntax.SyntaxList<MemberDeclarationSyntax> members,
            SyntaxToken closeBraceToken,
            SyntaxToken semicolonToken)
        {
            return this.Update(
                attributeLists,
                modifiers,
                keyword,
                identifier,
                typeParameterList,
                parameterList,
                baseList,
                constraintClauses,
                openBraceToken,
                members,
                closeBraceToken,
                semicolonToken);
        }
    }

    internal partial class InterfaceDeclarationSyntax
    {
        public override TypeDeclarationSyntax UpdateCore(
            CoreSyntax.SyntaxList<AttributeListSyntax> attributeLists,
            CoreSyntax.SyntaxList<SyntaxToken> modifiers,
            SyntaxToken keyword,
            SyntaxToken identifier,
            TypeParameterListSyntax typeParameterList,
            ParameterListSyntax parameterList,
            BaseListSyntax baseList,
            CoreSyntax.SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses,
            SyntaxToken openBraceToken,
            CoreSyntax.SyntaxList<MemberDeclarationSyntax> members,
            SyntaxToken closeBraceToken,
            SyntaxToken semicolonToken)
        {
            return this.Update(
                attributeLists,
                modifiers,
                keyword,
                identifier,
                typeParameterList,
                parameterList,
                baseList,
                constraintClauses,
                openBraceToken,
                members,
                closeBraceToken,
                semicolonToken);
        }
    }

    internal partial class RecordDeclarationSyntax
    {
        public override TypeDeclarationSyntax UpdateCore(
            CoreSyntax.SyntaxList<AttributeListSyntax> attributeLists,
            CoreSyntax.SyntaxList<SyntaxToken> modifiers,
            SyntaxToken keyword,
            SyntaxToken identifier,
            TypeParameterListSyntax typeParameterList,
            ParameterListSyntax parameterList,
            BaseListSyntax baseList,
            CoreSyntax.SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses,
            SyntaxToken openBraceToken,
            CoreSyntax.SyntaxList<MemberDeclarationSyntax> members,
            SyntaxToken closeBraceToken,
            SyntaxToken semicolonToken)
        {
            return this.Update(
                attributeLists,
                modifiers,
                keyword,
                this.ClassOrStructKeyword,
                identifier,
                typeParameterList,
                parameterList,
                baseList,
                constraintClauses,
                openBraceToken,
                members,
                closeBraceToken,
                semicolonToken);
        }
    }

    internal partial class StructDeclarationSyntax
    {
        public override TypeDeclarationSyntax UpdateCore(
            CoreSyntax.SyntaxList<AttributeListSyntax> attributeLists,
            CoreSyntax.SyntaxList<SyntaxToken> modifiers,
            SyntaxToken keyword,
            SyntaxToken identifier,
            TypeParameterListSyntax typeParameterList,
            ParameterListSyntax parameterList,
            BaseListSyntax baseList,
            CoreSyntax.SyntaxList<TypeParameterConstraintClauseSyntax> constraintClauses,
            SyntaxToken openBraceToken,
            CoreSyntax.SyntaxList<MemberDeclarationSyntax> members,
            SyntaxToken closeBraceToken,
            SyntaxToken semicolonToken)
        {
            return this.Update(
                attributeLists,
                modifiers,
                keyword,
                identifier,
                typeParameterList,
                parameterList,
                baseList,
                constraintClauses,
                openBraceToken,
                members,
                closeBraceToken,
                semicolonToken);
        }
    }
}
