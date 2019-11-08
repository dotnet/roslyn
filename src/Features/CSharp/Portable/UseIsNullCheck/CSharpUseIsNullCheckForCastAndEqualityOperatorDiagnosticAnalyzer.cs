// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.UseIsNullCheck;

namespace Microsoft.CodeAnalysis.CSharp.UseIsNullCheck
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpUseIsNullCheckForCastAndEqualityOperatorDiagnosticAnalyzer
        : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        private static readonly ImmutableDictionary<string, string> s_properties =
            ImmutableDictionary<string, string>.Empty.Add(UseIsNullConstants.Kind, UseIsNullConstants.CastAndEqualityKey);

        public CSharpUseIsNullCheckForCastAndEqualityOperatorDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UseIsNullCheckDiagnosticId,
                   CodeStyleOptions.PreferIsNullCheckOverReferenceEqualityMethod,
                   CSharpFeaturesResources.Use_is_null_check,
                   new LocalizableResourceString(nameof(FeaturesResources.Null_check_can_be_simplified), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.EqualsExpression, SyntaxKind.NotEqualsExpression);

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            var cancellationToken = context.CancellationToken;

            var semanticModel = context.SemanticModel;
            var syntaxTree = semanticModel.SyntaxTree;

            if (((CSharpParseOptions)syntaxTree.Options).LanguageVersion < LanguageVersion.CSharp7)
            {
                return;
            }

            var optionSet = context.Options.GetDocumentOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();
            if (optionSet == null)
            {
                return;
            }

            var option = optionSet.GetOption(CodeStyleOptions.PreferIsNullCheckOverReferenceEqualityMethod, semanticModel.Language);
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
            context.ReportDiagnostic(
                DiagnosticHelper.Create(
                    Descriptor, binaryExpression.GetLocation(), severity, additionalLocations: null, s_properties));
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
                        if (expressionType is ITypeParameterSymbol { HasReferenceTypeConstraint: false } typeParameter)
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
