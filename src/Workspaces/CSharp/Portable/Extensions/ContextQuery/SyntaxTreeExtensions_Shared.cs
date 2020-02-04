// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Extensions.ContextQuery
{
    internal static partial class SyntaxTreeExtensions
    {
        /// <summary>
        /// Are you possibly typing a tuple type or expression?
        /// This is used to suppress colon as a completion trigger (so that you can type element names).
        /// This is also used to recommend some keywords (like var).
        /// </summary>
        public static bool IsPossibleTupleContext(this SyntaxTree syntaxTree, SyntaxToken leftToken, int position)
        {
            leftToken = leftToken.GetPreviousTokenIfTouchingWord(position);

            // ($$
            // (a, $$
            if (IsPossibleTupleOpenParenOrComma(leftToken))
            {
                return true;
            }

            // ((a, b) $$
            // (..., (a, b) $$
            if (leftToken.IsKind(SyntaxKind.CloseParenToken))
            {
                if (leftToken.Parent.IsKind(
                        SyntaxKind.ParenthesizedExpression,
                        SyntaxKind.TupleExpression,
                        SyntaxKind.TupleType))
                {
                    var possibleCommaOrParen = FindTokenOnLeftOfNode(leftToken.Parent);
                    if (IsPossibleTupleOpenParenOrComma(possibleCommaOrParen))
                    {
                        return true;
                    }
                }
            }

            // (a $$
            // (..., b $$
            if (leftToken.IsKind(SyntaxKind.IdentifierToken))
            {
                var possibleCommaOrParen = FindTokenOnLeftOfNode(leftToken.Parent);
                if (IsPossibleTupleOpenParenOrComma(possibleCommaOrParen))
                {
                    return true;
                }
            }

            // (a.b $$
            // (..., a.b $$
            if (leftToken.IsKind(SyntaxKind.IdentifierToken) &&
                leftToken.Parent.IsKind(SyntaxKind.IdentifierName) &&
                leftToken.Parent.IsParentKind(SyntaxKind.QualifiedName, SyntaxKind.SimpleMemberAccessExpression))
            {
                var possibleCommaOrParen = FindTokenOnLeftOfNode(leftToken.Parent.Parent);
                if (IsPossibleTupleOpenParenOrComma(possibleCommaOrParen))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsPossibleTupleOpenParenOrComma(this SyntaxToken possibleCommaOrParen)
        {
            if (!possibleCommaOrParen.IsKind(SyntaxKind.OpenParenToken, SyntaxKind.CommaToken))
            {
                return false;
            }

            if (possibleCommaOrParen.Parent.IsKind(
                    SyntaxKind.ParenthesizedExpression,
                    SyntaxKind.TupleExpression,
                    SyntaxKind.TupleType,
                    SyntaxKind.CastExpression))
            {
                return true;
            }

            // in script
            if (possibleCommaOrParen.Parent.IsKind(SyntaxKind.ParameterList) &&
                possibleCommaOrParen.Parent.IsParentKind(SyntaxKind.ParenthesizedLambdaExpression))
            {
                var parenthesizedLambda = (ParenthesizedLambdaExpressionSyntax)possibleCommaOrParen.Parent.Parent;
                if (parenthesizedLambda.ArrowToken.IsMissing)
                {
                    return true;
                }
            }

            return false;
        }

        private static SyntaxToken FindTokenOnLeftOfNode(SyntaxNode node)
        {
            return node.FindTokenOnLeftOfPosition(node.SpanStart);
        }
    }
}
