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

            public virtual AnalyzedNode Reduce(Reducer reducer) => this;
            public virtual bool Contains(ExpressionSyntax e) => false;
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

            public override AnalyzedNode Reduce(Reducer reducer) => reducer.ReduceConjuction(this);

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
                    AsWhenClause(children.OfType<Evaluation>().Select(e => e.Expression).AggregateOrDefault(
                        (left, right) => BinaryExpression(SyntaxKind.LogicalAndExpression, left, right))),
                    Token(SyntaxKind.ColonToken));

            private static WhenClauseSyntax AsWhenClause(ExpressionSyntax e) => e is null ? null : WhenClause(e);
        }

        private sealed class PatternMatch : AnalyzedNode
        {
            public readonly ExpressionSyntax Expression;
            public readonly AnalyzedNode Pattern;

            public override NodeKind Kind => NodeKind.PatternMatch;

            public static AnalyzedNode Create(ExpressionSyntax expression, AnalyzedNode pattern)
            {
                if (pattern is null)
                    return null;
                return new PatternMatch(expression, pattern);
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

            public override AnalyzedNode Reduce(Reducer reducer) => reducer.ReducePatternMatch(this);

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
