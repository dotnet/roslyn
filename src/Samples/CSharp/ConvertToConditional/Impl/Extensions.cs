// *********************************************************
//
// Copyright © Microsoft Corporation
//
// Licensed under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in
// compliance with the License. You may obtain a copy of
// the License at
//
// http://www.apache.org/licenses/LICENSE-2.0 
//
// THIS CODE IS PROVIDED ON AN *AS IS* BASIS, WITHOUT WARRANTIES
// OR CONDITIONS OF ANY KIND, EITHER EXPRESS OR IMPLIED,
// INCLUDING WITHOUT LIMITATION ANY IMPLIED WARRANTIES
// OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY OR NON-INFRINGEMENT.
//
// See the Apache 2 License for the specific language
// governing permissions and limitations under the License.
//
// *********************************************************

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.Text;

namespace ConvertToConditionalCS
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
            var block = statement as BlockSyntax;
            if (block != null)
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
            var block = statement as BlockSyntax;
            if (block != null)
            {
                var statements = block.Statements.Where(s => !s.IsEmptyBlock()).ToList();

                return statements.Count == 1
                    ? block.Statements[0].SingleStatementOrSelf()
                    : null;
            }

            return statement;
        }
    }
}
