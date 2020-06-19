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
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Executes analyzers on a document for computing local syntax/semantic diagnostics.
    /// </summary>
    internal sealed class DocumentAnalysisExecutor
    {
        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>? _lazySyntaxDiagnostics;
        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<Diagnostic>>? _lazySemanticDiagnostics;

        public DocumentAnalysisExecutor(
            Document document,
            TextSpan? span,
            AnalysisKind kind,
            CompilationWithAnalyzers? compilationWithAnalyzers,
            DiagnosticAnalyzerInfoCache analyzerInfoCache)
        {
            Debug.Assert(kind == AnalysisKind.Syntax || kind == AnalysisKind.Semantic);

            Document = document;
            Span = span;
            Kind = kind;
            CompilationWithAnalyzers = compilationWithAnalyzers;
            AnalyzerInfoCache = analyzerInfoCache;
        }

        public Document Document { get; }
        public TextSpan? Span { get; }
        public AnalysisKind Kind { get; }
        public CompilationWithAnalyzers? CompilationWithAnalyzers { get; }
        public DiagnosticAnalyzerInfoCache AnalyzerInfoCache { get; }

        /// <summary>
        /// Return all local diagnostics (syntax, semantic) that belong to given document for the given StateSet (analyzer) by calculating them
        /// </summary>
        public async Task<IEnumerable<DiagnosticData>> ComputeDiagnosticsAsync(DiagnosticAnalyzer analyzer, CancellationToken cancellationToken)
        {
            var loadDiagnostic = await Document.State.GetLoadDiagnosticAsync(cancellationToken).ConfigureAwait(false);

            if (analyzer == FileContentLoadAnalyzer.Instance)
            {
                return loadDiagnostic != null ?
                    SpecializedCollections.SingletonEnumerable(DiagnosticData.Create(loadDiagnostic, Document)) :
                    SpecializedCollections.EmptyEnumerable<DiagnosticData>();
            }

            if (loadDiagnostic != null)
            {
                return SpecializedCollections.EmptyEnumerable<DiagnosticData>();
            }

            if (analyzer is DocumentDiagnosticAnalyzer documentAnalyzer)
            {
                var diagnostics = await AnalyzerHelper.ComputeDocumentDiagnosticAnalyzerDiagnosticsAsync(
                    documentAnalyzer, Document, Kind, CompilationWithAnalyzers?.Compilation, cancellationToken).ConfigureAwait(false);

                return diagnostics.ConvertToLocalDiagnostics(Document, Span);
            }

            // quick optimization to reduce allocations.
            if (CompilationWithAnalyzers == null || !analyzer.SupportAnalysisKind(Kind))
            {
                if (Kind == AnalysisKind.Syntax)
                {
                    Logger.Log(FunctionId.Diagnostics_SyntaxDiagnostic,
                        (r, d, a, k) => $"Driver: {r != null}, {d.Id}, {d.Project.Id}, {a}, {k}", CompilationWithAnalyzers, Document, analyzer, Kind);
                }

                return SpecializedCollections.EmptyEnumerable<DiagnosticData>();
            }

            // if project is not loaded successfully then, we disable semantic errors for compiler analyzers
            var isCompilerAnalyzer = analyzer.IsCompilerAnalyzer();
            if (Kind != AnalysisKind.Syntax && isCompilerAnalyzer)
            {
                var isEnabled = await Document.Project.HasSuccessfullyLoadedAsync(cancellationToken).ConfigureAwait(false);

                Logger.Log(FunctionId.Diagnostics_SemanticDiagnostic, (a, d, e) => $"{a}, ({d.Id}, {d.Project.Id}), Enabled:{e}", analyzer, Document, isEnabled);

                if (!isEnabled)
                {
                    return SpecializedCollections.EmptyEnumerable<DiagnosticData>();
                }
            }

            var skippedAnalyzerInfo = Document.Project.GetSkippedAnalyzersInfo(AnalyzerInfoCache);
            ImmutableArray<string> filteredIds;

            switch (Kind)
            {
                case AnalysisKind.Syntax:
                    var tree = await Document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                    if (tree == null)
                    {
                        return SpecializedCollections.EmptyEnumerable<DiagnosticData>();
                    }

                    var diagnostics = await GetSyntaxDiagnosticsAsync(tree, analyzer, isCompilerAnalyzer, cancellationToken).ConfigureAwait(false);

                    if (diagnostics.IsDefaultOrEmpty)
                    {
                        Logger.Log(FunctionId.Diagnostics_SyntaxDiagnostic, (d, a, t) => $"{d.Id}, {d.Project.Id}, {a}, {t.Length}", Document, analyzer, tree);
                    }
                    else if (skippedAnalyzerInfo.FilteredDiagnosticIdsForAnalyzers.TryGetValue(analyzer, out filteredIds))
                    {
                        diagnostics = diagnostics.Filter(filteredIds);
                    }

                    Debug.Assert(diagnostics.Length == CompilationWithAnalyzers.GetEffectiveDiagnostics(diagnostics, CompilationWithAnalyzers.Compilation).Count());
                    return diagnostics.ConvertToLocalDiagnostics(Document, Span);

                case AnalysisKind.Semantic:
                    var model = await Document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                    if (model == null)
                    {
                        return SpecializedCollections.EmptyEnumerable<DiagnosticData>();
                    }

                    diagnostics = await GetSemanticDiagnosticsAsync(model, analyzer, isCompilerAnalyzer, cancellationToken).ConfigureAwait(false);

                    if (skippedAnalyzerInfo.FilteredDiagnosticIdsForAnalyzers.TryGetValue(analyzer, out filteredIds))
                    {
                        diagnostics = diagnostics.Filter(filteredIds);
                    }

                    Debug.Assert(diagnostics.Length == CompilationWithAnalyzers.GetEffectiveDiagnostics(diagnostics, CompilationWithAnalyzers.Compilation).Count());
                    return diagnostics.ConvertToLocalDiagnostics(Document, Span);

                default:
                    throw ExceptionUtilities.UnexpectedValue(Kind);
            }
        }

        private async Task<ImmutableArray<Diagnostic>> GetSyntaxDiagnosticsAsync(SyntaxTree tree, DiagnosticAnalyzer analyzer, bool isCompilerAnalyzer, CancellationToken cancellationToken)
        {
            // PERF:
            //  1. Compute diagnostics for all analyzers with a single invocation into CompilationWithAnalyzers.
            //     This is critical for better analyzer execution performance.
            //  2. Ensure that the compiler analyzer is treated specially and does not block on diagnostic computation
            //     for rest of the analyzers. This is needed to ensure faster refresh for compiler diagnostics while typing.

            RoslynDebug.Assert(CompilationWithAnalyzers != null);

            if (isCompilerAnalyzer)
            {
                return await CompilationWithAnalyzers.GetAnalyzerSyntaxDiagnosticsAsync(tree, ImmutableArray.Create(analyzer), cancellationToken).ConfigureAwait(false);
            }

            if (_lazySyntaxDiagnostics == null)
            {
                // TODO: Move this invocation to OOP
                var treeDiagnostics = await CompilationWithAnalyzers.GetCategorizedAnalyzerSyntaxDiagnosticsAsync(tree, cancellationToken).ConfigureAwait(false);
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

            RoslynDebug.Assert(CompilationWithAnalyzers != null);

            if (isCompilerAnalyzer)
            {
#if DEBUG
                VerifySpanBasedCompilerDiagnostics(model);
#endif

                var adjustedSpan = await GetAdjustedSpanForCompilerAnalyzerAsync().ConfigureAwait(false);
                return await CompilationWithAnalyzers.GetAnalyzerSemanticDiagnosticsAsync(model, adjustedSpan, ImmutableArray.Create(analyzer), cancellationToken).ConfigureAwait(false);
            }

            if (_lazySemanticDiagnostics == null)
            {
                // TODO: Move this invocation to OOP
                var treeDiagnostics = await CompilationWithAnalyzers.GetCategorizedAnalyzerSemanticDiagnosticsAsync(model, Span, cancellationToken).ConfigureAwait(false);
                Interlocked.CompareExchange(ref _lazySemanticDiagnostics, treeDiagnostics, null);
            }

            return _lazySemanticDiagnostics.TryGetValue(analyzer, out var diagnostics) ?
                diagnostics :
                ImmutableArray<Diagnostic>.Empty;

            async Task<TextSpan?> GetAdjustedSpanForCompilerAnalyzerAsync()
            {
                // This method is to workaround a bug (https://github.com/dotnet/roslyn/issues/1557)
                // once that bug is fixed, we should be able to use given span as it is.

                Debug.Assert(isCompilerAnalyzer);

                if (!Span.HasValue)
                {
                    return null;
                }

                var service = Document.GetRequiredLanguageService<ISyntaxFactsService>();
                var root = await model.SyntaxTree.GetRootAsync(cancellationToken).ConfigureAwait(false);
                var startNode = service.GetContainingMemberDeclaration(root, Span.Value.Start);
                var endNode = service.GetContainingMemberDeclaration(root, Span.Value.End);

                if (startNode == endNode)
                {
                    // use full member span
                    if (service.IsMethodLevelMember(startNode))
                    {
                        return startNode.FullSpan;
                    }

                    // use span as it is
                    return Span;
                }

                var startSpan = service.IsMethodLevelMember(startNode) ? startNode.FullSpan : Span.Value;
                var endSpan = service.IsMethodLevelMember(endNode) ? endNode.FullSpan : Span.Value;

                return TextSpan.FromBounds(Math.Min(startSpan.Start, endSpan.Start), Math.Max(startSpan.End, endSpan.End));
            }

#if DEBUG
            void VerifySpanBasedCompilerDiagnostics(SemanticModel model)
            {
                if (!Span.HasValue)
                {
                    return;
                }

                // make sure what we got from range is same as what we got from whole diagnostics
                var rangeDeclaractionDiagnostics = model.GetDeclarationDiagnostics(Span.Value).ToArray();
                var rangeMethodBodyDiagnostics = model.GetMethodBodyDiagnostics(Span.Value).ToArray();
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
                bool shouldInclude(Diagnostic d) => Span.Value.IntersectsWith(d.Location.SourceSpan) && !IsUnusedImportDiagnostic(d);
            }
#endif
        }
    }
}
