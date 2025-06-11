// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageService;

namespace Microsoft.CodeAnalysis.UseCoalesceExpression;

/// <summary>
/// Looks for code of the form "!x.HasValue ? y : x.Value" and offers to convert it to "x ?? y";
/// </summary>
internal abstract class AbstractUseCoalesceExpressionForNullableTernaryConditionalCheckDiagnosticAnalyzer<
    TSyntaxKind,
    TExpressionSyntax,
    TConditionalExpressionSyntax,
    TBinaryExpressionSyntax,
    TMemberAccessExpression,
    TPrefixUnaryExpressionSyntax> : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    where TSyntaxKind : struct
    where TExpressionSyntax : SyntaxNode
    where TConditionalExpressionSyntax : TExpressionSyntax
    where TBinaryExpressionSyntax : TExpressionSyntax
    where TMemberAccessExpression : TExpressionSyntax
    where TPrefixUnaryExpressionSyntax : TExpressionSyntax
{
    protected AbstractUseCoalesceExpressionForNullableTernaryConditionalCheckDiagnosticAnalyzer()
        : base(IDEDiagnosticIds.UseCoalesceExpressionForNullableTernaryConditionalCheckDiagnosticId,
               EnforceOnBuildValues.UseCoalesceExpressionForNullable,
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
        var conditionalExpression = (TConditionalExpressionSyntax)context.Node;

        var cancellationToken = context.CancellationToken;

        var option = context.GetAnalyzerOptions().PreferCoalesceExpression;
        if (!option.Value || ShouldSkipAnalysis(context, option.Notification))
            return;

        var syntaxFacts = GetSyntaxFacts();
        syntaxFacts.GetPartsOfConditionalExpression(
            conditionalExpression, out var conditionNode, out var whenTrueNodeHigh, out var whenFalseNodeHigh);

        conditionNode = syntaxFacts.WalkDownParentheses(conditionNode);
        var whenTrueNodeLow = syntaxFacts.WalkDownParentheses(whenTrueNodeHigh);
        var whenFalseNodeLow = syntaxFacts.WalkDownParentheses(whenFalseNodeHigh);

        var notHasValueExpression = false;
        if (syntaxFacts.IsLogicalNotExpression(conditionNode))
        {
            notHasValueExpression = true;
            conditionNode = syntaxFacts.GetOperandOfPrefixUnaryExpression(conditionNode);
        }

        if (conditionNode is not TMemberAccessExpression conditionMemberAccess)
            return;

        syntaxFacts.GetPartsOfMemberAccessExpression(conditionMemberAccess, out var conditionExpression, out var conditionSimpleName);
        syntaxFacts.GetNameAndArityOfSimpleName(conditionSimpleName, out var conditionName, out _);

        if (conditionName != nameof(Nullable<>.HasValue))
            return;

        var whenPartToCheck = notHasValueExpression ? whenFalseNodeLow : whenTrueNodeLow;
        if (whenPartToCheck is not TMemberAccessExpression whenPartMemberAccess)
            return;

        syntaxFacts.GetPartsOfMemberAccessExpression(whenPartMemberAccess, out var whenPartExpression, out var whenPartSimpleName);
        syntaxFacts.GetNameAndArityOfSimpleName(whenPartSimpleName, out var whenPartName, out _);

        if (whenPartName != nameof(Nullable<>.Value))
            return;

        if (!syntaxFacts.AreEquivalent(conditionExpression, whenPartExpression))
            return;

        // Coalesce expression cannot be target typed.  So if we had a ternary that was target typed
        // that means the individual parts themselves had no best common type, which would not work
        // for a coalesce expression.
        var semanticModel = context.SemanticModel;
        if (IsTargetTyped(semanticModel, conditionalExpression, cancellationToken))
            return;

        // Syntactically this looks like something we can simplify.  Make sure we're 
        // actually looking at something Nullable (and not some type that uses a similar 
        // syntactic pattern).
        var nullableType = semanticModel.Compilation.GetTypeByMetadataName(typeof(Nullable<>).FullName!);
        if (nullableType == null)
            return;

        var type = semanticModel.GetTypeInfo(conditionExpression, cancellationToken);

        if (!nullableType.Equals(type.Type?.OriginalDefinition))
            return;

        var whenPartToKeep = notHasValueExpression ? whenTrueNodeHigh : whenFalseNodeHigh;
        var locations = ImmutableArray.Create(
            conditionalExpression.GetLocation(),
            conditionExpression.GetLocation(),
            whenPartToKeep.GetLocation());

        context.ReportDiagnostic(DiagnosticHelper.Create(
            Descriptor,
            conditionalExpression.GetLocation(),
            option.Notification,
            context.Options,
            locations,
            properties: null));
    }
}
