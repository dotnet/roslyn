// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.UseRecursivePatterns
{
    internal sealed partial class CSharpUseRecursivePatternsCodeRefactoringProvider
    {
        private abstract class AnalyzedNode : Visitor<AnalyzedNode>
        {
            public abstract T Accept<T>(Visitor<T> visitor);
            public abstract override string ToString();
            public abstract bool Contains(ExpressionSyntax e);

            protected static AnalyzedNode Visit(Conjuction left, Conjuction right) { throw new NotImplementedException(); }

            protected static AnalyzedNode Visit(Conjuction left, PatternMatch right)
            {
                if (left.Left.Contains(right.Expression))
                {
                    return new Conjuction(left.Left.Visit(right), left.Right);
                }

                if (left.Right.Contains(right.Expression))
                {
                    return new Conjuction(left.Left, left.Right.Visit(right));
                }

                return new Conjuction(left, right);
            }

            protected static AnalyzedNode Visit(PatternMatch left, PatternMatch right)
            {
                if (SyntaxFactory.AreEquivalent(left.Expression, right.Expression))
                {
                    return new PatternMatch(left.Expression, left.Pattern.Visit(right.Pattern));
                }

                if (left.Pattern.Contains(right.Expression))
                {
                    return new PatternMatch(left.Expression, left.Pattern.Visit(right));
                }

                if (right.Pattern.Contains(left.Expression))
                {
                    return new PatternMatch(right.Expression, right.Pattern.Visit(left));
                }

                return new Conjuction(left, right);
            }

            protected static AnalyzedNode Visit(VarPattern left, PatternMatch right)
            {
                if (left.Contains(right.Expression))
                    return new Conjuction(left, right.Pattern);

                return null;
            }

            protected static AnalyzedNode Visit(Conjuction left, ConstantPattern right) { throw new NotImplementedException(); }
            protected static AnalyzedNode Visit(Conjuction left, TypePattern right) { throw new NotImplementedException(); }
            protected static AnalyzedNode Visit(Conjuction left, NotNullPattern right) { throw new NotImplementedException(); }
            protected static AnalyzedNode Visit(Conjuction left, VarPattern right) { throw new NotImplementedException(); }

            // Conjuctive combinations
            protected static AnalyzedNode Visit(NotNullPattern left, VarPattern right) => new Conjuction(left, right);
            protected static AnalyzedNode Visit(PatternMatch left, TypePattern right) => new Conjuction(left, right);
            protected static AnalyzedNode Visit(TypePattern left, VarPattern right) => new Conjuction(right, left);

            // Superssesive combinations
            protected static AnalyzedNode Visit(NotNullPattern left, NotNullPattern right) => right;
            protected static AnalyzedNode Visit(NotNullPattern left, PatternMatch right) => right;
            protected static AnalyzedNode Visit(NotNullPattern left, TypePattern right) => right;

            // Unsupported combinations
            protected static AnalyzedNode Visit(ConstantPattern left, ConstantPattern right) => null;
            protected static AnalyzedNode Visit(ConstantPattern right, NotNullPattern left) => null;
            protected static AnalyzedNode Visit(ConstantPattern right, PatternMatch left) => null;
            protected static AnalyzedNode Visit(ConstantPattern left, TypePattern right) => null;
            protected static AnalyzedNode Visit(ConstantPattern left, VarPattern right) => null;
            protected static AnalyzedNode Visit(TypePattern left, TypePattern right) => null;
            protected static AnalyzedNode Visit(VarPattern left, VarPattern right) => null;
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

            public override T Accept<T>(Visitor<T> visitor) => visitor.VisitConjuction(this);
            public override string ToString() => $"{Left} AND {Right}";

            public override AnalyzedNode VisitConstantPattern(ConstantPattern node) => Visit(this, node);
            public override AnalyzedNode VisitNotNullPattern(NotNullPattern node) => Visit(this, node);
            public override AnalyzedNode VisitPatternMatch(PatternMatch node) => Visit(this, node);
            public override AnalyzedNode VisitTypePattern(TypePattern node) => Visit(this, node);
            public override AnalyzedNode VisitVarPattern(VarPattern node) => Visit(this, node);
            public override AnalyzedNode VisitConjuction(Conjuction node) => Visit(this, node);

            public override bool Contains(ExpressionSyntax e)
                => Left.Contains(e) || Right.Contains(e);
        }

        private sealed class PatternMatch : AnalyzedNode
        {
            public readonly ExpressionSyntax Expression;
            public readonly AnalyzedNode Pattern;

            public PatternMatch(ExpressionSyntax expression, AnalyzedNode pattern)
            {
                Debug.Assert(expression != null);
                Debug.Assert(pattern != null);
                Expression = expression;
                Pattern = pattern;
            }

            public override T Accept<T>(Visitor<T> visitor) => visitor.VisitPatternMatch(this);
            public override string ToString() => $"{Expression} is ({Pattern})";

            public override AnalyzedNode VisitConstantPattern(ConstantPattern node) => Visit(node, this);
            public override AnalyzedNode VisitNotNullPattern(NotNullPattern node) => Visit(node, this);
            public override AnalyzedNode VisitPatternMatch(PatternMatch node) => Visit(this, node);
            public override AnalyzedNode VisitTypePattern(TypePattern node) => Visit(this, node);
            public override AnalyzedNode VisitVarPattern(VarPattern node) => Visit(node, this);
            public override AnalyzedNode VisitConjuction(Conjuction node) => Visit(node, this);

            public override bool Contains(ExpressionSyntax e)
                => SyntaxFactory.AreEquivalent(this.Expression, e) || Pattern.Contains(e);
        }

        private sealed class ConstantPattern : AnalyzedNode
        {
            public readonly ExpressionSyntax Expression;

            public ConstantPattern(ExpressionSyntax expression)
            {
                Debug.Assert(expression != null);
                Expression = expression;
            }

            public override T Accept<T>(Visitor<T> visitor) => visitor.VisitConstantPattern(this);
            public override string ToString() => Expression.ToString();

            public override AnalyzedNode VisitConstantPattern(ConstantPattern node) => Visit(this, node);
            public override AnalyzedNode VisitNotNullPattern(NotNullPattern node) => Visit(this, node);
            public override AnalyzedNode VisitPatternMatch(PatternMatch node) => Visit(this, node);
            public override AnalyzedNode VisitTypePattern(TypePattern node) => Visit(this, node);
            public override AnalyzedNode VisitVarPattern(VarPattern node) => Visit(this, node);
            public override AnalyzedNode VisitConjuction(Conjuction node) => Visit(node, this);

            public override bool Contains(ExpressionSyntax e) => false;
        }

        private sealed class NotNullPattern : AnalyzedNode
        {
            public override T Accept<T>(Visitor<T> visitor) => visitor.VisitNotNullPattern(this);
            public override string ToString() => "{}";

            private NotNullPattern() { }

            public readonly static NotNullPattern Instance = new NotNullPattern();

            public override AnalyzedNode VisitConstantPattern(ConstantPattern node) => Visit(node, this);
            public override AnalyzedNode VisitNotNullPattern(NotNullPattern node) => Visit(node, this);
            public override AnalyzedNode VisitPatternMatch(PatternMatch node) => Visit(this, node);
            public override AnalyzedNode VisitTypePattern(TypePattern node) => Visit(this, node);
            public override AnalyzedNode VisitVarPattern(VarPattern node) => Visit(this, node);
            public override AnalyzedNode VisitConjuction(Conjuction node) => Visit(node, this);

            public override bool Contains(ExpressionSyntax e) => false;
        }

        private sealed class TypePattern : AnalyzedNode
        {
            public readonly TypeSyntax Type;

            public TypePattern(TypeSyntax type)
            {
                Debug.Assert(type != null);
                Type = type;
            }

            public override T Accept<T>(Visitor<T> visitor) => visitor.VisitTypePattern(this);
            public override string ToString() => Type.ToString();

            public override AnalyzedNode VisitConstantPattern(ConstantPattern node) => Visit(node, this);
            public override AnalyzedNode VisitNotNullPattern(NotNullPattern node) => Visit(node, this);
            public override AnalyzedNode VisitPatternMatch(PatternMatch node) => Visit(node, this);
            public override AnalyzedNode VisitTypePattern(TypePattern node) => Visit(node, this);
            public override AnalyzedNode VisitVarPattern(VarPattern node) => Visit(this, node);
            public override AnalyzedNode VisitConjuction(Conjuction node) => Visit(node, this);

            public override bool Contains(ExpressionSyntax e) => false;
        }

        private sealed class VarPattern : AnalyzedNode
        {
            public readonly SyntaxToken Identifier;

            public VarPattern(SyntaxToken identifier)
            {
                Debug.Assert(!identifier.IsKind(SyntaxKind.None));
                Identifier = identifier;
            }

            public override T Accept<T>(Visitor<T> visitor) => visitor.VisitVarPattern(this);
            public override string ToString() => $"var {Identifier}";

            public override AnalyzedNode VisitConstantPattern(ConstantPattern node) => Visit(node, this);
            public override AnalyzedNode VisitNotNullPattern(NotNullPattern node) => Visit(node, this);
            public override AnalyzedNode VisitPatternMatch(PatternMatch node) => Visit(this, node);
            public override AnalyzedNode VisitTypePattern(TypePattern node) => Visit(node, this);
            public override AnalyzedNode VisitVarPattern(VarPattern node) => Visit(this, node);
            public override AnalyzedNode VisitConjuction(Conjuction node) => Visit(node, this);

            public override bool Contains(ExpressionSyntax e)
            {
                return e is IdentifierNameSyntax id &&
                    SyntaxFactory.AreEquivalent(id.Identifier, this.Identifier);
            }
        }
    }
}
