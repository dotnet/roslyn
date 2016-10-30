// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
                   new LocalizableResourceString(nameof(FeaturesResources.Use_coalesce_expression), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        protected abstract TSyntaxKind GetSyntaxKindToAnalyze();
        protected abstract ISyntaxFactsService GetSyntaxFactsService();
        protected abstract bool IsEquals(TBinaryExpressionSyntax condition);
        protected abstract bool IsNotEquals(TBinaryExpressionSyntax condition);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeSyntax, GetSyntaxKindToAnalyze());
        }

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

            SyntaxNode conditionNode, whenTrueNode, whenFalseNode;
            syntaxFacts.GetPartsOfConditionalExpression(
                conditionalExpression, out conditionNode, out whenTrueNode, out whenFalseNode);

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

            SyntaxNode conditionLeft;
            SyntaxNode conditionRight;
            syntaxFacts.GetPartsOfBinaryExpression(condition, out conditionLeft, out conditionRight);

            var conditionRightIsNull = syntaxFacts.IsNullLiteralExpression(conditionRight);
            var conditionLeftIsNull = syntaxFacts.IsNullLiteralExpression(conditionLeft);

            if (conditionRightIsNull && conditionLeftIsNull)
            {
                // null == null    nothing to do here.
                return;
            }

            if (!conditionRightIsNull && !conditionLeftIsNull)
            {
                return;
            }

            var conditionPartToCheck = conditionRightIsNull ? conditionLeft : conditionRight;
            var whenPartToCheck = isEquals ? whenFalseNode : whenTrueNode;

            if (!syntaxFacts.AreEquivalent(conditionPartToCheck, whenPartToCheck))
            {
                return;
            }

            context.ReportDiagnostic(Diagnostic.Create(
                this.CreateDescriptor(this.DescriptorId, option.Notification.Value),
                conditionalExpression.GetLocation()));
        }
    }
}