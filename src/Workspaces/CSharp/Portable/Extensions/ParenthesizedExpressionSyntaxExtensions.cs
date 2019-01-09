// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Extensions;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Extensions
{
    internal static class ParenthesizedExpressionSyntaxExtensions
    {
        public static bool CanRemoveParentheses(this ParenthesizedExpressionSyntax node, SemanticModel semanticModel)
        {
            if (node.OpenParenToken.IsMissing || node.CloseParenToken.IsMissing)
            {
                // int x = (3;
                return false;
            }

            var expression = node.Expression;

            // The 'direct' expression that contains this parenthesized node.  Note: in the case
            // of code like: ```x is (y)``` there is an intermediary 'no-syntax' 'ConstantPattern'
            // node between the 'is-pattern' node and the parenthesized expression.  So we manually
            // jump past that as, for all intents and purposes, we want to consider the 'is' expression
            // as the parent expression of the (y) expression.
            var parentExpression = node.IsParentKind(SyntaxKind.ConstantPattern)
                ? node.Parent.Parent as ExpressionSyntax
                : node.Parent as ExpressionSyntax;

            // Have to be careful if we would remove parens and cause a + and a + to become a ++.
            // (same with - as well).
            var tokenBeforeParen = node.GetFirstToken().GetPreviousToken();
            var tokenAfterParen = node.Expression.GetFirstToken();
            var previousChar = tokenBeforeParen.Text.LastOrDefault();
            var nextChar = tokenAfterParen.Text.FirstOrDefault();

            if ((previousChar == '+' && nextChar == '+') ||
                (previousChar == '-' && nextChar == '-'))
            {
                return false;
            }

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

            // => (x)   ->   => x
            if (node.IsParentKind(SyntaxKind.ArrowExpressionClause))
            {
                return true;
            }

            // checked((x)) -> checked(x)
            if (node.IsParentKind(SyntaxKind.CheckedExpression) ||
                node.IsParentKind(SyntaxKind.UncheckedExpression))
            {
                return true;
            }
            // ((x, y)) -> (x, y)
            if (expression.IsKind(SyntaxKind.TupleExpression))
            {
                return true;
            }

            // int Prop => (x); -> int Prop => x;
            if (node.Parent is ArrowExpressionClauseSyntax arrowExpressionClause && arrowExpressionClause.Expression == node)
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
            //   var (y, z) = (x);      -> var (y, z) = x;
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
                (node.IsParentKind(SyntaxKind.ForEachStatement, SyntaxKind.ForEachVariableStatement) && ((CommonForEachStatementSyntax)node.Parent).Expression == node) ||
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
            //   (C)(this) -> (C)this
            if (node.IsParentKind(SyntaxKind.CastExpression) && expression.IsKind(SyntaxKind.ThisExpression))
            {
                return true;
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
            //   new {(x)} -> {x}
            //   new { a = (x)} -> { a = x }
            //   new { a = (x = c)} -> { a = x = c }
            if (node.Parent is AnonymousObjectMemberDeclaratorSyntax anonymousDeclarator)
            {
                // Assignment expressions are not allowed unless member is named
                if (anonymousDeclarator.NameEquals == null && expression.IsAnyAssignExpression())
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

            // x ?? (throw ...) -> x ?? throw ...
            if (expression.IsKind(SyntaxKind.ThrowExpression) &&
                node.IsParentKind(SyntaxKind.CoalesceExpression) &&
                ((BinaryExpressionSyntax)node.Parent).Right == node)
            {
                return true;
            }

            // case (x): -> case x:
            if (node.IsParentKind(SyntaxKind.CaseSwitchLabel))
            {
                return true;
            }

            // case (x) when y: -> case x when y:
            if (node.IsParentKind(SyntaxKind.ConstantPattern) &&
                node.Parent.IsParentKind(SyntaxKind.CasePatternSwitchLabel))
            {
                return true;
            }

            // case x when (y): -> case x when y:
            if (node.IsParentKind(SyntaxKind.WhenClause))
            {
                return true;
            }

            // #if (x)   ->   #if x
            if (node.Parent is DirectiveTriviaSyntax)
            {
                return true;
            }

            // If we have: (X)(++x) or (X)(--x), we don't want to remove the parens. doing so can
            // make the ++/-- now associate with the previous part of the cast expression.
            if (parentExpression.IsKind(SyntaxKind.CastExpression))
            {
                if (expression.IsKind(SyntaxKind.PreIncrementExpression) ||
                    expression.IsKind(SyntaxKind.PreDecrementExpression))
                {
                    return false;
                }
            }

            // (condition ? ref a : ref b ) = SomeValue, parenthesis can't be removed for when conditional expression appears at left
            // This syntax is only allowed since C# 7.2
            if (expression.IsKind(SyntaxKind.ConditionalExpression) &&
                node.IsLeftSideOfAnyAssignExpression())
            {
                return false;
            }

            // Operator precedence cases:
            // - If the parent is not an expression, do not remove parentheses
            // - Otherwise, parentheses may be removed if doing so does not change operator associations.
            return parentExpression != null && !RemovalChangesAssociation(node, parentExpression, semanticModel);
        }

        private static readonly ObjectPool<Stack<SyntaxNode>> s_nodeStackPool = new ObjectPool<Stack<SyntaxNode>>(() => new Stack<SyntaxNode>());

        private static bool RemovalMayIntroduceInterpolationAmbiguity(ParenthesizedExpressionSyntax node)
        {
            // First, find the parenting interpolation. If we find a parenthesize expression first,
            // we can bail out early.
            InterpolationSyntax interpolation = null;
            foreach (var ancestor in node.Parent.AncestorsAndSelf())
            {
                if (ancestor.IsKind(SyntaxKind.ParenthesizedExpression))
                {
                    return false;
                }

                if (ancestor.IsKind(SyntaxKind.Interpolation))
                {
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

        private static bool RemovalChangesAssociation(
            ParenthesizedExpressionSyntax node, ExpressionSyntax parentExpression, SemanticModel semanticModel)
        {
            var expression = node.Expression;
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

                if (parentExpression is BinaryExpressionSyntax parentBinaryExpression)
                {
                    // If both the expression and its parent are binary expressions and their kinds
                    // are the same, and the parenthesized expression is on hte right and the 
                    // operation is associative, it can sometimes be safe to remove these parens.
                    //
                    // i.e. if you have "a && (b && c)" it can be converted to "a && b && c" 
                    // as that new interpretation "(a && b) && c" operates the exact same way at 
                    // runtime.
                    //
                    // Specifically: 
                    //  1) the operands are still executed in the same order: a, b, then c.
                    //     So even if they have side effects, it will not matter.
                    //  2) the same shortcircuiting happens.
                    //  3) for logical operators the result will always be the same (there are 
                    //     additional conditions that are checked for non-logical operators).
                    if (IsAssociative(parentBinaryExpression.Kind()) &&
                        node.Expression.Kind() == parentBinaryExpression.Kind() &&
                        parentBinaryExpression.Right == node)
                    {
                        return !node.IsSafeToChangeAssociativity(
                            node.Expression, parentBinaryExpression.Left,
                            parentBinaryExpression.Right, semanticModel);
                    }

                    // Null-coalescing is right associative; removing parens from the LHS changes the association.
                    if (parentExpression.IsKind(SyntaxKind.CoalesceExpression))
                    {
                        return parentBinaryExpression.Left == node;
                    }

                    // All other binary operators are left associative; removing parens from the RHS changes the association.
                    return parentBinaryExpression.Right == node;
                }

                if (parentExpression is AssignmentExpressionSyntax parentAssignmentExpression)
                {
                    // Assignment expressions are right associative; removing parens from the LHS changes the association.
                    return parentAssignmentExpression.Left == node;
                }

                // If the parent is not a binary expression, association never changes.
                return false;
            }

            throw ExceptionUtilities.Unreachable;
        }

        private static bool IsAssociative(SyntaxKind kind)
        {
            switch (kind)
            {
                case SyntaxKind.AddExpression:
                case SyntaxKind.MultiplyExpression:
                case SyntaxKind.BitwiseOrExpression:
                case SyntaxKind.ExclusiveOrExpression:
                case SyntaxKind.LogicalOrExpression:
                case SyntaxKind.BitwiseAndExpression:
                case SyntaxKind.LogicalAndExpression:
                    return true;
            }

            return false;
        }

        private static bool RemovalMayIntroduceCastAmbiguity(ParenthesizedExpressionSyntax node)
        {
            // Be careful not to break the special case around (x)(-y)
            // as defined in section 7.7.6 of the C# language specification.
            //
            // cases we can't remove the parens for are:
            //
            //      (x)(+y)
            //      (x)(-y)
            //      (x)(&y) // unsafe code
            //      (x)(*y) // unsafe code
            //
            // Note: we can remove the parens if the (x) part is unambiguously a type.
            // i.e. if it something like:
            //
            //      (int)(...)
            //      (x[])(...)
            //      (X*)(...)
            //      (X?)(...)
            //      (global::X)(...)

            if (node.IsParentKind(SyntaxKind.CastExpression))
            {
                var castExpression = (CastExpressionSyntax)node.Parent;
                if (castExpression.Type.IsKind(
                        SyntaxKind.PredefinedType,
                        SyntaxKind.ArrayType,
                        SyntaxKind.PointerType,
                        SyntaxKind.NullableType))
                {
                    return false;
                }

                if (castExpression.Type is NameSyntax name && StartsWithAlias(name))
                {
                    return false;
                }

                var expression = node.Expression;

                if (expression.IsKind(
                        SyntaxKind.UnaryMinusExpression,
                        SyntaxKind.UnaryPlusExpression,
                        SyntaxKind.PointerIndirectionExpression,
                        SyntaxKind.AddressOfExpression))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool StartsWithAlias(NameSyntax name)
        {
            if (name.IsKind(SyntaxKind.AliasQualifiedName))
            {
                return true;
            }

            if (name is QualifiedNameSyntax qualifiedName)
            {
                return StartsWithAlias(qualifiedName.Left);
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

                if (node.Parent is BinaryExpressionSyntax binaryExpression &&
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
                if (argument.Parent is ArgumentListSyntax argumentList)
                {
                    var argumentIndex = argumentList.Arguments.IndexOf(argument);
                    if (argumentIndex > 0)
                    {
                        previousExpression = argumentList.Arguments[argumentIndex - 1].Expression;
                    }
                }
            }
            else if (node.Parent is InitializerExpressionSyntax initializer)
            {
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
                if (argument.Parent is ArgumentListSyntax argumentList)
                {
                    var argumentIndex = argumentList.Arguments.IndexOf(argument);
                    if (argumentIndex >= 0 && argumentIndex < argumentList.Arguments.Count - 1)
                    {
                        nextExpression = argumentList.Arguments[argumentIndex + 1].Expression;
                    }
                }
            }
            else if (node.Parent is InitializerExpressionSyntax initializer)
            {
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
