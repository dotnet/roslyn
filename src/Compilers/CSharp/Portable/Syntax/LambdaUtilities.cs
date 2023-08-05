// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
                case SyntaxKind.JoinClause:
                case SyntaxKind.GroupClause:
                case SyntaxKind.LocalFunctionStatement:
                    return true;

                case SyntaxKind.SelectClause:
                    var selectClause = (SelectClauseSyntax)node;
                    return !IsReducedSelectOrGroupByClause(selectClause, selectClause.Expression);

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
            Debug.Assert(lambdaBody.Parent is object);
            var lambda = lambdaBody.Parent;
            if (lambda.IsKind(SyntaxKind.ArrowExpressionClause))
            {
                // In case of expression bodied local functions there is a three level hierarchy: 
                // LocalFunctionStatement -> ArrowExpressionClause -> Expression.
                // And the lambda is the LocalFunctionStatement.
                lambda = lambda.Parent;
                Debug.Assert(lambda.IsKind(SyntaxKind.LocalFunctionStatement));
            }

            Debug.Assert(IsLambda(lambda));
            return lambda;
        }

        /// <summary>
        /// See SyntaxNode.GetCorrespondingLambdaBody.
        /// </summary>
        internal static SyntaxNode? TryGetCorrespondingLambdaBody(SyntaxNode oldBody, SyntaxNode newLambda)
        {
            Debug.Assert(oldBody.Parent is object);

            switch (newLambda.Kind())
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
                    var selectClause = (SelectClauseSyntax)newLambda;

                    // Select clause is not considered to be lambda if it's reduced,
                    // however to avoid complexity we allow it to be passed in and just return null.
                    return IsReducedSelectOrGroupByClause(selectClause, selectClause.Expression) ? null : selectClause.Expression;

                case SyntaxKind.JoinClause:
                    var oldJoin = (JoinClauseSyntax)oldBody.Parent;
                    var newJoin = (JoinClauseSyntax)newLambda;
                    Debug.Assert(oldJoin.LeftExpression == oldBody || oldJoin.RightExpression == oldBody);
                    return (oldJoin.LeftExpression == oldBody) ? newJoin.LeftExpression : newJoin.RightExpression;

                case SyntaxKind.GroupClause:
                    var oldGroup = (GroupClauseSyntax)oldBody.Parent;
                    var newGroup = (GroupClauseSyntax)newLambda;
                    Debug.Assert(oldGroup.GroupExpression == oldBody || oldGroup.ByExpression == oldBody);
                    return (oldGroup.GroupExpression == oldBody) ?
                        (IsReducedSelectOrGroupByClause(newGroup, newGroup.GroupExpression) ? null : newGroup.GroupExpression) : newGroup.ByExpression;

                case SyntaxKind.LocalFunctionStatement:
                    return GetLocalFunctionBody((LocalFunctionStatementSyntax)newLambda);

                default:
                    throw ExceptionUtilities.UnexpectedValue(newLambda.Kind());
            }
        }

        public static SyntaxNode GetNestedFunctionBody(SyntaxNode nestedFunction)
            => nestedFunction switch
            {
                AnonymousFunctionExpressionSyntax anonymousFunctionExpressionSyntax => anonymousFunctionExpressionSyntax.Body,
                LocalFunctionStatementSyntax localFunctionStatementSyntax => (CSharpSyntaxNode?)localFunctionStatementSyntax.Body ?? localFunctionStatementSyntax.ExpressionBody!.Expression,
                _ => throw ExceptionUtilities.UnexpectedValue(nestedFunction),
            };

        public static bool IsNotLambdaBody(SyntaxNode node)
        {
            return !IsLambdaBody(node);
        }

        /// <summary>
        /// Returns true if the specified <paramref name="node"/> represents a body of a lambda.
        /// </summary>
        public static bool IsLambdaBody(SyntaxNode node, bool allowReducedLambdas = false)
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
                    var anonymousFunction = (AnonymousFunctionExpressionSyntax)parent;
                    return anonymousFunction.Body == node;

                case SyntaxKind.LocalFunctionStatement:
                    var localFunction = (LocalFunctionStatementSyntax)parent;
                    return localFunction.Body == node;

                case SyntaxKind.ArrowExpressionClause:
                    var arrowExpressionClause = (ArrowExpressionClauseSyntax)parent;
                    return arrowExpressionClause.Expression == node && arrowExpressionClause.Parent is LocalFunctionStatementSyntax;

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
                    return selectClause.Expression == node && (allowReducedLambdas || !IsReducedSelectOrGroupByClause(selectClause, selectClause.Expression));

                case SyntaxKind.GroupClause:
                    var groupClause = (GroupClauseSyntax)parent;
                    return (groupClause.GroupExpression == node && (allowReducedLambdas || !IsReducedSelectOrGroupByClause(groupClause, groupClause.GroupExpression))) ||
                           groupClause.ByExpression == node;
            }

            return false;
        }

        /// <summary>
        /// When queries are translated into expressions select and group-by expressions such that
        /// 1) select/group-by expression is the same identifier as the "source" identifier and
        /// 2) at least one Where or OrderBy clause but no other clause is present in the contained query body or
        ///    the expression in question is a group-by expression and the body has no clause
        /// 
        /// do not translate into lambdas.
        /// By "source" identifier we mean the identifier specified in the from clause that initiates the query or the query continuation that includes the body.
        /// 
        /// The above condition can be derived from the language specification (chapter 7.16.2) as follows:
        /// - In order for 7.16.2.5 "Select clauses" to be applicable the following conditions must hold:
        ///   - There has to be at least one clause in the body, otherwise the query is reduced into a final form by 7.16.2.3 "Degenerate query expressions".
        ///   - Only where and order-by clauses may be present in the query body, otherwise a transformation in 7.16.2.4 "From, let, where, join and orderby clauses"
        ///     produces pattern that doesn't match the requirements of 7.16.2.5.
        ///   
        /// - In order for 7.16.2.6 "Groupby clauses" to be applicable the following conditions must hold:
        ///   - Only where and order-by clauses may be present in the query body, otherwise a transformation in 7.16.2.4 "From, let, where, join and orderby clauses"
        ///     produces pattern that doesn't match the requirements of 7.16.2.5.
        /// </summary>
        private static bool IsReducedSelectOrGroupByClause(SelectOrGroupClauseSyntax selectOrGroupClause, ExpressionSyntax selectOrGroupExpression)
        {
            if (!selectOrGroupExpression.IsKind(SyntaxKind.IdentifierName))
            {
                return false;
            }

            var selectorIdentifier = ((IdentifierNameSyntax)selectOrGroupExpression).Identifier;

            SyntaxToken sourceIdentifier;
            QueryBodySyntax containingBody;

            Debug.Assert(selectOrGroupClause.Parent!.Parent is object);
            var containingQueryOrContinuation = selectOrGroupClause.Parent.Parent;
            if (containingQueryOrContinuation.IsKind(SyntaxKind.QueryExpression))
            {
                var containingQuery = (QueryExpressionSyntax)containingQueryOrContinuation;
                containingBody = containingQuery.Body;
                sourceIdentifier = containingQuery.FromClause.Identifier;
            }
            else
            {
                var containingContinuation = (QueryContinuationSyntax)containingQueryOrContinuation;
                sourceIdentifier = containingContinuation.Identifier;
                containingBody = containingContinuation.Body;
            }

            if (!SyntaxFactory.AreEquivalent(sourceIdentifier, selectorIdentifier))
            {
                return false;
            }

            if (selectOrGroupClause.IsKind(SyntaxKind.SelectClause) && containingBody.Clauses.Count == 0)
            {
                return false;
            }

            foreach (var clause in containingBody.Clauses)
            {
                if (!clause.IsKind(SyntaxKind.WhereClause) && !clause.IsKind(SyntaxKind.OrderByClause))
                {
                    return false;
                }
            }

            return true;
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
        public static bool TryGetLambdaBodies(SyntaxNode node, [NotNullWhen(true)] out SyntaxNode? lambdaBody1, out SyntaxNode? lambdaBody2)
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
                    var selectClause = (SelectClauseSyntax)node;
                    if (IsReducedSelectOrGroupByClause(selectClause, selectClause.Expression))
                    {
                        return false;
                    }

                    lambdaBody1 = selectClause.Expression;
                    return true;

                case SyntaxKind.GroupClause:
                    var groupClause = (GroupClauseSyntax)node;
                    if (IsReducedSelectOrGroupByClause(groupClause, groupClause.GroupExpression))
                    {
                        lambdaBody1 = groupClause.ByExpression;
                    }
                    else
                    {
                        lambdaBody1 = groupClause.GroupExpression;
                        lambdaBody2 = groupClause.ByExpression;
                    }

                    return true;

                case SyntaxKind.LocalFunctionStatement:
                    lambdaBody1 = GetLocalFunctionBody((LocalFunctionStatementSyntax)node);
                    return lambdaBody1 != null;
            }

            return false;
        }

        /// <summary>
        /// Compares content of two nodes ignoring lambda bodies and trivia.
        /// </summary>
        public static bool AreEquivalentIgnoringLambdaBodies(SyntaxNode oldNode, SyntaxNode newNode)
        {
            // all tokens that don't belong to a lambda body:
            var oldTokens = oldNode.DescendantTokens(node => node == oldNode || !IsLambdaBodyStatementOrExpression(node));
            var newTokens = newNode.DescendantTokens(node => node == newNode || !IsLambdaBodyStatementOrExpression(node));

            return oldTokens.SequenceEqual(newTokens, SyntaxFactory.AreEquivalent);
        }

        /// <summary>
        /// "Pair lambda" is a synthesized lambda that creates an instance of an anonymous type representing a pair of values. 
        /// </summary>
        internal static bool IsQueryPairLambda(SyntaxNode syntax)
        {
            // TODO (bug https://github.com/dotnet/roslyn/issues/2663): 
            // Avoid generating these lambdas. Instead generate a static factory method on the anonymous type.
            return syntax.IsKind(SyntaxKind.GroupClause) ||
                   syntax.IsKind(SyntaxKind.JoinClause) ||
                   syntax.IsKind(SyntaxKind.FromClause);
        }

        /// <summary>
        /// Returns true if the specified node is of a kind that could represent a closure scope -- that
        /// is, a scope of a captured variable.
        /// Doesn't check whether or not the node actually declares any captured variable.
        /// </summary>
        internal static bool IsClosureScope(SyntaxNode node)
        {
            switch (node.Kind())
            {
                case SyntaxKind.CompilationUnit:
                case SyntaxKind.Block:
                case SyntaxKind.SwitchStatement:
                case SyntaxKind.ArrowExpressionClause:  // expression-bodied member
                case SyntaxKind.CatchClause:
                case SyntaxKind.ForStatement:
                case SyntaxKind.ForEachStatement:
                case SyntaxKind.ForEachVariableStatement:
                case SyntaxKind.UsingStatement:

                // ctor parameter captured by a lambda in a ctor initializer
                case SyntaxKind.ConstructorDeclaration:
                    return true;

                // Due to pattern-matching, any statement that contains an expression may introduce a scope.
                case SyntaxKind.DoStatement:
                case SyntaxKind.ExpressionStatement:
                case SyntaxKind.FixedStatement:
                case SyntaxKind.GotoCaseStatement:
                case SyntaxKind.IfStatement:
                case SyntaxKind.LockStatement:
                case SyntaxKind.ReturnStatement:
                case SyntaxKind.ThrowStatement:
                case SyntaxKind.WhileStatement:
                case SyntaxKind.YieldReturnStatement:
                    return true;

                case SyntaxKind.ClassDeclaration:
                case SyntaxKind.StructDeclaration:
                case SyntaxKind.RecordDeclaration:
                case SyntaxKind.RecordStructDeclaration:
                    // With dynamic analysis instrumentation, a type declaration can be the syntax associated
                    // with the analysis payload local of a synthesized constructor.
                    // If the synthesized constructor includes an initializer with a lambda,
                    // that lambda needs a closure that captures the analysis payload of the constructor.
                    return true;

                case SyntaxKind.SwitchExpression:
                case SyntaxKind.AwaitExpression:
                    // These translate into a BoundSpillSequence, which is then translated into a block
                    // containing temps required for spilling subexpressions. That block has the syntax of the switch
                    // expression or await expression.
                    return true;

                default:
                    // With the introduction of pattern-matching, many nodes now contain top-level
                    // expressions that may introduce pattern variables.
                    if (node.Parent != null)
                    {
                        switch (node.Parent.Kind())
                        {
                            case SyntaxKind.EqualsValueClause:
                                return true;

                            case SyntaxKind.ForStatement:
                                SeparatedSyntaxList<ExpressionSyntax> incrementors = ((ForStatementSyntax)node.Parent).Incrementors;
                                if (incrementors.FirstOrDefault() == node)
                                {
                                    return true;
                                }
                                break;
                        }
                    }

                    break;
            }

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

        /// <summary>
        /// Given a node that represents a variable declaration, lambda or a closure scope return the position to be used to calculate 
        /// the node's syntax offset with respect to its containing member.
        /// </summary>
        internal static int GetDeclaratorPosition(SyntaxNode node)
        {
            // To differentiate between nested switch expressions that start at the same offset, use the offset of the `switch` keyword.
            return (node is SwitchExpressionSyntax switchExpression) ? switchExpression.SwitchKeyword.SpanStart : node.SpanStart;
        }

        private static SyntaxNode? GetLocalFunctionBody(LocalFunctionStatementSyntax localFunctionStatementSyntax)
            => (SyntaxNode?)localFunctionStatementSyntax.Body ?? localFunctionStatementSyntax.ExpressionBody?.Expression;
    }
}
