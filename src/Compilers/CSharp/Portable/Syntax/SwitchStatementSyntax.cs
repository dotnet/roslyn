// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class SwitchStatementSyntax
    {
        public SwitchStatementSyntax Update(SyntaxToken switchKeyword, SyntaxToken openParenToken, ExpressionSyntax expression, SyntaxToken closeParenToken, SyntaxToken openBraceToken, SyntaxList<SwitchSectionSyntax> sections, SyntaxToken closeBraceToken)
            => Update(attributeLists: default, switchKeyword, openParenToken, expression, closeParenToken, openBraceToken, sections, closeBraceToken);
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
