// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Linq;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
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
                CSharpCodeStyleOptions.PreferPatternMatching,
                LanguageNames.CSharp,
                new LocalizableResourceString(nameof(CSharpAnalyzersResources.Use_pattern_matching), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)),
                new LocalizableResourceString(nameof(CSharpAnalyzersResources.Use_pattern_matching), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
        {
        }

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeNode,
                SyntaxKind.LogicalAndExpression,
                SyntaxKind.LogicalOrExpression,
                SyntaxKind.LogicalNotExpression);

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var expression = (ExpressionSyntax)context.Node;

            if (!IsTopmostExpression(expression))
                return;

            var syntaxTree = expression.SyntaxTree;
            var cancellationToken = context.CancellationToken;

            if (!((CSharpParseOptions)syntaxTree.Options).LanguageVersion.IsCSharp9OrAbove())
                return;

            var styleOption = context.Options.GetOption(CSharpCodeStyleOptions.PreferPatternMatching, syntaxTree, cancellationToken);
            if (!styleOption.Value)
                return;

            var operation = context.SemanticModel.GetOperation(expression, cancellationToken);
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
                expression.GetLocation(),
                styleOption.Notification.Severity,
                additionalLocations: null,
                properties: null));
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

        private static bool IsTopmostExpression(ExpressionSyntax node)
        {
            return node.WalkUpParentheses().Parent switch
            {
                LambdaExpressionSyntax _ => true,
                AssignmentExpressionSyntax _ => true,
                ConditionalExpressionSyntax _ => true,
                ExpressionSyntax _ => false,
                _ => true
            };
        }

        private static bool IsTrivial(AnalyzedPattern pattern)
        {
            return pattern switch
            {
                Not { Pattern: Constant _ } => true,
                Not { Pattern: Source { PatternSyntax: ConstantPatternSyntax _ } } => true,
                Not _ => false,
                Binary _ => false,
                _ => true
            };
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;
    }
}
