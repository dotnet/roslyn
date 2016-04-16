// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;
using System.Collections.Generic;

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
            if (expression.IsKind(SyntaxKind.ParenthesizedExpression) ||
                parentExpression.IsKind(SyntaxKind.ParenthesizedExpression))
            {
                return true;
            }

            // (x); -> x;
            if (node.IsParentKind(SyntaxKind.ExpressionStatement))
            {
                return true;
            }

            // Don't change (x?.Count).GetValueOrDefault() to x?.Count.GetValueOrDefault()
            if (expression.IsKind(SyntaxKind.ConditionalAccessExpression) && parentExpression is MemberAccessExpressionSyntax)
            {
                return false;
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
            //   catch when ((x))       -> catch when (x)
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
                (node.IsParentKind(SyntaxKind.UsingStatement) && ((UsingStatementSyntax)node.Parent).Expression == node) ||
                (node.IsParentKind(SyntaxKind.CatchFilterClause) && ((CatchFilterClauseSyntax)node.Parent).FilterExpression == node))
            {
                return true;
            }

            // Handle expression-level ambiguities
            if (RemovalMayIntroduceCastAmbiguity(node) ||
                RemovalMayIntroduceCommaListAmbiguity(node) ||
                RemovalMayIntroduceInterpolationAmbiguity(node))
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
            //   $"{(x)}" -> $"{x}"
            if (node.IsParentKind(SyntaxKind.Interpolation))
            {
                return true;
            }

            // Cases:
            //   ($"{x}") -> $"{x}"
            if (expression.IsKind(SyntaxKind.InterpolatedStringExpression))
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
            // - If the parent is not an expression, do not remove parentheses
            // - Otherwise, parentheses may be removed if doing so does not change operator associations.
            return parentExpression != null
                ? !RemovalChangesAssociation(node, expression, parentExpression)
                : false;
        }

        private static readonly ObjectPool<Stack<SyntaxNode>> s_nodeStackPool = new ObjectPool<Stack<SyntaxNode>>(() => new Stack<SyntaxNode>());

        private static bool RemovalMayIntroduceInterpolationAmbiguity(ParenthesizedExpressionSyntax node)
        {
            // First, find the parenting interpolation. If we find a parenthesize expression first,
            // we can bail out early.
            InterpolationSyntax interpolation = null;
            foreach (var ancestor in node.Parent.AncestorsAndSelf())
            {
                switch (ancestor.Kind())
                {
                    case SyntaxKind.ParenthesizedExpression:
                        return false;
                    case SyntaxKind.Interpolation:
                        interpolation = (InterpolationSyntax)ancestor;
                        break;
                }
            }

            if (interpolation == null)
            {
                return false;
            }

            // In order determine whether removing this parenthesized expression will introduce a
            // parsing ambiguity, we must dig into the child tokens and nodes to determine whether
            // they include any : or :: tokens. If they do, we can't remove the parentheses because
            // the parser would assume that the first : would begin the format clause of the interpolation.

            var stack = s_nodeStackPool.AllocateAndClear();
            try
            {
                stack.Push(node.Expression);

                while (stack.Count > 0)
                {
                    var expression = stack.Pop();

                    foreach (var nodeOrToken in expression.ChildNodesAndTokens())
                    {
                        // Note: There's no need drill into other parenthesized expressions, since any colons in them would be unambiguous.
                        if (nodeOrToken.IsNode && !nodeOrToken.IsKind(SyntaxKind.ParenthesizedExpression))
                        {
                            stack.Push(nodeOrToken.AsNode());
                        }
                        else if (nodeOrToken.IsToken)
                        {
                            if (nodeOrToken.IsKind(SyntaxKind.ColonToken) || nodeOrToken.IsKind(SyntaxKind.ColonColonToken))
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            finally
            {
                s_nodeStackPool.ClearAndFree(stack);
            }

            return false;
        }

        private static bool RemovalChangesAssociation(ParenthesizedExpressionSyntax node, ExpressionSyntax expression, ExpressionSyntax parentExpression)
        {
            var precedence = expression.GetOperatorPrecedence();
            var parentPrecedence = parentExpression.GetOperatorPrecedence();
            if (precedence == OperatorPrecedence.None || parentPrecedence == OperatorPrecedence.None)
            {
                // Be conservative if the expression or its parent has no precedence.
                return true;
            }

            if (precedence > parentPrecedence)
            {
                // Association never changes if the expression's precedence is higher than its parent.
                return false;
            }
            else if (precedence < parentPrecedence)
            {
                // Association always changes if the expression's precedence is lower that its parent.
                return true;
            }
            else if (precedence == parentPrecedence)
            {
                // If the expression's precedence is the same as its parent, and both are binary expressions,
                // check for associativity and commutability.

                if (!(expression is BinaryExpressionSyntax || expression is AssignmentExpressionSyntax))
                {
                    // If the expression is not a binary expression, association never changes.
                    return false;
                }

                var parentBinaryExpression = parentExpression as BinaryExpressionSyntax;
                if (parentBinaryExpression != null)
                {
                    // If both the expression and its parent are binary expressions and their kinds
                    // are the same, check to see if they are commutative (e.g. + or *).
                    if (parentBinaryExpression.IsKind(SyntaxKind.AddExpression, SyntaxKind.MultiplyExpression) &&
                        node.Expression.Kind() == parentBinaryExpression.Kind())
                    {
                        return false;
                    }

                    // Null-coalescing is right associative; removing parens from the LHS changes the association.
                    if (parentExpression.IsKind(SyntaxKind.CoalesceExpression))
                    {
                        return parentBinaryExpression.Left == node;
                    }

                    // All other binary operators are left associative; removing parens from the RHS changes the association.
                    return parentBinaryExpression.Right == node;
                }

                var parentAssignmentExpression = parentExpression as AssignmentExpressionSyntax;
                if (parentAssignmentExpression != null)
                {
                    // Assignment expressions are right associative; removing parens from the LHS changes the association.
                    return parentAssignmentExpression.Left == node;
                }

                // If the parent is not a binary expression, association never changes.
                return false;
            }

            throw ExceptionUtilities.Unreachable;
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
                    if (numericLiteral.Token.ValueText.StartsWith("-", StringComparison.Ordinal))
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

            return (IsSimpleOrDottedName(lessThanExpression.Left)
                    || lessThanExpression.Left.IsKind(SyntaxKind.CastExpression))
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
                && (greaterThanExpression.Right.IsKind(SyntaxKind.ParenthesizedExpression)
                    || greaterThanExpression.Right.IsKind(SyntaxKind.CastExpression));
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
