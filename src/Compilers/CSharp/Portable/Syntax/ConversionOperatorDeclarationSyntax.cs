// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class ConversionOperatorDeclarationSyntax
    {
        public ConversionOperatorDeclarationSyntax Update(
            SyntaxList<AttributeListSyntax> attributeLists,
            SyntaxTokenList modifiers,
            SyntaxToken implicitOrExplicitKeyword,
            SyntaxToken operatorKeyword,
            TypeSyntax type,
            ParameterListSyntax parameterList,
            BlockSyntax? body,
            ArrowExpressionClauseSyntax? expressionBody,
            SyntaxToken semicolonToken)
        {
            return Update(
                attributeLists: attributeLists,
                modifiers: modifiers,
                implicitOrExplicitKeyword: implicitOrExplicitKeyword,
                explicitInterfaceSpecifier: this.ExplicitInterfaceSpecifier,
                operatorKeyword: operatorKeyword,
                type: type,
                parameterList: parameterList,
                body: body,
                expressionBody: expressionBody,
                semicolonToken: semicolonToken);
        }

        public ConversionOperatorDeclarationSyntax Update(
            SyntaxList<AttributeListSyntax> attributeLists,
            SyntaxTokenList modifiers,
            SyntaxToken implicitOrExplicitKeyword,
            ExplicitInterfaceSpecifierSyntax? explicitInterfaceSpecifier,
            SyntaxToken operatorKeyword,
            TypeSyntax type,
            ParameterListSyntax parameterList,
            BlockSyntax? body,
            ArrowExpressionClauseSyntax? expressionBody,
            SyntaxToken semicolonToken)
        {
            return Update(
                attributeLists: attributeLists,
                modifiers: modifiers,
                implicitOrExplicitKeyword: implicitOrExplicitKeyword,
                explicitInterfaceSpecifier: explicitInterfaceSpecifier,
                operatorKeyword: operatorKeyword,
                checkedKeyword: this.CheckedKeyword,
                type: type,
                parameterList: parameterList,
                body: body,
                expressionBody: expressionBody,
                semicolonToken: semicolonToken);
        }
    }
}
