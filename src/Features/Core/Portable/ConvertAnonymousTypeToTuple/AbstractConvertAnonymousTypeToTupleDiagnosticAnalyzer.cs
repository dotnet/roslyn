// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.ConvertAnonymousTypeToTuple
{
    internal abstract class AbstractConvertAnonymousTypeToTupleCodeRefactoringProvider<TSyntaxKind> 
        : AbstractCodeStyleDiagnosticAnalyzer
        where TSyntaxKind : struct
    {
        protected AbstractConvertAnonymousTypeToTupleCodeRefactoringProvider()
            : base(IDEDiagnosticIds.ConvertAnonymousTypeToTupleDiagnosticId,
                   new LocalizableResourceString(nameof(FeaturesResources.Convert_anonymous_type_to_tuple), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Convert_anonymous_type_to_tuple), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        protected abstract TSyntaxKind GetAnonymousObjectCreationExpressionSyntaxKind();

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        public override bool OpenFileOnly(Workspace workspace)
            => false;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterCompilationStartAction(csac =>
            {
                // Only bother to offer to convert to a tuple if we actually know about System.ValueTuple.
                var valueTupleType = csac.Compilation.GetTypeByMetadataName(typeof(ValueTuple).FullName);
                if (valueTupleType != null)
                {
                    csac.RegisterSyntaxNodeAction(
                        AnalyzeSyntax,
                        GetAnonymousObjectCreationExpressionSyntaxKind());
                }
            });

        // Analysis is trivial.  All anonymous types are marked as being convertible to a tuple.
        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
            => context.ReportDiagnostic(
                DiagnosticHelper.Create(
                    Descriptor, context.Node.GetFirstToken().GetLocation(), ReportDiagnostic.Hidden,
                    additionalLocations: null, properties: null));
    }
}
