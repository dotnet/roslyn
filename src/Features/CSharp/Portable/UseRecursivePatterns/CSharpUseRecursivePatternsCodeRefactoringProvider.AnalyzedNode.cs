// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseRecursivePatterns
{
    using static SyntaxFactory;

    internal sealed partial class CSharpUseRecursivePatternsCodeRefactoringProvider
    {
        [Flags]
        private enum NodeKind
        {
            Conjunction = 1,
            VarPattern = 1 << 1,
            TypePattern = 1 << 2,
            NotNullPattern = 1 << 3,
            ConstantPattern = 1 << 4,
            PatternMatch = 1 << 5,
            Evaluation = 1 << 6,
            PositionalPattern = 1 << 7,
            DiscardPattern = 1 << 8,
        }

        private abstract class AnalyzedNode
        {
            public abstract NodeKind Kind { get; }

            public virtual bool IsReduced { get; private set; }

            public AnalyzedNode MarkAsReduced()
            {
                this.IsReduced = true;
                return this;
            }

            public virtual bool Contains(ExpressionSyntax e) => false;

            public virtual AnalyzedNode Reduce() => this;
            public virtual void GetChildren(List<AnalyzedNode> nodes) => nodes.Add(this);

            public virtual ExpressionSyntax AsExpressionSyntax() => throw ExceptionUtilities.UnexpectedValue(this);
            public virtual PatternSyntax AsPatternSyntax() => throw ExceptionUtilities.UnexpectedValue(this);

            public abstract override string ToString();
        }

        private sealed class Conjunction : AnalyzedNode
        {
            public readonly AnalyzedNode Left;
            public readonly AnalyzedNode Right;

            public override NodeKind Kind => NodeKind.Conjunction;

            public static Conjunction Create(AnalyzedNode left, AnalyzedNode right)
            {
                if (left is null || right is null)
                    return null;

                return new Conjunction(left, right);
            }

            public Conjunction(AnalyzedNode left, AnalyzedNode right)
            {
                Debug.Assert(left != null);
                Debug.Assert(right != null);
                Left = left;
                Right = right;
            }

            public override string ToString() => $"{Left} AND {Right}";

            public override bool Contains(ExpressionSyntax e) => Left.Contains(e) || Right.Contains(e);

            public override bool IsReduced => base.IsReduced || Left.IsReduced || Right.IsReduced;

            public override AnalyzedNode Reduce() => Intersection(Left.Reduce(), Right.Reduce());

            public override void GetChildren(List<AnalyzedNode> nodes)
            {
                Left.GetChildren(nodes);
                Right.GetChildren(nodes);
            }

            public override PatternSyntax AsPatternSyntax() => AsPatternSyntax(out _);

            private PatternSyntax AsPatternSyntax(out List<AnalyzedNode> children)
            {
                children = new List<AnalyzedNode>();
                GetChildren(children);
                return RecursivePattern(
                    children.OfType<TypePattern>().SingleOrDefault()?.Type,
                    children.OfType<PositionalPattern>().SingleOrDefault()?.AsPositionalPatternClauseSyntax(),
                    PropertyPatternClause(SeparatedList(
                        children.OfType<PatternMatch>().Select(match => match.AsSubpatternSyntax()))),
                    children.OfType<VarPattern>().SingleOrDefault()?.AsVariableDesignationSyntax());
            }

            public override ExpressionSyntax AsExpressionSyntax()
                => BinaryExpression(SyntaxKind.LogicalAndExpression,
                    Left.AsExpressionSyntax(), Right.AsExpressionSyntax());

            public SyntaxNode AsCasePatternSwitchLabelSyntax()
                => CasePatternSwitchLabel(AsPatternSyntax(out var children),
                    children.OfType<Evaluation>().Select(x => x.Expression).ToArray() is var v && v.Length > 0
                        ? WhenClause(v.Aggregate(
                            (left, right) => BinaryExpression(SyntaxKind.LogicalAndExpression, left, right)))
                        : null,
                    Token(SyntaxKind.ColonToken));

            private static AnalyzedNode Intersection(AnalyzedNode left, AnalyzedNode right)
            {
                if (left is null || right is null)
                    return null;

                // Since the bitwise-OR operator is symmetrical, each case covers both orderings for each pair.
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
                        return new Conjunction(left, right);

                    case NodeKind.DiscardPattern | NodeKind.Conjunction:
                    case NodeKind.DiscardPattern | NodeKind.TypePattern:
                    case NodeKind.DiscardPattern | NodeKind.PatternMatch:
                    case NodeKind.DiscardPattern | NodeKind.NotNullPattern:
                    case NodeKind.DiscardPattern | NodeKind.VarPattern:
                    case NodeKind.DiscardPattern | NodeKind.ConstantPattern:
                    case NodeKind.DiscardPattern | NodeKind.PositionalPattern:
                        return left.Kind == NodeKind.DiscardPattern ? right : left;

                    case NodeKind.NotNullPattern | NodeKind.Conjunction:
                    case NodeKind.NotNullPattern | NodeKind.TypePattern:
                    case NodeKind.NotNullPattern | NodeKind.PatternMatch:
                    case NodeKind.NotNullPattern | NodeKind.PositionalPattern:
                        return left.Kind == NodeKind.NotNullPattern ? right : left;

                    case NodeKind.DiscardPattern | NodeKind.DiscardPattern:
                    case NodeKind.NotNullPattern | NodeKind.NotNullPattern:
                        return left;

                    case NodeKind.ConstantPattern | NodeKind.PositionalPattern:
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

            private static AnalyzedNode IntersectionCore(AnalyzedNode node, Conjunction conjunction)
            {
                return Intersection(Intersection(node, conjunction.Left), conjunction.Right);
            }

            private static AnalyzedNode IntersectionCore(Conjunction conjunction, PatternMatch match)
            {
                if (conjunction.Left.Contains(match.Expression))
                {
                    return Conjunction.Create(Intersection(conjunction.Left, match.MarkAsReduced()), conjunction.Right);
                }

                if (conjunction.Right.Contains(match.Expression))
                {
                    return Conjunction.Create(conjunction.Left, Intersection(conjunction.Right, match.MarkAsReduced()));
                }

                return new Conjunction(conjunction, match);
            }

            private static AnalyzedNode IntersectionCore(PatternMatch leftMatch, PatternMatch rightMatch)
            {
                if (AreEquivalent(leftMatch.Expression, rightMatch.Expression))
                {
                    return PatternMatch.Create(leftMatch.Expression, Intersection(leftMatch.Pattern, rightMatch.Pattern));
                }

                if (leftMatch.Pattern.Contains(rightMatch.Expression))
                {
                    return PatternMatch.Create(leftMatch.Expression, Intersection(leftMatch.Pattern, rightMatch));
                }

                if (rightMatch.Pattern.Contains(leftMatch.Expression))
                {
                    return PatternMatch.Create(rightMatch.Expression, Intersection(rightMatch.Pattern, leftMatch));
                }

                return new Conjunction(leftMatch, rightMatch);
            }

            private static AnalyzedNode IntersectionCore(VarPattern var, PatternMatch match)
            {
                if (var.Contains(match.Expression))
                {
                    return new Conjunction(var.MarkAsReduced(), match.Pattern);
                }

                return new Conjunction(var, match);
            }

            private static AnalyzedNode IntersectionCore(PositionalPattern positional, PatternMatch match)
            {
                var subpatterns = new List<(NameColonSyntax, AnalyzedNode)>(positional.Subpatterns.Length);
                foreach (var sub in positional.Subpatterns)
                {
                    if (!sub.Pattern.Contains(match.Expression))
                    {
                        subpatterns.Add(sub);
                        continue;
                    }

                    var newSub = Intersection(sub.Pattern, match.Pattern);
                    if (newSub is null)
                        return null;

                    subpatterns.Add((sub.NameColonOpt, newSub));
                }

                return new PositionalPattern(subpatterns.ToImmutableArray());
            }

            private static AnalyzedNode IntersectionCore(PositionalPattern left, PositionalPattern right)
            {
                if (left.Subpatterns.Length == right.Subpatterns.Length)
                {
                    return new PositionalPattern(left.Subpatterns.ZipAsArray(right.Subpatterns,
                        (leftSub, rightSub) => (leftSub.NameColonOpt ?? rightSub.NameColonOpt, Intersection(leftSub.Pattern, rightSub.Pattern))));
                }

                return null;
            }
        }

        private sealed class PatternMatch : AnalyzedNode
        {
            public readonly ExpressionSyntax Expression;
            public readonly AnalyzedNode Pattern;

            public override NodeKind Kind => NodeKind.PatternMatch;

            public override bool IsReduced => base.IsReduced || Pattern.IsReduced;

            public static AnalyzedNode Create(ExpressionSyntax expression, AnalyzedNode pattern)
            {
                if (pattern is null)
                    return null;
                return new PatternMatch(expression, pattern).MarkAsReduced();
            }

            public PatternMatch(ExpressionSyntax expression, AnalyzedNode pattern)
            {
                Debug.Assert(expression != null);
                Debug.Assert(pattern != null);
                Expression = expression;
                Pattern = pattern;
            }

            public override string ToString() => $"{Expression} is ({Pattern})";

            public override bool Contains(ExpressionSyntax e)
                => AreEquivalent(this.Expression, e) || Pattern.Contains(e);

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
                            new Conjunction(new TypePattern((TypeSyntax)asExpression.Right).MarkAsReduced(),
                                MakePatternMatch(conditionalAccess.WhenNotNull, pattern)));

                    case SyntaxKind.SimpleMemberAccessExpression:
                        var memberAccess = (MemberAccessExpressionSyntax)expression;
                        return MakePatternMatch(memberAccess.Expression,
                            new PatternMatch(memberAccess.Name, pattern));

                    case var value:
                        throw ExceptionUtilities.UnexpectedValue(value);
                }
            }

            public override AnalyzedNode Reduce()
                => MakePatternMatch(Expression, Pattern.Reduce());

            public SubpatternSyntax AsSubpatternSyntax()
                => Subpattern(NameColon((IdentifierNameSyntax)Expression), Pattern.AsPatternSyntax());

            public override PatternSyntax AsPatternSyntax()
                => RecursivePattern(null, null, PropertyPatternClause(SingletonSeparatedList(AsSubpatternSyntax())), null);

            public override ExpressionSyntax AsExpressionSyntax()
                => IsPatternExpression(Expression, Pattern.AsPatternSyntax());
        }

        private sealed class ConstantPattern : AnalyzedNode
        {
            public readonly ExpressionSyntax Expression;

            public override NodeKind Kind => NodeKind.ConstantPattern;

            public ConstantPattern(ExpressionSyntax expression)
            {
                Debug.Assert(expression != null);
                Expression = expression;
            }

            public override string ToString() => Expression.ToString();

            public override PatternSyntax AsPatternSyntax()
                => ConstantPattern(Expression);
        }

        private sealed class NotNullPattern : AnalyzedNode
        {
            public readonly static AnalyzedNode Instance = new NotNullPattern();

            public override NodeKind Kind => NodeKind.NotNullPattern;

            private NotNullPattern() { }

            public override string ToString() => "{}";

            public override PatternSyntax AsPatternSyntax()
                => RecursivePattern(null, null, PropertyPatternClause(SeparatedList<SubpatternSyntax>()), null);
        }

        private sealed class DiscardPattern : AnalyzedNode
        {
            public readonly static AnalyzedNode Instance = new DiscardPattern();

            public override NodeKind Kind => NodeKind.DiscardPattern;

            private DiscardPattern() { }

            public override string ToString() => "_";

            public override PatternSyntax AsPatternSyntax()
                => DiscardPattern();
        }

        private sealed class TypePattern : AnalyzedNode
        {
            public readonly TypeSyntax Type;

            public override NodeKind Kind => NodeKind.TypePattern;

            public TypePattern(TypeSyntax type)
            {
                Debug.Assert(type != null);
                Type = type;
            }

            public override string ToString() => Type.ToString();

            public override PatternSyntax AsPatternSyntax()
                => DeclarationPattern(Type, DiscardDesignation());
        }

        private sealed class VarPattern : AnalyzedNode
        {
            public readonly SyntaxToken Identifier;

            public override NodeKind Kind => NodeKind.VarPattern;

            public VarPattern(SyntaxToken identifier)
            {
                Debug.Assert(!identifier.IsKind(SyntaxKind.None));
                Identifier = identifier;
            }

            public override string ToString() => $"var {Identifier}";

            public override bool Contains(ExpressionSyntax e)
                => e.IsKind(SyntaxKind.IdentifierName, out IdentifierNameSyntax name) &&
                    SyntaxFactory.AreEquivalent(name.Identifier, Identifier);

            public override PatternSyntax AsPatternSyntax()
                => SyntaxFactory.VarPattern(AsVariableDesignationSyntax());

            public VariableDesignationSyntax AsVariableDesignationSyntax()
                => SingleVariableDesignation(Identifier);
        }

        private sealed class Evaluation : AnalyzedNode
        {
            public readonly ExpressionSyntax Expression;

            public override NodeKind Kind => NodeKind.Evaluation;

            public Evaluation(ExpressionSyntax expression)
            {
                Debug.Assert(expression != null);
                Expression = expression;
            }

            public override string ToString() => $"{Expression}";

            public override ExpressionSyntax AsExpressionSyntax() => Expression;
        }

        private sealed class PositionalPattern : AnalyzedNode
        {
            public readonly ImmutableArray<(NameColonSyntax NameColonOpt, AnalyzedNode Pattern)> Subpatterns;

            public override NodeKind Kind => NodeKind.PositionalPattern;

            public PositionalPattern(ImmutableArray<(NameColonSyntax, AnalyzedNode)> subpatterns)
            {
                Subpatterns = subpatterns;
            }

            public override string ToString() => $"({string.Join(", ", this.Subpatterns)})";

            public override bool Contains(ExpressionSyntax e)
                => Subpatterns.Any(sub => sub.Pattern.Contains(e));

            public override PatternSyntax AsPatternSyntax()
                => RecursivePattern(null, AsPositionalPatternClauseSyntax(), null, null);

            public PositionalPatternClauseSyntax AsPositionalPatternClauseSyntax()
                => PositionalPatternClause(SeparatedList(
                    Subpatterns.Select(sub => Subpattern(sub.NameColonOpt, sub.Pattern.AsPatternSyntax()))));
        }
    }
}
