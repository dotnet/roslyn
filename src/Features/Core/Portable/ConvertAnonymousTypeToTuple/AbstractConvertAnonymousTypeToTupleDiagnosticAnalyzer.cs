// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.ConvertAnonymousTypeToTuple
{
    internal abstract class AbstractConvertAnonymousTypeToTupleDiagnosticAnalyzer<
        TSyntaxKind,
        TAnonymousObjectCreationExpressionSyntax>
        : AbstractBuiltInCodeStyleDiagnosticAnalyzer
        where TSyntaxKind : struct
        where TAnonymousObjectCreationExpressionSyntax : SyntaxNode
    {
        protected AbstractConvertAnonymousTypeToTupleDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.ConvertAnonymousTypeToTupleDiagnosticId,
                   option: null,
                   new LocalizableResourceString(nameof(FeaturesResources.Convert_to_tuple), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Convert_to_tuple), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        protected abstract TSyntaxKind GetAnonymousObjectCreationExpressionSyntaxKind();
        protected abstract int GetInitializerCount(TAnonymousObjectCreationExpressionSyntax anonymousType);

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(
                AnalyzeSyntax,
                GetAnonymousObjectCreationExpressionSyntaxKind());

        // Analysis is trivial.  All anonymous types with more than two fields are marked as being
        // convertible to a tuple.
        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            var anonymousType = (TAnonymousObjectCreationExpressionSyntax)context.Node;
            if (GetInitializerCount(anonymousType) < 2)
            {
                return;
            }

            context.ReportDiagnostic(
                DiagnosticHelper.Create(
                    Descriptor, context.Node.GetFirstToken().GetLocation(), ReportDiagnostic.Hidden,
                    additionalLocations: null, properties: null));
        }
    }
}
