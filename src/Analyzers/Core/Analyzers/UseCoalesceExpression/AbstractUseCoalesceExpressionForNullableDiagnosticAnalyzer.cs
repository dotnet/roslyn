﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                   CodeStyleOptions2.PreferCoalesceExpression,
                   new LocalizableResourceString(nameof(AnalyzersResources.Use_coalesce_expression), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected abstract ISyntaxFacts GetSyntaxFacts();

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

            var option = context.GetOption(CodeStyleOptions2.PreferCoalesceExpression, conditionalExpression.Language);
            if (!option.Value)
            {
                return;
            }

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

            if (!(conditionNode is TMemberAccessExpression conditionMemberAccess))
            {
                return;
            }

            syntaxFacts.GetPartsOfMemberAccessExpression(conditionMemberAccess, out var conditionExpression, out var conditionSimpleName);
            syntaxFacts.GetNameAndArityOfSimpleName(conditionSimpleName, out var conditionName, out _);

            if (conditionName != nameof(Nullable<int>.HasValue))
            {
                return;
            }

            var whenPartToCheck = notHasValueExpression ? whenFalseNodeLow : whenTrueNodeLow;
            if (!(whenPartToCheck is TMemberAccessExpression whenPartMemberAccess))
            {
                return;
            }

            syntaxFacts.GetPartsOfMemberAccessExpression(whenPartMemberAccess, out var whenPartExpression, out var whenPartSimpleName);
            syntaxFacts.GetNameAndArityOfSimpleName(whenPartSimpleName, out var whenPartName, out _);

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
            var nullableType = semanticModel.Compilation.GetTypeByMetadataName(typeof(Nullable<>).FullName!);
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
