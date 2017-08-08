// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;

namespace Microsoft.CodeAnalysis.UseNullPropagation
{
    internal static class UseNullPropagationConstants
    {
        public const string WhenPartIsNullable = nameof(WhenPartIsNullable);
    }

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

        public override bool OpenFileOnly(Workspace workspace) => false;
        public override DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SemanticDocumentAnalysis;

        protected abstract TSyntaxKind GetSyntaxKindToAnalyze();
        protected abstract bool IsEquals(TBinaryExpressionSyntax condition);
        protected abstract bool IsNotEquals(TBinaryExpressionSyntax condition);
        protected abstract bool ShouldAnalyze(ParseOptions options);

        protected abstract ISyntaxFactsService GetSyntaxFactsService();
        protected abstract ISemanticFactsService GetSemanticFactsService();

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(startContext =>
            {
                var expressionTypeOpt = startContext.Compilation.GetTypeByMetadataName("System.Linq.Expressions.Expression`1");
                startContext.RegisterSyntaxNodeAction(
                    c => AnalyzeSyntax(c, expressionTypeOpt), GetSyntaxKindToAnalyze());
            });

        }

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context, INamedTypeSymbol expressionTypeOpt)
        {
            var conditionalExpression = (TConditionalExpressionSyntax)context.Node;
            if (!ShouldAnalyze(conditionalExpression.SyntaxTree.Options))
            {
                return;
            }

            var syntaxTree = conditionalExpression.SyntaxTree;
            var cancellationToken = context.CancellationToken;
            var optionSet = context.Options.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }

            var option = optionSet.GetOption(CodeStyleOptions.PreferNullPropagation, conditionalExpression.Language);
            if (!option.Value)
            {
                return;
            }

            var syntaxFacts = this.GetSyntaxFactsService();
            syntaxFacts.GetPartsOfConditionalExpression(
                conditionalExpression, out var conditionNode, out var whenTrueNode, out var whenFalseNode);

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

            syntaxFacts.GetPartsOfBinaryExpression(condition, out var conditionLeft, out var conditionRight);

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

            // Needs to be of the form:
            //      x == null ? null : ...    or
            //      x != null ? ...  : null;
            if (isEquals && !syntaxFacts.IsNullLiteralExpression(whenTrueNode))
            {
                return;
            }

            if (isNotEquals && !syntaxFacts.IsNullLiteralExpression(whenFalseNode))
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

            // ?. is not available in expression-trees.  Disallow the fix in that case.
            var semanticFacts = GetSemanticFactsService();
            var semanticModel = context.SemanticModel;
            if (semanticFacts.IsInExpressionTree(semanticModel, conditionNode, expressionTypeOpt, cancellationToken))
            {
                return;
            }

            var locations = ImmutableArray.Create(
                conditionalExpression.GetLocation(),
                conditionPartToCheck.GetLocation(),
                whenPartToCheck.GetLocation());

            var properties = ImmutableDictionary<string, string>.Empty;
            var whenPartIsNullable = semanticModel.GetTypeInfo(whenPartMatch).Type?.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
            if (whenPartIsNullable)
            {
                properties = properties.Add(UseNullPropagationConstants.WhenPartIsNullable, "");
            }

            context.ReportDiagnostic(Diagnostic.Create(
                this.GetDescriptorWithSeverity(option.Notification.Value),
                conditionalExpression.GetLocation(),
                locations,
                properties));
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
            if (node is TInvocationExpression invocation)
            {
                return syntaxFacts.GetExpressionOfInvocationExpression(invocation);
            }

            if (node is TMemberAccessExpression memberAccess)
            {
                return syntaxFacts.GetExpressionOfMemberAccessExpression(memberAccess);
            }

            if (node is TConditionalAccessExpression conditionalAccess)
            {
                return syntaxFacts.GetExpressionOfConditionalAccessExpression(conditionalAccess);
            }

            if (node is TElementAccessExpression elementAccess)
            {
                return syntaxFacts.GetExpressionOfElementAccessExpression(elementAccess);
            }

            return null;
        }
    }
}
