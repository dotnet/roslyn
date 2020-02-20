// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            if (!(context.Options is WorkspaceAnalyzerOptions workspaceAnalyzerOptions))
            {
                return;
            }

            var tree = context.Tree;
            var cancellationToken = context.CancellationToken;

            var options = context.Options.GetAnalyzerOptionSet(tree, cancellationToken);
            var workspace = workspaceAnalyzerOptions.Services.Workspace;
            FormattingAnalyzerHelper.AnalyzeSyntaxTree(context, workspace, Descriptor, options);
        }
    }
}
