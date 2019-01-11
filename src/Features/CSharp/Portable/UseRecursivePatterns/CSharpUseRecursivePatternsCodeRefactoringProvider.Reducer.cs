// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseRecursivePatterns
{
    internal sealed partial class CSharpUseRecursivePatternsCodeRefactoringProvider
    {
        private sealed class Reducer : Visitor<AnalyzedNode>
        {
            private static readonly Reducer s_instance = new Reducer();

            private Reducer() { }

            public static AnalyzedNode Reduce(AnalyzedNode analyzedNode) => s_instance.Visit(analyzedNode);

            public override AnalyzedNode VisitPatternMatch(PatternMatch node) => MakePatternMatch(node.Expression, Visit(node.Pattern));

            public override AnalyzedNode VisitConjuction(Conjuction node) => Visit(node.Left).Visit(Visit(node.Right));

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

            public override AnalyzedNode VisitSourcePattern(SourcePattern node)
            {
                switch (node.Pattern)
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

            public override AnalyzedNode VisitConstantPattern(ConstantPattern node) => node;
            public override AnalyzedNode VisitNotNullPattern(NotNullPattern node) => node;
            public override AnalyzedNode VisitTypePattern(TypePattern node) => node;
            public override AnalyzedNode VisitVarPattern(VarPattern node) => node;
        }
    }
}
