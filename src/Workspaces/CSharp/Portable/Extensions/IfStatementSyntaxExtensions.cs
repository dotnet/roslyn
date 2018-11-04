// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    using static SyntaxFactory;

    internal static class IfStatementSyntaxExtensions
    {
        /// <summary>
        /// Returns the 'header' span of the if statement.  Header-span goes from the <see
        /// cref="SyntaxNode.SpanStart"/> if the if-statement up to the end of its <see
        /// cref="IfStatementSyntax.CloseParenToken"/> if it has one, or the end of its <see
        /// cref="IfStatementSyntax.Condition"/> otherwise.
        /// </summary>
        public static TextSpan GetHeaderSpan(this IfStatementSyntax ifStatement)
        {
            var end = ifStatement.CloseParenToken == default
                ? (SyntaxNodeOrToken)ifStatement.Condition
                : ifStatement.CloseParenToken;

            return TextSpan.FromBounds(ifStatement.SpanStart, end.Span.End);
        }

        /// <summary>
        /// Returns true if this is an `if !(...)` guard-if statement.
        /// </summary>
        public static bool IsIfGuard(this IfStatementSyntax ifStatement)
            => ifStatement.OpenParenToken == default;

        /// <summary>
        /// Adds parentheses around the condition of this if-statement if the if-statement is
        /// missince parentheses, and they would be necessary for the if-statement to be valid.
        /// Parentheses are necessary if this is before C# 8, or if this is after C# 8, but the
        /// condition would not be a legal guard clause condition. i.e. the condition is not of
        /// the form `!(...)`.
        /// </summary>
        public static IfStatementSyntax WithParenthesesIfNecessary(this IfStatementSyntax ifStatement)
        {
            var condition = ifStatement.Condition;
            if (!ifStatement.IsIfGuard())
            {
                return ifStatement;
            }

            var options = (CSharpParseOptions)ifStatement.SyntaxTree.Options;
            if (options.LanguageVersion >= LanguageVersion.CSharp8 &&
                ifStatement.Condition.IsValidIfGuardCondition())
            {
                return ifStatement;
            }

            return ifStatement.WithCondition(condition.WithoutTrivia())
                              .WithOpenParenToken(Token(SyntaxKind.OpenParenToken).WithLeadingTrivia(condition.GetLeadingTrivia()))
                              .WithCloseParenToken(Token(SyntaxKind.CloseParenToken).WithLeadingTrivia(condition.GetTrailingTrivia()));
        }
    }
}
