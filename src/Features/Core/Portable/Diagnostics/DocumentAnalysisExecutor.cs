// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Executes analyzers on a document for computing local syntax/semantic diagnostics for a specific <see cref="DocumentAnalysisScope"/>.
    /// </summary>
    internal sealed class DocumentAnalysisExecutor
    {
        private readonly CompilationWithAnalyzers? _compilationWithAnalyzers;
        private readonly DiagnosticAnalyzerInfoCache _analyzerInfoCache;
        private readonly ImmutableArray<DiagnosticAnalyzer> _compilationBasedAnalyzersInAnalysisScope;

        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>? _lazySyntaxDiagnostics;
        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>? _lazySemanticDiagnostics;

        public DocumentAnalysisExecutor(
            DocumentAnalysisScope analysisScope,
            CompilationWithAnalyzers? compilationWithAnalyzers,
            DiagnosticAnalyzerInfoCache analyzerInfoCache)
        {
            AnalysisScope = analysisScope;
            _compilationWithAnalyzers = compilationWithAnalyzers;
            _analyzerInfoCache = analyzerInfoCache;

            var compilationBasedAnalyzers = compilationWithAnalyzers?.Analyzers.ToImmutableHashSet();
            _compilationBasedAnalyzersInAnalysisScope = compilationBasedAnalyzers != null
                ? analysisScope.Analyzers.WhereAsArray(compilationBasedAnalyzers.Contains)
                : ImmutableArray<DiagnosticAnalyzer>.Empty;
        }

        public DocumentAnalysisScope AnalysisScope { get; }

        /// <summary>
        /// Return all local diagnostics (syntax, semantic) that belong to given document for the given analyzer by calculating them.
        /// </summary>
        public async Task<IEnumerable<DiagnosticData>> ComputeDiagnosticsAsync(DiagnosticAnalyzer analyzer, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(AnalysisScope.Analyzers.Contains(analyzer));

            var document = AnalysisScope.Document;
            var span = AnalysisScope.Span;
            var kind = AnalysisScope.Kind;

            var loadDiagnostic = await document.State.GetLoadDiagnosticAsync(cancellationToken).ConfigureAwait(false);

            if (analyzer == FileContentLoadAnalyzer.Instance)
            {
                return loadDiagnostic != null ?
                    SpecializedCollections.SingletonEnumerable(DiagnosticData.Create(loadDiagnostic, document)) :
                    SpecializedCollections.EmptyEnumerable<DiagnosticData>();
            }

            if (loadDiagnostic != null)
            {
                return SpecializedCollections.EmptyEnumerable<DiagnosticData>();
            }

            if (analyzer is DocumentDiagnosticAnalyzer documentAnalyzer)
            {
                var diagnostics = await AnalyzerHelper.ComputeDocumentDiagnosticAnalyzerDiagnosticsAsync(
                    documentAnalyzer, document, kind, _compilationWithAnalyzers?.Compilation, cancellationToken).ConfigureAwait(false);

                return diagnostics.ConvertToLocalDiagnostics(document, span);
            }

            // quick optimization to reduce allocations.
            if (_compilationWithAnalyzers == null || !analyzer.SupportAnalysisKind(kind))
            {
                if (kind == AnalysisKind.Syntax)
                {
                    Logger.Log(FunctionId.Diagnostics_SyntaxDiagnostic,
                        (r, d, a, k) => $"Driver: {r != null}, {d.Id}, {d.Project.Id}, {a}, {k}", _compilationWithAnalyzers, document, analyzer, kind);
                }

                return SpecializedCollections.EmptyEnumerable<DiagnosticData>();
            }

            // if project is not loaded successfully then, we disable semantic errors for compiler analyzers
            var isCompilerAnalyzer = analyzer.IsCompilerAnalyzer();
            if (kind != AnalysisKind.Syntax && isCompilerAnalyzer)
            {
                var isEnabled = await document.Project.HasSuccessfullyLoadedAsync(cancellationToken).ConfigureAwait(false);

                Logger.Log(FunctionId.Diagnostics_SemanticDiagnostic, (a, d, e) => $"{a}, ({d.Id}, {d.Project.Id}), Enabled:{e}", analyzer, document, isEnabled);

                if (!isEnabled)
                {
                    return SpecializedCollections.EmptyEnumerable<DiagnosticData>();
                }
            }

            var skippedAnalyzerInfo = document.Project.GetSkippedAnalyzersInfo(_analyzerInfoCache);
            ImmutableArray<string> filteredIds;

            switch (kind)
            {
                case AnalysisKind.Syntax:
                    var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                    if (tree == null)
                    {
                        return SpecializedCollections.EmptyEnumerable<DiagnosticData>();
                    }

                    var diagnostics = await GetSyntaxDiagnosticsAsync(tree, analyzer, isCompilerAnalyzer, cancellationToken).ConfigureAwait(false);

                    if (diagnostics.IsDefaultOrEmpty)
                    {
                        Logger.Log(FunctionId.Diagnostics_SyntaxDiagnostic, (d, a, t) => $"{d.Id}, {d.Project.Id}, {a}, {t.Length}", document, analyzer, tree);
                    }
                    else if (skippedAnalyzerInfo.FilteredDiagnosticIdsForAnalyzers.TryGetValue(analyzer, out filteredIds))
                    {
                        diagnostics = diagnostics.Filter(filteredIds);
                    }

                    Debug.Assert(diagnostics.Length == CompilationWithAnalyzers.GetEffectiveDiagnostics(diagnostics, _compilationWithAnalyzers.Compilation).Count());
                    return diagnostics.ConvertToLocalDiagnostics(document, span);

                case AnalysisKind.Semantic:
                    var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                    if (model == null)
                    {
                        return SpecializedCollections.EmptyEnumerable<DiagnosticData>();
                    }

                    diagnostics = await GetSemanticDiagnosticsAsync(model, analyzer, isCompilerAnalyzer, cancellationToken).ConfigureAwait(false);

                    if (skippedAnalyzerInfo.FilteredDiagnosticIdsForAnalyzers.TryGetValue(analyzer, out filteredIds))
                    {
                        diagnostics = diagnostics.Filter(filteredIds);
                    }

                    Debug.Assert(diagnostics.Length == CompilationWithAnalyzers.GetEffectiveDiagnostics(diagnostics, _compilationWithAnalyzers.Compilation).Count());
                    return diagnostics.ConvertToLocalDiagnostics(document, span);

                default:
                    throw ExceptionUtilities.UnexpectedValue(kind);
            }
        }

        private async Task<ImmutableArray<Diagnostic>> GetSyntaxDiagnosticsAsync(SyntaxTree tree, DiagnosticAnalyzer analyzer, bool isCompilerAnalyzer, CancellationToken cancellationToken)
        {
            // PERF:
            //  1. Compute diagnostics for all analyzers with a single invocation into CompilationWithAnalyzers.
            //     This is critical for better analyzer execution performance.
            //  2. Ensure that the compiler analyzer is treated specially and does not block on diagnostic computation
            //     for rest of the analyzers. This is needed to ensure faster refresh for compiler diagnostics while typing.

            RoslynDebug.Assert(_compilationWithAnalyzers != null);
            RoslynDebug.Assert(_compilationBasedAnalyzersInAnalysisScope.Contains(analyzer));

            if (isCompilerAnalyzer)
            {
                // TODO: Move this invocation to OOP
                return await _compilationWithAnalyzers.GetAnalyzerSyntaxDiagnosticsAsync(tree, ImmutableArray.Create(analyzer), cancellationToken).ConfigureAwait(false);
            }

            if (_lazySyntaxDiagnostics == null)
            {
                // TODO: Move this invocation to OOP
                var analysisResult = await _compilationWithAnalyzers.GetAnalysisResultAsync(tree, _compilationBasedAnalyzersInAnalysisScope, cancellationToken).ConfigureAwait(false);
                var treeDiagnostics = analysisResult.SyntaxDiagnostics.TryGetValue(tree, out var value) ? value : ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>.Empty;
                Interlocked.CompareExchange(ref _lazySyntaxDiagnostics, treeDiagnostics, null);
            }

            return _lazySyntaxDiagnostics.TryGetValue(analyzer, out var diagnostics) ?
                diagnostics :
                ImmutableArray<Diagnostic>.Empty;
        }

        private async Task<ImmutableArray<Diagnostic>> GetSemanticDiagnosticsAsync(SemanticModel model, DiagnosticAnalyzer analyzer, bool isCompilerAnalyzer, CancellationToken cancellationToken)
        {
            // PERF:
            //  1. Compute diagnostics for all analyzers with a single invocation into CompilationWithAnalyzers.
            //     This is critical for better analyzer execution performance through re-use of bound node cache.
            //  2. Ensure that the compiler analyzer is treated specially and does not block on diagnostic computation
            //     for rest of the analyzers. This is needed to ensure faster refresh for compiler diagnostics while typing.

            RoslynDebug.Assert(_compilationWithAnalyzers != null);

            var span = AnalysisScope.Span;
            if (isCompilerAnalyzer)
            {
#if DEBUG
                VerifySpanBasedCompilerDiagnostics(model);
#endif

                var adjustedSpan = await GetAdjustedSpanForCompilerAnalyzerAsync().ConfigureAwait(false);

                // TODO: Move this invocation to OOP
                return await _compilationWithAnalyzers.GetAnalyzerSemanticDiagnosticsAsync(model, adjustedSpan, ImmutableArray.Create(analyzer), cancellationToken).ConfigureAwait(false);
            }

            // We specially handle IInlineSourceSuppressionsAnalyzer by passing in the 'CompilationWithAnalyzers'
            // context to compute unnecessary inline source suppression diagnostics.
            // This is required because this analyzer relies on reported compiler + analyzer diagnostics
            // for unnecessary inline source suppression analysis.
            if (analyzer is IPragmaSuppressionsAnalyzer suppressionsAnalyzer &&
                !AnalysisScope.Span.HasValue)
            {
                using var _ = ArrayBuilder<Diagnostic>.GetInstance(out var builder);
                await suppressionsAnalyzer.AnalyzeAsync(model, span, _compilationWithAnalyzers,
                    _analyzerInfoCache.GetDiagnosticDescriptors, IsCompilationEndAnalyzer, builder.Add, cancellationToken).ConfigureAwait(false);
                return builder.ToImmutable();
            }

            if (_lazySemanticDiagnostics == null)
            {
                // TODO: Move this invocation to OOP
                var analysisResult = await _compilationWithAnalyzers.GetAnalysisResultAsync(model, span, _compilationBasedAnalyzersInAnalysisScope, cancellationToken).ConfigureAwait(false);
                var treeDiagnostics = analysisResult.SemanticDiagnostics.TryGetValue(model.SyntaxTree, out var value) ? value : ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>.Empty;
                Interlocked.CompareExchange(ref _lazySemanticDiagnostics, treeDiagnostics, null);
            }

            return _lazySemanticDiagnostics.TryGetValue(analyzer, out var diagnostics) ?
                diagnostics :
                ImmutableArray<Diagnostic>.Empty;

            bool IsCompilationEndAnalyzer(DiagnosticAnalyzer analyzer)
            {
                RoslynDebug.AssertNotNull(_compilationWithAnalyzers);
                return _analyzerInfoCache.IsCompilationEndAnalyzer(analyzer, AnalysisScope.Document.Project, _compilationWithAnalyzers.Compilation) ?? true;
            }

            async Task<TextSpan?> GetAdjustedSpanForCompilerAnalyzerAsync()
            {
                // This method is to workaround a bug (https://github.com/dotnet/roslyn/issues/1557)
                // once that bug is fixed, we should be able to use given span as it is.

                Debug.Assert(isCompilerAnalyzer);

                if (!span.HasValue)
                {
                    return null;
                }

                var service = AnalysisScope.Document.GetRequiredLanguageService<ISyntaxFactsService>();
                var root = await model.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
                var startNode = service.GetContainingMemberDeclaration(root, span.Value.Start);
                var endNode = service.GetContainingMemberDeclaration(root, span.Value.End);

                if (startNode == endNode)
                {
                    // use full member span
                    if (service.IsMethodLevelMember(startNode))
                    {
                        return startNode.FullSpan;
                    }

                    // use span as it is
                    return span;
                }

                var startSpan = service.IsMethodLevelMember(startNode) ? startNode.FullSpan : span.Value;
                var endSpan = service.IsMethodLevelMember(endNode) ? endNode.FullSpan : span.Value;

                return TextSpan.FromBounds(Math.Min(startSpan.Start, endSpan.Start), Math.Max(startSpan.End, endSpan.End));
            }

#if DEBUG
            void VerifySpanBasedCompilerDiagnostics(SemanticModel model)
            {
                if (!span.HasValue)
                {
                    return;
                }

                // make sure what we got from range is same as what we got from whole diagnostics
                var rangeDeclaractionDiagnostics = model.GetDeclarationDiagnostics(span.Value).ToArray();
                var rangeMethodBodyDiagnostics = model.GetMethodBodyDiagnostics(span.Value).ToArray();
                var rangeDiagnostics = rangeDeclaractionDiagnostics.Concat(rangeMethodBodyDiagnostics).Where(shouldInclude).ToArray();

                var wholeDeclarationDiagnostics = model.GetDeclarationDiagnostics().ToArray();
                var wholeMethodBodyDiagnostics = model.GetMethodBodyDiagnostics().ToArray();
                var wholeDiagnostics = wholeDeclarationDiagnostics.Concat(wholeMethodBodyDiagnostics).Where(shouldInclude).ToArray();

                if (!AnalyzerHelper.AreEquivalent(rangeDiagnostics, wholeDiagnostics))
                {
                    // otherwise, report non-fatal watson so that we can fix those cases
                    FatalError.ReportWithoutCrash(new Exception("Bug in GetDiagnostics"));

                    // make sure we hold onto these for debugging.
                    GC.KeepAlive(rangeDeclaractionDiagnostics);
                    GC.KeepAlive(rangeMethodBodyDiagnostics);
                    GC.KeepAlive(rangeDiagnostics);
                    GC.KeepAlive(wholeDeclarationDiagnostics);
                    GC.KeepAlive(wholeMethodBodyDiagnostics);
                    GC.KeepAlive(wholeDiagnostics);
                }

                return;

                static bool IsUnusedImportDiagnostic(Diagnostic d)
                {
                    switch (d.Id)
                    {
                        case "CS8019":
                        case "BC50000":
                        case "BC50001":
                            return true;
                        default:
                            return false;
                    }
                }

                // Exclude unused import diagnostics since they are never reported when a span is passed.
                // (See CSharp/VisualBasicCompilation.GetDiagnosticsForMethodBodiesInTree.)
                bool shouldInclude(Diagnostic d) => span.Value.IntersectsWith(d.Location.SourceSpan) && !IsUnusedImportDiagnostic(d);
            }
#endif
        }
    }
}
