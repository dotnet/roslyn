// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.UseInferredMemberName
{
    internal abstract class AbstractUseInferredMemberNameDiagnosticAnalyzer : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        abstract protected void LanguageSpecificAnalyzeSyntax(SyntaxNodeAnalysisContext context, SyntaxTree syntaxTree, OptionSet optionSet);

        public AbstractUseInferredMemberNameDiagnosticAnalyzer()
            : base(IDEDiagnosticIds.UseInferredMemberNameDiagnosticId,
                   CodeStyleOptions.PreferInferredTupleNames,
                   new LocalizableResourceString(nameof(FeaturesResources.Use_inferred_member_name), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                   new LocalizableResourceString(nameof(FeaturesResources.Member_name_can_be_simplified), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        protected void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            var cancellationToken = context.CancellationToken;

            var syntaxTree = context.Node.SyntaxTree;
            var optionSet = context.Options.GetAnalyzerOptionSetAsync(syntaxTree, cancellationToken).GetAwaiter().GetResult();

            LanguageSpecificAnalyzeSyntax(context, syntaxTree, optionSet);
        }
    }
}
