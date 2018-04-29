// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.UseIsNullCheck
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpUseIsNullCheckForCastAndEqualityOperatorDiagnosticAnalyzer
        : AbstractCodeStyleDiagnosticAnalyzer
    {
        public CSharpUseIsNullCheckForCastAndEqualityOperatorDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UseIsNullCheckForCastAndEqualityOperatorDiagnosticId,
                   CSharpFeaturesResources.Use_is_null_check,
                   new LocalizableResourceString(nameof(FeaturesResources.Null_check_can_be_simplified), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        public override bool OpenFileOnly(Workspace workspace)
            => false;

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

            var option = optionSet.GetOption(CodeStyleOptions.PreferIsNullCheckOverCastAndEqualityOperator, semanticModel.Language);
            if (!option.Value)
            {
                return;
            }

            var binaryExpression = (BinaryExpressionSyntax)context.Node;

            if (!IsObjectCastAndReferenceEquals(semanticModel, binaryExpression.Left, binaryExpression.Right) &&
                !IsObjectCastAndReferenceEquals(semanticModel, binaryExpression.Right, binaryExpression.Left))
            {
                return;
            }

            var severity = option.Notification.Value;
            context.ReportDiagnostic(
                Diagnostic.Create(
                    GetDescriptorWithSeverity(severity), binaryExpression.GetLocation()));
        }

        private bool IsObjectCastAndReferenceEquals(
            SemanticModel semanticModel, ExpressionSyntax left, ExpressionSyntax right)
        {
            if (left is CastExpressionSyntax castExpression &&
                right.IsKind(SyntaxKind.NullLiteralExpression))
            {
                // make sure it's a cast to object, and that the thing we're casting actually has a type.
                return semanticModel.GetTypeInfo(castExpression.Type).Type?.SpecialType == SpecialType.System_Object &&
                       semanticModel.GetTypeInfo(castExpression.Expression).Type != null;
            }

            return false;
        }
    }
}
