// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.SimplifyConditional
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpSimplifyConditionalDiagnosticAnalyzer :
        AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        public const string Negate = nameof(Negate);
        private static readonly ImmutableDictionary<string, string> s_negateProperties =
            ImmutableDictionary<string, string>.Empty.Add(Negate, Negate);

        public CSharpSimplifyConditionalDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.SimplifyConditionalExpressionDiagnosticId,
                   CodeStyleOptions.PreferSimplifiedConditionalExpression,
                   title: new LocalizableResourceString(nameof(FeaturesResources.Simplify_conditional_expression), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   messageFormat: new LocalizableResourceString(nameof(FeaturesResources.Simplify_conditional_expression), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeConditionalExpression, SyntaxKind.ConditionalExpression);

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

            var conditionalExpression = (ConditionalExpressionSyntax)context.Node;
            var condition = conditionalExpression.Condition;
            var whenTrue = conditionalExpression.WhenTrue;
            var whenFalse = conditionalExpression.WhenFalse;

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

            bool IsSimpleBooleanType(ExpressionSyntax node)
            {
                var typeInfo = semanticModel.GetTypeInfo(node, cancellationToken);
                var conversion = semanticModel.GetConversion(node, cancellationToken);

                return
                    conversion.MethodSymbol == null &&
                    typeInfo.Type?.SpecialType == SpecialType.System_Boolean &&
                    typeInfo.ConvertedType?.SpecialType == SpecialType.System_Boolean;
            }

            bool IsTrue(ExpressionSyntax node) => IsBoolValue(node, true);
            bool IsFalse(ExpressionSyntax node) => IsBoolValue(node, false);

            bool IsBoolValue(ExpressionSyntax node, bool value)
            {
                var constantValue = semanticModel.GetConstantValue(node, cancellationToken);
                return constantValue.HasValue && constantValue.Value is bool b && b == value;
            }
        }
    }
}
