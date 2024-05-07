// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.SimplifyBooleanExpression
{
    using static SimplifyBooleanExpressionConstants;

    internal abstract class AbstractSimplifyConditionalDiagnosticAnalyzer<
        TSyntaxKind,
        TExpressionSyntax,
        TConditionalExpressionSyntax> :
        AbstractBuiltInCodeStyleDiagnosticAnalyzer
        where TSyntaxKind : struct
        where TExpressionSyntax : SyntaxNode
        where TConditionalExpressionSyntax : TExpressionSyntax
    {
        private static readonly ImmutableDictionary<string, string?> s_takeCondition
            = ImmutableDictionary<string, string?>.Empty;
        private static readonly ImmutableDictionary<string, string?> s_negateCondition
            = s_takeCondition.Add(Negate, Negate);
        private static readonly ImmutableDictionary<string, string?> s_takeConditionOrWhenFalse
            = s_takeCondition.Add(Or, Or).Add(WhenFalse, WhenFalse);
        private static readonly ImmutableDictionary<string, string?> s_negateConditionAndWhenFalse
            = s_negateCondition.Add(And, And).Add(WhenFalse, WhenFalse);
        private static readonly ImmutableDictionary<string, string?> s_negateConditionOrWhenTrue
            = s_negateCondition.Add(Or, Or).Add(WhenTrue, WhenTrue);
        private static readonly ImmutableDictionary<string, string?> s_takeConditionAndWhenTrue
            = s_takeCondition.Add(And, And).Add(WhenTrue, WhenTrue);
        private static readonly ImmutableDictionary<string, string?> s_takeConditionAndWhenFalse
            = s_takeCondition.Add(And, And).Add(WhenFalse, WhenFalse);

        protected AbstractSimplifyConditionalDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.SimplifyConditionalExpressionDiagnosticId,
                   EnforceOnBuildValues.SimplifyConditionalExpression,
                   CodeStyleOptions2.PreferSimplifiedBooleanExpressions,
                   new LocalizableResourceString(nameof(AnalyzersResources.Simplify_conditional_expression), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
                   new LocalizableResourceString(nameof(AnalyzersResources.Conditional_expression_can_be_simplified), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)))
        {
        }

        protected abstract ISyntaxFacts SyntaxFacts { get; }

        protected abstract CommonConversion GetConversion(SemanticModel semanticModel, TExpressionSyntax node, CancellationToken cancellationToken);

        public sealed override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected sealed override void InitializeWorker(AnalysisContext context)
        {
            var syntaxKinds = SyntaxFacts.SyntaxKinds;
            context.RegisterSyntaxNodeAction(
                AnalyzeConditionalExpression, syntaxKinds.Convert<TSyntaxKind>(syntaxKinds.ConditionalExpression));
        }

        private void AnalyzeConditionalExpression(SyntaxNodeAnalysisContext context)
        {
            var styleOption = context.GetAnalyzerOptions().PreferSimplifiedBooleanExpressions;
            if (!styleOption.Value || ShouldSkipAnalysis(context, styleOption.Notification))
            {
                // Bail immediately if the user has disabled this feature.
                return;
            }

            var semanticModel = context.SemanticModel;
            var cancellationToken = context.CancellationToken;
            var conditionalExpression = (TConditionalExpressionSyntax)context.Node;
            SyntaxFacts.GetPartsOfConditionalExpression(
                conditionalExpression, out var conditionNode, out var whenTrueNode, out var whenFalseNode);
            var condition = (TExpressionSyntax)conditionNode;
            var whenTrue = (TExpressionSyntax)whenTrueNode;
            var whenFalse = (TExpressionSyntax)whenFalseNode;

            // Only offer when everything is a basic boolean type.  That way we don't have to worry
            // about any sort of subtle cases with implicit or bool conversions.
            if (!IsSimpleBooleanType(condition) ||
                !IsSimpleBooleanType(whenTrue) ||
                !IsSimpleBooleanType(whenFalse))
            {
                return;
            }

            var whenTrue_isTrue = IsTrue(whenTrue);
            var whenTrue_isFalse = IsFalse(whenTrue);

            var whenFalse_isTrue = IsTrue(whenFalse);
            var whenFalse_isFalse = IsFalse(whenFalse);

            if (whenTrue_isTrue && whenFalse_isFalse)
            {
                // c ? true : false     =>      c
                ReportDiagnostic(s_takeCondition);
            }
            else if (whenTrue_isFalse && whenFalse_isTrue)
            {
                // c ? false : true     =>      !c
                ReportDiagnostic(s_negateCondition);
            }
            else if (whenTrue_isFalse && whenFalse_isFalse)
            {
                // c ? false : false      =>      c && false
                // Note: this is a slight optimization over the when `c ? false : wf`
                // case below.  It allows to generate `c && false` instead of `!c && false`
                ReportDiagnostic(s_takeConditionAndWhenFalse);
            }
            else if (whenTrue_isTrue)
            {
                // c ? true : wf        =>      c || wf
                ReportDiagnostic(s_takeConditionOrWhenFalse);
            }
            else if (whenTrue_isFalse)
            {
                // c ? false : wf       =>      !c && wf
                ReportDiagnostic(s_negateConditionAndWhenFalse);
            }
            else if (whenFalse_isTrue)
            {
                // c ? wt : true        =>      !c or wt
                ReportDiagnostic(s_negateConditionOrWhenTrue);
            }
            else if (whenFalse_isFalse)
            {
                // c ? wt : false       =>      c && wt
                ReportDiagnostic(s_takeConditionAndWhenTrue);
            }

            return;

            // local functions

            void ReportDiagnostic(ImmutableDictionary<string, string?> properties)
                => context.ReportDiagnostic(DiagnosticHelper.Create(
                    Descriptor,
                    conditionalExpression.GetLocation(),
                    styleOption.Notification,
                    additionalLocations: null,
                    properties));

            bool IsSimpleBooleanType(TExpressionSyntax node)
            {
                var typeInfo = semanticModel.GetTypeInfo(node, cancellationToken);
                var conversion = GetConversion(semanticModel, node, cancellationToken);

                return
                    conversion.MethodSymbol == null &&
                    typeInfo.Type?.SpecialType == SpecialType.System_Boolean &&
                    typeInfo.ConvertedType?.SpecialType == SpecialType.System_Boolean;
            }

            bool IsTrue(TExpressionSyntax node) => IsBoolValue(node, true);
            bool IsFalse(TExpressionSyntax node) => IsBoolValue(node, false);

            bool IsBoolValue(TExpressionSyntax node, bool value)
            {
                var constantValue = semanticModel.GetConstantValue(node, cancellationToken);
                return constantValue.HasValue && constantValue.Value is bool b && b == value;
            }
        }
    }
}
