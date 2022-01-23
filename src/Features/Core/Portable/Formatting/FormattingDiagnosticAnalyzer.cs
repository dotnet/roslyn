// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Formatting
{
    [DiagnosticAnalyzer(LanguageNames.CSharp, LanguageNames.VisualBasic)]
    internal class FormattingDiagnosticAnalyzer
        : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        public FormattingDiagnosticAnalyzer()
            : base(
                IDEDiagnosticIds.FormattingDiagnosticId,
                EnforceOnBuildValues.Formatting,
                option: null,   // No unique option to configure diagnosticId
                new LocalizableResourceString(nameof(FeaturesResources.Fix_formatting), FeaturesResources.ResourceManager, typeof(FeaturesResources)),
                new LocalizableResourceString(nameof(FeaturesResources.Fix_formatting), FeaturesResources.ResourceManager, typeof(FeaturesResources)))
        {
        }

        public override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SyntaxTreeWithoutSemanticsAnalysis;

        protected override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);

        private void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
        {
            if (context.Options is not WorkspaceAnalyzerOptions workspaceAnalyzerOptions)
            {
                return;
            }

            var tree = context.Tree;
            var cancellationToken = context.CancellationToken;
            var optionSet = context.Options.GetAnalyzerOptionSet(tree, cancellationToken);
            var options = SyntaxFormattingOptions.Create(optionSet, workspaceAnalyzerOptions.Services, tree.Options.Language);

            FormattingAnalyzerHelper.AnalyzeSyntaxTree(context, workspaceAnalyzerOptions.Services, Descriptor, options);
        }
    }
}
