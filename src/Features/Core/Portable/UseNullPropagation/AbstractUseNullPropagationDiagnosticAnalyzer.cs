// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.UseNullPropagation
{
    internal abstract class AbstractUseNullPropagationDiagnosticAnalyzer<
        TSyntaxKind,
        TExpressionSyntax,
        TConditionalExpressionSyntax,
        TBinaryExpressionSyntax,
        TInvocationExpression,
        TMemberAccessExpression,
        TConditionalAccessExpression,
        TElementAccessExpression> : AbstractCodeStyleDiagnosticAnalyzer
        where TSyntaxKind : struct
        where TExpressionSyntax : SyntaxNode
        where TConditionalExpressionSyntax : TExpressionSyntax
        where TBinaryExpressionSyntax : TExpressionSyntax
        where TInvocationExpression : TExpressionSyntax
        where TMemberAccessExpression : TExpressionSyntax
        where TConditionalAccessExpression : TExpressionSyntax
        where TElementAccessExpression : TExpressionSyntax
    {
        protected AbstractUseNullPropagationDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UseNullPropagationDiagnosticId,
                   new LocalizableResourceString(nameof(FeaturesResources.Use_null_propagation), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Null_check_can_be_simplified), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        protected abstract TSyntaxKind GetSyntaxKindToAnalyze();
        protected abstract ISyntaxFactsService GetSyntaxFactsService();
        protected abstract bool IsEquals(TBinaryExpressionSyntax condition);
        protected abstract bool IsNotEquals(TBinaryExpressionSyntax condition);
        protected abstract bool ShouldAnalyze(ParseOptions options);

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeSyntax, GetSyntaxKindToAnalyze());

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            var conditionalExpression = (TConditionalExpressionSyntax)context.Node;
            if (!ShouldAnalyze(conditionalExpression.SyntaxTree.Options))
            {
                return;
            }

            var optionSet = context.Options.GetOptionSet();
            var option = optionSet.GetOption(CodeStyleOptions.PreferNullPropagation, conditionalExpression.Language);
            if (!option.Value)
            {
                return;
            }

            var syntaxFacts = this.GetSyntaxFactsService();

            SyntaxNode conditionNode, whenTrueNode, whenFalseNode;
            syntaxFacts.GetPartsOfConditionalExpression(
                conditionalExpression, out conditionNode, out whenTrueNode, out whenFalseNode);

            conditionNode = syntaxFacts.WalkDownParentheses(conditionNode);

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

            var conditionLeftIsNull = syntaxFacts.IsNullLiteralExpression(conditionLeft);
            var conditionRightIsNull = syntaxFacts.IsNullLiteralExpression(conditionRight);

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

            var whenPartMatch = GetWhenPartMatch(syntaxFacts, conditionPartToCheck, whenPartToCheck);
            if (whenPartMatch == null)
            {
                return;
            }

            var locations = ImmutableArray.Create(
                conditionalExpression.GetLocation(),
                conditionPartToCheck.GetLocation(),
                whenPartToCheck.GetLocation());

            context.ReportDiagnostic(Diagnostic.Create(
                this.CreateDescriptorWithSeverity(option.Notification.Value),
                conditionalExpression.GetLocation(),
                locations));
        }

        internal static SyntaxNode GetWhenPartMatch(
            ISyntaxFactsService syntaxFacts, SyntaxNode expressionToMatch, SyntaxNode whenPart)
        {
            var current = whenPart;
            while (true)
            {
                var unwrapped = Unwrap(syntaxFacts, current);
                if (unwrapped == null)
                {
                    return null;
                }

                if ((current is TMemberAccessExpression) ||
                    (current is TElementAccessExpression))
                {
                    if (syntaxFacts.AreEquivalent(unwrapped, expressionToMatch))
                    {
                        return unwrapped;
                    }
                }

                current = unwrapped;
            }
        }

        private static SyntaxNode Unwrap(ISyntaxFactsService syntaxFacts, SyntaxNode node)
        {
            var invocation = node as TInvocationExpression;
            if (invocation != null)
            {
                return syntaxFacts.GetExpressionOfInvocationExpression(invocation);
            }

            var memberAccess = node as TMemberAccessExpression;
            if (memberAccess != null)
            {
                return syntaxFacts.GetExpressionOfMemberAccessExpression(memberAccess);
            }

            var conditionalAccess = node as TConditionalAccessExpression;
            if (conditionalAccess != null)
            {
                return syntaxFacts.GetExpressionOfConditionalAccessExpression(conditionalAccess);
            }

            var elementAccess = node as TElementAccessExpression;
            if (elementAccess != null)
            {
                return syntaxFacts.GetExpressionOfElementAccessExpression(elementAccess);
            }

            return null;
        }
    }
}