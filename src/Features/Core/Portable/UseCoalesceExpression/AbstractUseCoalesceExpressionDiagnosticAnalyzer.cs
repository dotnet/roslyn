// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.UseCoalesceExpression
{
    /// <summary>
    /// Looks for code of the form "x == null ? y : x" and offers to convert it to "x ?? y";
    /// </summary>
    internal abstract class AbstractUseCoalesceExpressionDiagnosticAnalyzer<
        TSyntaxKind,
        TExpressionSyntax,
        TConditionalExpressionSyntax,
        TBinaryExpressionSyntax> : AbstractBuiltInCodeStyleDiagnosticAnalyzer
        where TSyntaxKind : struct
        where TExpressionSyntax : SyntaxNode
        where TConditionalExpressionSyntax : TExpressionSyntax
        where TBinaryExpressionSyntax : TExpressionSyntax
    {
        protected AbstractUseCoalesceExpressionDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UseCoalesceExpressionDiagnosticId,
                   CodeStyleOptions.PreferCoalesceExpression,
                   new LocalizableResourceString(nameof(FeaturesResources.Use_coalesce_expression), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Null_check_can_be_simplified), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected abstract TSyntaxKind GetSyntaxKindToAnalyze();
        protected abstract ISyntaxFactsService GetSyntaxFactsService();
        protected abstract bool IsEquals(TBinaryExpressionSyntax condition);
        protected abstract bool IsNotEquals(TBinaryExpressionSyntax condition);

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeSyntax, GetSyntaxKindToAnalyze());

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            var conditionalExpression = (TConditionalExpressionSyntax)context.Node;

            var syntaxTree = context.Node.SyntaxTree;
            var cancellationToken = context.CancellationToken;
            var optionSet = context.Options.GetAnalyzerOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();

            var option = optionSet.GetOption(CodeStyleOptions.PreferCoalesceExpression, conditionalExpression.Language);
            if (!option.Value)
            {
                return;
            }

            var syntaxFacts = GetSyntaxFactsService();
            syntaxFacts.GetPartsOfConditionalExpression(
                conditionalExpression, out var conditionNode, out var whenTrueNodeHigh, out var whenFalseNodeHigh);

            conditionNode = syntaxFacts.WalkDownParentheses(conditionNode);
            var whenTrueNodeLow = syntaxFacts.WalkDownParentheses(whenTrueNodeHigh);
            var whenFalseNodeLow = syntaxFacts.WalkDownParentheses(whenFalseNodeHigh);

            if (!(conditionNode is TBinaryExpressionSyntax condition))
            {
                return;
            }

            var isEquals = IsEquals(condition);
            var isNotEquals = IsNotEquals(condition);
            if (!isEquals && !isNotEquals)
            {
                return;
            }

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
            {
                return;
            }

            if (!syntaxFacts.AreEquivalent(
                    conditionRightIsNull ? conditionLeftLow : conditionRightLow,
                    isEquals ? whenFalseNodeLow : whenTrueNodeLow))
            {
                return;
            }

            var semanticModel = context.SemanticModel;
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
                option.Notification.Severity,
                locations,
                properties: null));
        }
    }
}
