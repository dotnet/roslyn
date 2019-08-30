// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.VisualStudio.CodingConventions;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    internal abstract class AbstractFormattingAnalyzer
        : AbstractCodeStyleDiagnosticAnalyzer
    {
        protected AbstractFormattingAnalyzer()
            : base(
                IDEDiagnosticIds.FormattingDiagnosticId,
                new LocalizableResourceString(nameof(CodeStyleResources.Fix_formatting), CodeStyleResources.ResourceManager, typeof(CodeStyleResources)),
                new LocalizableResourceString(nameof(CodeStyleResources.Fix_formatting), CodeStyleResources.ResourceManager, typeof(CodeStyleResources)))
        {
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Descriptor);

        protected abstract ISyntaxFormattingService SyntaxFormattingService { get; }

        protected override void InitializeWorker(AnalysisContext context)
        {
            context.RegisterSyntaxTreeAction(c =>
            {
                var codingConventionsManager = new AnalyzerConfigCodingConventionsManager(c.Tree, c.Options);
                AnalyzeSyntaxTree(c, codingConventionsManager);
            });
        }

        protected abstract OptionSet ApplyFormattingOptions(OptionSet optionSet, ICodingConventionContext codingConventionContext);

        private void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context, ICodingConventionsManager codingConventionsManager)
        {
            var tree = context.Tree;
            var cancellationToken = context.CancellationToken;

            OptionSet options = CompilerAnalyzerConfigOptions.Empty;
            if (File.Exists(tree.FilePath))
            {
                var codingConventionContext = codingConventionsManager.GetConventionContextAsync(tree.FilePath, cancellationToken).GetAwaiter().GetResult();
                options = ApplyFormattingOptions(options, codingConventionContext);
            }

            FormattingAnalyzerHelper.AnalyzeSyntaxTree(context, SyntaxFormattingService, Descriptor, options);
        }
    }
}
