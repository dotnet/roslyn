// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.UseIsNullCheck;

namespace Microsoft.CodeAnalysis.CSharp.UseIsNullCheck
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpUseIsNullCheckForCastAndEqualityOperatorDiagnosticAnalyzer
        : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        private static readonly ImmutableDictionary<string, string?> s_properties =
            ImmutableDictionary<string, string?>.Empty.Add(UseIsNullConstants.Kind, UseIsNullConstants.CastAndEqualityKey);
        private static readonly ImmutableDictionary<string, string?> s_NegatedProperties =
            s_properties.Add(UseIsNullConstants.Negated, "");

        public CSharpUseIsNullCheckForCastAndEqualityOperatorDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UseIsNullCheckDiagnosticId,
                   EnforceOnBuildValues.UseIsNullCheck,
                   CodeStyleOptions2.PreferIsNullCheckOverReferenceEqualityMethod,
                   CSharpAnalyzersResources.Use_is_null_check,
                   new LocalizableResourceString(nameof(AnalyzersResources.Null_check_can_be_simplified), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterCompilationStartAction(context =>
            {
                if (context.Compilation.LanguageVersion() < LanguageVersion.CSharp7)
                    return;

                context.RegisterSyntaxNodeAction(n => AnalyzeSyntax(n), SyntaxKind.EqualsExpression, SyntaxKind.NotEqualsExpression);
            });

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            var cancellationToken = context.CancellationToken;

            var semanticModel = context.SemanticModel;
            var syntaxTree = semanticModel.SyntaxTree;

            var option = context.Options.GetOption(CodeStyleOptions2.PreferIsNullCheckOverReferenceEqualityMethod, semanticModel.Language, syntaxTree, cancellationToken);
            if (!option.Value)
            {
                return;
            }

            var binaryExpression = (BinaryExpressionSyntax)context.Node;

            if (!IsObjectCastAndNullCheck(semanticModel, binaryExpression.Left, binaryExpression.Right) &&
                !IsObjectCastAndNullCheck(semanticModel, binaryExpression.Right, binaryExpression.Left))
            {
                return;
            }

            var severity = option.Notification.Severity;
            var properties = binaryExpression.Kind() == SyntaxKind.EqualsExpression
                ? s_properties
                : s_NegatedProperties;
            context.ReportDiagnostic(
                DiagnosticHelper.Create(
                    Descriptor, binaryExpression.GetLocation(), severity, additionalLocations: null, properties));
        }

        private static bool IsObjectCastAndNullCheck(
            SemanticModel semanticModel, ExpressionSyntax left, ExpressionSyntax right)
        {
            if (left is CastExpressionSyntax castExpression &&
                right.IsKind(SyntaxKind.NullLiteralExpression))
            {
                // make sure it's a cast to object, and that the thing we're casting actually has a type.
                if (semanticModel.GetTypeInfo(castExpression.Type).Type?.SpecialType == SpecialType.System_Object)
                {
                    var expressionType = semanticModel.GetTypeInfo(castExpression.Expression).Type;
                    if (expressionType != null)
                    {
                        if (expressionType is ITypeParameterSymbol typeParameter &&
                            !typeParameter.HasReferenceTypeConstraint)
                        {
                            return false;
                        }

                        return true;
                    }
                }
            }

            return false;
        }
    }
}
