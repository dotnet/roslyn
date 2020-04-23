// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.CodeStyle;
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
            => context.RegisterSyntaxNodeAction(AnalyzeNode, CSharpUsePatternCombinatorsHelpers.SyntaxKinds);

        public void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            // TODO need an option for user to disable the feature

            // TODO need to check language version >= C# 9.0

            var parentNode = context.Node;

            var expression = CSharpUsePatternCombinatorsHelpers.GetExpression(parentNode);
            if (expression is null)
                return;

            var operation = context.SemanticModel.GetOperation(expression);
            if (operation is null)
                return;

            var pattern = CSharpUsePatternCombinatorsAnalyzer.Analyze(operation, out _);
            if (pattern is null)
                return;

            if (!ShouldReportDiagnostic(pattern))
                return;

            context.ReportDiagnostic(DiagnosticHelper.Create(
                Descriptor,
                location: expression.GetLocation(),
                effectiveSeverity: ReportDiagnostic.Warn,
                additionalLocations: new[] { parentNode.GetLocation() },
                properties: null,
                messageArgs: null));
        }

        private static bool ShouldReportDiagnostic(AnalyzedPattern pattern)
        {
            switch (pattern)
            {
                case Not { Pattern: Constant _ }:
                    break;
                case Not _:
                case Binary _:
                    return true;
            }

            return false;
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory() =>
            DiagnosticAnalyzerCategory.SemanticSpanAnalysis;
    }
}
