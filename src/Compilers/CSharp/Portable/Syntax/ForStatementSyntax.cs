// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class ForStatementSyntax
    {
        public ForStatementSyntax Update(SyntaxToken forKeyword, SyntaxToken openParenToken, VariableDeclarationSyntax declaration, SeparatedSyntaxList<ExpressionSyntax> initializers, SyntaxToken firstSemicolonToken, ExpressionSyntax condition, SyntaxToken secondSemicolonToken, SeparatedSyntaxList<ExpressionSyntax> incrementors, SyntaxToken closeParenToken, StatementSyntax statement)
        {
            return Update(
                forKeyword: forKeyword,
                openParenToken: openParenToken,
                refKeyword: this.RefKeyword,
                deconstruction: this.deconstruction,
                declaration: declaration,
                initializers: initializers,
                firstSemicolonToken: firstSemicolonToken,
                condition: condition,
                secondSemicolonToken: secondSemicolonToken,
                incrementors: incrementors,
                closeParenToken: closeParenToken,
                statement: statement);
        }
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        /// <summary>Creates a new ForStatementSyntax instance.</summary>
        public static ForStatementSyntax ForStatement(SyntaxToken forKeyword, SyntaxToken openParenToken, VariableDeclarationSyntax declaration, SeparatedSyntaxList<ExpressionSyntax> initializers, SyntaxToken firstSemicolonToken, ExpressionSyntax condition, SyntaxToken secondSemicolonToken, SeparatedSyntaxList<ExpressionSyntax> incrementors, SyntaxToken closeParenToken, StatementSyntax statement)
        {
            return ForStatement(
                forKeyword: forKeyword,
                openParenToken: openParenToken,
                refKeyword: default(SyntaxToken),
                deconstruction: null,
                declaration: declaration,
                initializers: initializers,
                firstSemicolonToken: firstSemicolonToken,
                condition: condition,
                secondSemicolonToken: secondSemicolonToken,
                incrementors: incrementors,
                closeParenToken: closeParenToken,
                statement: statement);
        }

        /// <summary>Creates a new ForStatementSyntax instance.</summary>
        public static ForStatementSyntax ForStatement(VariableDeclarationSyntax declaration, SeparatedSyntaxList<ExpressionSyntax> initializers, ExpressionSyntax condition, SeparatedSyntaxList<ExpressionSyntax> incrementors, StatementSyntax statement)
        {
            return ForStatement(
                SyntaxFactory.Token(SyntaxKind.ForKeyword),
                SyntaxFactory.Token(SyntaxKind.OpenParenToken),
                default(SyntaxToken),
                null,
                declaration,
                initializers,
                SyntaxFactory.Token(SyntaxKind.SemicolonToken),
                condition,
                SyntaxFactory.Token(SyntaxKind.SemicolonToken),
                incrementors,
                SyntaxFactory.Token(SyntaxKind.CloseParenToken),
                statement);
        }
    }
}
