// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class IfStatementSyntax
    {
        public IfStatementSyntax Update(SyntaxToken ifKeyword, SyntaxToken openParenToken, ExpressionSyntax condition, SyntaxToken closeParenToken, StatementSyntax statement, ElseClauseSyntax @else)
            => Update(attributeLists: default, ifKeyword, openParenToken, condition, closeParenToken, statement, @else);
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        public static IfStatementSyntax IfStatement(ExpressionSyntax condition, StatementSyntax statement, ElseClauseSyntax @else)
            => IfStatement(attributeLists: default, condition, statement, @else);

        public static IfStatementSyntax IfStatement(SyntaxToken ifKeyword, SyntaxToken openParenToken, ExpressionSyntax condition, SyntaxToken closeParenToken, StatementSyntax statement, ElseClauseSyntax @else)
            => IfStatement(attributeLists: default, ifKeyword, openParenToken, condition, closeParenToken, statement, @else);
    }
}
