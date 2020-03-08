// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Operations;

namespace Microsoft.CodeAnalysis.SimplifyConditional
{
    internal abstract class AbstractSimplifyConditionalDiagnosticAnalyzer<
        TSyntaxKind,
        TExpressionSyntax,
        TConditionalExpressionSyntax> :
        AbstractBuiltInCodeStyleDiagnosticAnalyzer
        where TSyntaxKind : struct
        where TExpressionSyntax : SyntaxNode
        where TConditionalExpressionSyntax : TExpressionSyntax
    {
        private const string Negate = AbstractSimplifyConditionalCodeFixProvider.Negate;
        private static readonly ImmutableDictionary<string, string> s_negateProperties =
            ImmutableDictionary<string, string>.Empty.Add(Negate, Negate);

        protected AbstractSimplifyConditionalDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.SimplifyConditionalExpressionDiagnosticId,
                   CodeStyleOptions.PreferSimplifiedConditionalExpression,
                   new LocalizableResourceString(nameof(FeaturesResources.Simplify_conditional_expression), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Conditional_expression_can_be_simplified), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        protected abstract ISyntaxFacts SyntaxFacts { get; }

        protected abstract CommonConversion GetConversion(SemanticModel semanticModel, TExpressionSyntax node, CancellationToken cancellationToken);

        public sealed override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected sealed override void InitializeWorker(AnalysisContext context)
        {
            var syntaxKinds = this.SyntaxFacts.SyntaxKinds;
            context.RegisterSyntaxNodeAction(
                AnalyzeConditionalExpression, syntaxKinds.Convert<TSyntaxKind>(syntaxKinds.ConditionalExpression));
        }

        private void AnalyzeConditionalExpression(SyntaxNodeAnalysisContext context)
        {
            var semanticModel = context.SemanticModel;
            var syntaxTree = semanticModel.SyntaxTree;
            var options = context.Options;
            var cancellationToken = context.CancellationToken;

            var styleOption = options.GetOption(
                CodeStyleOptions.PreferSimplifiedConditionalExpression,
                semanticModel.Language, syntaxTree, cancellationToken);
            if (!styleOption.Value)
            {
                // Bail immediately if the user has disabled this feature.
                return;
            }

            var conditionalExpression = (TConditionalExpressionSyntax)context.Node;
            this.SyntaxFacts.GetPartsOfConditionalExpression(
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

            var isTrueFalsePattern = IsTrue(whenTrue) && IsFalse(whenFalse);
            var isFalseTruePattern = IsFalse(whenTrue) && IsTrue(whenFalse);

            if (!isTrueFalsePattern && !isFalseTruePattern)
                return;

            var severity = styleOption.Notification.Severity;
            var properties = isTrueFalsePattern
                ? ImmutableDictionary<string, string>.Empty
                : s_negateProperties;

            context.ReportDiagnostic(DiagnosticHelper.Create(
                this.Descriptor,
                conditionalExpression.GetLocation(),
                severity,
                additionalLocations: null,
                properties));

            return;

            // local functions

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
