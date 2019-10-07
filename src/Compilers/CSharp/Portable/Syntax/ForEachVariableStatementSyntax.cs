// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class ForEachVariableStatementSyntax
    {
        public ForEachVariableStatementSyntax Update(SyntaxToken forEachKeyword, SyntaxToken openParenToken, ExpressionSyntax variable, SyntaxToken inKeyword, ExpressionSyntax expression, SyntaxToken closeParenToken, StatementSyntax statement)
            => Update(awaitKeyword: default, forEachKeyword, openParenToken, variable, inKeyword, expression, closeParenToken, statement);

        public ForEachVariableStatementSyntax Update(SyntaxToken awaitKeyword, SyntaxToken forEachKeyword, SyntaxToken openParenToken, ExpressionSyntax variable, SyntaxToken inKeyword, ExpressionSyntax expression, SyntaxToken closeParenToken, StatementSyntax statement)
            => Update(attributeLists: default, awaitKeyword, forEachKeyword, openParenToken, variable, inKeyword, expression, closeParenToken, statement);
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        public static ForEachVariableStatementSyntax ForEachVariableStatement(SyntaxToken forEachKeyword, SyntaxToken openParenToken, ExpressionSyntax variable, SyntaxToken inKeyword, ExpressionSyntax expression, SyntaxToken closeParenToken, StatementSyntax statement)
            => ForEachVariableStatement(awaitKeyword: default, forEachKeyword, openParenToken, variable, inKeyword, expression, closeParenToken, statement);

        public static ForEachVariableStatementSyntax ForEachVariableStatement(SyntaxToken awaitKeyword, SyntaxToken forEachKeyword, SyntaxToken openParenToken, ExpressionSyntax variable, SyntaxToken inKeyword, ExpressionSyntax expression, SyntaxToken closeParenToken, StatementSyntax statement)
            => ForEachVariableStatement(attributeLists: default, awaitKeyword, forEachKeyword, openParenToken, variable, inKeyword, expression, closeParenToken, statement);
    }
}
