// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class OperatorDeclarationSyntax
    {
        public OperatorDeclarationSyntax Update(
            SyntaxList<AttributeListSyntax> attributeLists,
            SyntaxTokenList modifiers,
            TypeSyntax returnType,
            SyntaxToken operatorKeyword,
            SyntaxToken operatorToken,
            ParameterListSyntax parameterList,
            BlockSyntax? body,
            ArrowExpressionClauseSyntax? expressionBody,
            SyntaxToken semicolonToken)
        {
            return Update(
                attributeLists: attributeLists,
                modifiers: modifiers,
                returnType: returnType,
                explicitInterfaceSpecifier: this.ExplicitInterfaceSpecifier,
                operatorKeyword: operatorKeyword,
                operatorToken: operatorToken,
                parameterList: parameterList,
                body: body,
                expressionBody: expressionBody,
                semicolonToken: semicolonToken);
        }

        public OperatorDeclarationSyntax Update(
            SyntaxList<AttributeListSyntax> attributeLists,
            SyntaxTokenList modifiers,
            TypeSyntax returnType,
            ExplicitInterfaceSpecifierSyntax? explicitInterfaceSpecifier,
            SyntaxToken operatorKeyword,
            SyntaxToken operatorToken,
            ParameterListSyntax parameterList,
            BlockSyntax? body,
            ArrowExpressionClauseSyntax? expressionBody,
            SyntaxToken semicolonToken)
        {
            return Update(
                attributeLists: attributeLists,
                modifiers: modifiers,
                returnType: returnType,
                explicitInterfaceSpecifier: explicitInterfaceSpecifier,
                operatorKeyword: operatorKeyword,
                checkedKeyword: this.CheckedKeyword,
                operatorToken: operatorToken,
                parameterList: parameterList,
                body: body,
                expressionBody: expressionBody,
                semicolonToken: semicolonToken);
        }
    }
}
