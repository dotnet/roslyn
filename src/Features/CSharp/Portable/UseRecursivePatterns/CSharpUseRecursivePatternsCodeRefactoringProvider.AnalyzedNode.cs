// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Simplification;
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

            public virtual AnalyzedNode? Visit(Reducer reducer) => this;
            public virtual bool Contains(ExpressionSyntax e) => false;
            public virtual void AddChildren(ArrayBuilder<AnalyzedNode> nodes) => nodes.Add(this);

            protected IReadOnlyCollection<AnalyzedNode> GetChildren()
            {
                var builder = ArrayBuilder<AnalyzedNode>.GetInstance();
                AddChildren(builder);
                return builder.ToArrayAndFree();
            }

            public virtual ExpressionSyntax AsExpressionSyntax() => throw ExceptionUtilities.UnexpectedValue(this.Kind);
            public virtual PatternSyntax AsPatternSyntax() => throw ExceptionUtilities.UnexpectedValue(this.Kind);

#if DEBUG
            public sealed override string? ToString()
            {
                return this switch
                {
                    VarPattern n => $"V:{n.Identifier}",
                    Evaluation n => $"E:{n.Expression}",
                    Conjunction n => $"{n.Left} AND {n.Right}",
                    TypePattern n => $"T:{n.Type}",
                    PatternMatch n => $"{n.Expression} is ({n.Pattern})",
                    DiscardPattern n => "_",
                    NotNullPattern n => "{}",
                    ConstantPattern n => $"C:{n.Expression}",
                    PositionalPattern n => $"P:({string.Join(", ", n.Subpatterns.Select(p => p.Pattern))})",
                    _ => null
                };
            }
#endif
        }

        // Represents a combination of either
        //
        //     - two operands of the &&-operator.
        //     - two patterns against the same expression.
        //
        // For example, if we have:
        //
        //      e1 is T v && v.Property
        //
        // We produce a tree of the following form:
        //
        //      Conjunction(
        //          PatternMatch(e1,
        //              Conjunction(TypePattern(T), VarPattern(v))),
        //          PatternMatch(v.Property, ConstantPattern(true)))
        //
        private sealed class Conjunction : AnalyzedNode
        {
            public readonly AnalyzedNode Left;
            public readonly AnalyzedNode Right;

            public override NodeKind Kind => NodeKind.Conjunction;

            public static Conjunction? Create(AnalyzedNode? left, AnalyzedNode? right)
                => left is null || right is null ? null : new Conjunction(left, right);

            public Conjunction(AnalyzedNode left, AnalyzedNode right)
                => (Left, Right) = (left, right);

            public override bool Contains(ExpressionSyntax e) => Left.Contains(e) || Right.Contains(e);

            public override AnalyzedNode? Visit(Reducer reducer) => reducer.VisitConjunction(this);

            public override void AddChildren(ArrayBuilder<AnalyzedNode> nodes)
            {
                Left.AddChildren(nodes);
                Right.AddChildren(nodes);
            }

            public override PatternSyntax AsPatternSyntax()
            {
                var (pattern, conditionOpt) = RewriteChildren();
                // If we're rewriting this as a pattern, there should be no
                // evaluation node left. So we'd expect condition to be null.
                Debug.Assert(conditionOpt is null);
                return pattern;
            }

            public CasePatternSwitchLabelSyntax AsCasePatternSwitchLabelSyntax()
            {
                var (pattern, conditionOpt) = RewriteChildren();
                return CasePatternSwitchLabel(pattern, AsWhenClauseSyntax(conditionOpt), Token(SyntaxKind.ColonToken));
            }

            public SwitchExpressionArmSyntax AsSwitchExpressionArmSyntax(ExpressionSyntax expression)
            {
                var (pattern, conditionOpt) = RewriteChildren();
                return SwitchExpressionArm(pattern, AsWhenClauseSyntax(conditionOpt), expression);
            }

            private (PatternSyntax, ExpressionSyntax?) RewriteChildren()
            {
                // Descend into children nodes and construct a recursive pattern.
                // We also capture remaining evaluation nodes to be used in the when-clause.
                // Note that we can't rely on Left or Right to be an evaluation node,
                // since these could be nested anywhere in the reduced tree.
                var children = GetChildren();
                var pattern = RecursivePattern(
                    children.OfType<TypePattern>().SingleOrDefault()?.Type,
                    children.OfType<PositionalPattern>().SingleOrDefault()?.AsPositionalPatternClauseSyntax(),
                    AsPropertyPatternClauseSyntax(children.OfType<PatternMatch>().Select(match => match.AsSubpatternSyntax()).ToArray()),
                    children.OfType<VarPattern>().SingleOrDefault()?.AsVariableDesignationSyntax());
                var conditionOpt = children.OfType<Evaluation>().Select(e => e.Expression).AggregateOrDefault(
                    (left, right) => BinaryExpression(SyntaxKind.LogicalAndExpression, left, right));
                return (pattern.WithAdditionalAnnotations(Simplifier.Annotation), conditionOpt);
            }

            private static BinaryExpressionSyntax GetLogicalAndExpression(ExpressionSyntax left, ExpressionSyntax right)
                => BinaryExpression(SyntaxKind.LogicalAndExpression, left: left, right: right.WithTrailingTrivia(),
                    operatorToken: GetToken(SyntaxKind.AmpersandAmpersandToken,
                        newlineAfter: (right is IsPatternExpressionSyntax p ? p.Expression : right).Span.Length > 50));

            public override ExpressionSyntax AsExpressionSyntax()
                => GetLogicalAndExpression(Left.AsExpressionSyntax(), Right.AsExpressionSyntax());

            private static WhenClauseSyntax? AsWhenClauseSyntax(ExpressionSyntax? e)
                => e is null ? null : WhenClause(e);
        }

        // Represents a pattern-match against some expression.
        // The following expressions will produce a pattern-match node:
        //
        //      - IsExpression
        //      - IsPatternExpression
        //      - EqualsExpression
        //      - PrefixUnaryExpression
        //          (only from !-operator, we'll synthesize a ConstantPattern(false) if appropriate)
        //      - MemberAccessExpression
        //          (we'll synthesize a match with ConstantPattern(true) if appropriate)
        //
        // All these will be eventually converted back to an IsPatternExpression syntax node.
        private sealed class PatternMatch : AnalyzedNode
        {
            public readonly ExpressionSyntax Expression;
            public readonly AnalyzedNode Pattern;

            public override NodeKind Kind => NodeKind.PatternMatch;

            public static AnalyzedNode? Create(ExpressionSyntax expression, AnalyzedNode? pattern)
                => pattern is null ? null : new PatternMatch(expression, pattern);

            public PatternMatch(ExpressionSyntax expression, AnalyzedNode pattern)
                => (Expression, Pattern) = (expression, pattern);

            public override bool Contains(ExpressionSyntax e)
                => AreEquivalent(this.Expression, e) || Pattern.Contains(e);

            public override AnalyzedNode? Visit(Reducer reducer)
                => reducer.VisitPatternMatch(this);

            // Every time we make a nested pattern-match, we should make sure that we have an identifier on the left,
            public SubpatternSyntax AsSubpatternSyntax()
                => Subpattern(NameColon((IdentifierNameSyntax)Expression), Pattern.AsPatternSyntax());

            public override PatternSyntax AsPatternSyntax()
                => RecursivePattern(null, null,
                    AsPropertyPatternClauseSyntax(ImmutableArray.Create(AsSubpatternSyntax())), null);

            public override ExpressionSyntax AsExpressionSyntax()
                => IsPatternExpression(Expression, Pattern.AsPatternSyntax());
        }

        private sealed class ConstantPattern : AnalyzedNode
        {
            public readonly ExpressionSyntax Expression;

            public override NodeKind Kind => NodeKind.ConstantPattern;

            public ConstantPattern(ExpressionSyntax expression)
                => Expression = expression;

            public override PatternSyntax AsPatternSyntax()
                => ConstantPattern(Expression.WithoutTrivia());
        }

        // Represent the not-null pattern. We make this node in the following places:
        //
        //      - if we have an explicit null check in code: `e != null`
        //      - if we have a type-check with identity or implicit reference conversion `e is object`
        //      - if we have a recursive pattern of the form `{}` in code.
        // 
        // This will be rewritten as a recursive pattern with an empty property pattern clause ({})
        // which implies a null-check.
        private sealed class NotNullPattern : AnalyzedNode
        {
            public static readonly AnalyzedNode Instance = new NotNullPattern();

            public override NodeKind Kind => NodeKind.NotNullPattern;

            private NotNullPattern() { }

            public override PatternSyntax AsPatternSyntax()
                => RecursivePattern(null, null, AsPropertyPatternClauseSyntax(), null);

            private static PropertyPatternClauseSyntax AsPropertyPatternClauseSyntax()
                => PropertyPatternClause(
                    GetToken(SyntaxKind.OpenBraceToken),
                    SeparatedList<SubpatternSyntax>(),
                    GetToken(SyntaxKind.CloseBraceToken));
        }

        private sealed class DiscardPattern : AnalyzedNode
        {
            public static readonly AnalyzedNode Instance = new DiscardPattern();

            public override NodeKind Kind => NodeKind.DiscardPattern;

            private DiscardPattern() { }

            public override PatternSyntax AsPatternSyntax()
                => DiscardPattern();
        }

        // Represent a type-check. We make this node from the following:
        //
        //      - A direct cast: (T)e
        //          (note that we have a slight semantic change here since (T)e could throw)
        //      - A safe cast: e as T
        //      - A declaration pattern: T t
        //      - A type test: is T
        // 
        // This can be morphed into a recursive pattern or rewritten as a standalone declaration pattern.
        private sealed class TypePattern : AnalyzedNode
        {
            public readonly TypeSyntax Type;

            public override NodeKind Kind => NodeKind.TypePattern;

            public TypePattern(TypeSyntax type) => Type = type;

            public override PatternSyntax AsPatternSyntax()
                => DeclarationPattern(Type, DiscardDesignation(GetToken(SyntaxKind.UnderscoreToken)));

            public static AnalyzedNode? Create(TypeSyntax? type)
                => type is null ? null : new TypePattern(type);
        }

        // Represents a variable designation in a var-pattern.
        //
        //   e is var v -> PatternMatch(e, VarPattern(v))
        //   e is var(x, y) -> PatternMatch(e, PositionalPattern(VarPattern(x), VarPattern(y)))
        //
        private sealed class VarPattern : AnalyzedNode
        {
            public readonly SyntaxToken Identifier;

            public override NodeKind Kind => NodeKind.VarPattern;

            public VarPattern(SyntaxToken identifier)
            {
                Debug.Assert(!identifier.IsKind(SyntaxKind.None));
                Identifier = identifier;
            }

            public override bool Contains(ExpressionSyntax e)
                => e is IdentifierNameSyntax name && AreEquivalent(name.Identifier, Identifier);

            public override PatternSyntax AsPatternSyntax()
                => VarPattern(AsVariableDesignationSyntax());

            public VariableDesignationSyntax AsVariableDesignationSyntax()
                => SingleVariableDesignation(Identifier.WithLeadingTrivia(Space).WithTrailingTrivia());
        }

        // An arbitrary expression. We capture side-effects and other nodes as an evaluation.
        // For example, if we have:
        //
        //      e is var v && v.Contains(42)
        // 
        // We'll produce the following tree:
        //
        //      Conjunction(
        //          PatternMatch(e, VarPattern(42)),
        //          Evaluation(x.Contains(42)))
        //
        private sealed class Evaluation : AnalyzedNode
        {
            public readonly ExpressionSyntax Expression;

            public override NodeKind Kind => NodeKind.Evaluation;

            public Evaluation(ExpressionSyntax expression) => Expression = expression;

            public override ExpressionSyntax AsExpressionSyntax() => Expression;
        }

        // A positional-pattern. Example:
        //
        //      e is (42, {}) -> PatternMatch(e, PositionalPattern(ConstantPattern(42), NotNullPattern))
        //
        private sealed class PositionalPattern : AnalyzedNode
        {
            public readonly ImmutableArray<(NameColonSyntax? NameColonOpt, AnalyzedNode Pattern)> Subpatterns;

            public override NodeKind Kind => NodeKind.PositionalPattern;

            public PositionalPattern(ImmutableArray<(NameColonSyntax?, AnalyzedNode)> subpatterns)
                => Subpatterns = subpatterns;

            public override bool Contains(ExpressionSyntax e)
                => Subpatterns.Any(sub => sub.Pattern.Contains(e));

            public override PatternSyntax AsPatternSyntax()
                => RecursivePattern(null, AsPositionalPatternClauseSyntax(), null, null);

            public PositionalPatternClauseSyntax AsPositionalPatternClauseSyntax()
                => PositionalPatternClause(SeparatedList(
                    Subpatterns.Select(sub => Subpattern(sub.NameColonOpt, sub.Pattern.AsPatternSyntax())),
                    GetSeparators(Subpatterns.Length - 1, multiline: false)));

            public static AnalyzedNode? Create(IEnumerable<(NameColonSyntax? NameColon, AnalyzedNode)>? subpatterns)
                => subpatterns is null ? null : new PositionalPattern(subpatterns.ToImmutableArray());
        }

        // Helpers

        private static SyntaxToken GetToken(SyntaxKind token, bool newlineBefore = false, bool newlineAfter = false)
            => Token(newlineBefore ? TriviaList(ElasticCarriageReturnLineFeed) : default,
                     token,
                     newlineAfter ? TriviaList(ElasticCarriageReturnLineFeed) : default);

        private static IEnumerable<SyntaxToken> GetSeparators(int count, bool multiline)
            => ArrayBuilder<SyntaxToken>.GetInstance(count,
                GetToken(SyntaxKind.CommaToken, newlineAfter: multiline)).ToArrayAndFree();

        private static PropertyPatternClauseSyntax AsPropertyPatternClauseSyntax(IReadOnlyCollection<SubpatternSyntax> subpatterns)
        {
            var multiline = subpatterns.Sum(x => x.Span.Length) > 50;
            return PropertyPatternClause(
                GetToken(SyntaxKind.OpenBraceToken, newlineAfter: multiline),
                SeparatedList(subpatterns, GetSeparators(subpatterns.Count - 1, multiline)),
                GetToken(SyntaxKind.CloseBraceToken, newlineBefore: multiline));
        }
    }
}
