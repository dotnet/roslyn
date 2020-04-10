// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;

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
            => context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);

        private void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
        {
            var analyzerConfigOptions = context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Tree);
            FormattingAnalyzerHelper.AnalyzeSyntaxTree(context, SyntaxFormattingService, Descriptor, analyzerConfigOptions);
        }
    }
}
