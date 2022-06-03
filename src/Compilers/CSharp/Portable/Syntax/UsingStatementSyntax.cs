// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class UsingStatementSyntax
    {
        public UsingStatementSyntax Update(SyntaxToken usingKeyword, SyntaxToken openParenToken, VariableDeclarationSyntax? declaration, ExpressionSyntax? expression, SyntaxToken closeParenToken, StatementSyntax statement)
            => Update(AwaitKeyword, usingKeyword, openParenToken, declaration, expression, closeParenToken, statement);

        public UsingStatementSyntax Update(SyntaxToken awaitKeyword, SyntaxToken usingKeyword, SyntaxToken openParenToken, VariableDeclarationSyntax? declaration, ExpressionSyntax? expression, SyntaxToken closeParenToken, StatementSyntax statement)
            => Update(AttributeLists, awaitKeyword, usingKeyword, openParenToken, declaration, expression, closeParenToken, statement);
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        public static UsingStatementSyntax UsingStatement(SyntaxToken usingKeyword, SyntaxToken openParenToken, VariableDeclarationSyntax? declaration, ExpressionSyntax? expression, SyntaxToken closeParenToken, StatementSyntax statement)
            => UsingStatement(awaitKeyword: default, usingKeyword, openParenToken, declaration, expression, closeParenToken, statement);

        public static UsingStatementSyntax UsingStatement(SyntaxToken awaitKeyword, SyntaxToken usingKeyword, SyntaxToken openParenToken, VariableDeclarationSyntax? declaration, ExpressionSyntax? expression, SyntaxToken closeParenToken, StatementSyntax statement)
            => UsingStatement(attributeLists: default, awaitKeyword, usingKeyword, openParenToken, declaration, expression, closeParenToken, statement);

        public static UsingStatementSyntax UsingStatement(VariableDeclarationSyntax? declaration, ExpressionSyntax? expression, StatementSyntax statement)
            => UsingStatement(attributeLists: default, declaration, expression, statement);
    }
}
