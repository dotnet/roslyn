// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseRecursivePatterns
{
    internal sealed partial class CSharpUseRecursivePatternsCodeRefactoringProvider
    {
        private sealed class Reducer : Visitor<AnalyzedNode>
        {
            private static readonly Reducer s_instance = new Reducer();

            private Reducer() { }

            public static AnalyzedNode Reduce(AnalyzedNode analyzedNode) => s_instance.Visit(analyzedNode);

            public override AnalyzedNode VisitPatternMatch(PatternMatch node) => MakePatternMatch(node.Expression, Visit(node.Pattern));

            public override AnalyzedNode VisitConjuction(Conjuction node) => Union(Visit(node.Left), Visit(node.Right));

            private static AnalyzedNode Visit(Conjuction left, PatternMatch right)
            {
                if (left.Left.Contains(right.Expression))
                {
                    return new Conjuction(Union(left.Left, right), left.Right);
                }

                if (left.Right.Contains(right.Expression))
                {
                    return new Conjuction(left.Left, Union(left.Right, right));
                }

                return new Conjuction(left, right);
            }

            private static AnalyzedNode Visit(PatternMatch left, PatternMatch right)
            {
                if (SyntaxFactory.AreEquivalent(left.Expression, right.Expression))
                {
                    return new PatternMatch(left.Expression, Union(left.Pattern, right.Pattern));
                }

                if (left.Pattern.Contains(right.Expression))
                {
                    return new PatternMatch(left.Expression, Union(left.Pattern, right));
                }

                if (right.Pattern.Contains(left.Expression))
                {
                    return new PatternMatch(right.Expression, Union(right.Pattern, left));
                }

                return new Conjuction(left, right);
            }

            private static AnalyzedNode Visit(VarPattern left, PatternMatch right)
            {
                if (left.Contains(right.Expression))
                    return new Conjuction(left, right.Pattern);

                return new Conjuction(left, right);
            }

            private static AnalyzedNode Union(AnalyzedNode left, AnalyzedNode right)
            {
                switch (left.Kind | right.Kind)
                {
                    case NodeKind.Conjunction | NodeKind.Conjunction:
                    case NodeKind.Conjunction | NodeKind.TypePattern:
                    case NodeKind.Conjunction | NodeKind.VarPattern:
                        throw new NotImplementedException();

                    case NodeKind.PatternMatch | NodeKind.PatternMatch:
                        return Visit((PatternMatch)left, (PatternMatch)right);

                    case NodeKind.Conjunction | NodeKind.PatternMatch:
                        return left.Kind == NodeKind.PatternMatch
                            ? Visit((Conjuction)right, (PatternMatch)left)
                            : Visit((Conjuction)left, (PatternMatch)right);

                    case NodeKind.PatternMatch | NodeKind.VarPattern:
                        return left.Kind == NodeKind.PatternMatch
                            ? Visit((VarPattern)right, (PatternMatch)left)
                            : Visit((VarPattern)left, (PatternMatch)right);

                    case NodeKind.TypePattern | NodeKind.VarPattern:
                    case NodeKind.PatternMatch | NodeKind.TypePattern:
                    case NodeKind.NotNullPattern | NodeKind.VarPattern:
                        return new Conjuction(left, right);

                    case NodeKind.NotNullPattern | NodeKind.Conjunction:
                    case NodeKind.NotNullPattern | NodeKind.TypePattern:
                    case NodeKind.NotNullPattern | NodeKind.PatternMatch:
                    case NodeKind.NotNullPattern | NodeKind.NotNullPattern:
                        return left.Kind == NodeKind.NotNullPattern ? right : left;

                    case NodeKind.ConstantPattern | NodeKind.Conjunction:
                    case NodeKind.ConstantPattern | NodeKind.PatternMatch:
                    case NodeKind.ConstantPattern | NodeKind.TypePattern:
                    case NodeKind.ConstantPattern | NodeKind.ConstantPattern:
                    case NodeKind.ConstantPattern | NodeKind.VarPattern:
                    case NodeKind.ConstantPattern | NodeKind.NotNullPattern:
                    case NodeKind.TypePattern | NodeKind.TypePattern:
                    case NodeKind.VarPattern | NodeKind.VarPattern:
                        return null;

                    case var value:
                        throw ExceptionUtilities.UnexpectedValue(value);
                }
            }

            private static PatternMatch MakePatternMatch(ExpressionSyntax expression, AnalyzedNode pattern)
            {
                switch (expression.Kind())
                {
                    case SyntaxKind.IdentifierName:
                        return new PatternMatch(expression, pattern);

                    case SyntaxKind.MemberBindingExpression:
                        var memberBinding = (MemberBindingExpressionSyntax)expression;
                        return new PatternMatch(memberBinding.Name, pattern);

                    case SyntaxKind.ConditionalAccessExpression:
                        var conditionalAccess = (ConditionalAccessExpressionSyntax)expression;
                        var asExpression = (BinaryExpressionSyntax)conditionalAccess.Expression.WalkDownParentheses();
                        return MakePatternMatch(asExpression.Left,
                            new Conjuction(new TypePattern((TypeSyntax)asExpression.Right),
                                MakePatternMatch(conditionalAccess.WhenNotNull, pattern)));

                    case SyntaxKind.SimpleMemberAccessExpression:
                        var memberAccess = (MemberAccessExpressionSyntax)expression;
                        return MakePatternMatch(memberAccess.Expression,
                            new PatternMatch(memberAccess.Name, pattern));

                    case var value:
                        throw ExceptionUtilities.UnexpectedValue(value);
                }
            }

            public override AnalyzedNode VisitConstantPattern(ConstantPattern node) => node;
            public override AnalyzedNode VisitNotNullPattern(NotNullPattern node) => node;
            public override AnalyzedNode VisitTypePattern(TypePattern node) => node;
            public override AnalyzedNode VisitVarPattern(VarPattern node) => node;
        }
    }
}
