// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseRecursivePatterns
{
    internal sealed partial class CSharpUseRecursivePatternsCodeRefactoringProvider
    {
        private sealed class Analyzer : CSharpSyntaxVisitor<AnalyzedNode>
        {
            private readonly SemanticModel _semanticModel;

            private Analyzer(SemanticModel semanticModel)
            {
                _semanticModel = semanticModel;
            }

            public static AnalyzedNode Analyze(SyntaxNode node, SemanticModel semanticModel)
            {
                return new Analyzer(semanticModel).Visit(node);
            }

            public override AnalyzedNode VisitCasePatternSwitchLabel(CasePatternSwitchLabelSyntax node)
            {
                // Attempt to combine the pattern on the left and the condition of the when-clause.
                if (node.WhenClause != null)
                {
                    return new Conjunction(Visit(node.Pattern), Visit(node.WhenClause.Condition));
                }

                return null;
            }

            public override AnalyzedNode VisitSwitchExpressionArm(SwitchExpressionArmSyntax node)
            {
                if (node.WhenClause != null)
                {
                    return new Conjunction(Visit(node.Pattern), Visit(node.WhenClause.Condition));
                }

                return null;
            }

            public override AnalyzedNode VisitBinaryExpression(BinaryExpressionSyntax node)
            {
                var left = node.Left;
                var right = node.Right;
                switch (node.Kind())
                {
                    case SyntaxKind.EqualsExpression when IsConstant(right):
                        return new PatternMatch(left, new ConstantPattern(right));

                    case SyntaxKind.EqualsExpression when IsConstant(left):
                        return new PatternMatch(right, new ConstantPattern(left));

                    case SyntaxKind.NotEqualsExpression when IsConstantNull(right):
                        return new PatternMatch(left, NotNullPattern.Instance);

                    case SyntaxKind.NotEqualsExpression when IsConstantNull(left):
                        return new PatternMatch(right, NotNullPattern.Instance);

                    case SyntaxKind.IsExpression:
                        return new PatternMatch(left,
                            IsLoweredToNullCheck(left, right)
                                ? NotNullPattern.Instance
                                : new TypePattern((TypeSyntax)right));

                    // Analyze and combine both operands of an &&-operator.
                    case SyntaxKind.LogicalAndExpression
                        // No need to Visit(right) if Visit(left) has already failed.
                        when Visit(left) is AnalyzedNode analyzedLeft &&
                            Visit(right) is AnalyzedNode analyzedRight:
                        return new Conjunction(analyzedLeft, analyzedRight);
                }

                // Otherwise, yield as an evaluation node.
                return new Evaluation(node);
            }

            private bool IsLoweredToNullCheck(ExpressionSyntax e, ExpressionSyntax type)
            {
                // Check if the type-check has an implicit reference or identity conversion.
                return _semanticModel.ClassifyConversion(e,
                    _semanticModel.GetTypeInfo(type).Type).IsIdentityOrImplicitReference();
            }

            private bool IsConstantNull(ExpressionSyntax e)
            {
                // TODO to ease testing we only check for null literal.
                return e.IsKind(SyntaxKind.NullLiteralExpression);
                var constant = _semanticModel.GetConstantValue(e);
                return constant.HasValue && constant.Value is null;
            }

            private bool IsConstant(ExpressionSyntax e)
            {
                // TODO to ease testing we don't check for constants.
                return true;
                return _semanticModel.GetConstantValue(e).HasValue;
            }

            public override AnalyzedNode VisitIsPatternExpression(IsPatternExpressionSyntax node)
                => new PatternMatch(node.Expression, Visit(node.Pattern));

            public override AnalyzedNode VisitConstantPattern(ConstantPatternSyntax node)
                => new ConstantPattern(node.Expression);

            public override AnalyzedNode VisitDeclarationPattern(DeclarationPatternSyntax node)
                => new Conjunction(new TypePattern(node.Type), Visit(node.Designation));

            public override AnalyzedNode VisitDiscardPattern(DiscardPatternSyntax node)
                => DiscardPattern.Instance;

            public override AnalyzedNode VisitDiscardDesignation(DiscardDesignationSyntax node)
                => DiscardPattern.Instance;

            public override AnalyzedNode VisitParenthesizedVariableDesignation(ParenthesizedVariableDesignationSyntax node)
                => new PositionalPattern(node.Variables.SelectAsArray(v => ((NameColonSyntax)null, Visit(v))));

            public override AnalyzedNode VisitSingleVariableDesignation(SingleVariableDesignationSyntax node)
                => new VarPattern(node.Identifier);

            public override AnalyzedNode VisitVarPattern(VarPatternSyntax node)
                => Visit(node.Designation);

            public override AnalyzedNode VisitRecursivePattern(RecursivePatternSyntax node)
            {
                var nodes = ArrayBuilder<AnalyzedNode>.GetInstance();
                if (node.Type != null)
                {
                    nodes.Add(new TypePattern(node.Type));
                }

                if (node.PositionalPatternClause != null)
                {
                    nodes.Add(new PositionalPattern(node.PositionalPatternClause.Subpatterns
                        .SelectAsArray(sub => (sub.NameColon, Visit(sub.Pattern)))));
                }

                if (node.PropertyPatternClause != null)
                {
                    nodes.AddRange(node.PropertyPatternClause.Subpatterns
                        .Select(sub => new PatternMatch(sub.NameColon.Name, Visit(sub.Pattern))));
                }

                if (node.Designation != null)
                {
                    nodes.Add(Visit(node.Designation));
                }

                if (nodes.Count == 0)
                {
                    nodes.Free();
                    return NotNullPattern.Instance;
                }

                return nodes.ToImmutableAndFree().Aggregate((left, right) => new Conjunction(left, right));
            }

            public override AnalyzedNode VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
            {
                // Avoid creating superfluous `is true` if this can't turned to a property pattern.
                if (IsIdentifierOrSimpleMemberAccess(node))
                {
                    // An expression of the form `(e.Property)` can be rewritten as a pattern-match `e is {Property: true}`
                    return new PatternMatch(node,
                        new ConstantPattern(SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression)));
                }

                return new Evaluation(node);
            }

            public override AnalyzedNode VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node)
            {
                // An expression of the form `!(e.Property)` can be rewritten as a pattern-match `e is {Property: false}`
                if (node.IsKind(SyntaxKind.LogicalNotExpression) &&
                    IsIdentifierOrSimpleMemberAccess(node.Operand))
                {
                    return new PatternMatch(node.Operand,
                        new ConstantPattern(SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression)));
                }

                return new Evaluation(node);
            }

            public override AnalyzedNode DefaultVisit(SyntaxNode node)
            {
                // In all other cases we yield an evaluation node to concatenate to the resultant pattern-match(es).
                if (node is ExpressionSyntax expression)
                {
                    return new Evaluation(expression);
                }

                return null;
            }
        }

        private static bool IsIdentifierOrSimpleMemberAccess(ExpressionSyntax node)
        {
            switch (node.Kind())
            {
                default:
                    return false;
                case SyntaxKind.IdentifierName:
                    return true;
                case SyntaxKind.MemberBindingExpression:
                    return IsIdentifierOrSimpleMemberAccess(((MemberBindingExpressionSyntax)node).Name);
                case SyntaxKind.ParenthesizedExpression:
                    return IsIdentifierOrSimpleMemberAccess(((ParenthesizedExpressionSyntax)node).Expression);
                case SyntaxKind.SimpleMemberAccessExpression:
                    return IsIdentifierOrSimpleMemberAccess(((MemberAccessExpressionSyntax)node).Name);
                case SyntaxKind.ConditionalAccessExpression:
                    return IsIdentifierOrSimpleMemberAccess(((ConditionalAccessExpressionSyntax)node).WhenNotNull);
            }
        }
    }
}
