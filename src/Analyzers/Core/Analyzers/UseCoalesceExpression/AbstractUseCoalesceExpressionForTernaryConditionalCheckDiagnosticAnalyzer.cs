// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageService;

namespace Microsoft.CodeAnalysis.UseCoalesceExpression;

/// <summary>
/// Looks for code of the form "x == null ? y : x" and offers to convert it to "x ?? y";
/// </summary>
internal abstract class AbstractUseCoalesceExpressionForTernaryConditionalCheckDiagnosticAnalyzer<
    TSyntaxKind,
    TExpressionSyntax,
    TConditionalExpressionSyntax,
    TBinaryExpressionSyntax> : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    where TSyntaxKind : struct
    where TExpressionSyntax : SyntaxNode
    where TConditionalExpressionSyntax : TExpressionSyntax
    where TBinaryExpressionSyntax : TExpressionSyntax
{
    protected AbstractUseCoalesceExpressionForTernaryConditionalCheckDiagnosticAnalyzer()
        : base(IDEDiagnosticIds.UseCoalesceExpressionForTernaryConditionalCheckDiagnosticId,
               EnforceOnBuildValues.UseCoalesceExpression,
               CodeStyleOptions2.PreferCoalesceExpression,
               new LocalizableResourceString(nameof(AnalyzersResources.Use_coalesce_expression), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
               new LocalizableResourceString(nameof(AnalyzersResources.Null_check_can_be_simplified), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)))
    {
    }

    public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
        => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

    protected abstract ISyntaxFacts GetSyntaxFacts();
    protected abstract bool IsTargetTyped(SemanticModel semanticModel, TConditionalExpressionSyntax conditional, System.Threading.CancellationToken cancellationToken);

    protected override void InitializeWorker(AnalysisContext context)
    {
        var syntaxKinds = GetSyntaxFacts().SyntaxKinds;
        context.RegisterSyntaxNodeAction(AnalyzeSyntax,
            syntaxKinds.Convert<TSyntaxKind>(syntaxKinds.TernaryConditionalExpression));
    }

    private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
    {
        var cancellationToken = context.CancellationToken;
        var conditionalExpression = (TConditionalExpressionSyntax)context.Node;

        var option = context.GetAnalyzerOptions().PreferCoalesceExpression;
        if (!option.Value || ShouldSkipAnalysis(context, option.Notification))
            return;

        var syntaxFacts = GetSyntaxFacts();
        syntaxFacts.GetPartsOfConditionalExpression(
            conditionalExpression, out var conditionNode, out var whenTrueNodeHigh, out var whenFalseNodeHigh);

        conditionNode = syntaxFacts.WalkDownParentheses(conditionNode);
        var whenTrueNodeLow = syntaxFacts.WalkDownParentheses(whenTrueNodeHigh);
        var whenFalseNodeLow = syntaxFacts.WalkDownParentheses(whenFalseNodeHigh);

        if (conditionNode is not TBinaryExpressionSyntax condition)
            return;

        var syntaxKinds = syntaxFacts.SyntaxKinds;
        var isEquals = syntaxKinds.ReferenceEqualsExpression == condition.RawKind;
        var isNotEquals = syntaxKinds.ReferenceNotEqualsExpression == condition.RawKind;
        if (!isEquals && !isNotEquals)
            return;

        syntaxFacts.GetPartsOfBinaryExpression(condition, out var conditionLeftHigh, out var conditionRightHigh);

        var conditionLeftLow = syntaxFacts.WalkDownParentheses(conditionLeftHigh);
        var conditionRightLow = syntaxFacts.WalkDownParentheses(conditionRightHigh);

        var conditionLeftIsNull = syntaxFacts.IsNullLiteralExpression(conditionLeftLow);
        var conditionRightIsNull = syntaxFacts.IsNullLiteralExpression(conditionRightLow);

        if (conditionRightIsNull && conditionLeftIsNull)
        {
            // null == null    nothing to do here.
            return;
        }

        if (!conditionRightIsNull && !conditionLeftIsNull)
            return;

        if (!syntaxFacts.AreEquivalent(
                conditionRightIsNull ? conditionLeftLow : conditionRightLow,
                isEquals ? whenFalseNodeLow : whenTrueNodeLow))
        {
            return;
        }

        // Coalesce expression cannot be target typed.  So if we had a ternary that was target typed
        // that means the individual parts themselves had no best common type, which would not work
        // for a coalesce expression.
        var semanticModel = context.SemanticModel;
        if (IsTargetTyped(semanticModel, conditionalExpression, cancellationToken))
            return;

        var conditionType = semanticModel.GetTypeInfo(
            conditionLeftIsNull ? conditionRightLow : conditionLeftLow, cancellationToken).Type;
        if (conditionType != null &&
            !conditionType.IsReferenceType)
        {
            // Note: it is intentional that we do not support nullable types here.  If you have:
            //
            //  int? x;
            //  var z = x == null ? y : x;
            //  
            // then that's not the same as:   x ?? y.   ?? will unwrap the nullable, producing a 
            // int and not an int? like we have in the above code.  
            //
            // Note: we could look for:  x == null ? y : x.Value, and simplify that in the future.
            return;
        }

        var conditionPartToCheck = conditionRightIsNull ? conditionLeftHigh : conditionRightHigh;
        var whenPartToCheck = isEquals ? whenTrueNodeHigh : whenFalseNodeHigh;
        var locations = ImmutableArray.Create(
            conditionalExpression.GetLocation(),
            conditionPartToCheck.GetLocation(),
            whenPartToCheck.GetLocation());

        context.ReportDiagnostic(DiagnosticHelper.Create(
            Descriptor,
            conditionalExpression.GetLocation(),
            option.Notification,
            context.Options,
            locations,
            properties: null));
    }
}
