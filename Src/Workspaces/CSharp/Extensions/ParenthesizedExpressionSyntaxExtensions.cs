// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class ParenthesizedExpressionSyntaxExtensions
    {
        public static bool CanRemoveParentheses(this ParenthesizedExpressionSyntax node)
        {
            var expression = node.Expression;
            var parentExpression = node.Parent as ExpressionSyntax;

            // Simplest cases:
            //   ((x)) -> (x)
            if (expression.IsKind(SyntaxKind.ParenthesizedExpression))
            {
                return true;
            }

            // Easy statement-level cases:
            //   var y = (x);           -> var y = x;
            //   if ((x))               -> if (x)
            //   return (x);            -> return x;
            //   yield return (x);      -> yield return x;
            //   throw (x);             -> throw x;
            //   switch ((x))           -> switch (x)
            //   while ((x))            -> while (x)
            //   do { } while ((x))     -> do { } while (x)
            //   for(;(x);)             -> for(;x;)
            //   foreach (var y in (x)) -> foreach (var y in x)
            //   lock ((x))             -> lock (x)
            //   using ((x))            -> using (x)
            if ((node.IsParentKind(SyntaxKind.EqualsValueClause) && ((EqualsValueClauseSyntax)node.Parent).Value == node) ||
                (node.IsParentKind(SyntaxKind.IfStatement) && ((IfStatementSyntax)node.Parent).Condition == node) ||
                (node.IsParentKind(SyntaxKind.ReturnStatement) && ((ReturnStatementSyntax)node.Parent).Expression == node) ||
                (node.IsParentKind(SyntaxKind.YieldReturnStatement) && ((YieldStatementSyntax)node.Parent).Expression == node) ||
                (node.IsParentKind(SyntaxKind.ThrowStatement) && ((ThrowStatementSyntax)node.Parent).Expression == node) ||
                (node.IsParentKind(SyntaxKind.SwitchStatement) && ((SwitchStatementSyntax)node.Parent).Expression == node) ||
                (node.IsParentKind(SyntaxKind.WhileStatement) && ((WhileStatementSyntax)node.Parent).Condition == node) ||
                (node.IsParentKind(SyntaxKind.DoStatement) && ((DoStatementSyntax)node.Parent).Condition == node) ||
                (node.IsParentKind(SyntaxKind.ForStatement) && ((ForStatementSyntax)node.Parent).Condition == node) ||
                (node.IsParentKind(SyntaxKind.ForEachStatement) && ((ForEachStatementSyntax)node.Parent).Expression == node) ||
                (node.IsParentKind(SyntaxKind.LockStatement) && ((LockStatementSyntax)node.Parent).Expression == node) ||
                (node.IsParentKind(SyntaxKind.UsingStatement) && ((UsingStatementSyntax)node.Parent).Expression == node))
            {
                return true;
            }

            // Handle expression-level ambiguities
            if (RemovalMayIntroduceCastAmbiguity(node) ||
                RemovalMayIntroduceCommaListAmbiguity(node))
            {
                return false;
            }

            // Cases:
            //   y((x)) -> y(x)
            if (node.IsParentKind(SyntaxKind.Argument) && ((ArgumentSyntax)node.Parent).Expression == node)
            {
                return true;
            }

            // Cases:
            //   {(x)} -> {x}
            if (node.Parent is InitializerExpressionSyntax)
            {
                // Assignment expressions are not allowed in initializers
                if (expression.IsAnyAssignExpression())
                {
                    return false;
                }

                return true;
            }

            // Cases:
            // where (x + 1 > 14) -> where x + 1 > 14
            if (node.Parent is QueryClauseSyntax)
            {
                return true;
            }

            // Cases:
            //   (x)   -> x
            //   (x.y) -> x.y
            if (IsSimpleOrDottedName(expression))
            {
                return true;
            }

            // Cases:
            //   ('')    -> ''
            //   ("")    -> ""
            //   (false) -> false
            //   (true)  -> true
            //   (null)  -> null
            //   (1)     -> 1
            if (expression.IsAnyLiteralExpression())
            {
                return true;
            }

            // Operator precedence cases:
            var precedence = expression.GetOperatorPrecedence();
            if (parentExpression != null)
            {
                var parentPrecedence = parentExpression.GetOperatorPrecedence();

                // Only remove if the expression's precedence is higher than its parent.
                if (parentPrecedence != OperatorPrecedence.None &&
                    precedence > parentPrecedence)
                {
                    return true;
                }

                // If the expression's precedence is the same as its parent, and both are binary expressions,
                // check for associativity and commutability.
                if (precedence != OperatorPrecedence.None && precedence == parentPrecedence)
                {
                    var binaryExpression = expression as BinaryExpressionSyntax;
                    var parentBinaryExpression = parentExpression as BinaryExpressionSyntax;
                    if (binaryExpression == null || parentBinaryExpression == null)
                    {
                        return true;
                    }

                    // Handle associate cases. Note that all binary expressions except assignment 
                    // and null-coalescing are left associative.
                    if (parentBinaryExpression.Left == node)
                    {
                        if (!parentBinaryExpression.IsAnyAssignExpression() &&
                            !parentBinaryExpression.IsKind(SyntaxKind.CoalesceExpression))
                        {
                            return true;
                        }
                    }
                    else if (parentBinaryExpression.Right == node)
                    {
                        if (parentBinaryExpression.IsAnyAssignExpression() ||
                            parentBinaryExpression.IsKind(SyntaxKind.CoalesceExpression))
                        {
                            return true;
                        }
                    }

                    // If both the expression and it's parent are binary expressions and their kinds
                    // are the same, check to see if they are commutative (e.g. + or *).
                    if (parentBinaryExpression.IsKind(SyntaxKind.AddExpression, SyntaxKind.MultiplyExpression) &&
                        expression.CSharpKind() == parentExpression.CSharpKind())
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool RemovalMayIntroduceCastAmbiguity(ParenthesizedExpressionSyntax node)
        {
            // Be careful not to break the special case around (x)(-y)
            // as defined in section 7.7.6 of the C# language specification.

            if (node.IsParentKind(SyntaxKind.CastExpression))
            {
                var castExpression = (CastExpressionSyntax)node.Parent;
                if (castExpression.Type is PredefinedTypeSyntax)
                {
                    return false;
                }

                var expression = node.Expression;

                if (expression.IsKind(SyntaxKind.UnaryMinusExpression))
                {
                    return true;
                }

                if (expression.IsKind(SyntaxKind.NumericLiteralExpression))
                {
                    var numericLiteral = (LiteralExpressionSyntax)expression;
                    if (numericLiteral.Token.ValueText.StartsWith("-"))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool RemovalMayIntroduceCommaListAmbiguity(ParenthesizedExpressionSyntax node)
        {
            if (IsSimpleOrDottedName(node.Expression))
            {
                // We can't remove parentheses from an identifier name in the following cases:
                //   F((x) < x, x > (1 + 2))
                //   F(x < (x), x > (1 + 2))
                //   F(x < x, (x) > (1 + 2))
                //   {(x) < x, x > (1 + 2)}
                //   {x < (x), x > (1 + 2)}
                //   {x < x, (x) > (1 + 2)}

                var binaryExpression = node.Parent as BinaryExpressionSyntax;
                if (binaryExpression != null &&
                    binaryExpression.IsKind(SyntaxKind.LessThanExpression, SyntaxKind.GreaterThanExpression) &&
                    (binaryExpression.IsParentKind(SyntaxKind.Argument) || binaryExpression.Parent is InitializerExpressionSyntax))
                {
                    if (binaryExpression.IsKind(SyntaxKind.LessThanExpression))
                    {
                        if ((binaryExpression.Left == node && IsSimpleOrDottedName(binaryExpression.Right)) ||
                            (binaryExpression.Right == node && IsSimpleOrDottedName(binaryExpression.Left)))
                        {
                            if (IsNextExpressionPotentiallyAmbiguous(binaryExpression))
                            {
                                return true;
                            }
                        }

                        return false;
                    }
                    else if (binaryExpression.IsKind(SyntaxKind.GreaterThanExpression))
                    {
                        if (binaryExpression.Left == node &&
                            binaryExpression.Right.IsKind(SyntaxKind.ParenthesizedExpression, SyntaxKind.CastExpression))
                        {
                            if (IsPreviousExpressionPotentiallyAmbiguous(binaryExpression))
                            {
                                return true;
                            }
                        }

                        return false;
                    }
                }
            }
            else if (node.Expression.IsKind(SyntaxKind.LessThanExpression))
            {
                // We can't remove parentheses from a less-than expression in the following cases:
                //   F((x < x), x > (1 + 2))
                //   {(x < x), x > (1 + 2)}

                var lessThanExpression = (BinaryExpressionSyntax)node.Expression;
                if (IsNextExpressionPotentiallyAmbiguous(node))
                {
                    return true;
                }

                return false;
            }
            else if (node.Expression.IsKind(SyntaxKind.GreaterThanExpression))
            {
                // We can't remove parentheses from a greater-than expression in the following cases:
                //   F(x < x, (x > (1 + 2)))
                //   {x < x, (x > (1 + 2))}

                var greaterThanExpression = (BinaryExpressionSyntax)node.Expression;
                if (IsPreviousExpressionPotentiallyAmbiguous(node))
                {
                    return true;
                }

                return false;
            }

            return false;
        }

        private static bool IsPreviousExpressionPotentiallyAmbiguous(ExpressionSyntax node)
        {
            ExpressionSyntax previousExpression = null;

            if (node.IsParentKind(SyntaxKind.Argument))
            {
                var argument = (ArgumentSyntax)node.Parent;
                var argumentList = argument.Parent as ArgumentListSyntax;
                if (argumentList != null)
                {
                    var argumentIndex = argumentList.Arguments.IndexOf(argument);
                    if (argumentIndex > 0)
                    {
                        previousExpression = argumentList.Arguments[argumentIndex - 1].Expression;
                    }
                }
            }
            else if (node.Parent is InitializerExpressionSyntax)
            {
                var initializer = (InitializerExpressionSyntax)node.Parent;
                var expressionIndex = initializer.Expressions.IndexOf(node);
                if (expressionIndex > 0)
                {
                    previousExpression = initializer.Expressions[expressionIndex - 1];
                }
            }

            if (previousExpression == null ||
                !previousExpression.IsKind(SyntaxKind.LessThanExpression))
            {
                return false;
            }

            var lessThanExpression = (BinaryExpressionSyntax)previousExpression;

            return IsSimpleOrDottedName(lessThanExpression.Left)
                && IsSimpleOrDottedName(lessThanExpression.Right);
        }

        private static bool IsNextExpressionPotentiallyAmbiguous(ExpressionSyntax node)
        {
            ExpressionSyntax nextExpression = null;

            if (node.IsParentKind(SyntaxKind.Argument))
            {
                var argument = (ArgumentSyntax)node.Parent;
                var argumentList = argument.Parent as ArgumentListSyntax;
                if (argumentList != null)
                {
                    var argumentIndex = argumentList.Arguments.IndexOf(argument);
                    if (argumentIndex >= 0 && argumentIndex < argumentList.Arguments.Count - 1)
                    {
                        nextExpression = argumentList.Arguments[argumentIndex + 1].Expression;
                    }
                }
            }
            else if (node.Parent is InitializerExpressionSyntax)
            {
                var initializer = (InitializerExpressionSyntax)node.Parent;
                var expressionIndex = initializer.Expressions.IndexOf(node);
                if (expressionIndex >= 0 && expressionIndex < initializer.Expressions.Count - 1)
                {
                    nextExpression = initializer.Expressions[expressionIndex + 1];
                }
            }

            if (nextExpression == null ||
                !nextExpression.IsKind(SyntaxKind.GreaterThanExpression))
            {
                return false;
            }

            var greaterThanExpression = (BinaryExpressionSyntax)nextExpression;

            return IsSimpleOrDottedName(greaterThanExpression.Left)
                && greaterThanExpression.Right.IsKind(SyntaxKind.ParenthesizedExpression);
        }

        private static bool IsSimpleOrDottedName(ExpressionSyntax expression)
        {
            return expression.IsKind(
                SyntaxKind.IdentifierName,
                SyntaxKind.QualifiedName,
                SyntaxKind.SimpleMemberAccessExpression);
        }
    }
}
