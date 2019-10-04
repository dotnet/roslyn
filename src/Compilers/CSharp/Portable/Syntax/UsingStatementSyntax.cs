// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class UsingStatementSyntax
    {
        public UsingStatementSyntax Update(SyntaxToken usingKeyword, SyntaxToken openParenToken, VariableDeclarationSyntax declaration, ExpressionSyntax expression, SyntaxToken closeParenToken, StatementSyntax statement)
            => Update(awaitKeyword: default, usingKeyword, openParenToken, declaration, expression, closeParenToken, statement);

        public UsingStatementSyntax Update(SyntaxToken awaitKeyword, SyntaxToken usingKeyword, SyntaxToken openParenToken, VariableDeclarationSyntax declaration, ExpressionSyntax expression, SyntaxToken closeParenToken, StatementSyntax statement)
            => Update(attributeLists: default, awaitKeyword, usingKeyword, openParenToken, declaration, expression, closeParenToken, statement);
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        public static UsingStatementSyntax UsingStatement(SyntaxToken usingKeyword, SyntaxToken openParenToken, VariableDeclarationSyntax declaration, ExpressionSyntax expression, SyntaxToken closeParenToken, StatementSyntax statement)
            => UsingStatement(awaitKeyword: default, usingKeyword, openParenToken, declaration, expression, closeParenToken, statement);

        public static UsingStatementSyntax UsingStatement(SyntaxToken awaitKeyword, SyntaxToken usingKeyword, SyntaxToken openParenToken, VariableDeclarationSyntax declaration, ExpressionSyntax expression, SyntaxToken closeParenToken, StatementSyntax statement)
            => UsingStatement(attributeLists: default, awaitKeyword, usingKeyword, openParenToken, declaration, expression, closeParenToken, statement);

        public static UsingStatementSyntax UsingStatement(VariableDeclarationSyntax declaration, ExpressionSyntax expression, StatementSyntax statement)
            => UsingStatement(attributeLists: default, declaration, expression, statement);
    }
}
