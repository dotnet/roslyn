// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;

namespace Microsoft.CodeAnalysis.CodeStyle
{
    internal abstract class AbstractFormattingAnalyzer
        : AbstractBuiltInCodeStyleDiagnosticAnalyzer
    {
        protected AbstractFormattingAnalyzer()
            : base(
                IDEDiagnosticIds.FormattingDiagnosticId,
                EnforceOnBuildValues.Formatting,
                option: null,
                new LocalizableResourceString(nameof(CodeStyleResources.Fix_formatting), CodeStyleResources.ResourceManager, typeof(CodeStyleResources)),
                new LocalizableResourceString(nameof(CodeStyleResources.Fix_formatting), CodeStyleResources.ResourceManager, typeof(CodeStyleResources)))
        {
        }

        public sealed override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(Descriptor);

        public sealed override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SyntaxTreeWithoutSemanticsAnalysis;

        protected abstract ISyntaxFormattingService SyntaxFormattingService { get; }

        protected sealed override void InitializeWorker(AnalysisContext context)
            => context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);

        private void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
        {
            var options = SyntaxFormattingService.GetFormattingOptions(context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Tree));
            FormattingAnalyzerHelper.AnalyzeSyntaxTree(context, SyntaxFormattingService, Descriptor, options);
        }
    }
}
