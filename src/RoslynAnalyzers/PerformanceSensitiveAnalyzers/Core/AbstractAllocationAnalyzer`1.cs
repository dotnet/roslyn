// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.PerformanceSensitiveAnalyzers
{
    internal abstract class AbstractAllocationAnalyzer<TLanguageKindEnum>
            : AbstractAllocationAnalyzer
            where TLanguageKindEnum : struct
    {
        protected abstract ImmutableArray<TLanguageKindEnum> Expressions { get; }

        protected sealed override ImmutableArray<OperationKind> Operations => ImmutableArray<OperationKind>.Empty;

        protected abstract void AnalyzeNode(SyntaxNodeAnalysisContext context, in PerformanceSensitiveInfo info);

        protected override void AnalyzeNode(OperationAnalysisContext context, in PerformanceSensitiveInfo info) { }

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();

            // This analyzer is triggered by an attribute, even if it appears in generated code
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);

            context.RegisterCompilationStartAction(compilationStartContext =>
            {
                var compilation = compilationStartContext.Compilation;
                var attributeSymbol = compilation.GetOrCreateTypeByMetadataName(AllocationRules.PerformanceSensitiveAttributeName);

                // Bail if PerformanceSensitiveAttribute is not declared in the compilation.
                if (attributeSymbol == null)
                {
                    return;
                }

                compilationStartContext.RegisterCodeBlockStartAction<TLanguageKindEnum>(blockStartContext =>
                {
                    var checker = new AttributeChecker(attributeSymbol);
                    RegisterSyntaxAnalysis(blockStartContext, checker);
                });
            });
        }

        private void RegisterSyntaxAnalysis(CodeBlockStartAnalysisContext<TLanguageKindEnum> codeBlockStartAnalysisContext, AttributeChecker performanceSensitiveAttributeChecker)
        {
            var owningSymbol = codeBlockStartAnalysisContext.OwningSymbol;

            if (!performanceSensitiveAttributeChecker.TryGetContainsPerformanceSensitiveInfo(owningSymbol, out var info))
            {
                return;
            }

            codeBlockStartAnalysisContext.RegisterSyntaxNodeAction(
                syntaxNodeContext =>
                {
                    AnalyzeNode(syntaxNodeContext, in info);
                },
                Expressions);
        }
    }
}
