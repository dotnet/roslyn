// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
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

            public sealed override AnalyzedNode VisitSourcePattern(SourcePattern node) => throw ExceptionUtilities.Unreachable;
        }

        private sealed class Conjuction : AnalyzedNode
        {
            public readonly AnalyzedNode Left;
            public readonly AnalyzedNode Right;

            public Conjuction(AnalyzedNode left, AnalyzedNode right) => (Left, Right) = (left, right);
            public override T Accept<T>(Visitor<T> visitor) => visitor.VisitConjuction(this);
            public override string ToString() => $"{Left} AND {Right}";

            public override AnalyzedNode VisitConjuction(Conjuction node)
            {
                throw new NotImplementedException();
            }

            public override AnalyzedNode VisitConstantPattern(ConstantPattern node)
            {
                throw new NotImplementedException();
            }

            public override AnalyzedNode VisitNotNullPattern(NotNullPattern node)
            {
                throw new NotImplementedException();
            }

            private sealed class Finder : Visitor<bool>
            {
                private readonly ExpressionSyntax _expression;

                public Finder(ExpressionSyntax expression) => _expression = expression;
                public override bool VisitConjuction(Conjuction node) => Visit(node.Left) || Visit(node.Right);
                public override bool VisitConstantPattern(ConstantPattern node) => false;
                public override bool VisitNotNullPattern(NotNullPattern node) => false;
                public override bool VisitPatternMatch(PatternMatch node) => SyntaxFactory.AreEquivalent(_expression, node.Expression);
                public override bool VisitSourcePattern(SourcePattern node) => false;
                public override bool VisitTypePattern(TypePattern node) => false;
                public override bool VisitVarPattern(VarPattern node) => false;
            }

            public override AnalyzedNode VisitPatternMatch(PatternMatch node)
            {
                var leftSub = this.Left.Visit(node);
                var rightSub = this.Right.Visit(node);

                var finder = new Finder(node.Expression);
                if (!finder.Visit(leftSub))
                {
                    return new Conjuction(leftSub, this.Right);
                }
                else
                {
                    return new Conjuction(this.Left, rightSub);
                }
            }

            public override AnalyzedNode VisitTypePattern(TypePattern node)
            {
                throw new NotImplementedException();
            }

            public override AnalyzedNode VisitVarPattern(VarPattern node)
            {
                throw new NotImplementedException();
            }
        }

        private sealed class PatternMatch : AnalyzedNode
        {
            public readonly ExpressionSyntax Expression;
            public readonly AnalyzedNode Pattern;

            public PatternMatch(ExpressionSyntax expression, AnalyzedNode pattern) => (Expression, Pattern) = (expression, pattern);
            public override T Accept<T>(Visitor<T> visitor) => visitor.VisitPatternMatch(this);
            public override string ToString() => $"{Expression} is ({Pattern})";

            public PatternMatch Expand()
            {
                switch (Expression.Kind())
                {
                    case SyntaxKind.IdentifierName:
                        return this;

                    case SyntaxKind.MemberBindingExpression:
                        var memberBinding = (MemberBindingExpressionSyntax)Expression;
                        return new PatternMatch((IdentifierNameSyntax)memberBinding.Name, Pattern);

                    case SyntaxKind.ConditionalAccessExpression:
                        var conditionalAccess = (ConditionalAccessExpressionSyntax)Expression;
                        var asExpression = (BinaryExpressionSyntax)conditionalAccess.Expression.WalkDownParentheses();
                        return new PatternMatch(asExpression.Left,
                            new Conjuction(
                                new TypePattern((TypeSyntax)asExpression.Right),
                                new PatternMatch(conditionalAccess.WhenNotNull, Pattern).Expand())).Expand();

                    case SyntaxKind.SimpleMemberAccessExpression:
                        var memberAccess = (MemberAccessExpressionSyntax)Expression;
                        // TODO this is wasteful, refactor
                        return new PatternMatch(memberAccess.Expression,
                            new PatternMatch(memberAccess.Name, Pattern)).Expand();

                    case var value:
                        throw ExceptionUtilities.UnexpectedValue(value);
                }
            }

            public override AnalyzedNode VisitPatternMatch(PatternMatch node)
            {
                if (SyntaxFactory.AreEquivalent(this.Expression, node.Expression))
                    return new PatternMatch(this.Expression, this.Pattern.Visit(node.Pattern));

                return this.Pattern.Visit(node) is AnalyzedNode n
                    ? new PatternMatch(this.Expression, n)
                    : (AnalyzedNode)new Conjuction(this, node);
            }

            public override AnalyzedNode VisitConjuction(Conjuction node)
            {
                throw new NotImplementedException();
            }

            public override AnalyzedNode VisitConstantPattern(ConstantPattern node)
            {
                throw new NotImplementedException();
            }

            public override AnalyzedNode VisitTypePattern(TypePattern node)
            {
                throw new NotImplementedException();
            }

            public override AnalyzedNode VisitNotNullPattern(NotNullPattern node)
            {
                throw new NotImplementedException();
            }

            public override AnalyzedNode VisitVarPattern(VarPattern node)
            {
                throw new NotImplementedException();
            }
        }

        private sealed class ConstantPattern : AnalyzedNode
        {
            public readonly ExpressionSyntax Expression;

            public ConstantPattern(ExpressionSyntax expression) => Expression = expression;
            public override T Accept<T>(Visitor<T> visitor) => visitor.VisitConstantPattern(this);
            public override string ToString() => Expression.ToString();

            public override AnalyzedNode VisitConjuction(Conjuction node)
            {
                throw new NotImplementedException();
            }

            public override AnalyzedNode VisitConstantPattern(ConstantPattern node)
            {
                throw new NotImplementedException();
            }

            public override AnalyzedNode VisitNotNullPattern(NotNullPattern node)
            {
                throw new NotImplementedException();
            }

            public override AnalyzedNode VisitPatternMatch(PatternMatch node)
            {
                return null;
            }

            public override AnalyzedNode VisitTypePattern(TypePattern node)
            {
                throw new NotImplementedException();
            }

            public override AnalyzedNode VisitVarPattern(VarPattern node)
            {
                throw new NotImplementedException();
            }
        }

        private sealed class NotNullPattern : AnalyzedNode
        {
            public override T Accept<T>(Visitor<T> visitor) => visitor.VisitNotNullPattern(this);
            public override string ToString() => "{}";

            public override AnalyzedNode VisitConjuction(Conjuction node)
            {
                throw new NotImplementedException();
            }

            public override AnalyzedNode VisitConstantPattern(ConstantPattern node)
            {
                throw new NotImplementedException();
            }

            public override AnalyzedNode VisitNotNullPattern(NotNullPattern node)
            {
                throw new NotImplementedException();
            }

            public override AnalyzedNode VisitPatternMatch(PatternMatch node)
            {
                return node;
            }

            public override AnalyzedNode VisitTypePattern(TypePattern node)
            {
                throw new NotImplementedException();
            }

            public override AnalyzedNode VisitVarPattern(VarPattern node)
            {
                throw new NotImplementedException();
            }
        }

        private sealed class TypePattern : AnalyzedNode
        {
            public readonly TypeSyntax Type;

            public TypePattern(TypeSyntax type) => Type = type;
            public override T Accept<T>(Visitor<T> visitor) => visitor.VisitTypePattern(this);
            public override string ToString() => Type.ToString();

            public override AnalyzedNode VisitConjuction(Conjuction node)
            {
                throw new NotImplementedException();
            }

            public override AnalyzedNode VisitConstantPattern(ConstantPattern node)
            {
                throw new NotImplementedException();
            }

            public override AnalyzedNode VisitNotNullPattern(NotNullPattern node)
            {
                throw new NotImplementedException();
            }

            public override AnalyzedNode VisitPatternMatch(PatternMatch node)
            {
                return new Conjuction(this, node);
            }

            public override AnalyzedNode VisitTypePattern(TypePattern node)
            {
                throw new NotImplementedException();
            }

            public override AnalyzedNode VisitVarPattern(VarPattern node)
            {
                throw new NotImplementedException();
            }
        }

        private sealed class SourcePattern : AnalyzedNode
        {
            public readonly PatternSyntax Pattern;

            public SourcePattern(PatternSyntax pattern) => Pattern = pattern;
            public override T Accept<T>(Visitor<T> visitor) => visitor.VisitSourcePattern(this);
            public override string ToString() => Pattern.ToString();

            public AnalyzedNode Expand()
            {
                switch (Pattern)
                {
                    case ConstantPatternSyntax n:
                        return new ConstantPattern(n.Expression);

                    case DeclarationPatternSyntax n when n.Designation is DiscardDesignationSyntax:
                        return new TypePattern(n.Type);

                    case DeclarationPatternSyntax n when n.Designation is SingleVariableDesignationSyntax d:
                        return new Conjuction(new TypePattern(n.Type), new VarPattern(d.Identifier));

                    case VarPatternSyntax n when n.Designation is SingleVariableDesignationSyntax d:
                        return new VarPattern(d.Identifier);

                    case RecursivePatternSyntax n:
                        throw new NotImplementedException();

                    case var value:
                        throw ExceptionUtilities.UnexpectedValue(value);
                }
            }

            public override AnalyzedNode VisitPatternMatch(PatternMatch node)
            {
                throw new NotImplementedException();
            }

            public override AnalyzedNode VisitConjuction(Conjuction node)
            {
                throw new NotImplementedException();
            }

            public override AnalyzedNode VisitConstantPattern(ConstantPattern node)
            {
                throw new NotImplementedException();
            }

            public override AnalyzedNode VisitTypePattern(TypePattern node)
            {
                throw new NotImplementedException();
            }

            public override AnalyzedNode VisitNotNullPattern(NotNullPattern node)
            {
                throw new NotImplementedException();
            }

            public override AnalyzedNode VisitVarPattern(VarPattern node)
            {
                throw new NotImplementedException();
            }
        }

        private sealed class VarPattern : AnalyzedNode
        {
            public readonly SyntaxToken Identifier;

            public VarPattern(SyntaxToken identifier) => Identifier = identifier;
            public override T Accept<T>(Visitor<T> visitor) => visitor.VisitVarPattern(this);
            public override string ToString() => $"var {Identifier}";

            public override AnalyzedNode VisitConjuction(Conjuction node)
            {
                throw new NotImplementedException();
            }

            public override AnalyzedNode VisitConstantPattern(ConstantPattern node)
            {
                throw new NotImplementedException();
            }

            public override AnalyzedNode VisitNotNullPattern(NotNullPattern node)
            {
                throw new NotImplementedException();
            }

            public override AnalyzedNode VisitPatternMatch(PatternMatch node)
            {
                if (node.Expression is IdentifierNameSyntax identifier)
                    if (SyntaxFactory.AreEquivalent(this.Identifier, identifier.Identifier))
                        return new Conjuction(this, node.Pattern);

                return null;
            }

            public override AnalyzedNode VisitTypePattern(TypePattern node)
            {
                throw new NotImplementedException();
            }

            public override AnalyzedNode VisitVarPattern(VarPattern node)
            {
                throw new NotImplementedException();
            }
        }
    }
}
