// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.UseCoalesceExpression
{
    internal abstract class AbstractUseCoalesceExpressionForNullableDiagnosticAnalyzer<
        TSyntaxKind,
        TExpressionSyntax,
        TConditionalExpressionSyntax,
        TBinaryExpressionSyntax,
        TMemberAccessExpression,
        TPrefixUnaryExpressionSyntax> : AbstractCodeStyleDiagnosticAnalyzer
        where TSyntaxKind : struct
        where TExpressionSyntax : SyntaxNode
        where TConditionalExpressionSyntax : TExpressionSyntax
        where TBinaryExpressionSyntax : TExpressionSyntax
        where TMemberAccessExpression : TExpressionSyntax
        where TPrefixUnaryExpressionSyntax : TExpressionSyntax
    {
        protected AbstractUseCoalesceExpressionForNullableDiagnosticAnalyzer() 
            : base(IDEDiagnosticIds.UseCoalesceExpressionForNullableDiagnosticId,
                   new LocalizableResourceString(nameof(FeaturesResources.Use_coalesce_expression), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        protected abstract TSyntaxKind GetSyntaxKindToAnalyze();
        protected abstract ISyntaxFactsService GetSyntaxFactsService();

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

            var notHasValueExpression = false;
            if (syntaxFacts.IsLogicalNotExpression(conditionNode))
            {
                notHasValueExpression = true;
                conditionNode = syntaxFacts.GetOperandOfPrefixUnaryExpression(conditionNode);
            }

            var conditionMemberAccess = conditionNode as TMemberAccessExpression;
            if (conditionMemberAccess == null)
            {
                return;
            }

            SyntaxNode conditionExpression, conditionSimpleName;
            syntaxFacts.GetPartsOfMemberAccessExpression(conditionMemberAccess, out conditionExpression, out conditionSimpleName);

            string conditionName; int unused;
            syntaxFacts.GetNameAndArityOfSimpleName(conditionSimpleName, out conditionName, out unused);

            if (conditionName != nameof(Nullable<int>.HasValue))
            {
                return;
            }

            var whenPartToCheck = notHasValueExpression ? whenFalseNodeLow : whenTrueNodeLow;
            var whenPartMemberAccess = whenPartToCheck as TMemberAccessExpression;
            if (whenPartMemberAccess == null)
            {
                return;
            }

            SyntaxNode whenPartExpression, whenPartSimpleName;
            syntaxFacts.GetPartsOfMemberAccessExpression(whenPartMemberAccess, out whenPartExpression, out whenPartSimpleName);

            string whenPartName;
            syntaxFacts.GetNameAndArityOfSimpleName(whenPartSimpleName, out whenPartName, out unused);

            if (whenPartName != nameof(Nullable<int>.Value))
            {
                return;
            }

            if (!syntaxFacts.AreEquivalent(conditionExpression, whenPartExpression))
            {
                return;
            }

            // Syntactically this looks like something we can simplify.  Make sure we're 
            // actually looking at something Nullable (and not some type that uses a similar 
            // syntactic pattern).
            var semanticModel = context.SemanticModel;
            var nullableType = semanticModel.Compilation.GetTypeByMetadataName("System.Nullable`1");
            if (nullableType == null)
            {
                return;
            }

            var cancellationToken = context.CancellationToken;
            var type = semanticModel.GetTypeInfo(conditionExpression, cancellationToken);

            if (!nullableType.Equals(type.Type?.OriginalDefinition))
            {
                return;
            }

            var whenPartToKeep = notHasValueExpression ? whenTrueNodeHigh : whenFalseNodeHigh;
            var locations = ImmutableArray.Create(
                conditionalExpression.GetLocation(),
                conditionExpression.GetLocation(),
                whenPartToKeep.GetLocation());

            context.ReportDiagnostic(Diagnostic.Create(
                this.CreateDescriptorWithSeverity(option.Notification.Value),
                conditionalExpression.GetLocation(),
                locations));
        }
    }
}