// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class DestructorDeclarationSyntax
    {
        public DestructorDeclarationSyntax Update(
            SyntaxList<AttributeListSyntax> attributeLists,
            SyntaxTokenList modifiers,
            SyntaxToken tildeToken,
            SyntaxToken identifier,
            ParameterListSyntax parameterList,
            BlockSyntax body,
            SyntaxToken semicolonToken)
            => Update(
                attributeLists,
                modifiers,
                tildeToken,
                identifier,
                parameterList,
                body,
                expressionBody: null,
                semicolonToken);
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        public static DestructorDeclarationSyntax DestructorDeclaration(
            SyntaxList<AttributeListSyntax> attributeLists,
            SyntaxTokenList modifiers,
            SyntaxToken identifier,
            ParameterListSyntax parameterList,
            BlockSyntax body)
            => DestructorDeclaration(
                attributeLists,
                modifiers,
                SyntaxFactory.Token(SyntaxKind.TildeToken),
                identifier,
                parameterList,
                body,
                expressionBody: null,
                default(SyntaxToken));

        public static DestructorDeclarationSyntax DestructorDeclaration(
            SyntaxList<AttributeListSyntax> attributeLists,
            SyntaxTokenList modifiers,
            SyntaxToken tildeToken,
            SyntaxToken identifier,
            ParameterListSyntax parameterList,
            BlockSyntax? body,
            SyntaxToken semicolonToken)
            => DestructorDeclaration(
                attributeLists,
                modifiers,
                tildeToken,
                identifier,
                parameterList,
                body,
                expressionBody: null,
                semicolonToken);

        public static DestructorDeclarationSyntax DestructorDeclaration(
            SyntaxList<AttributeListSyntax> attributeLists,
            SyntaxTokenList modifiers,
            SyntaxToken identifier,
            ParameterListSyntax parameterList,
            ArrowExpressionClauseSyntax expressionBody)
            => DestructorDeclaration(
                attributeLists,
                modifiers,
                SyntaxFactory.Token(SyntaxKind.TildeToken),
                identifier,
                parameterList,
                body: null,
                expressionBody,
                default(SyntaxToken));

        public static DestructorDeclarationSyntax DestructorDeclaration(
            SyntaxList<AttributeListSyntax> attributeLists,
            SyntaxTokenList modifiers,
            SyntaxToken tildeToken,
            SyntaxToken identifier,
            ParameterListSyntax parameterList,
            ArrowExpressionClauseSyntax expressionBody,
            SyntaxToken semicolonToken)
            => DestructorDeclaration(
                attributeLists,
                modifiers,
                tildeToken,
                identifier,
                parameterList,
                body: null,
                expressionBody,
                semicolonToken);

    }
}
