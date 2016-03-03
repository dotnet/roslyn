// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    /// <summary>
    /// Diagnostic Executor that only relies on compiler layer. this might be replaced by new CompilationWithAnalyzer API.
    /// </summary>
    internal static class CompilerDiagnosticExecutor
    {
        public static async Task<AnalysisResult> AnalyzeAsync(
            this Compilation compilation, ImmutableArray<DiagnosticAnalyzer> analyzers, CompilationWithAnalyzersOptions options, CancellationToken cancellationToken)
        {
            // Create driver that holds onto compilation and associated analyzers
            var analyzerDriver = compilation.WithAnalyzers(analyzers, options);

            // Run all analyzers at once.
            // REVIEW: why there are 2 different cancellation token? one that I can give to constructor and one I can give in to each method?
            // REVIEW: we drop all those allocations for the diagnostics returned. can we avoid this?
            await analyzerDriver.GetAnalyzerDiagnosticsAsync(cancellationToken).ConfigureAwait(false);

            // this is wierd, but now we iterate through each analyzer for each tree to get cached result.
            // REVIEW: no better way to do this?
            var noSpanFilter = default(TextSpan?);

            var resultMap = new AnalysisResult.ResultMap();
            foreach (var analyzer in analyzers)
            {
                // REVIEW: more unnecessary allocations just to get diagnostics per analyzer
                var oneAnalyzers = ImmutableArray.Create(analyzer);

                foreach (var tree in compilation.SyntaxTrees)
                {
                    var model = compilation.GetSemanticModel(tree);

                    resultMap.AddSyntaxDiagnostics(analyzer, tree, await analyzerDriver.GetAnalyzerSyntaxDiagnosticsAsync(tree, oneAnalyzers, cancellationToken).ConfigureAwait(false));
                    resultMap.AddSemanticDiagnostics(analyzer, tree, await analyzerDriver.GetAnalyzerSemanticDiagnosticsAsync(model, noSpanFilter, oneAnalyzers, cancellationToken).ConfigureAwait(false));
                }

                resultMap.AddCompilationDiagnostics(analyzer, await analyzerDriver.GetAnalyzerCompilationDiagnosticsAsync(oneAnalyzers, cancellationToken).ConfigureAwait(false));
            }

            return new AnalysisResult(resultMap);
        }
    }

    // REVIEW: this will probably go away once we have new API.
    //         now things run sequencially, so no thread-safety.
    internal struct AnalysisResult
    {
        private readonly ResultMap resultMap;

        public AnalysisResult(ResultMap resultMap)
        {
            this.resultMap = resultMap;
        }

        internal struct ResultMap
        {
            private Dictionary<SyntaxTree, List<Diagnostic>> _lazySyntaxLocals;
            private Dictionary<SyntaxTree, List<Diagnostic>> _lazySemanticLocals;

            private Dictionary<SyntaxTree, List<Diagnostic>> _lazyNonLocals;
            private List<Diagnostic> _lazyOthers;

            public void AddSyntaxDiagnostics(DiagnosticAnalyzer analyzer, SyntaxTree tree, ImmutableArray<Diagnostic> diagnostics)
            {
                AddDiagnostics(ref _lazySyntaxLocals, tree, diagnostics);
            }

            public void AddSemanticDiagnostics(DiagnosticAnalyzer analyzer, SyntaxTree tree, ImmutableArray<Diagnostic> diagnostics)
            {
                AddDiagnostics(ref _lazySemanticLocals, tree, diagnostics);
            }

            public void AddCompilationDiagnostics(DiagnosticAnalyzer analyzer, ImmutableArray<Diagnostic> diagnostics)
            {
                Dictionary<SyntaxTree, List<Diagnostic>> dummy = null;
                AddDiagnostics(ref dummy, tree: null, diagnostics: diagnostics);

                // dummy should be always null
                Contract.Requires(dummy == null);
            }

            private void AddDiagnostics(
                ref Dictionary<SyntaxTree, List<Diagnostic>> _lazyLocals, SyntaxTree tree, ImmutableArray<Diagnostic> diagnostics)
            {
                if (diagnostics.Length == 0)
                {
                    return;
                }

                for (var i = 0; i < diagnostics.Length; i++)
                {
                    var diagnostic = diagnostics[i];

                    // REVIEW: what is our plan for additional locations? 
                    switch (diagnostic.Location.Kind)
                    {
                        case LocationKind.None:
                        case LocationKind.ExternalFile:
                            {
                                // no location or reported to external files
                                _lazyOthers = _lazyOthers ?? new List<Diagnostic>();
                                _lazyOthers.Add(diagnostic);
                                break;
                            }
                        case LocationKind.SourceFile:
                            {
                                if (tree != null && diagnostic.Location.SourceTree == tree)
                                {
                                    // local diagnostics to a file
                                    _lazyLocals = _lazyLocals ?? new Dictionary<SyntaxTree, List<Diagnostic>>();
                                    _lazyLocals.GetOrAdd(diagnostic.Location.SourceTree, _ => new List<Diagnostic>()).Add(diagnostic);
                                }
                                else if (diagnostic.Location.SourceTree != null)
                                {
                                    // non local diagnostics to a file
                                    _lazyNonLocals = _lazyNonLocals ?? new Dictionary<SyntaxTree, List<Diagnostic>>();
                                    _lazyNonLocals.GetOrAdd(diagnostic.Location.SourceTree, _ => new List<Diagnostic>()).Add(diagnostic);
                                }
                                else
                                {
                                    // non local diagnostics without location
                                    _lazyOthers = _lazyOthers ?? new List<Diagnostic>();
                                    _lazyOthers.Add(diagnostic);
                                }

                                break;
                            }
                        case LocationKind.MetadataFile:
                        case LocationKind.XmlFile:
                            {
                                // something we don't care
                                continue;
                            }
                        default:
                            {
                                Contract.Fail("should not reach");
                                break;
                            }
                    }
                }
            }
        }
    }
}
