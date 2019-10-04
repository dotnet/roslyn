// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class ForEachStatementSyntax
    {
        public ForEachStatementSyntax Update(SyntaxToken forEachKeyword, SyntaxToken openParenToken, TypeSyntax type, SyntaxToken identifier, SyntaxToken inKeyword, ExpressionSyntax expression, SyntaxToken closeParenToken, StatementSyntax statement)
            => Update(awaitKeyword: default, forEachKeyword, openParenToken, type, identifier, inKeyword, expression, closeParenToken, statement);

        public ForEachStatementSyntax Update(SyntaxToken awaitKeyword, SyntaxToken forEachKeyword, SyntaxToken openParenToken, TypeSyntax type, SyntaxToken identifier, SyntaxToken inKeyword, ExpressionSyntax expression, SyntaxToken closeParenToken, StatementSyntax statement)
            => Update(attributeLists: default, awaitKeyword, forEachKeyword, openParenToken, type, identifier, inKeyword, expression, closeParenToken, statement);
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        public static ForEachStatementSyntax ForEachStatement(SyntaxToken forEachKeyword, SyntaxToken openParenToken, TypeSyntax type, SyntaxToken identifier, SyntaxToken inKeyword, ExpressionSyntax expression, SyntaxToken closeParenToken, StatementSyntax statement)
            => ForEachStatement(awaitKeyword: default, forEachKeyword, openParenToken, type, identifier, inKeyword, expression, closeParenToken, statement);

        public static ForEachStatementSyntax ForEachStatement(SyntaxToken awaitKeyword, SyntaxToken forEachKeyword, SyntaxToken openParenToken, TypeSyntax type, SyntaxToken identifier, SyntaxToken inKeyword, ExpressionSyntax expression, SyntaxToken closeParenToken, StatementSyntax statement)
            => ForEachStatement(attributeLists: default, awaitKeyword, forEachKeyword, openParenToken, type, identifier, inKeyword, expression, closeParenToken, statement);
    }
}
