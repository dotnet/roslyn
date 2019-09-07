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
            if (semanticModel.GetSymbolInfo(binaryExpression).Symbol?.ContainingType?.SpecialType != SpecialType.System_Object)
            {
                return;
            }

            ExpressionSyntax notNull;
            if (binaryExpression.Left.IsKind(SyntaxKind.NullLiteralExpression))
            {
                notNull = binaryExpression.Right;
            }
            else if (binaryExpression.Right.IsKind(SyntaxKind.NullLiteralExpression))
            {
                notNull = binaryExpression.Left;
            }
            else
            {
                return;
            }
            if (notNull.IsKind(SyntaxKind.NullLiteralExpression))
            {
                return;
            }

            var hasCast = false;
            if (notNull is CastExpressionSyntax castExpression &&
                semanticModel.GetTypeInfo(castExpression.Type).Type?.SpecialType == SpecialType.System_Object)
            {
                hasCast = true;
                notNull = castExpression.Expression;
            }

            var exprType = semanticModel.GetTypeInfo(notNull).Type;
            if (exprType.IsValueType)
            {
                // `t == null` can't happen.
                // `(object)t == null` can be safely converted to `(object)t is null`.
                // `t is null` won't be permited.
                hasCast = false;
            }
            else if (!exprType.IsReferenceType) // Unconstrained type parameter
            {
                // Check 8.0 if https://github.com/dotnet/csharplang/issues/1284 is considered implemented.
                if (hasCast)
                {
                    // if (<8.0)
                    hasCast = false;
                }
                else
                {
                    // if (<8.0)
                    return;
                }
            }

            var properties = ImmutableDictionary<string, string>.Empty.Add(
                UseIsNullConstants.Kind, UseIsNullConstants.CastAndEqualityKey);
            if (hasCast)
            {
                properties = properties.Add(CSharpUseIsNullCheckForCastAndEqualityOperatorCodeFixProvider.RemoveObjectCast, "");
            }

            var severity = option.Notification.Severity;
            context.ReportDiagnostic(
                DiagnosticHelper.Create(
                    Descriptor, binaryExpression.GetLocation(), severity, additionalLocations: null, properties));
        }
    }
}
