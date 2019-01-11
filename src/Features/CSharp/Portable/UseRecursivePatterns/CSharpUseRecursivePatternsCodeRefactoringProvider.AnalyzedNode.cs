// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseRecursivePatterns
{
    internal sealed partial class CSharpUseRecursivePatternsCodeRefactoringProvider
    {
        private abstract class AnalyzedNode : Visitor<AnalyzedNode>
        {
            public abstract T Accept<T>(Visitor<T> visitor);
            public abstract override string ToString();

            protected static AnalyzedNode Visit(Conjuction left, Conjuction right) { throw new NotImplementedException(); }

            protected static AnalyzedNode Visit(Conjuction left, PatternMatch right)
            {
                if (Finder.Find(right.Expression, left.Left))
                {
                    return new Conjuction(left.Left.Visit(right), left.Right);
                }

                if (Finder.Find(right.Expression, left.Right))
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

                if (Finder.Find(right.Expression, left.Pattern))
                {
                    return new PatternMatch(left.Expression, left.Pattern.Visit(right));
                }

                if (Finder.Find(left.Expression, right.Pattern))
                {
                    return new PatternMatch(right.Expression, right.Pattern.Visit(left));
                }

                return new Conjuction(left, right);
            }

            protected static AnalyzedNode Visit(PatternMatch left, VarPattern right)
            {
                if (left.Expression is IdentifierNameSyntax identifier)
                    if (SyntaxFactory.AreEquivalent(right.Identifier, identifier.Identifier))
                        return new Conjuction(right, left.Pattern);

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
            protected static AnalyzedNode Visit(NotNullPattern left, ConstantPattern right) => null;
            protected static AnalyzedNode Visit(PatternMatch left, ConstantPattern right) => null;
            protected static AnalyzedNode Visit(ConstantPattern left, TypePattern right) => null;
            protected static AnalyzedNode Visit(ConstantPattern left, VarPattern right) => null;
            protected static AnalyzedNode Visit(TypePattern left, TypePattern right) => null;
            protected static AnalyzedNode Visit(VarPattern left, VarPattern right) => null;

            public sealed override AnalyzedNode VisitSourcePattern(SourcePattern node) => throw ExceptionUtilities.Unreachable;

            private sealed class Finder : Visitor<bool>
            {
                private readonly IdentifierNameSyntax _expression;

                private Finder(IdentifierNameSyntax expression)
                    => _expression = expression;

                public static bool Find(ExpressionSyntax expression, AnalyzedNode node)
                    => expression is IdentifierNameSyntax id && new Finder(id).Visit(node);

                public override bool VisitConjuction(Conjuction node)
                    => Visit(node.Left) || Visit(node.Right);

                public override bool VisitPatternMatch(PatternMatch node)
                    => SyntaxFactory.AreEquivalent(_expression, node.Expression) || Visit(node.Pattern);

                public override bool VisitVarPattern(VarPattern node)
                    => SyntaxFactory.AreEquivalent(_expression.Identifier, node.Identifier);

                public override bool VisitConstantPattern(ConstantPattern node) => false;
                public override bool VisitNotNullPattern(NotNullPattern node) => false;
                public override bool VisitSourcePattern(SourcePattern node) => false;
                public override bool VisitTypePattern(TypePattern node) => false;
            }
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

            public override AnalyzedNode VisitConstantPattern(ConstantPattern node) => Visit(this, node);
            public override AnalyzedNode VisitNotNullPattern(NotNullPattern node) => Visit(node, this);
            public override AnalyzedNode VisitPatternMatch(PatternMatch node) => Visit(this, node);
            public override AnalyzedNode VisitTypePattern(TypePattern node) => Visit(this, node);
            public override AnalyzedNode VisitVarPattern(VarPattern node) => Visit(this, node);
            public override AnalyzedNode VisitConjuction(Conjuction node) => Visit(node, this);
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
            public override AnalyzedNode VisitNotNullPattern(NotNullPattern node) => Visit(node, this);
            public override AnalyzedNode VisitPatternMatch(PatternMatch node) => Visit(node, this);
            public override AnalyzedNode VisitTypePattern(TypePattern node) => Visit(this, node);
            public override AnalyzedNode VisitVarPattern(VarPattern node) => Visit(this, node);
            public override AnalyzedNode VisitConjuction(Conjuction node) => Visit(node, this);
        }

        private sealed class NotNullPattern : AnalyzedNode
        {
            public override T Accept<T>(Visitor<T> visitor) => visitor.VisitNotNullPattern(this);
            public override string ToString() => "{}";

            private NotNullPattern() { }

            public readonly static NotNullPattern Instance = new NotNullPattern();

            public override AnalyzedNode VisitConstantPattern(ConstantPattern node) => Visit(this, node);
            public override AnalyzedNode VisitNotNullPattern(NotNullPattern node) => Visit(node, this);
            public override AnalyzedNode VisitPatternMatch(PatternMatch node) => Visit(this, node);
            public override AnalyzedNode VisitTypePattern(TypePattern node) => Visit(this, node);
            public override AnalyzedNode VisitVarPattern(VarPattern node) => Visit(this, node);
            public override AnalyzedNode VisitConjuction(Conjuction node) => Visit(node, this);
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
        }

        private sealed class SourcePattern : AnalyzedNode
        {
            public readonly PatternSyntax Pattern;

            public SourcePattern(PatternSyntax pattern)
            {
                Debug.Assert(pattern != null);
                Pattern = pattern;
            }

            public override T Accept<T>(Visitor<T> visitor) => visitor.VisitSourcePattern(this);
            public override string ToString() => Pattern.ToString();

            public override AnalyzedNode VisitConstantPattern(ConstantPattern node) => throw ExceptionUtilities.Unreachable;
            public override AnalyzedNode VisitNotNullPattern(NotNullPattern node) => throw ExceptionUtilities.Unreachable;
            public override AnalyzedNode VisitPatternMatch(PatternMatch node) => throw ExceptionUtilities.Unreachable;
            public override AnalyzedNode VisitTypePattern(TypePattern node) => throw ExceptionUtilities.Unreachable;
            public override AnalyzedNode VisitVarPattern(VarPattern node) => throw ExceptionUtilities.Unreachable;
            public override AnalyzedNode VisitConjuction(Conjuction node) => throw ExceptionUtilities.Unreachable;
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
            public override AnalyzedNode VisitPatternMatch(PatternMatch node) => Visit(node, this);
            public override AnalyzedNode VisitTypePattern(TypePattern node) => Visit(node, this);
            public override AnalyzedNode VisitVarPattern(VarPattern node) => Visit(this, node);
            public override AnalyzedNode VisitConjuction(Conjuction node) => Visit(node, this);
        }
    }
}
