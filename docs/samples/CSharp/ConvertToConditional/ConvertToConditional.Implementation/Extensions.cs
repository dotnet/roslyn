// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;

namespace ConvertToConditional
{
    internal static class Extensions
    {
        public static ExpressionSyntax Parenthesize(this ExpressionSyntax expression)
        {
            return SyntaxFactory.ParenthesizedExpression(expression: expression);
        }

        public static ExpressionSyntax ParenthesizeIfNeeded(this ExpressionSyntax expression)
        {
            if (expression is BinaryExpressionSyntax ||
                expression is ConditionalExpressionSyntax ||
                expression is ParenthesizedLambdaExpressionSyntax ||
                expression is SimpleLambdaExpressionSyntax)
            {
                return expression.Parenthesize();
            }

            return expression;
        }

        public static CastExpressionSyntax CastTo(this ExpressionSyntax expression, ITypeSymbol type)
        {
            return SyntaxFactory.CastExpression(
                type: SyntaxFactory.ParseTypeName(type.ToDisplayString()).WithAdditionalAnnotations(Simplifier.Annotation),
                expression: expression.ParenthesizeIfNeeded());
        }

        /// <summary>
        /// Returns true if the given statement is a <see cref="BlockSyntax"/> containing
        /// no statements (or other empty blocks).
        /// </summary>
        public static bool IsEmptyBlock(this StatementSyntax statement)
        {
            if (statement is BlockSyntax block)
            {
                if (block.Statements.Count == 0)
                {
                    return true;
                }

                if (block.Statements.Any(s => !s.IsEmptyBlock()))
                {
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns the given statement if it is not a <see cref="BlockSyntax"/>. If it is a
        /// <see cref="BlockSyntax"/>, nested statements are searched recursively until a single
        /// statement is found.
        /// </summary>
        public static StatementSyntax SingleStatementOrSelf(this StatementSyntax statement)
        {
            if (statement is BlockSyntax block)
            {
                List<StatementSyntax> statements = block.Statements.Where(s => !s.IsEmptyBlock()).ToList();

                return statements.Count == 1
                    ? block.Statements[0].SingleStatementOrSelf()
                    : null;
            }

            return statement;
        }
    }
}
