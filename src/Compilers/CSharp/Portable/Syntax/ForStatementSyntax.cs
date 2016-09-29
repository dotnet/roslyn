using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.CSharp.Syntax
{
    public partial class ForStatementSyntax
    {
        public ForStatementSyntax Update(SyntaxToken forKeyword, SyntaxToken openParenToken, VariableDeclarationSyntax declaration, SeparatedSyntaxList<ExpressionSyntax> initializers, SyntaxToken firstSemicolonToken, ExpressionSyntax condition, SyntaxToken secondSemicolonToken, SeparatedSyntaxList<ExpressionSyntax> incrementors, SyntaxToken closeParenToken, StatementSyntax statement)
        {
            return Update(ForKeyword, openParenToken, null, declaration, initializers, firstSemicolonToken, condition, secondSemicolonToken, incrementors, closeParenToken, statement);
        }
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        /// <summary>Creates a new ForStatementSyntax instance.</summary>
        public static ForStatementSyntax ForStatement(
            SyntaxToken forKeyword,
            SyntaxToken openParenToken,
            VariableDeclarationSyntax declaration,
            SeparatedSyntaxList<ExpressionSyntax> initializers,
            SyntaxToken firstSemicolonToken,
            ExpressionSyntax condition,
            SyntaxToken secondSemicolonToken,
            SeparatedSyntaxList<ExpressionSyntax> incrementors,
            SyntaxToken closeParenToken,
            StatementSyntax statement)
        {
            return ForStatement(
                forKeyword, openParenToken, null, declaration, initializers,
                firstSemicolonToken, condition, secondSemicolonToken, incrementors,
                closeParenToken, statement);
        }
    }
}

namespace Microsoft.CodeAnalysis.CSharp
{
    public partial class SyntaxFactory
    {
        /// <summary>Creates a new ForStatementSyntax instance.</summary>
        public static ForStatementSyntax ForStatement(
            VariableDeclarationSyntax declaration,
            SeparatedSyntaxList<ExpressionSyntax> initializers,
            ExpressionSyntax condition,
            SeparatedSyntaxList<ExpressionSyntax> incrementors,
            StatementSyntax statement)
        {
            return SyntaxFactory.ForStatement(
                SyntaxFactory.Token(SyntaxKind.ForKeyword),
                SyntaxFactory.Token(SyntaxKind.OpenParenToken),
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
