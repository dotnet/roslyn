// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.CodingConventions;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    internal abstract class AbstractFormattingAnalyzerImpl
    {
        private readonly DiagnosticDescriptor _descriptor;

        protected AbstractFormattingAnalyzerImpl(DiagnosticDescriptor descriptor)
        {
            _descriptor = descriptor;
        }

        internal void InitializeWorker(AnalysisContext context)
        {
            var workspace = new AdhocWorkspace();
            var codingConventionsManager = CodingConventionsManagerFactory.CreateCodingConventionsManager();

            context.RegisterSyntaxTreeAction(c => AnalyzeSyntaxTree(c, workspace, codingConventionsManager));
        }

        protected abstract OptionSet ApplyFormattingOptions(OptionSet optionSet, ICodingConventionContext codingConventionContext);

        private void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context, Workspace workspace, ICodingConventionsManager codingConventionsManager)
        {
            var tree = context.Tree;
            var cancellationToken = context.CancellationToken;

            var options = workspace.Options;
            if (File.Exists(tree.FilePath))
            {
                var codingConventionContext = codingConventionsManager.GetConventionContextAsync(tree.FilePath, cancellationToken).GetAwaiter().GetResult();
                options = ApplyFormattingOptions(options, codingConventionContext);
            }

            FormattingAnalyzerHelper.AnalyzeSyntaxTree(context, _descriptor, workspace, options);
        }
    }
}
