// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class SwitchStatementSyntax
    {
        public SwitchStatementSyntax Update(SyntaxToken switchKeyword, SyntaxToken openParenToken, ExpressionSyntax expression, SyntaxToken closeParenToken, SyntaxToken openBraceToken, SyntaxList<SwitchSectionSyntax> sections, SyntaxToken closeBraceToken)
            => Update(AttributeLists, switchKeyword, openParenToken, expression, closeParenToken, openBraceToken, sections, closeBraceToken);
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        public static SwitchStatementSyntax SwitchStatement(SyntaxToken switchKeyword, SyntaxToken openParenToken, ExpressionSyntax expression, SyntaxToken closeParenToken, SyntaxToken openBraceToken, SyntaxList<SwitchSectionSyntax> sections, SyntaxToken closeBraceToken)
            => SwitchStatement(attributeLists: default, switchKeyword, openParenToken, expression, closeParenToken, openBraceToken, sections, closeBraceToken);
    }
}
