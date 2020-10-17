﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
                   CodeStyleOptions2.PreferIsNullCheckOverReferenceEqualityMethod,
                   CSharpAnalyzersResources.Use_is_null_check,
                   new LocalizableResourceString(nameof(AnalyzersResources.Null_check_can_be_simplified), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)))
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
