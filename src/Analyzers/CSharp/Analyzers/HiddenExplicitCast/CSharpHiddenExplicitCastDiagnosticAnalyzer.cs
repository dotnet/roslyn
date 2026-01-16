// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
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
    public const string Type = nameof(Type);

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
        => context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.CastExpression);

    private static IConversionOperation? GetInitialOperation(
        SemanticModel semanticModel,
        CastExpressionSyntax castExpression,
        CancellationToken cancellationToken)
    {
        var currentExpression = castExpression.Expression;
        while (true)
        {
            var inner = currentExpression.WalkDownParentheses().WalkDownSuppressions();
            if (inner == currentExpression)
                break;

            currentExpression = inner;
        }

        var innerOperation = semanticModel.GetOperation(currentExpression, cancellationToken);
        if (innerOperation is null)
            return null;

        IConversionOperation? highestExplicitConversion = null;
        for (var current = innerOperation.Parent; current != null; current = current.Parent)
        {
            if (current is not IConversionOperation conversionOperation)
                break;

            if (conversionOperation.GetConversion().IsExplicit && conversionOperation.Syntax == castExpression)
                highestExplicitConversion = conversionOperation;
        }

        return highestExplicitConversion;
        //if (semanticModel.GetOperation(castExpression, cancellationToken) is IConversionOperation conversionOperation1)
        //    return conversionOperation1;

        //if (castExpression.Parent is EqualsValueClauseSyntax equalsValue &&
        //    semanticModel.GetOperation(equalsValue, cancellationToken) is IVariableInitializerOperation { Value: IConversionOperation conversionOperation2 })
        //{
        //    return conversionOperation2;
        //}

        //return null;
    }

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

        var outerConversionOperation = GetInitialOperation(semanticModel, castExpression, cancellationToken);
        if (outerConversionOperation is null)
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

        var typeToInsertCastFor = innerConversionOperation.Type.ToMinimalDisplayString(semanticModel, castExpression.SpanStart);
        context.ReportDiagnostic(DiagnosticHelper.Create(
            Descriptor,
            castExpression.GetLocation(),
            option.Notification,
            context.Options,
            additionalLocations: null,
            properties: ImmutableDictionary<string, string?>.Empty.Add(Type, typeToInsertCastFor),
            $"({outerConversionOperation.Type.ToMinimalDisplayString(semanticModel, castExpression.SpanStart)})",
            innerConversionOperation.Operand.Type.ToMinimalDisplayString(semanticModel, castExpression.SpanStart),
            typeToInsertCastFor));
    }
}
