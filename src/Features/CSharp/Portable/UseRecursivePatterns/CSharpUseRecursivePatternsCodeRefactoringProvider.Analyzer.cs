// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseRecursivePatterns
{
    internal sealed partial class CSharpUseRecursivePatternsCodeRefactoringProvider
    {
        private sealed class Analyzer : CSharpSyntaxVisitor<AnalyzedNode?>
        {
            private readonly SemanticModel _semanticModel;

            private Analyzer(SemanticModel semanticModel)
                => _semanticModel = semanticModel;

            public static AnalyzedNode? Analyze(SyntaxNode node, SemanticModel semanticModel)
                => new Analyzer(semanticModel).Visit(node);

            public override AnalyzedNode? VisitCasePatternSwitchLabel(CasePatternSwitchLabelSyntax node)
                => node.WhenClause != null ? Conjunction.Create(Visit(node.Pattern), Visit(node.WhenClause.Condition)) : null;

            public override AnalyzedNode? VisitSwitchExpressionArm(SwitchExpressionArmSyntax node)
                => node.WhenClause != null ? Conjunction.Create(Visit(node.Pattern), Visit(node.WhenClause.Condition)) : null;

            public override AnalyzedNode? VisitBinaryExpression(BinaryExpressionSyntax node)
            {
                var (left, right) = (node.Left, node.Right);
                return node.Kind() switch
                {
                    SyntaxKind.EqualsExpression when IsConstant(right) => new PatternMatch(left, new ConstantPattern(right)),
                    SyntaxKind.EqualsExpression when IsConstant(left) => new PatternMatch(right, new ConstantPattern(left)),
                    SyntaxKind.NotEqualsExpression when IsConstantNull(right) => new PatternMatch(left, NotNullPattern.Instance),
                    SyntaxKind.NotEqualsExpression when IsConstantNull(left) => new PatternMatch(right, NotNullPattern.Instance),
                    SyntaxKind.IsExpression => new PatternMatch(left, TypeCheckAsPattern(left, (TypeSyntax)right)),
                    SyntaxKind.LogicalAndExpression => Conjunction.Create(Visit(left), Visit(right)),
                    _ => new Evaluation(node),
                };
            }

            private AnalyzedNode TypeCheckAsPattern(ExpressionSyntax e, TypeSyntax type)
                => TreatAsNullCheck(e, type) ? NotNullPattern.Instance : new TypePattern(type);

            private bool TreatAsNullCheck(ExpressionSyntax e, TypeSyntax type)
                => _semanticModel.ClassifyConversion(e, _semanticModel.GetTypeInfo(type).Type).IsIdentityOrImplicitReference();

            private bool IsConstantNull(ExpressionSyntax e)
                => _semanticModel.GetConstantValue(e) is { HasValue: true, Value: null };

            private bool IsConstant(ExpressionSyntax e)
                => _semanticModel.GetConstantValue(e).HasValue;

            public override AnalyzedNode VisitIsPatternExpression(IsPatternExpressionSyntax node)
                => new PatternMatch(node.Expression, Visit(node.Pattern)!);

            public override AnalyzedNode VisitConstantPattern(ConstantPatternSyntax node)
                => new ConstantPattern(node.Expression);

            public override AnalyzedNode VisitDeclarationPattern(DeclarationPatternSyntax node)
                => new Conjunction(new TypePattern(node.Type), Visit(node.Designation)!);

            public override AnalyzedNode VisitDiscardPattern(DiscardPatternSyntax node)
                => DiscardPattern.Instance;

            public override AnalyzedNode VisitDiscardDesignation(DiscardDesignationSyntax node)
                => DiscardPattern.Instance;

            public override AnalyzedNode VisitParenthesizedVariableDesignation(ParenthesizedVariableDesignationSyntax node)
                => new PositionalPattern(node.Variables.SelectAsArray(v => ((NameColonSyntax?)null, Visit(v)!)));

            public override AnalyzedNode VisitSingleVariableDesignation(SingleVariableDesignationSyntax node)
                => new VarPattern(node.Identifier);

            public override AnalyzedNode VisitVarPattern(VarPatternSyntax node)
                => Visit(node.Designation)!;

            public override AnalyzedNode VisitRecursivePattern(RecursivePatternSyntax node)
            {
                var nodes = ArrayBuilder<AnalyzedNode>.GetInstance();
                nodes.AddIfNotNull(TypePattern.Create(node.Type));
                nodes.AddIfNotNull(PositionalPattern.Create(node.PositionalPatternClause?.Subpatterns.Select(sub => (sub.NameColon, Visit(sub.Pattern)!))));
                nodes.AddRangeIfNotNull(node.PropertyPatternClause?.Subpatterns.Select(sub => new PatternMatch(sub.NameColon!.Name, Visit(sub.Pattern)!)));
                nodes.AddIfNotNull(Visit(node.Designation));
                var result = nodes.AggregateOrDefault((left, right) => new Conjunction(left, right)) ?? NotNullPattern.Instance;
                nodes.Free();
                return result;
            }

            // An expression of the form `(e.Property)` can be rewritten as a pattern-match `e is {Property: true}`
            public override AnalyzedNode VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
                => new PatternMatch(node,
                    new ConstantPattern(SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression)));

            // An expression of the form `!(e.Property)` can be rewritten as a pattern-match `e is {Property: false}`
            public override AnalyzedNode VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node)
                => node.IsKind(SyntaxKind.LogicalNotExpression)
                    ? (AnalyzedNode)new PatternMatch(node.Operand,
                        new ConstantPattern(SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression)))
                    : new Evaluation(node);

            // In all other cases use the expression as-is.
            public override AnalyzedNode? DefaultVisit(SyntaxNode node)
                => node is ExpressionSyntax expression ? new Evaluation(expression) : null;
        }
    }
}
