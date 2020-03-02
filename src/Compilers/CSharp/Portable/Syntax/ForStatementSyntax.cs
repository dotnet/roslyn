// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class ForStatementSyntax
    {
        public ForStatementSyntax Update(SyntaxToken forKeyword, SyntaxToken openParenToken, VariableDeclarationSyntax declaration, SeparatedSyntaxList<ExpressionSyntax> initializers, SyntaxToken firstSemicolonToken, ExpressionSyntax condition, SyntaxToken secondSemicolonToken, SeparatedSyntaxList<ExpressionSyntax> incrementors, SyntaxToken closeParenToken, StatementSyntax statement)
            => Update(attributeLists: default, forKeyword, openParenToken, declaration, initializers, firstSemicolonToken, condition, secondSemicolonToken, incrementors, closeParenToken, statement);
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        public static ForStatementSyntax ForStatement(VariableDeclarationSyntax declaration, SeparatedSyntaxList<ExpressionSyntax> initializers, ExpressionSyntax condition, SeparatedSyntaxList<ExpressionSyntax> incrementors, StatementSyntax statement)
            => ForStatement(attributeLists: default, declaration, initializers, condition, incrementors, statement);

        public static ForStatementSyntax ForStatement(SyntaxToken forKeyword, SyntaxToken openParenToken, VariableDeclarationSyntax declaration, SeparatedSyntaxList<ExpressionSyntax> initializers, SyntaxToken firstSemicolonToken, ExpressionSyntax condition, SyntaxToken secondSemicolonToken, SeparatedSyntaxList<ExpressionSyntax> incrementors, SyntaxToken closeParenToken, StatementSyntax statement)
            => ForStatement(attributeLists: default, forKeyword, openParenToken, declaration, initializers, firstSemicolonToken, condition, secondSemicolonToken, incrementors, closeParenToken, statement);
    }
}

