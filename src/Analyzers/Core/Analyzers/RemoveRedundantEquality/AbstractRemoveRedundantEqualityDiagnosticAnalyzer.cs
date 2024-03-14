// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.RemoveRedundantEquality;

internal abstract class AbstractRemoveRedundantEqualityDiagnosticAnalyzer(ISyntaxFacts syntaxFacts)
    : AbstractBuiltInCodeStyleDiagnosticAnalyzer(
        IDEDiagnosticIds.RemoveRedundantEqualityDiagnosticId,
        EnforceOnBuildValues.RemoveRedundantEquality,
        option: null,
        new LocalizableResourceString(nameof(AnalyzersResources.Remove_redundant_equality), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)))
{
    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    protected override void InitializeWorker(AnalysisContext context)
        => context.RegisterOperationAction(AnalyzeBinaryOperator, OperationKind.BinaryOperator);

    private void AnalyzeBinaryOperator(OperationAnalysisContext context)
    {
        if (ShouldSkipAnalysis(context, notification: null))
            return;

        // We shouldn't report diagnostic on overloaded operator as the behavior can change.
        var operation = (IBinaryOperation)context.Operation;
        if (operation.OperatorMethod is not null)
            return;

        if (operation.OperatorKind is not (BinaryOperatorKind.Equals or BinaryOperatorKind.NotEquals))
            return;

        if (!syntaxFacts.IsBinaryExpression(operation.Syntax))
        {
            return;
        }

        var rightOperand = operation.RightOperand;
        var leftOperand = operation.LeftOperand;

        if (rightOperand.Type is null || leftOperand.Type is null)
            return;

        if (rightOperand.Type.SpecialType != SpecialType.System_Boolean ||
            leftOperand.Type.SpecialType != SpecialType.System_Boolean)
        {
            return;
        }

        var isOperatorEquals = operation.OperatorKind == BinaryOperatorKind.Equals;
        syntaxFacts.GetPartsOfBinaryExpression(operation.Syntax, out _, out var operatorToken, out _);

        var properties = ImmutableDictionary.CreateBuilder<string, string?>();
        if (TryGetLiteralValue(rightOperand) is bool rightBool)
        {
            properties.Add(RedundantEqualityConstants.RedundantSide, RedundantEqualityConstants.Right);
            if (rightBool != isOperatorEquals)
                properties.Add(RedundantEqualityConstants.Negate, RedundantEqualityConstants.Negate);
        }
        else if (TryGetLiteralValue(leftOperand) is bool leftBool)
        {
            properties.Add(RedundantEqualityConstants.RedundantSide, RedundantEqualityConstants.Left);
            if (leftBool != isOperatorEquals)
                properties.Add(RedundantEqualityConstants.Negate, RedundantEqualityConstants.Negate);
        }
        else
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(Descriptor,
            operatorToken.GetLocation(),
            additionalLocations: [operation.Syntax.GetLocation()],
            properties: properties.ToImmutable()));

        return;

        static bool? TryGetLiteralValue(IOperation operand)
        {
            // Make sure we only simplify literals to avoid changing
            // something like the following example:
            // const bool Activated = true; ... if (state == Activated)
            if (operand is
                {
                    Kind: OperationKind.Literal,
                    ConstantValue: { HasValue: true, Value: bool constValue }
                })
            {
                return constValue;
            }

            return null;
        }
    }
}
