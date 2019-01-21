// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;
using Microsoft.CodeAnalysis.PooledObjects;


namespace Microsoft.CodeAnalysis.CSharp.UseRecursivePatterns
{
    using Microsoft.CodeAnalysis.CSharp.Extensions;
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
        }

        private abstract class AnalyzedNode
        {
            public abstract NodeKind Kind { get; }

            public virtual bool Contains(ExpressionSyntax e) => false;
            public virtual bool CanReduce => false;
            public virtual AnalyzedNode Reduce() => this;

            public abstract SyntaxNode Rewrite(bool asPattern);
            public abstract override string ToString();
        }

        private sealed class Conjuction : AnalyzedNode
        {
            public readonly AnalyzedNode Left;
            public readonly AnalyzedNode Right;

            public Conjuction(AnalyzedNode left, AnalyzedNode right)
            {
                Debug.Assert(left != null);
                Debug.Assert(right != null);
                Left = left;
                Right = right;
            }

            public override NodeKind Kind => NodeKind.Conjunction;
            public override string ToString() => $"{Left} AND {Right}";

            public override bool Contains(ExpressionSyntax e) => Left.Contains(e) || Right.Contains(e);

            public override bool CanReduce => throw new NotImplementedException();

            public override AnalyzedNode Reduce() => Union(Left.Reduce(), Right.Reduce());

            private static void CollectChildren(Conjuction node, ArrayBuilder<SubpatternSyntax> nodes, ref TypeSyntax type, ref SyntaxToken identifier)
            {
                CollectChildren(node.Left, nodes, ref type, ref identifier);
                CollectChildren(node.Right, nodes, ref type, ref identifier);
            }

            private static void CollectChildren(AnalyzedNode node, ArrayBuilder<SubpatternSyntax> nodes, ref TypeSyntax type, ref SyntaxToken identifier)
            {
                switch (node)
                {
                    case Conjuction n:
                        CollectChildren(n, nodes, ref type, ref identifier);
                        break;
                    case TypePattern n:
                        type = n.Type;
                        break;
                    case VarPattern n:
                        identifier = n.Identifier;
                        break;
                    default:
                        nodes.Add((SubpatternSyntax)node.Rewrite(true));
                        break;
                }
            }

            public override SyntaxNode Rewrite(bool asPattern)
            {
                if (asPattern)
                {
                    var nodes = ArrayBuilder<SubpatternSyntax>.GetInstance();
                    TypeSyntax type = null;
                    SyntaxToken identifier = default;
                    CollectChildren(this, nodes, ref type, ref identifier);

                    return RecursivePattern(
                        type, null,
                        PropertyPatternClause(SeparatedList(nodes.ToArrayAndFree())),
                        identifier.IsKind(SyntaxKind.None) ? null : SingleVariableDesignation(identifier));
                }
                else
                {
                    return BinaryExpression(SyntaxKind.LogicalAndExpression,
                        (ExpressionSyntax)Left.Rewrite(asPattern: false),
                        (ExpressionSyntax)Right.Rewrite(asPattern: false));
                }
            }

            private static AnalyzedNode UnionCore(Conjuction left, PatternMatch right)
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

            private static AnalyzedNode UnionCore(PatternMatch left, PatternMatch right)
            {
                if (AreEquivalent(left.Expression, right.Expression))
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

            private static AnalyzedNode UnionCore(VarPattern left, PatternMatch right)
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
                        var rightConjuction = (Conjuction)right;
                        return Union(new Conjuction(left, rightConjuction.Left), rightConjuction.Right);

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

            public override AnalyzedNode Reduce()
            {
                return MakePatternMatch(Expression, Pattern.Reduce());
            }

            private static PatternSyntax AsPattern(SyntaxNode node)
            {
                switch (node)
                {
                    case PatternSyntax n:
                        return n;
                    case SubpatternSyntax n:
                        return RecursivePattern(null, null, PropertyPatternClause(SingletonSeparatedList(n)), null);
                    case var value:
                        throw ExceptionUtilities.UnexpectedValue(value.Kind());
                }
            }

            public override SyntaxNode Rewrite(bool asPattern)
            {
                if (asPattern)
                {
                    return Subpattern(NameColon((IdentifierNameSyntax)Expression), AsPattern(Pattern.Rewrite(true)));
                }
                else
                {
                    return IsPatternExpression(Expression, AsPattern(Pattern.Rewrite(asPattern: true)));
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

            public override SyntaxNode Rewrite(bool asPattern)
            {
                Debug.Assert(asPattern);
                return ConstantPattern(Expression);
            }
        }

        private sealed class NotNullPattern : AnalyzedNode
        {
            public override string ToString() => "{}";

            private NotNullPattern() { }

            public readonly static NotNullPattern Instance = new NotNullPattern();

            public override NodeKind Kind => NodeKind.NotNullPattern;

            public override SyntaxNode Rewrite(bool asPattern)
            {
                Debug.Assert(asPattern);
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

            public override SyntaxNode Rewrite(bool asPattern)
            {
                Debug.Assert(asPattern);
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

            public override SyntaxNode Rewrite(bool asPattern)
            {
                throw new NotImplementedException();
            }
        }
    }
}
