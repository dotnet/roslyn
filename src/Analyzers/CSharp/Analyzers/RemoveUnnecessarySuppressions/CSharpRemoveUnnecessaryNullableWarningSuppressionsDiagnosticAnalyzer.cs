// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.RemoveUnnecessarySuppressions;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed partial class CSharpRemoveUnnecessaryNullableWarningSuppressionsDiagnosticAnalyzer()
    : AbstractBuiltInUnnecessaryCodeStyleDiagnosticAnalyzer(
        IDEDiagnosticIds.RemoveUnnecessaryNullableWarningSuppression,
        EnforceOnBuildValues.RemoveUnnecessaryNullableWarningSuppression,
        option: null,
        new LocalizableResourceString(nameof(AnalyzersResources.Remove_unnecessary_suppression), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
        new LocalizableResourceString(nameof(AnalyzersResources.Suppression_is_unnecessary), AnalyzersResources.ResourceManager, typeof(CompilerExtensionsResources)))
{
    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
        => context.RegisterSemanticModelAction(AnalyzeSemanticModel);

    private void AnalyzeSemanticModel(SemanticModelAnalysisContext context)
    {
        if (ShouldSkipAnalysis(context, notification: null))
            return;

        using var _ = ArrayBuilder<PostfixUnaryExpressionSyntax>.GetInstance(out var unnecessaryNodes);
        UnnecessaryNullableWarningSuppressionsUtilities.AddUnnecessaryNodes(context.SemanticModel, unnecessaryNodes, context.CancellationToken);

        foreach (var unaryNode in unnecessaryNodes)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Descriptor,
                unaryNode.OperatorToken.GetLocation(),
                [unaryNode.GetLocation()]));
        }
    }
}
