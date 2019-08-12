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
        TPrefixUnaryExpressionSyntax> : AbstractBuiltInCodeStyleDiagnosticAnalyzer
        where TSyntaxKind : struct
        where TExpressionSyntax : SyntaxNode
        where TConditionalExpressionSyntax : TExpressionSyntax
        where TBinaryExpressionSyntax : TExpressionSyntax
        where TMemberAccessExpression : TExpressionSyntax
        where TPrefixUnaryExpressionSyntax : TExpressionSyntax
    {
        protected AbstractUseCoalesceExpressionForNullableDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UseCoalesceExpressionForNullableDiagnosticId,
                   CodeStyleOptions.PreferCoalesceExpression,
                   new LocalizableResourceString(nameof(FeaturesResources.Use_coalesce_expression), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected abstract TSyntaxKind GetSyntaxKindToAnalyze();
        protected abstract ISyntaxFactsService GetSyntaxFactsService();

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeSyntax, GetSyntaxKindToAnalyze());

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            var conditionalExpression = (TConditionalExpressionSyntax)context.Node;

            var syntaxTree = context.Node.SyntaxTree;
            var cancellationToken = context.CancellationToken;
            var optionSet = context.Options.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }

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

            syntaxFacts.GetPartsOfMemberAccessExpression(conditionMemberAccess, out var conditionExpression, out var conditionSimpleName);
            syntaxFacts.GetNameAndArityOfSimpleName(conditionSimpleName, out var conditionName, out var unused);

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

            syntaxFacts.GetPartsOfMemberAccessExpression(whenPartMemberAccess, out var whenPartExpression, out var whenPartSimpleName);
            syntaxFacts.GetNameAndArityOfSimpleName(whenPartSimpleName, out var whenPartName, out unused);

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
            var nullableType = semanticModel.Compilation.GetTypeByMetadataName(typeof(Nullable<>).FullName);
            if (nullableType == null)
            {
                return;
            }

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

            context.ReportDiagnostic(DiagnosticHelper.Create(
                Descriptor,
                conditionalExpression.GetLocation(),
                option.Notification.Severity,
                locations,
                properties: null));
        }
    }
}
