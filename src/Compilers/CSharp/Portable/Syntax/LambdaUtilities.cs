// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal static class LambdaUtilities
    {
        /// <summary>
        /// Returns true if the specified node represents a lambda.
        /// </summary>
        public static bool IsLambda(SyntaxNode node)
        {
            switch (node.Kind())
            {
                case SyntaxKind.ParenthesizedLambdaExpression:
                case SyntaxKind.SimpleLambdaExpression:
                case SyntaxKind.AnonymousMethodExpression:
                case SyntaxKind.LetClause:
                case SyntaxKind.WhereClause:
                case SyntaxKind.AscendingOrdering:
                case SyntaxKind.DescendingOrdering:
                case SyntaxKind.SelectClause:
                case SyntaxKind.JoinClause:
                case SyntaxKind.GroupClause:
                    return true;

                case SyntaxKind.FromClause:
                    // The first from clause of a query expression is not a lambda.
                    return !node.Parent.IsKind(SyntaxKind.QueryExpression);
            }

            return false;
        }

        public static bool IsNotLambda(SyntaxNode node) => !IsLambda(node);

        /// <summary>
        /// Given a node that represents a lambda body returns a node that represents the lambda.
        /// </summary>
        public static SyntaxNode GetLambda(SyntaxNode lambdaBody)
        {
            var lambda = lambdaBody.Parent;
            Debug.Assert(IsLambda(lambda));
            return lambda;
        }

        /// <summary>
        /// See SyntaxNode.GetCorrespondingLambdaBody.
        /// </summary>
        internal static SyntaxNode GetCorrespondingLambdaBody(SyntaxNode oldBody, SyntaxNode newLambda)
        {
            var oldLambda = oldBody.Parent;
            switch (oldLambda.Kind())
            {
                case SyntaxKind.ParenthesizedLambdaExpression:
                case SyntaxKind.SimpleLambdaExpression:
                case SyntaxKind.AnonymousMethodExpression:
                    return ((AnonymousFunctionExpressionSyntax)newLambda).Body;

                case SyntaxKind.FromClause:
                    return ((FromClauseSyntax)newLambda).Expression;

                case SyntaxKind.LetClause:
                    return ((LetClauseSyntax)newLambda).Expression;

                case SyntaxKind.WhereClause:
                    return ((WhereClauseSyntax)newLambda).Condition;

                case SyntaxKind.AscendingOrdering:
                case SyntaxKind.DescendingOrdering:
                    return ((OrderingSyntax)newLambda).Expression;

                case SyntaxKind.SelectClause:
                    return ((SelectClauseSyntax)newLambda).Expression;

                case SyntaxKind.JoinClause:
                    var oldJoin = (JoinClauseSyntax)oldLambda;
                    var newJoin = (JoinClauseSyntax)newLambda;
                    Debug.Assert(oldJoin.LeftExpression == oldBody || oldJoin.RightExpression == oldBody);
                    return (oldJoin.LeftExpression == oldBody) ? newJoin.LeftExpression : newJoin.RightExpression;

                case SyntaxKind.GroupClause:
                    var oldGroup = (GroupClauseSyntax)oldLambda;
                    var newGroup = (GroupClauseSyntax)newLambda;
                    Debug.Assert(oldGroup.GroupExpression == oldBody || oldGroup.ByExpression == oldBody);
                    return (oldGroup.GroupExpression == oldBody) ? newGroup.GroupExpression : newGroup.ByExpression;

                default:
                    throw ExceptionUtilities.UnexpectedValue(oldLambda.Kind());
            }
        }

        /// <summary>
        /// Returns true if the specified <paramref name="node"/> represents a body of a lambda.
        /// </summary>
        public static bool IsLambdaBody(SyntaxNode node)
        {
            var parent = node?.Parent;
            if (parent == null)
            {
                return false;
            }

            switch (parent.Kind())
            {
                case SyntaxKind.ParenthesizedLambdaExpression:
                case SyntaxKind.SimpleLambdaExpression:
                case SyntaxKind.AnonymousMethodExpression:
                    return true;

                case SyntaxKind.FromClause:
                    var fromClause = (FromClauseSyntax)parent;
                    return fromClause.Expression == node && fromClause.Parent is QueryBodySyntax;

                case SyntaxKind.JoinClause:
                    var joinClause = (JoinClauseSyntax)parent;
                    return joinClause.LeftExpression == node || joinClause.RightExpression == node;

                case SyntaxKind.LetClause:
                    var letClause = (LetClauseSyntax)parent;
                    return letClause.Expression == node;

                case SyntaxKind.WhereClause:
                    var whereClause = (WhereClauseSyntax)parent;
                    return whereClause.Condition == node;

                case SyntaxKind.AscendingOrdering:
                case SyntaxKind.DescendingOrdering:
                    var ordering = (OrderingSyntax)parent;
                    return ordering.Expression == node;

                case SyntaxKind.SelectClause:
                    var selectClause = (SelectClauseSyntax)parent;
                    return selectClause.Expression == node;

                case SyntaxKind.GroupClause:
                    var groupClause = (GroupClauseSyntax)parent;
                    return groupClause.GroupExpression == node || groupClause.ByExpression == node;
            }

            return false;
        }

        /// <remarks>
        /// In C# lambda bodies are expressions or block statements. In both cases it's a single node.
        /// In VB a lambda body might be a sequence of nodes (statements). 
        /// We define this function to minimize differences between C# and VB implementation.
        /// </remarks>
        public static bool IsLambdaBodyStatementOrExpression(SyntaxNode node)
        {
            return IsLambdaBody(node);
        }

        public static bool IsLambdaBodyStatementOrExpression(SyntaxNode node, out SyntaxNode lambdaBody)
        {
            lambdaBody = node;
            return IsLambdaBody(node);
        }

        /// <summary>
        /// If the specified node represents a lambda returns a node (or nodes) that represent its body (bodies).
        /// </summary>
        public static bool TryGetLambdaBodies(SyntaxNode node, out SyntaxNode lambdaBody1, out SyntaxNode lambdaBody2)
        {
            lambdaBody1 = null;
            lambdaBody2 = null;

            switch (node.Kind())
            {
                case SyntaxKind.ParenthesizedLambdaExpression:
                case SyntaxKind.SimpleLambdaExpression:
                case SyntaxKind.AnonymousMethodExpression:
                    lambdaBody1 = ((AnonymousFunctionExpressionSyntax)node).Body;
                    return true;

                case SyntaxKind.FromClause:
                    // The first from clause of a query expression is not a lambda.
                    if (node.Parent.IsKind(SyntaxKind.QueryExpression))
                    {
                        return false;
                    }

                    lambdaBody1 = ((FromClauseSyntax)node).Expression;
                    return true;

                case SyntaxKind.JoinClause:
                    var joinClause = (JoinClauseSyntax)node;
                    lambdaBody1 = joinClause.LeftExpression;
                    lambdaBody2 = joinClause.RightExpression;
                    return true;

                case SyntaxKind.LetClause:
                    lambdaBody1 = ((LetClauseSyntax)node).Expression;
                    return true;

                case SyntaxKind.WhereClause:
                    lambdaBody1 = ((WhereClauseSyntax)node).Condition;
                    return true;

                case SyntaxKind.AscendingOrdering:
                case SyntaxKind.DescendingOrdering:
                    lambdaBody1 = ((OrderingSyntax)node).Expression;
                    return true;

                case SyntaxKind.SelectClause:
                    lambdaBody1 = ((SelectClauseSyntax)node).Expression;
                    return true;

                case SyntaxKind.GroupClause:
                    var groupClause = (GroupClauseSyntax)node;
                    lambdaBody1 = groupClause.GroupExpression;
                    lambdaBody2 = groupClause.ByExpression;
                    return true;
            }

            return false;
        }

        /// <summary>
        /// "Pair lambda" is a synthesized lambda that creates an instance of an anonymous type representing a pair of values. 
        /// TODO: Avoid generating these lambdas. Instead generate a method on the anonymous type, or use KeyValuePair instead.
        /// </summary>
        internal static bool IsQueryPairLambda(SyntaxNode syntax)
        {
            return syntax.IsKind(SyntaxKind.GroupClause) ||
                   syntax.IsKind(SyntaxKind.JoinClause) ||
                   syntax.IsKind(SyntaxKind.FromClause);
        }

        /// <summary>
        /// Returns true if the specified node can represent a closure scope -- that is a scope of a captured variable.
        /// Doesn't validate whether or not the node actually declares any captured variable.
        /// </summary>
        internal static bool IsClosureScope(SyntaxNode node)
        {
            switch (node.Kind())
            {
                case SyntaxKind.Block:
                case SyntaxKind.SwitchStatement:
                case SyntaxKind.ArrowExpressionClause:  // expression-bodied member
                case SyntaxKind.CatchClause:
                case SyntaxKind.ForStatement:
                case SyntaxKind.ForEachStatement:
                case SyntaxKind.UsingStatement:

                // ctor parameter captured by a lambda in a ctor initializer
                case SyntaxKind.ConstructorDeclaration:
                    return true;

                default:
                    if (IsLambdaBody(node))
                    {
                        return true;
                    }

                    // TODO: EE expression
                    if (node is ExpressionSyntax && node.Parent != null && node.Parent.Parent == null)
                    {
                        return true;
                    }

                    return false;
            }
        }
    }
}
