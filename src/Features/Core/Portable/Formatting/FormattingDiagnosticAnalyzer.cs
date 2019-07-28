// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

            var options = context.Options.GetDocumentOptionSetAsync(tree, cancellationToken).GetAwaiter().GetResult();
            if (options == null)
            {
                return;
            }

            var workspace = workspaceAnalyzerOptions.Services.Workspace;
            FormattingAnalyzerHelper.AnalyzeSyntaxTree(context, workspace, Descriptor, options);
        }
    }
}
