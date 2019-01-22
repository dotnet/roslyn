// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseRecursivePatterns
{
    internal sealed partial class CSharpUseRecursivePatternsCodeRefactoringProvider
    {
        private sealed class Analyzer : CSharpSyntaxVisitor<AnalyzedNode>
        {
            private static readonly Analyzer s_instance = new Analyzer();

            private Analyzer() { }

            public static AnalyzedNode Analyze(SyntaxNode node)
            {
                return s_instance.Visit(node);
            }

            public override AnalyzedNode VisitCasePatternSwitchLabel(CasePatternSwitchLabelSyntax node)
            {
                if (node.WhenClause != null)
                {
                    return new Conjuction(Visit(node.Pattern), Visit(node.WhenClause.Condition));
                }

                return null;
            }

            public override AnalyzedNode VisitBinaryExpression(BinaryExpressionSyntax node)
            {
                var left = node.Left;
                var right = node.Right;

                switch (node.Kind())
                {
                    case SyntaxKind.EqualsExpression when AnalyzeLeftOfPatternMatch(left):
                        return new PatternMatch(left, new ConstantPattern(right));

                    case SyntaxKind.IsExpression:
                        return new PatternMatch(left, new TypePattern((TypeSyntax)right));

                    case SyntaxKind.NotEqualsExpression when right.IsKind(SyntaxKind.NullLiteralExpression):
                        return new PatternMatch(left, NotNullPattern.Instance);

                    case SyntaxKind.LogicalAndExpression
                        when Visit(left) is AnalyzedNode analyzedLeft &&
                            Visit(right) is AnalyzedNode analyzedRight:
                        return new Conjuction(analyzedLeft, analyzedRight);
                }

                return null;
            }

            private static bool AnalyzeLeftOfPatternMatch(ExpressionSyntax node)
            {
                switch (node.Kind())
                {
                    case SyntaxKind.IdentifierName:
                        return true;

                    case SyntaxKind.MemberBindingExpression:
                        var memberBinding = (MemberBindingExpressionSyntax)node;
                        return memberBinding.Name.IsKind(SyntaxKind.IdentifierName);

                    case SyntaxKind.ConditionalAccessExpression:
                        var conditionalAccess = (ConditionalAccessExpressionSyntax)node;
                        var expression = conditionalAccess.Expression.WalkDownParentheses();
                        return expression.IsKind(SyntaxKind.AsExpression) &&
                            AnalyzeLeftOfPatternMatch(conditionalAccess.WhenNotNull);

                    case SyntaxKind.SimpleMemberAccessExpression:
                        var memberAccess = (MemberAccessExpressionSyntax)node;
                        return memberAccess.Name.IsKind(SyntaxKind.IdentifierName) &&
                            AnalyzeLeftOfPatternMatch(memberAccess.Expression);
                }

                return false;
            }

            public override AnalyzedNode VisitIsPatternExpression(IsPatternExpressionSyntax node)
            {
                return new PatternMatch(node.Expression, Visit(node.Pattern));
            }

            public override AnalyzedNode VisitConstantPattern(ConstantPatternSyntax node)
            {
                return new ConstantPattern(node.Expression);
            }

            public override AnalyzedNode VisitDeclarationPattern(DeclarationPatternSyntax node)
            {
                switch (node.Designation)
                {
                    case SingleVariableDesignationSyntax n:
                        return new Conjuction(new TypePattern(node.Type), new VarPattern(n.Identifier));

                    case DiscardDesignationSyntax n:
                        return new TypePattern(node.Type);

                    case ParenthesizedVariableDesignationSyntax n:
                        throw new NotImplementedException();

                    case var value:
                        throw ExceptionUtilities.UnexpectedValue(value);
                }
            }

            public override AnalyzedNode VisitVarPattern(VarPatternSyntax node)
            {
                switch (node.Designation)
                {
                    case SingleVariableDesignationSyntax n:
                        return new VarPattern(n.Identifier);
                }

                return null;
            }

            public override AnalyzedNode VisitRecursivePattern(RecursivePatternSyntax node)
            {
                throw new NotImplementedException();
            }

            public override AnalyzedNode VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
            {
                if (AnalyzeLeftOfPatternMatch(node))
                {
                    return new PatternMatch(node,
                        new ConstantPattern(SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression)));
                }

                return null;
            }

            public override AnalyzedNode VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node)
            {
                if (node.IsKind(SyntaxKind.LogicalNotExpression) &&
                    AnalyzeLeftOfPatternMatch(node.Operand))
                {
                    return new PatternMatch(node.Operand,
                        new ConstantPattern(SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression)));
                }

                return null;
            }

            public override AnalyzedNode DefaultVisit(SyntaxNode node)
            {
                if (node is ExpressionSyntax expression)
                {
                    return new Evaluation(expression);
                }

                return null;
            }
        }
    }
}
