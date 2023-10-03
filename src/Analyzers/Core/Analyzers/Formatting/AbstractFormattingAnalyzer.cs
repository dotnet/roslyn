// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
                new LocalizableResourceString(nameof(AnalyzersResources.Fix_formatting), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)),
                new LocalizableResourceString(nameof(AnalyzersResources.Fix_formatting), AnalyzersResources.ResourceManager, typeof(AnalyzersResources)))
        {
        }

        public sealed override DiagnosticAnalyzerCategory GetAnalyzerCategory()
            => DiagnosticAnalyzerCategory.SyntaxTreeWithoutSemanticsAnalysis;

        protected abstract ISyntaxFormatting SyntaxFormatting { get; }

        protected sealed override void InitializeWorker(AnalysisContext context)
            => context.RegisterCompilationStartAction(context =>
                context.RegisterSyntaxTreeAction(treeContext => AnalyzeSyntaxTree(treeContext, context.Compilation.Options)));

        /// <summary>
        /// Fixing formatting is high priority.  It's something the user wants to be able to fix quickly, is driven by
        /// them acting on an error reported in code, and can be computed fast as it only uses syntax not semantics.
        /// It's also the 8th most common fix that people use, and is picked almost all the times it is shown.
        /// </summary>
        public override bool IsHighPriority => true;

        private void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context, CompilationOptions compilationOptions)
        {
            if (ShouldSkipAnalysis(context, compilationOptions, notification: null))
                return;

            var options = context.GetAnalyzerOptions().GetSyntaxFormattingOptions(SyntaxFormatting);
            FormattingAnalyzerHelper.AnalyzeSyntaxTree(context, SyntaxFormatting, Descriptor, options);
        }
    }
}
