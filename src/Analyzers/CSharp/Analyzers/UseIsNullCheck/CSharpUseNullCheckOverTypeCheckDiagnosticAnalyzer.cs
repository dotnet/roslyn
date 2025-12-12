// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.UseIsNullCheck;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpUseNullCheckOverTypeCheckDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
{
    public CSharpUseNullCheckOverTypeCheckDiagnosticAnalyzer()
        : base(IDEDiagnosticIds.UseNullCheckOverTypeCheckDiagnosticId,
               EnforceOnBuildValues.UseNullCheckOverTypeCheck,
               CSharpCodeStyleOptions.PreferNullCheckOverTypeCheck,
               new LocalizableResourceString(nameof(CSharpAnalyzersResources.Prefer_null_check_over_type_check), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)),
               new LocalizableResourceString(nameof(CSharpAnalyzersResources.Null_check_can_be_clarified), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
    {
    }

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
    {
        context.RegisterCompilationStartAction(context =>
        {
            var compilation = context.Compilation;
            if (compilation.LanguageVersion() < LanguageVersion.CSharp9)
                return;

            var expressionType = compilation.ExpressionOfTType();
            context.RegisterOperationAction(c => AnalyzeIsTypeOperation(c, expressionType), OperationKind.IsType);
            context.RegisterOperationAction(c => AnalyzeNegatedPatternOperation(c), OperationKind.NegatedPattern);
        });
    }

    private bool ShouldAnalyze(OperationAnalysisContext context, out NotificationOption2 notificationOption)
    {
        var option = context.GetCSharpAnalyzerOptions().PreferNullCheckOverTypeCheck;
        if (!option.Value || ShouldSkipAnalysis(context, option.Notification))
        {
            notificationOption = NotificationOption2.Silent;
            return false;
        }

        notificationOption = option.Notification;
        return true;
    }

    private void AnalyzeNegatedPatternOperation(OperationAnalysisContext context)
    {
        if (!ShouldAnalyze(context, out var notificationOption) ||
            context.Operation.Syntax is not UnaryPatternSyntax)
        {
            return;
        }

        var negatedPattern = (INegatedPatternOperation)context.Operation;
        // Matches 'x is not MyType'
        // InputType is the type of 'x'
        // MatchedType is 'MyType'
        // We check InheritsFromOrEquals so that we report a diagnostic on the following:
        // 1. x is not object (which is also equivalent to 'is null' check)
        // 2. derivedObj is parentObj (which is the same as the previous point).
        // 3. str is string (where str is a string, this is also equivalent to 'is null' check).
        // This doesn't match `x is not MyType y` because in such case, negatedPattern.Pattern will
        // be `DeclarationPattern`, not `TypePattern`.
        if (negatedPattern.Pattern is ITypePatternOperation typePatternOperation &&
            typePatternOperation.InputType.InheritsFromOrEquals(typePatternOperation.MatchedType))
        {
            context.ReportDiagnostic(
                DiagnosticHelper.Create(
                    Descriptor, context.Operation.Syntax.GetLocation(), notificationOption, context.Options, additionalLocations: null, properties: null));
        }
    }

    private void AnalyzeIsTypeOperation(OperationAnalysisContext context, INamedTypeSymbol? expressionType)
    {
        var operation = context.Operation;
        var semanticModel = operation.SemanticModel;
        var syntax = operation.Syntax;

        Contract.ThrowIfNull(semanticModel);

        if (!ShouldAnalyze(context, out var notificationOption) || syntax is not BinaryExpressionSyntax)
            return;

        if (CSharpSemanticFacts.Instance.IsInExpressionTree(semanticModel, syntax, expressionType, context.CancellationToken))
            return;

        var isTypeOperation = (IIsTypeOperation)operation;

        // Matches 'x is MyType'
        // isTypeOperation.TypeOperand is 'MyType'
        // isTypeOperation.ValueOperand.Type is the type of 'x'.
        // We check InheritsFromOrEquals for the same reason as stated in AnalyzeNegatedPatternOperation.
        // This doesn't match `x is MyType y` because in such case, we have an IsPattern instead of IsType operation.
        if (isTypeOperation.ValueOperand.Type is not null &&
            isTypeOperation.ValueOperand.Type.InheritsFromOrEquals(isTypeOperation.TypeOperand))
        {
            context.ReportDiagnostic(
                DiagnosticHelper.Create(
                    Descriptor, syntax.GetLocation(), notificationOption, context.Options, additionalLocations: null, properties: null));
        }
    }
}
