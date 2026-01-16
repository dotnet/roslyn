// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.CSharp.Analyzers.HiddenExplicitCast;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpHiddenExplicitCastDiagnosticAnalyzer()
    : AbstractBuiltInCodeStyleDiagnosticAnalyzer(
          diagnosticId: IDEDiagnosticIds.HiddenExplicitCastDiagnosticId,
          EnforceOnBuildValues.HiddenExplicitCast,
          CodeStyleOptions2.PreferNonHiddenExplicitCastInSource,
          title: new LocalizableResourceString(nameof(AnalyzersResources.Add_explicit_cast), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
          messageFormat: new LocalizableResourceString(nameof(AnalyzersResources._0_implicitly_converts_1_to_2_Add_an_explicit_cast_to_make_intent_clearer_as_it_may_fail_at_runtime), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)))
{
    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
        => context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.CastExpression);

    private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    {
        var semanticModel = context.SemanticModel;
        var cancellationToken = context.CancellationToken;
        var castExpression = (CastExpressionSyntax)context.Node;

        var option = context.GetAnalyzerOptions().PreferNonHiddenExplicitCastInSource;
        if (!option.Value ||
            ShouldSkipAnalysis(context, option.Notification))
        {
            return;
        }

        if (semanticModel.GetOperation(castExpression, cancellationToken) is not IConversionOperation outerConversionOperation)
            return;

        var outerConversion = outerConversionOperation.GetConversion();
        if (!outerConversion.IsExplicit)
            return;

        if (outerConversionOperation.Operand is not IConversionOperation innerConversionOperation)
            return;

        if (outerConversionOperation.Type is null || innerConversionOperation.Type is null || innerConversionOperation.Operand.Type is null)
            return;

        var innerConversion = innerConversionOperation.GetConversion();
        if (!innerConversion.IsExplicit)
            return;

        if (!innerConversionOperation.IsImplicit)
            return;

        context.ReportDiagnostic(DiagnosticHelper.Create(
            Descriptor,
            castExpression.GetLocation(),
            option.Notification,
            context.Options,
            additionalLocations: null,
            properties: null,
            $"({outerConversionOperation.Type.ToMinimalDisplayString(semanticModel, castExpression.SpanStart)})",
            innerConversionOperation.Operand.Type,
            innerConversionOperation.Type));
    }
}
