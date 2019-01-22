// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
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
        }

        private enum RewriteMode
        {
            Pattern,
            Expression,
            SwitchPatternLabel,
        }

        private abstract class AnalyzedNode
        {
            public abstract NodeKind Kind { get; }

            public virtual bool Contains(ExpressionSyntax e) => false;
            public virtual bool CanReduce => false;
            public virtual AnalyzedNode Reduce() => this;
            public virtual void GetChildren(List<AnalyzedNode> nodes) => nodes.Add(this);

            public abstract SyntaxNode Rewrite(RewriteMode mode);
            public abstract override string ToString();
        }

        private sealed class Conjuction : AnalyzedNode
        {
            public readonly AnalyzedNode Left;
            public readonly AnalyzedNode Right;

            public override NodeKind Kind => NodeKind.Conjunction;

            public Conjuction(AnalyzedNode left, AnalyzedNode right)
            {
                Debug.Assert(left != null);
                Debug.Assert(right != null);
                Left = left;
                Right = right;
            }

            public override string ToString() => $"{Left} AND {Right}";

            public override bool Contains(ExpressionSyntax e) => Left.Contains(e) || Right.Contains(e);

            public override AnalyzedNode Reduce() => Union(Left.Reduce(), Right.Reduce());

            public override void GetChildren(List<AnalyzedNode> nodes)
            {
                Left.GetChildren(nodes);
                Right.GetChildren(nodes);
            }

            public override SyntaxNode Rewrite(RewriteMode mode)
            {
                if (mode == RewriteMode.Expression)
                {
                    return BinaryExpression(SyntaxKind.LogicalAndExpression,
                        (ExpressionSyntax)Left.Rewrite(RewriteMode.Expression),
                        (ExpressionSyntax)Right.Rewrite(RewriteMode.Expression));
                }

                var nodes = new List<AnalyzedNode>();
                GetChildren(nodes);

                var pattern = RecursivePattern(
                    nodes.OfType<TypePattern>().SingleOrDefault()?.Type,
                    null,
                    PropertyPatternClause(SeparatedList(nodes.OfType<PatternMatch>().Select(match => match.Rewrite(RewriteMode.Pattern)))),
                    nodes.OfType<VarPattern>().SingleOrDefault() is VarPattern v ? SingleVariableDesignation(v.Identifier) : null);

                if (mode == RewriteMode.Pattern)
                {
                    return pattern;
                }

                return CasePatternSwitchLabel(pattern,
                    nodes.OfType<Evaluation>().SingleOrDefault()?.Expression is ExpressionSyntax e ? WhenClause(e) : null,
                    Token(SyntaxKind.ColonToken));
            }

            private static AnalyzedNode UnionCore(Conjuction conjuction, PatternMatch match)
            {
                if (conjuction.Left.Contains(match.Expression))
                {
                    return new Conjuction(Union(conjuction.Left, match), conjuction.Right);
                }

                if (conjuction.Right.Contains(match.Expression))
                {
                    return new Conjuction(conjuction.Left, Union(conjuction.Right, match));
                }

                return new Conjuction(conjuction, match);
            }

            private static AnalyzedNode UnionCore(PatternMatch leftMatch, PatternMatch rightMatch)
            {
                if (AreEquivalent(leftMatch.Expression, rightMatch.Expression))
                {
                    return new PatternMatch(leftMatch.Expression, Union(leftMatch.Pattern, rightMatch.Pattern));
                }

                if (leftMatch.Pattern.Contains(rightMatch.Expression))
                {
                    return new PatternMatch(leftMatch.Expression, Union(leftMatch.Pattern, rightMatch));
                }

                if (rightMatch.Pattern.Contains(leftMatch.Expression))
                {
                    return new PatternMatch(rightMatch.Expression, Union(rightMatch.Pattern, leftMatch));
                }

                return new Conjuction(leftMatch, rightMatch);
            }

            private static AnalyzedNode UnionCore(VarPattern left, PatternMatch right)
            {
                if (left.Contains(right.Expression))
                {
                    return new Conjuction(left, right.Pattern);
                }

                return new Conjuction(left, right);
            }

            private static AnalyzedNode Union(AnalyzedNode left, AnalyzedNode right)
            {
                // Since the bitwise-OR operator is symmetrical, each case covers both orderings for each pair.
                // We always have C(N+1, 2) cases where N is the number of kinds. So the following switch is
                // exhaustive at the moment. The +1 addition accounts for union logic of each node with itself.
                switch (left.Kind | right.Kind)
                {
                    case NodeKind.Conjunction | NodeKind.Conjunction:
                        var conjuction = (Conjuction)right;
                        return Union(Union(left, conjuction.Left), conjuction.Right);

                    case NodeKind.PatternMatch | NodeKind.PatternMatch:
                        return UnionCore((PatternMatch)left, (PatternMatch)right);

                    case NodeKind.Conjunction | NodeKind.PatternMatch:
                        return left.Kind == NodeKind.PatternMatch
                            ? UnionCore((Conjuction)right, (PatternMatch)left)
                            : UnionCore((Conjuction)left, (PatternMatch)right);

                    case NodeKind.PatternMatch | NodeKind.VarPattern:
                        return left.Kind == NodeKind.PatternMatch
                            ? UnionCore((VarPattern)right, (PatternMatch)left)
                            : UnionCore((VarPattern)left, (PatternMatch)right);

                    case NodeKind.TypePattern | NodeKind.VarPattern:
                    case NodeKind.PatternMatch | NodeKind.TypePattern:
                    case NodeKind.NotNullPattern | NodeKind.VarPattern:
                    case NodeKind.Conjunction | NodeKind.TypePattern:
                    case NodeKind.Conjunction | NodeKind.VarPattern:
                    case NodeKind.Evaluation | NodeKind.Conjunction:
                    case NodeKind.Evaluation | NodeKind.PatternMatch:
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
                    case NodeKind.Evaluation | NodeKind.Evaluation:
                    case NodeKind.Evaluation | NodeKind.ConstantPattern:
                    case NodeKind.Evaluation | NodeKind.NotNullPattern:
                    case NodeKind.Evaluation | NodeKind.TypePattern:
                    case NodeKind.Evaluation | NodeKind.VarPattern:
                    case NodeKind.TypePattern | NodeKind.TypePattern:
                    case NodeKind.VarPattern | NodeKind.VarPattern:
                        return null;

                    case var value:
                        throw ExceptionUtilities.UnexpectedValue(value);
                }
            }
        }

        private sealed class PatternMatch : AnalyzedNode
        {
            public readonly ExpressionSyntax Expression;
            public readonly AnalyzedNode Pattern;

            public override NodeKind Kind => NodeKind.PatternMatch;

            public override bool CanReduce => throw new NotImplementedException();

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

            public override AnalyzedNode Reduce() => MakePatternMatch(Expression, Pattern.Reduce());

            private static PatternSyntax AsPattern(SyntaxNode node)
            {
                switch (node)
                {
                    case PatternSyntax pattern:
                        return pattern;
                    case SubpatternSyntax subpattern:
                        return RecursivePattern(null, null, PropertyPatternClause(SingletonSeparatedList(subpattern)), null);
                    case var value:
                        throw ExceptionUtilities.UnexpectedValue(value.Kind());
                }
            }

            public override SyntaxNode Rewrite(RewriteMode mode)
            {
                switch (mode)
                {
                    case RewriteMode.Pattern:
                        return Subpattern(NameColon((IdentifierNameSyntax)Expression), AsPattern(Pattern.Rewrite(RewriteMode.Pattern)));
                    case RewriteMode.Expression:
                        return IsPatternExpression(Expression, AsPattern(Pattern.Rewrite(RewriteMode.Pattern)));
                    case var value:
                        throw ExceptionUtilities.UnexpectedValue(value);
                }
            }
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

            public override SyntaxNode Rewrite(RewriteMode mode)
            {
                Debug.Assert(mode == RewriteMode.Pattern);
                return ConstantPattern(Expression);
            }
        }

        private sealed class NotNullPattern : AnalyzedNode
        {
            public readonly static NotNullPattern Instance = new NotNullPattern();

            public override NodeKind Kind => NodeKind.NotNullPattern;

            private NotNullPattern() { }

            public override string ToString() => "{}";

            public override SyntaxNode Rewrite(RewriteMode mode)
            {
                Debug.Assert(mode == RewriteMode.Pattern);
                return RecursivePattern(null, null, PropertyPatternClause(SeparatedList<SubpatternSyntax>()), null);
            }
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

            public override SyntaxNode Rewrite(RewriteMode mode)
            {
                Debug.Assert(mode == RewriteMode.Pattern);
                return DeclarationPattern(Type, DiscardDesignation());
            }
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
            {
                return e is IdentifierNameSyntax id &&
                    SyntaxFactory.AreEquivalent(id.Identifier, this.Identifier);
            }

            public override SyntaxNode Rewrite(RewriteMode mode)
            {
                Debug.Assert(mode == RewriteMode.Pattern);
                return SingleVariableDesignation(Identifier);
            }
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

            public override SyntaxNode Rewrite(RewriteMode mode)
            {
                Debug.Assert(mode == RewriteMode.Expression);
                return Expression;
            }
        }
    }
}
