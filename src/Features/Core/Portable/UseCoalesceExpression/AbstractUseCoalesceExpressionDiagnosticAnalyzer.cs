// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.UseCoalesceExpression
{
    internal abstract class AbstractUseCoalesceExpressionDiagnosticAnalyzer<
        TSyntaxKind,
        TExpressionSyntax,
        TConditionalExpressionSyntax,
        TBinaryExpressionSyntax> : AbstractCodeStyleDiagnosticAnalyzer
        where TSyntaxKind : struct
        where TExpressionSyntax : SyntaxNode
        where TConditionalExpressionSyntax : TExpressionSyntax
        where TBinaryExpressionSyntax : TExpressionSyntax
    {
        protected AbstractUseCoalesceExpressionDiagnosticAnalyzer() 
            : base(IDEDiagnosticIds.UseCoalesceExpressionDiagnosticId,
                   new LocalizableResourceString(nameof(FeaturesResources.Use_coalesce_expression), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Null_check_can_be_simplified), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        protected abstract TSyntaxKind GetSyntaxKindToAnalyze();
        protected abstract ISyntaxFactsService GetSyntaxFactsService();
        protected abstract bool IsEquals(TBinaryExpressionSyntax condition);
        protected abstract bool IsNotEquals(TBinaryExpressionSyntax condition);

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeSyntax, GetSyntaxKindToAnalyze());

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            var conditionalExpression = (TConditionalExpressionSyntax)context.Node;

            var optionSet = context.Options.GetOptionSet();
            var option = optionSet.GetOption(CodeStyleOptions.PreferCoalesceExpression, conditionalExpression.Language);
            if (!option.Value)
            {
                return;
            }

            var syntaxFacts = this.GetSyntaxFactsService();

            SyntaxNode conditionNode, whenTrueNodeHigh, whenFalseNodeHigh;
            syntaxFacts.GetPartsOfConditionalExpression(
                conditionalExpression, out conditionNode, out whenTrueNodeHigh, out whenFalseNodeHigh);

            conditionNode = syntaxFacts.WalkDownParentheses(conditionNode);
            var whenTrueNodeLow = syntaxFacts.WalkDownParentheses(whenTrueNodeHigh);
            var whenFalseNodeLow = syntaxFacts.WalkDownParentheses(whenFalseNodeHigh);

            var condition = conditionNode as TBinaryExpressionSyntax;
            if (condition == null)
            {
                return;
            }

            var isEquals = IsEquals(condition);
            var isNotEquals = IsNotEquals(condition);
            if (!isEquals && !isNotEquals)
            {
                return;
            }

            SyntaxNode conditionLeftHigh;
            SyntaxNode conditionRightHigh;
            syntaxFacts.GetPartsOfBinaryExpression(condition, out conditionLeftHigh, out conditionRightHigh);

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

            var conditionPartToCheck = conditionRightIsNull ? conditionLeftHigh : conditionRightHigh;
            var whenPartToCheck = isEquals ? whenTrueNodeHigh : whenFalseNodeHigh;
            var locations = ImmutableArray.Create(
                conditionalExpression.GetLocation(),
                conditionPartToCheck.GetLocation(),
                whenPartToCheck.GetLocation());

            context.ReportDiagnostic(Diagnostic.Create(
                this.CreateDescriptorWithSeverity(option.Notification.Value),
                conditionalExpression.GetLocation(),
                locations));
        }
    }
}