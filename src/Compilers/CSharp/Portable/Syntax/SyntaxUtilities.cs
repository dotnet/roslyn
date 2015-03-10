// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class SyntaxUtilities
    {
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
                    if (SyntaxFacts.IsLambdaBody(node))
                    {
                        return true;
                    }

                    // TODO: EE expression
                    if (node is ExpressionSyntax && node.Parent.Parent == null)
                    {
                        return true;
                    }

                    return false;
            }
        }
    }
}
