// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using Microsoft.CodeAnalysis.CSharp.ExtractMethod;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
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
            private readonly SemanticModel _semanticModel;

            private Reducer(SemanticModel semanticModel)
            {
                _semanticModel = semanticModel;
            }

            public static AnalyzedNode? Reduce(AnalyzedNode node, SemanticModel semanticModel)
            {
                var reducer = new Reducer(semanticModel);
                var result = reducer.Visit(node);
                return reducer._isNonTrivial ? result : null;
            }

            public AnalyzedNode? VisitConjunction(Conjunction node)
                => VisitConjunction(Visit(node.Left), Visit(node.Right));

            public AnalyzedNode? VisitPatternMatch(PatternMatch node)
                => VisitPatternMatch(node.Expression, Visit(node.Pattern));

            private AnalyzedNode? Visit(AnalyzedNode node)
                => node.Visit(this);

            private AnalyzedNode? VisitConjunction(AnalyzedNode? left, AnalyzedNode? right)
            {
                // Bail out in case previous intersection attempts have failed.
                if (left is null || right is null)
                {
                    return null;
                }

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
                            ? Visit(right, (Conjunction)left)
                            : Visit(left, (Conjunction)right);

                    case NodeKind.PatternMatch | NodeKind.PatternMatch:
                        return Visit((PatternMatch)left, (PatternMatch)right);

                    case NodeKind.PositionalPattern | NodeKind.PositionalPattern:
                        return Visit((PositionalPattern)left, (PositionalPattern)right);

                    case NodeKind.TypePattern | NodeKind.TypePattern:
                        return Visit((TypePattern)left, (TypePattern)right);

                    case NodeKind.Conjunction | NodeKind.PatternMatch:
                        return left.Kind == NodeKind.PatternMatch
                            ? Visit((Conjunction)right, (PatternMatch)left)
                            : Visit((Conjunction)left, (PatternMatch)right);

                    case NodeKind.PatternMatch | NodeKind.VarPattern:
                        return left.Kind == NodeKind.PatternMatch
                            ? Visit((VarPattern)right, (PatternMatch)left)
                            : Visit((VarPattern)left, (PatternMatch)right);

                    case NodeKind.PositionalPattern | NodeKind.PatternMatch:
                        return left.Kind == NodeKind.PatternMatch
                            ? Visit((PositionalPattern)right, (PatternMatch)left)
                            : Visit((PositionalPattern)left, (PatternMatch)right);

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

            private static AnalyzedNode? Visit(TypePattern left, TypePattern right)
                => AreEquivalent(left.Type, right.Type) ? left : null;

            private AnalyzedNode? Visit(AnalyzedNode node, Conjunction conjunction)
                // We deconstruct a conjunction to attempt the intersection recursively.
                // This way, we capture an invalid intersection if any of the nested pairs are invalid.
                => VisitConjunction(VisitConjunction(node, conjunction.Left), conjunction.Right);

            private AnalyzedNode? Visit(Conjunction conjunction, PatternMatch match)
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
                    return Conjunction.Create(VisitConjunction(conjunction.Left, match), conjunction.Right);
                }

                if (conjunction.Right.Contains(match.Expression))
                {
                    _isNonTrivial = true;
                    return Conjunction.Create(conjunction.Left, VisitConjunction(conjunction.Right, match));
                }

                // Otherwise, return both nodes, e.g. this could be another property pattern on the same object.
                return new Conjunction(conjunction, match);
            }

            private AnalyzedNode? Visit(PatternMatch leftMatch, PatternMatch rightMatch)
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
                    return PatternMatch.Create(leftMatch.Expression, VisitConjunction(leftMatch.Pattern, rightMatch.Pattern));
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
                    return PatternMatch.Create(leftMatch.Expression, VisitConjunction(leftMatch.Pattern, rightMatch));
                }

                if (rightMatch.Pattern.Contains(leftMatch.Expression))
                {
                    _isNonTrivial = true;
                    return PatternMatch.Create(rightMatch.Expression, VisitConjunction(rightMatch.Pattern, leftMatch));
                }

                return new Conjunction(leftMatch, rightMatch);
            }

            private AnalyzedNode Visit(VarPattern var, PatternMatch match)
            {
                // For example, in a tree of the following form:
                //
                //      e1 is var v && v is {Property: true}
                //
                // We'll try to combine `var v` and `v is {Property: true}`
                // because `v` is the common expression between the two.
                if (var.Contains(match.Expression))
                {
                    _isNonTrivial = true;

                    // The result would look like this:
                    //
                    //      PatternMatch(e1, 
                    //          Conjunction(VarPattern(v), PatternMatch(Property, ConstantPattern(true))))
                    //
                    // Note we don't pass match's expression (v) because it already exist on the outer match.
                    return new Conjunction(var, match.Pattern);
                }

                return new Conjunction(var, match);
            }

            private AnalyzedNode? Visit(PositionalPattern positional, PatternMatch match)
            {
                var subpatterns = positional.Subpatterns;
                for (var index = 0; index < subpatterns.Length; index++)
                {
                    var subpattern = subpatterns[index];
                    var pattern = subpattern.Pattern;

                    // Check if we can merge the pattern-match with either of subpatterns.
                    if (pattern.Contains(match.Expression))
                    {
                        var node = VisitConjunction(pattern, match.Pattern);
                        if (node is null)
                        {
                            return null;
                        }

                        return new PositionalPattern(subpatterns.SetItem(index, (subpattern.NameColonOpt, node)));
                    }
                }

                // Otherwise, return both nodes, this will be rewritten as a recursive pattern of the form `(p1, p2) {id: p3}`
                return new Conjunction(positional, match);
            }

            private AnalyzedNode? Visit(PositionalPattern left, PositionalPattern right)
            {
                var leftSubpatterns = left.Subpatterns;
                var rightSubpatterns = right.Subpatterns;

                if (leftSubpatterns.Length != rightSubpatterns.Length)
                {
                    return null;
                }

                var builder = ArrayBuilder<(NameColonSyntax?, AnalyzedNode)>.GetInstance(leftSubpatterns.Length);
                for (var index = 0; index < leftSubpatterns.Length; index++)
                {
                    var leftSub = leftSubpatterns[index];
                    var rightSub = rightSubpatterns[index];

                    var node = VisitConjunction(leftSub.Pattern, rightSub.Pattern);
                    if (node is null)
                    {
                        builder.Free();
                        return null;
                    }

                    builder.Add((leftSub.NameColonOpt ?? rightSub.NameColonOpt, node));
                }

                return new PositionalPattern(builder.ToImmutableAndFree());
            }

            private PatternMatch? VisitPatternMatch(ExpressionSyntax expression, AnalyzedNode? pattern)
            {
                if (pattern is null)
                {
                    return null;
                }

                // Try to expand a match of the form
                //
                //      expression.Property.AnotherProperty is A
                //
                // to
                //
                //      expression is {Property: {AnotherProperty: A}}
                //
                // so that we can check if the identifiers are matched in other places and merge.
                return VisitPatternMatchCore(expression, pattern) ?? new PatternMatch(expression, pattern);
            }

            private PatternMatch? VisitPatternMatchCore(ExpressionSyntax expression, AnalyzedNode pattern)
            {
                switch (expression)
                {
                    case IdentifierNameSyntax node:
                        return new PatternMatch(node, pattern);

                    case ParenthesizedExpressionSyntax node:
                        return VisitPatternMatchCore(node.Expression, pattern);

                    case MemberBindingExpressionSyntax node:
                        return VisitPatternMatchCore(node.Name, pattern);

                    case ConditionalAccessExpressionSyntax node:
                        return VisitPatternMatch(node.Expression, VisitPatternMatchCore(node.WhenNotNull, pattern));

                    case CastExpressionSyntax node when _semanticModel.GetTypeInfo(node.Type).Type.IsObjectType():
                        // Skip object cast as it has no effect to the pattern-match.
                        return VisitPatternMatchCore(node.Expression, pattern);

                    case CastExpressionSyntax node:
                        return VisitPatternMatch(node.Expression, VisitConjunction(new TypePattern(node.Type), pattern));

                    case MemberAccessExpressionSyntax { RawKind: (int)SyntaxKind.SimpleMemberAccessExpression } node
                        // The expression on the left must no be a type or namespace,
                        // because something like `TypeOrNamespace is P` is not valid.
                        when !(_semanticModel.GetSymbolInfo(node.Expression).Symbol is INamespaceOrTypeSymbol):
                        return VisitPatternMatch(node.Expression, VisitPatternMatchCore(node.Name, pattern));

                    case BinaryExpressionSyntax { RawKind: (int)SyntaxKind.AsExpression } node:
                        // Marking as non-trivial because we want to offer the fix for something like:
                        //
                        //      (e as T)?.Property == true -> e is T {Property: true}
                        //
                        _isNonTrivial = true;
                        return VisitPatternMatch(node.Left, VisitConjunction(new TypePattern((TypeSyntax)node.Right), pattern));

                    default:
                        return null;
                }
            }
        }
    }
}
