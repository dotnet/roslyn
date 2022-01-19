// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class ConstructorDeclarationSyntax
    {
        public ConstructorDeclarationSyntax Update(
            SyntaxList<AttributeListSyntax> attributeLists,
            SyntaxTokenList modifiers,
            SyntaxToken identifier,
            ParameterListSyntax parameterList,
            ConstructorInitializerSyntax initializer,
            BlockSyntax body,
            SyntaxToken semicolonToken)
            => Update(
                attributeLists,
                modifiers,
                identifier,
                parameterList,
                initializer,
                body,
                expressionBody: null,
                semicolonToken);
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        public static ConstructorDeclarationSyntax ConstructorDeclaration(
            SyntaxList<AttributeListSyntax> attributeLists,
            SyntaxTokenList modifiers,
            SyntaxToken identifier,
            ParameterListSyntax parameterList,
            ConstructorInitializerSyntax? initializer,
            BlockSyntax body)
            => ConstructorDeclaration(
                attributeLists,
                modifiers,
                identifier,
                parameterList,
                initializer,
                body,
                expressionBody: null,
                default(SyntaxToken));

        public static ConstructorDeclarationSyntax ConstructorDeclaration(
            SyntaxList<AttributeListSyntax> attributeLists,
            SyntaxTokenList modifiers,
            SyntaxToken identifier,
            ParameterListSyntax parameterList,
            ConstructorInitializerSyntax? initializer,
            BlockSyntax? body,
            SyntaxToken semicolonToken)
            => ConstructorDeclaration(
                attributeLists,
                modifiers,
                identifier,
                parameterList,
                initializer,
                body,
                expressionBody: null,
                semicolonToken);

        public static ConstructorDeclarationSyntax ConstructorDeclaration(
            SyntaxList<AttributeListSyntax> attributeLists,
            SyntaxTokenList modifiers,
            SyntaxToken identifier,
            ParameterListSyntax parameterList,
            ConstructorInitializerSyntax initializer,
            ArrowExpressionClauseSyntax expressionBody)
            => ConstructorDeclaration(
                attributeLists,
                modifiers,
                identifier,
                parameterList,
                initializer,
                body: null,
                expressionBody,
                default(SyntaxToken));

        public static ConstructorDeclarationSyntax ConstructorDeclaration(
            SyntaxList<AttributeListSyntax> attributeLists,
            SyntaxTokenList modifiers,
            SyntaxToken identifier,
            ParameterListSyntax parameterList,
            ConstructorInitializerSyntax initializer,
            ArrowExpressionClauseSyntax expressionBody,
            SyntaxToken semicolonToken)
            => ConstructorDeclaration(
                attributeLists,
                modifiers,
                identifier,
                parameterList,
                initializer,
                body: null,
                expressionBody,
                semicolonToken);

    }
}
