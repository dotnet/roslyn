// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseRecursivePatterns
{
    using static SyntaxFactory;

    internal sealed partial class CSharpUseRecursivePatternsCodeRefactoringProvider
    {
        private sealed class Reducer
        {
            // Track whether we have made progress by merging nodes.
            // If this remains false, we will not offer the fix.
            private bool _isNonTrivial;

            private Reducer() { }

            // The reducer, pairs each node with each other and check if the combination is valid.
            public static AnalyzedNode Reduce(AnalyzedNode node, out bool isNonTrivial)
            {
                var reducer = new Reducer();
                var result = reducer.Reduce(node);
                isNonTrivial = reducer._isNonTrivial;
                return result;
            }

            private AnalyzedNode Intersection(AnalyzedNode left, AnalyzedNode right)
            {
                // Bail out in case previous intersection attempts have failed.
                if (left is null || right is null)
                    return null;

                // Since the bitwise-OR operator is symmetrical, each case covers both orderings for each pair.
                // We always have C(N+1, 2) cases where N is the number of kinds; so the following switch is exhaustive.
                switch (left.Kind | right.Kind)
                {
                    case NodeKind.Conjunction | NodeKind.Conjunction:
                    case NodeKind.Conjunction | NodeKind.TypePattern:
                    case NodeKind.Conjunction | NodeKind.VarPattern:
                    case NodeKind.Conjunction | NodeKind.ConstantPattern:
                    case NodeKind.Conjunction | NodeKind.PositionalPattern:
                        return left.Kind == NodeKind.Conjunction
                            ? IntersectionCore(right, (Conjunction)left)
                            : IntersectionCore(left, (Conjunction)right);

                    case NodeKind.PatternMatch | NodeKind.PatternMatch:
                        return IntersectionCore((PatternMatch)left, (PatternMatch)right);

                    case NodeKind.PositionalPattern | NodeKind.PositionalPattern:
                        return IntersectionCore((PositionalPattern)left, (PositionalPattern)right);

                    case NodeKind.TypePattern | NodeKind.TypePattern:
                        return IntersectionCore((TypePattern)left, (TypePattern)right);

                    case NodeKind.Conjunction | NodeKind.PatternMatch:
                        return left.Kind == NodeKind.PatternMatch
                            ? IntersectionCore((Conjunction)right, (PatternMatch)left)
                            : IntersectionCore((Conjunction)left, (PatternMatch)right);

                    case NodeKind.PatternMatch | NodeKind.VarPattern:
                        return left.Kind == NodeKind.PatternMatch
                            ? IntersectionCore((VarPattern)right, (PatternMatch)left)
                            : IntersectionCore((VarPattern)left, (PatternMatch)right);

                    case NodeKind.PositionalPattern | NodeKind.PatternMatch:
                        return left.Kind == NodeKind.PatternMatch
                            ? IntersectionCore((PositionalPattern)right, (PatternMatch)left)
                            : IntersectionCore((PositionalPattern)left, (PatternMatch)right);

                    case NodeKind.Evaluation | NodeKind.ConstantPattern:
                    case NodeKind.Evaluation | NodeKind.NotNullPattern:
                    case NodeKind.Evaluation | NodeKind.TypePattern:
                    case NodeKind.Evaluation | NodeKind.VarPattern:
                    case NodeKind.Evaluation | NodeKind.DiscardPattern:
                    case NodeKind.Evaluation | NodeKind.Conjunction:
                    case NodeKind.Evaluation | NodeKind.PositionalPattern:
                    case NodeKind.Evaluation | NodeKind.PatternMatch:
                    case NodeKind.Evaluation | NodeKind.Evaluation:
                    case NodeKind.TypePattern | NodeKind.VarPattern:
                    case NodeKind.PatternMatch | NodeKind.TypePattern:
                    case NodeKind.NotNullPattern | NodeKind.VarPattern:
                    case NodeKind.PositionalPattern | NodeKind.VarPattern:
                    case NodeKind.PositionalPattern | NodeKind.TypePattern:
                        // Both sides are valid next to each other.
                        return new Conjunction(left, right);

                    case NodeKind.DiscardPattern | NodeKind.Conjunction:
                    case NodeKind.DiscardPattern | NodeKind.TypePattern:
                    case NodeKind.DiscardPattern | NodeKind.PatternMatch:
                    case NodeKind.DiscardPattern | NodeKind.NotNullPattern:
                    case NodeKind.DiscardPattern | NodeKind.VarPattern:
                    case NodeKind.DiscardPattern | NodeKind.ConstantPattern:
                    case NodeKind.DiscardPattern | NodeKind.PositionalPattern:
                        // We replace the discard with whatever we find on the other side.
                        return left.Kind == NodeKind.DiscardPattern ? right : left;

                    case NodeKind.NotNullPattern | NodeKind.Conjunction:
                    case NodeKind.NotNullPattern | NodeKind.TypePattern:
                    case NodeKind.NotNullPattern | NodeKind.PatternMatch:
                    case NodeKind.NotNullPattern | NodeKind.PositionalPattern:
                        // We replace the not-null pattern with whatever we find on the other side,
                        // since all those patterns also imply a null-check.
                        return left.Kind == NodeKind.NotNullPattern ? right : left;

                    case NodeKind.DiscardPattern | NodeKind.DiscardPattern:
                    case NodeKind.NotNullPattern | NodeKind.NotNullPattern:
                        // Identical patterns.
                        return left;

                    case NodeKind.ConstantPattern | NodeKind.PositionalPattern:
                    case NodeKind.ConstantPattern | NodeKind.PatternMatch:
                    case NodeKind.ConstantPattern | NodeKind.TypePattern:
                    case NodeKind.ConstantPattern | NodeKind.ConstantPattern:
                    case NodeKind.ConstantPattern | NodeKind.VarPattern:
                    case NodeKind.ConstantPattern | NodeKind.NotNullPattern:
                    case NodeKind.VarPattern | NodeKind.VarPattern:
                        // Invalid pair for an intersection.
                        return null;

                    case var value:
                        throw ExceptionUtilities.UnexpectedValue(value);
                }
            }

            private AnalyzedNode IntersectionCore(TypePattern left, TypePattern right)
            {
                // If types match, we return a single type-pattern, otherwise fail.
                if (AreEquivalent(left.Type, right.Type))
                    return left;

                return null;
            }

            private AnalyzedNode IntersectionCore(AnalyzedNode node, Conjunction conjunction)
            {
                // We deconstruct a conjunction to attempt the intersection recursively.
                // This way, we capture an invalid intersection if any of the nested pairs are invalid.
                return Intersection(Intersection(node, conjunction.Left), conjunction.Right);
            }

            private AnalyzedNode IntersectionCore(Conjunction conjunction, PatternMatch match)
            {
                // We need to decide which side has an intersection with the pattern-match.
                // For example, if we have:
                //
                //      (e1 is A && e2 is B) && e1 is C
                //
                // we merge both e1 matches and attempt to intersect patterns A and C
                if (conjunction.Left.Contains(match.Expression))
                {
                    // Mark as non-trivial, since if the expression is found on either side, we will merge it to a single node.
                    _isNonTrivial = true;
                    return Conjunction.Create(Intersection(conjunction.Left, match), conjunction.Right);
                }

                if (conjunction.Right.Contains(match.Expression))
                {
                    _isNonTrivial = true;
                    return Conjunction.Create(conjunction.Left, Intersection(conjunction.Right, match));
                }

                // Otherwise, return both nodes, e.g. this could be another property pattern on the same object.
                return new Conjunction(conjunction, match);
            }

            private AnalyzedNode IntersectionCore(PatternMatch leftMatch, PatternMatch rightMatch)
            {
                // First, we check if both pattern-matches are against the same expression.
                // For example, if we have:
                //
                //      e1 is A && e1 is B
                //
                // we merge both e1 matches and attempt to intersect patterns A and B
                if (AreEquivalent(leftMatch.Expression, rightMatch.Expression))
                {
                    // Mark as non-trivial, because we have made a single node out of two.
                    _isNonTrivial = true;
                    return PatternMatch.Create(leftMatch.Expression, Intersection(leftMatch.Pattern, rightMatch.Pattern));
                }

                // To accommodate for a chained match with a var-pattern, we also try to find the expression in the pattern.
                // For example, if we have:
                //
                //      e1 is var x && x is A
                //
                // We make a single match against e1 and attempt to intersect the pattern A with var x.
                if (leftMatch.Pattern.Contains(rightMatch.Expression))
                {
                    _isNonTrivial = true;
                    return PatternMatch.Create(leftMatch.Expression, Intersection(leftMatch.Pattern, rightMatch));
                }

                if (rightMatch.Pattern.Contains(leftMatch.Expression))
                {
                    _isNonTrivial = true;
                    return PatternMatch.Create(rightMatch.Expression, Intersection(rightMatch.Pattern, leftMatch));
                }

                // Otherwise, return both nodes, e.g. these could be two patterns against different expressions.
                return new Conjunction(leftMatch, rightMatch);
            }

            private AnalyzedNode IntersectionCore(VarPattern var, PatternMatch match)
            {
                // We have reached the var-pattern, look for identical expressions and merge.
                if (var.Contains(match.Expression))
                {
                    _isNonTrivial = true;
                    return new Conjunction(var, match.Pattern);
                }

                // Otherwise, return both nodes, this will be rewritten as the variable designation on a recursive pattern.
                return new Conjunction(var, match);
            }

            private AnalyzedNode IntersectionCore(PositionalPattern positional, PatternMatch match)
            {
                var subpatterns = positional.Subpatterns;
                for (var index = 0; index < subpatterns.Length; index++)
                {
                    var subpattern = subpatterns[index];
                    var pattern = subpattern.Pattern;

                    // Check if we can merge the pattern-match with either of subpatterns.
                    if (pattern.Contains(match.Expression))
                    {
                        var intersection = Intersection(pattern, match.Pattern);
                        if (intersection is null)
                            return null;

                        return new PositionalPattern(subpatterns.SetItem(index, (subpattern.NameColonOpt, intersection)));
                    }
                }

                // Otherwise, return both nodes, this will be rewritten as a recursive pattern of the form `(p1, p2) {id: p3}`
                return new Conjunction(positional, match);
            }

            private AnalyzedNode IntersectionCore(PositionalPattern left, PositionalPattern right)
            {
                var leftSubpatterns = left.Subpatterns;
                var rightSubpatterns = right.Subpatterns;

                // Two positional-pattern only match if both have the same length.
                if (leftSubpatterns.Length != rightSubpatterns.Length)
                    return null;

                var builder = new List<(NameColonSyntax, AnalyzedNode)>(leftSubpatterns.Length);
                for (var index = 0; index < leftSubpatterns.Length; index++)
                {
                    var leftSub = leftSubpatterns[index];
                    var rightSub = rightSubpatterns[index];

                    // Attempt the intersection for corresponding subpatterns.
                    var intersection = Intersection(leftSub.Pattern, rightSub.Pattern);
                    if (intersection is null)
                        return null;

                    // Keep the name if either side is named.
                    builder.Add((leftSub.NameColonOpt ?? rightSub.NameColonOpt, intersection));
                }

                return new PositionalPattern(builder.ToImmutableArray());
            }

            public AnalyzedNode ReduceConjunction(Conjunction node)
                => Intersection(Reduce(node.Left), Reduce(node.Right));

            public AnalyzedNode ReducePatternMatch(PatternMatch node)
                => MakePatternMatch(node.Expression, Reduce(node.Pattern));

            private AnalyzedNode Reduce(AnalyzedNode node)
                => node.Reduce(this);

            private PatternMatch MakePatternMatch(ExpressionSyntax expression, AnalyzedNode pattern)
            {
                // Try to expand a match of the form 
                //
                //      expression.Property.AnotherProperty is A
                //
                // to
                //
                //      expression is {Property: {AnotherProperty: A}}
                //
                // so that we can check if the identifiers are matched in other places and merge.
                switch (expression.Kind())
                {
                    default:
                        return new PatternMatch(expression, pattern);

                    case SyntaxKind.ParenthesizedExpression:
                        return MakePatternMatch(((ParenthesizedExpressionSyntax)expression).Expression, pattern);

                    case SyntaxKind.MemberBindingExpression
                        when (MemberBindingExpressionSyntax)expression is var node
                            && IsIdentifierOrSimpleMemberAccess(node.Name):
                        return new PatternMatch(node.Name, pattern);

                    case SyntaxKind.ConditionalAccessExpression
                        when (ConditionalAccessExpressionSyntax)expression is var node
                            && IsIdentifierOrSimpleMemberAccess(node.WhenNotNull):
                        return MakePatternMatch(node.Expression, node.WhenNotNull, pattern)
                            ?? MakePatternMatch(node.Expression, MakePatternMatch(node.WhenNotNull, pattern));

                    case SyntaxKind.SimpleMemberAccessExpression
                        when (MemberAccessExpressionSyntax)expression is var node
                            && IsIdentifierOrSimpleMemberAccess(node.Name):
                        return MakePatternMatch(node.Expression, node.Name, pattern)
                            ?? MakePatternMatch(node.Expression, new PatternMatch(node.Name, pattern));
                }
            }

            private PatternMatch MakePatternMatch(ExpressionSyntax left, ExpressionSyntax rest, AnalyzedNode pattern)
            {
                switch (left.Kind())
                {
                    default:
                        return null;

                    case SyntaxKind.ParenthesizedExpression:
                        return MakePatternMatch(((ParenthesizedExpressionSyntax)left).Expression, rest, pattern);

                    // Check for type-casts to yield a type-pattern.
                    case SyntaxKind.AsExpression when (BinaryExpressionSyntax)left is var node:
                        // Marking as non-trivial because we want to offer the fix for something like:
                        //
                        //      (e as T)?.Property == true -> e is T {Property: true}
                        //
                        _isNonTrivial = true;
                        return MakePatternMatch(node.Left,
                            new Conjunction(new TypePattern((TypeSyntax)node.Right), MakePatternMatch(rest, pattern)));

                    case SyntaxKind.CastExpression when (CastExpressionSyntax)left is var node:
                        // Not marking as non-trivial because we don't want to offer the fix for something like:
                        //
                        //      ((T)e).Property == true
                        //
                        // But still yield a type-pattern in case the type is checked in other places so we can merge the two.
                        return MakePatternMatch(node.Expression,
                            new Conjunction(new TypePattern(node.Type), MakePatternMatch(rest, pattern)));
                }
            }
        }
    }
}
