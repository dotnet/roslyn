// Copyright (c) Microsoft. All Rights Reserved. Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class SyntaxUtilities
    {
        /// <summary>
        /// <see cref="SyntaxNode.GetCorrespondingLambdaBody(SyntaxNode)"/>
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
    }
}
