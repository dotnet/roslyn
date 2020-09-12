// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal sealed partial class DiagnosticAnalyzerInfoCache
    {
        private readonly ConditionalWeakTable<AnalyzerOptions, ConcurrentDictionary<DiagnosticAnalyzer, bool?>> _compilationEndAnalyzerInfo
            = new ConditionalWeakTable<AnalyzerOptions, ConcurrentDictionary<DiagnosticAnalyzer, bool?>>();

        public async Task<bool?> IsCompilationEndAnalyzerAsync(DiagnosticAnalyzer analyzer, Project project, CancellationToken cancellationToken)
        {
            if (!project.SupportsCompilation)
            {
                return false;
            }

            var analyzerOptions = project.AnalyzerOptions;

            // PERF: Avoid fetching compilation if we have already computed the value.
            var endAnalyzerMap = _compilationEndAnalyzerInfo.GetOrCreateValue(analyzerOptions);
            if (endAnalyzerMap.TryGetValue(analyzer, out var isCompilationEndAnalyzer))
            {
                return isCompilationEndAnalyzer;
            }

            var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
            return GetOrComputeIsCompilationEndAnalyzer(analyzer, analyzerOptions, compilation, endAnalyzerMap);
        }

        public bool? IsCompilationEndAnalyzer(DiagnosticAnalyzer analyzer, AnalyzerOptions analyzerOptions, Compilation compilation)
        {
            var endAnalyzerMap = _compilationEndAnalyzerInfo.GetOrCreateValue(analyzerOptions);
            return GetOrComputeIsCompilationEndAnalyzer(analyzer, analyzerOptions, compilation, endAnalyzerMap);
        }

        private static bool? GetOrComputeIsCompilationEndAnalyzer(
            DiagnosticAnalyzer analyzer,
            AnalyzerOptions analyzerOptions,
            Compilation compilation,
            ConcurrentDictionary<DiagnosticAnalyzer, bool?> endAnalyzerMap)
        {
            return endAnalyzerMap.AddOrUpdate(
                analyzer,
                addValueFactory: ComputeIsCompilationEndAnalyzer,
                updateValueFactory: (a, currentValue) => currentValue != null ? currentValue : ComputeIsCompilationEndAnalyzer(a));

            // Local functions
            bool? ComputeIsCompilationEndAnalyzer(DiagnosticAnalyzer analyzer)
            {
                Contract.ThrowIfNull(compilation);

                try
                {
                    // currently, this is only way to see whether analyzer has compilation end analysis or not.
                    // also, analyzer being compilation end analyzer or not is dynamic. so this can return different value based on
                    // given compilation or options.
                    //
                    // but for now, this is what we decided in design meeting until we decide how to deal with compilation end analyzer
                    // long term
                    var context = new CollectCompilationActionsContext(compilation, analyzerOptions);
                    analyzer.Initialize(context);

                    return context.IsCompilationEndAnalyzer;
                }
                catch
                {
                    // analyzer.initialize can throw. when that happens, we will try again next time.
                    // we are not logging anything here since it will be logged by CompilationWithAnalyzer later
                    // in the error list
                    return null;
                }
            }
        }

        /// <summary>
        /// Right now, there is no API compiler will tell us whether DiagnosticAnalyzer has compilation end analysis or not
        /// 
        /// </summary>
        private class CollectCompilationActionsContext : AnalysisContext
        {
            private readonly Compilation _compilation;
            private readonly AnalyzerOptions _analyzerOptions;

            public CollectCompilationActionsContext(Compilation compilation, AnalyzerOptions analyzerOptions)
            {
                _compilation = compilation;
                _analyzerOptions = analyzerOptions;
            }

            public bool IsCompilationEndAnalyzer { get; private set; } = false;

            public override void RegisterCompilationAction(Action<CompilationAnalysisContext> action)
            {
                if (action == null)
                {
                    return;
                }

                IsCompilationEndAnalyzer = true;
            }

            public override void RegisterCompilationStartAction(Action<CompilationStartAnalysisContext> action)
            {
                if (action == null)
                {
                    return;
                }

                var nestedContext = new CollectNestedCompilationContext(_compilation, _analyzerOptions, CancellationToken.None);
                action(nestedContext);

                IsCompilationEndAnalyzer |= nestedContext.IsCompilationEndAnalyzer;
            }

            #region not used
            public override void RegisterCodeBlockAction(Action<CodeBlockAnalysisContext> action) { }
            public override void RegisterCodeBlockStartAction<TLanguageKindEnum>(Action<CodeBlockStartAnalysisContext<TLanguageKindEnum>> action) { }
            public override void RegisterSemanticModelAction(Action<SemanticModelAnalysisContext> action) { }
            public override void RegisterSymbolAction(Action<SymbolAnalysisContext> action, ImmutableArray<SymbolKind> symbolKinds) { }
            public override void RegisterSyntaxNodeAction<TLanguageKindEnum>(Action<SyntaxNodeAnalysisContext> action, ImmutableArray<TLanguageKindEnum> syntaxKinds) { }
            public override void RegisterSyntaxTreeAction(Action<SyntaxTreeAnalysisContext> action) { }
            public override void ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags analysisMode) { }
            public override void EnableConcurrentExecution() { }
            public override void RegisterOperationAction(Action<OperationAnalysisContext> action, ImmutableArray<OperationKind> operationKinds) { }
            public override void RegisterOperationBlockAction(Action<OperationBlockAnalysisContext> action) { }
            public override void RegisterOperationBlockStartAction(Action<OperationBlockStartAnalysisContext> action) { }
            public override void RegisterSymbolStartAction(Action<SymbolStartAnalysisContext> action, SymbolKind symbolKind) { }
            public override void RegisterAdditionalFileAction(Action<AdditionalFileAnalysisContext> action) { }

            #endregion

            private class CollectNestedCompilationContext : CompilationStartAnalysisContext
            {
                public bool IsCompilationEndAnalyzer { get; private set; } = false;

                public CollectNestedCompilationContext(Compilation compilation, AnalyzerOptions options, CancellationToken cancellationToken)
                    : base(compilation, options, cancellationToken)
                {
                }

                public override void RegisterCompilationEndAction(Action<CompilationAnalysisContext> action)
                {
                    if (action == null)
                    {
                        return;
                    }

                    IsCompilationEndAnalyzer = true;
                }

                #region not used
                public override void RegisterCodeBlockAction(Action<CodeBlockAnalysisContext> action) { }
                public override void RegisterCodeBlockStartAction<TLanguageKindEnum>(Action<CodeBlockStartAnalysisContext<TLanguageKindEnum>> action) { }
                public override void RegisterSemanticModelAction(Action<SemanticModelAnalysisContext> action) { }
                public override void RegisterSymbolAction(Action<SymbolAnalysisContext> action, ImmutableArray<SymbolKind> symbolKinds) { }
                public override void RegisterSyntaxNodeAction<TLanguageKindEnum>(Action<SyntaxNodeAnalysisContext> action, ImmutableArray<TLanguageKindEnum> syntaxKinds) { }
                public override void RegisterSyntaxTreeAction(Action<SyntaxTreeAnalysisContext> action) { }
                public override void RegisterOperationAction(Action<OperationAnalysisContext> action, ImmutableArray<OperationKind> operationKinds) { }
                public override void RegisterOperationBlockAction(Action<OperationBlockAnalysisContext> action) { }
                public override void RegisterOperationBlockStartAction(Action<OperationBlockStartAnalysisContext> action) { }
                public override void RegisterSymbolStartAction(Action<SymbolStartAnalysisContext> action, SymbolKind symbolKind) { }
                public override void RegisterAdditionalFileAction(Action<AdditionalFileAnalysisContext> action) { }

                #endregion
            }
        }
    }
}
