// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Shared.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.UsePatternCombinators
{
    using static AnalyzedPattern;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpUsePatternCombinatorsDiagnosticAnalyzer :
        AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        public CSharpUsePatternCombinatorsDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UsePatternCombinatorsDiagnosticId,
                option: null,
                new LocalizableResourceString(nameof(CSharpAnalyzersResources.Use_pattern_matching),
                    CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
        {
        }

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeNode,
                SyntaxKind.SwitchExpressionArm,
                SyntaxKind.ConditionalExpression,
                SyntaxKind.ForStatement,
                SyntaxKind.EqualsValueClause,
                SyntaxKind.IfStatement,
                SyntaxKind.WhenClause,
                SyntaxKind.WhileStatement,
                SyntaxKind.DoStatement,
                SyntaxKind.ReturnStatement,
                SyntaxKind.YieldReturnStatement,
                SyntaxKind.ArrowExpressionClause,
                SyntaxKind.SimpleAssignmentExpression,
                SyntaxKind.SimpleLambdaExpression,
                SyntaxKind.ParenthesizedLambdaExpression,
                SyntaxKind.Argument);

        private static ExpressionSyntax? GetExpression(SyntaxNode node)
            => node switch
            {
                SwitchExpressionArmSyntax n => n.Expression,
                ConditionalExpressionSyntax n => n.Condition,
                ForStatementSyntax n => n.Condition,
                EqualsValueClauseSyntax n => n.Value,
                IfStatementSyntax n => n.Condition,
                WhenClauseSyntax n => n.Condition,
                WhileStatementSyntax n => n.Condition,
                DoStatementSyntax n => n.Condition,
                ReturnStatementSyntax n => n.Expression,
                YieldStatementSyntax n => n.Expression,
                ArrowExpressionClauseSyntax n => n.Expression,
                AssignmentExpressionSyntax n => n.Right,
                LambdaExpressionSyntax n => n.ExpressionBody,
                ArgumentSyntax n when n.GetRefKind() == RefKind.None => n.Expression,
                _ => null,
            };

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            // TODO need an option for user to disable the feature
            var parentNode = context.Node;

            if (!((CSharpParseOptions)parentNode.SyntaxTree.Options).LanguageVersion.IsCSharp9OrAbove())
                return;

            var expression = GetExpression(parentNode);
            if (expression is null)
                return;

            var operation = context.SemanticModel.GetOperation(expression, context.CancellationToken);
            if (operation is null)
                return;

            var pattern = CSharpUsePatternCombinatorsAnalyzer.Analyze(operation);
            if (pattern is null)
                return;

            // Avoid rewriting trivial patterns, such as a single relational or a constant pattern.
            if (IsTrivial(pattern))
                return;

            // C# 9.0 does not support pattern variables under `not` and `or` combinators.
            if (HasIllegalPatternVariables(pattern))
                return;

            context.ReportDiagnostic(DiagnosticHelper.Create(
                Descriptor,
                location: expression.GetLocation(),
                effectiveSeverity: ReportDiagnostic.Warn,
                additionalLocations: null,
                properties: null,
                messageArgs: null));
        }

        private static bool HasIllegalPatternVariables(AnalyzedPattern pattern, bool permitDesignations = true)
        {
            switch (pattern)
            {
                case Not p:
                    return HasIllegalPatternVariables(p.Pattern, permitDesignations: false);
                case Binary p:
                    if (p.IsDisjunctive)
                        permitDesignations = false;
                    return HasIllegalPatternVariables(p.Left, permitDesignations) ||
                           HasIllegalPatternVariables(p.Right, permitDesignations);
                case Source p when !permitDesignations:
                    return p.PatternSyntax.DescendantNodes()
                        .OfType<SingleVariableDesignationSyntax>()
                        .Any(variable => !variable.Identifier.IsMissing);
                default:
                    return false;
            }
        }

        private static bool IsTrivial(AnalyzedPattern pattern)
            => pattern switch
            {
                Not { Pattern: Constant _ } => true,
                Not { Pattern: Source { PatternSyntax: ConstantPatternSyntax _ } } => true,
                Not _ => false,
                Binary _ => false,
                _ => true
            };

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;
    }
}
