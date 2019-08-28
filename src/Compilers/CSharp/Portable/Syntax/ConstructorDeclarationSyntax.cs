// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
                default(ArrowExpressionClauseSyntax),
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
            ConstructorInitializerSyntax initializer,
            BlockSyntax body)
            => ConstructorDeclaration(
                attributeLists,
                modifiers,
                identifier,
                parameterList,
                initializer,
                body,
                default(ArrowExpressionClauseSyntax),
                default(SyntaxToken));

        public static ConstructorDeclarationSyntax ConstructorDeclaration(
            SyntaxList<AttributeListSyntax> attributeLists,
            SyntaxTokenList modifiers,
            SyntaxToken identifier,
            ParameterListSyntax parameterList,
            ConstructorInitializerSyntax initializer,
            BlockSyntax body,
            SyntaxToken semicolonToken)
            => ConstructorDeclaration(
                attributeLists,
                modifiers,
                identifier,
                parameterList,
                initializer,
                body,
                default(ArrowExpressionClauseSyntax),
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
                default(BlockSyntax),
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
                default(BlockSyntax),
                expressionBody,
                semicolonToken);

    }
}
