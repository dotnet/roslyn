// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.UseRecursivePatterns
{
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
            public abstract T Accept<T>(Visitor<T> visitor);
            public abstract bool Contains(ExpressionSyntax e);
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

            public override T Accept<T>(Visitor<T> visitor) => visitor.VisitConjuction(this);
            public override string ToString() => $"{Left} AND {Right}";

            public override bool Contains(ExpressionSyntax e)
                => Left.Contains(e) || Right.Contains(e);
        }

        private sealed class PatternMatch : AnalyzedNode
        {
            public readonly ExpressionSyntax Expression;
            public readonly AnalyzedNode Pattern;

            public override NodeKind Kind => NodeKind.PatternMatch;

            public PatternMatch(ExpressionSyntax expression, AnalyzedNode pattern)
            {
                Debug.Assert(expression != null);
                Debug.Assert(pattern != null);
                Expression = expression;
                Pattern = pattern;
            }

            public override T Accept<T>(Visitor<T> visitor) => visitor.VisitPatternMatch(this);
            public override string ToString() => $"{Expression} is ({Pattern})";

            public override bool Contains(ExpressionSyntax e)
                => SyntaxFactory.AreEquivalent(this.Expression, e) || Pattern.Contains(e);
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

            public override T Accept<T>(Visitor<T> visitor) => visitor.VisitConstantPattern(this);
            public override string ToString() => Expression.ToString();

            public override bool Contains(ExpressionSyntax e) => false;
        }

        private sealed class NotNullPattern : AnalyzedNode
        {
            public override T Accept<T>(Visitor<T> visitor) => visitor.VisitNotNullPattern(this);
            public override string ToString() => "{}";

            private NotNullPattern() { }

            public readonly static NotNullPattern Instance = new NotNullPattern();

            public override NodeKind Kind => NodeKind.NotNullPattern;

            public override bool Contains(ExpressionSyntax e) => false;
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

            public override T Accept<T>(Visitor<T> visitor) => visitor.VisitTypePattern(this);
            public override string ToString() => Type.ToString();

            public override bool Contains(ExpressionSyntax e) => false;
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

            public override T Accept<T>(Visitor<T> visitor) => visitor.VisitVarPattern(this);
            public override string ToString() => $"var {Identifier}";

            public override bool Contains(ExpressionSyntax e)
            {
                return e is IdentifierNameSyntax id &&
                    SyntaxFactory.AreEquivalent(id.Identifier, this.Identifier);
            }
        }
    }
}
