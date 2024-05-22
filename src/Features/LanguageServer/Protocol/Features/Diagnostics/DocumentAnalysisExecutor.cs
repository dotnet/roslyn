// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Executes analyzers on a document for computing local syntax/semantic/additional file diagnostics for a specific <see cref="DocumentAnalysisScope"/>.
    /// </summary>
    internal sealed partial class DocumentAnalysisExecutor
    {
        private readonly CompilationWithAnalyzers? _compilationWithAnalyzers;
        private readonly InProcOrRemoteHostAnalyzerRunner _diagnosticAnalyzerRunner;
        private readonly bool _isExplicit;
        private readonly bool _logPerformanceInfo;
        private readonly Action? _onAnalysisException;

        private readonly ImmutableArray<DiagnosticAnalyzer> _compilationBasedAnalyzersInAnalysisScope;

        private ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult>? _lazySyntaxDiagnostics;
        private ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult>? _lazySemanticDiagnostics;

        public DocumentAnalysisExecutor(
            DocumentAnalysisScope analysisScope,
            CompilationWithAnalyzers? compilationWithAnalyzers,
            InProcOrRemoteHostAnalyzerRunner diagnosticAnalyzerRunner,
            bool isExplicit,
            bool logPerformanceInfo,
            Action? onAnalysisException = null)
        {
            AnalysisScope = analysisScope;
            _compilationWithAnalyzers = compilationWithAnalyzers;
            _diagnosticAnalyzerRunner = diagnosticAnalyzerRunner;
            _isExplicit = isExplicit;
            _logPerformanceInfo = logPerformanceInfo;
            _onAnalysisException = onAnalysisException;

            var compilationBasedAnalyzers = compilationWithAnalyzers?.Analyzers.ToImmutableHashSet();
            _compilationBasedAnalyzersInAnalysisScope = compilationBasedAnalyzers != null
                ? analysisScope.Analyzers.WhereAsArray(compilationBasedAnalyzers.Contains)
                : [];
        }

        public DocumentAnalysisScope AnalysisScope { get; }

        public DocumentAnalysisExecutor With(DocumentAnalysisScope analysisScope)
            => new(analysisScope, _compilationWithAnalyzers, _diagnosticAnalyzerRunner, _isExplicit, _logPerformanceInfo, _onAnalysisException);

        /// <summary>
        /// Return all local diagnostics (syntax, semantic) that belong to given document for the given analyzer by calculating them.
        /// </summary>
        public async Task<IEnumerable<DiagnosticData>> ComputeDiagnosticsAsync(DiagnosticAnalyzer analyzer, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(AnalysisScope.Analyzers.Contains(analyzer));

            var textDocument = AnalysisScope.TextDocument;
            var span = AnalysisScope.Span;
            var kind = AnalysisScope.Kind;

            var document = textDocument as Document;
            RoslynDebug.Assert(document != null || kind == AnalysisKind.Syntax, "We only support syntactic analysis for non-source documents");

            var loadDiagnostic = await textDocument.State.GetLoadDiagnosticAsync(cancellationToken).ConfigureAwait(false);

            if (analyzer == FileContentLoadAnalyzer.Instance)
            {
                return loadDiagnostic != null
                    ? SpecializedCollections.SingletonEnumerable(DiagnosticData.Create(loadDiagnostic, textDocument))
                    : SpecializedCollections.EmptyEnumerable<DiagnosticData>();
            }

            if (loadDiagnostic != null)
            {
                return SpecializedCollections.EmptyEnumerable<DiagnosticData>();
            }

            if (analyzer == GeneratorDiagnosticsPlaceholderAnalyzer.Instance)
            {
                // We will count generator diagnostics as semantic diagnostics; some filtering to either syntax/semantic is necessary or else we'll report diagnostics twice.
                if (kind == AnalysisKind.Semantic)
                {
                    var generatorDiagnostics = await GetSourceGeneratorDiagnosticsAsync(textDocument.Project, cancellationToken).ConfigureAwait(false);
                    return ConvertToLocalDiagnostics(generatorDiagnostics, textDocument, span);
                }
                else
                {
                    return SpecializedCollections.EmptyEnumerable<DiagnosticData>();
                }
            }

            if (analyzer is DocumentDiagnosticAnalyzer documentAnalyzer)
            {
                if (document == null)
                {
                    return SpecializedCollections.EmptyEnumerable<DiagnosticData>();
                }

                var documentDiagnostics = await ComputeDocumentDiagnosticAnalyzerDiagnosticsAsync(
                    documentAnalyzer, document, kind, _compilationWithAnalyzers?.Compilation, cancellationToken).ConfigureAwait(false);

                return ConvertToLocalDiagnostics(documentDiagnostics, document, span);
            }

            // quick optimization to reduce allocations.
            if (_compilationWithAnalyzers == null || !analyzer.SupportAnalysisKind(kind))
            {
                if (kind == AnalysisKind.Syntax)
                {
                    Logger.Log(FunctionId.Diagnostics_SyntaxDiagnostic,
                        (r, d, a, k) => $"Driver: {r != null}, {d.Id}, {d.Project.Id}, {a}, {k}", _compilationWithAnalyzers, textDocument, analyzer, kind);
                }

                return SpecializedCollections.EmptyEnumerable<DiagnosticData>();
            }

            // if project is not loaded successfully then, we disable semantic errors for compiler analyzers
            var isCompilerAnalyzer = analyzer.IsCompilerAnalyzer();
            if (kind != AnalysisKind.Syntax && isCompilerAnalyzer)
            {
                var isEnabled = await textDocument.Project.HasSuccessfullyLoadedAsync(cancellationToken).ConfigureAwait(false);

                Logger.Log(FunctionId.Diagnostics_SemanticDiagnostic, (a, d, e) => $"{a}, ({d.Id}, {d.Project.Id}), Enabled:{e}", analyzer, textDocument, isEnabled);

                if (!isEnabled)
                {
                    return SpecializedCollections.EmptyEnumerable<DiagnosticData>();
                }
            }

            if (document == null && textDocument is not AdditionalDocument)
            {
                // We currently support document analysis only for source documents and additional documents.
                return SpecializedCollections.EmptyEnumerable<DiagnosticData>();
            }

            var diagnostics = kind switch
            {
                AnalysisKind.Syntax => await GetSyntaxDiagnosticsAsync(analyzer, isCompilerAnalyzer, cancellationToken).ConfigureAwait(false),
                AnalysisKind.Semantic => await GetSemanticDiagnosticsAsync(analyzer, isCompilerAnalyzer, cancellationToken).ConfigureAwait(false),
                _ => throw ExceptionUtilities.UnexpectedValue(kind),
            };

            // Remap diagnostic locations, if required.
            diagnostics = await RemapDiagnosticLocationsIfRequiredAsync(textDocument, diagnostics, cancellationToken).ConfigureAwait(false);

            if (span.HasValue && document != null)
            {
                var sourceText = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

                // TODO: Unclear if using the unmapped span here is correct.  It does feel somewhat appropriate as the
                // caller should be asking about diagnostics in an actual document, and not where they were remapped to.
                diagnostics = diagnostics.WhereAsArray(
                    d => d.DocumentId is null || span.Value.IntersectsWith(d.DataLocation.UnmappedFileSpan.GetClampedTextSpan(sourceText)));
            }

#if DEBUG
            var diags = await diagnostics.ToDiagnosticsAsync(textDocument.Project, cancellationToken).ConfigureAwait(false);
            Debug.Assert(diags.Length == CompilationWithAnalyzers.GetEffectiveDiagnostics(diags, _compilationWithAnalyzers.Compilation).Count());
            Debug.Assert(diagnostics.Length == ConvertToLocalDiagnostics(diags, textDocument, span).Count());
#endif

            return diagnostics;
        }

        private async Task<ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult>> GetAnalysisResultAsync(DocumentAnalysisScope analysisScope, CancellationToken cancellationToken)
        {
            RoslynDebug.Assert(_compilationWithAnalyzers != null);

            try
            {
                var resultAndTelemetry = await _diagnosticAnalyzerRunner.AnalyzeDocumentAsync(analysisScope, _compilationWithAnalyzers,
                    _isExplicit, _logPerformanceInfo, getTelemetryInfo: false, cancellationToken).ConfigureAwait(false);
                return resultAndTelemetry.AnalysisResult;
            }
            catch when (_onAnalysisException != null)
            {
                _onAnalysisException.Invoke();
                throw;
            }
        }

        private async Task<ImmutableArray<Diagnostic>> GetSourceGeneratorDiagnosticsAsync(Project project, CancellationToken cancellationToken)
        {
            try
            {
                return await _diagnosticAnalyzerRunner.GetSourceGeneratorDiagnosticsAsync(project, cancellationToken).ConfigureAwait(false);
            }
            catch when (_onAnalysisException != null)
            {
                _onAnalysisException.Invoke();
                throw;
            }
        }

        private async Task<ImmutableArray<DiagnosticData>> GetCompilerAnalyzerDiagnosticsAsync(DiagnosticAnalyzer analyzer, TextSpan? span, CancellationToken cancellationToken)
        {
            RoslynDebug.Assert(analyzer.IsCompilerAnalyzer());
            RoslynDebug.Assert(_compilationWithAnalyzers != null);
            RoslynDebug.Assert(_compilationBasedAnalyzersInAnalysisScope.Contains(analyzer));
            RoslynDebug.Assert(AnalysisScope.TextDocument is Document);

            var analysisScope = AnalysisScope.WithAnalyzers([analyzer]).WithSpan(span);
            var analysisResult = await GetAnalysisResultAsync(analysisScope, cancellationToken).ConfigureAwait(false);
            if (!analysisResult.TryGetValue(analyzer, out var result))
            {
                return [];
            }

            return result.GetDocumentDiagnostics(analysisScope.TextDocument.Id, analysisScope.Kind);
        }

        private async Task<ImmutableArray<DiagnosticData>> GetSyntaxDiagnosticsAsync(DiagnosticAnalyzer analyzer, bool isCompilerAnalyzer, CancellationToken cancellationToken)
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
                if (AnalysisScope.TextDocument is not Document)
                {
                    return [];
                }

                return await GetCompilerAnalyzerDiagnosticsAsync(analyzer, AnalysisScope.Span, cancellationToken).ConfigureAwait(false);
            }

            if (_lazySyntaxDiagnostics == null)
            {
                using var _ = TelemetryLogging.LogBlockTimeAggregated(FunctionId.RequestDiagnostics_Summary, $"{nameof(GetSyntaxDiagnosticsAsync)}.{nameof(GetAnalysisResultAsync)}");

                var analysisScope = AnalysisScope.WithAnalyzers(_compilationBasedAnalyzersInAnalysisScope);
                var syntaxDiagnostics = await GetAnalysisResultAsync(analysisScope, cancellationToken).ConfigureAwait(false);
                Interlocked.CompareExchange(ref _lazySyntaxDiagnostics, syntaxDiagnostics, null);
            }

            return _lazySyntaxDiagnostics.TryGetValue(analyzer, out var diagnosticAnalysisResult)
                ? diagnosticAnalysisResult.GetDocumentDiagnostics(AnalysisScope.TextDocument.Id, AnalysisScope.Kind)
                : [];
        }

        private async Task<ImmutableArray<DiagnosticData>> GetSemanticDiagnosticsAsync(DiagnosticAnalyzer analyzer, bool isCompilerAnalyzer, CancellationToken cancellationToken)
        {
            // PERF:
            //  1. Compute diagnostics for all analyzers with a single invocation into CompilationWithAnalyzers.
            //     This is critical for better analyzer execution performance through re-use of bound node cache.
            //  2. Ensure that the compiler analyzer is treated specially and does not block on diagnostic computation
            //     for rest of the analyzers. This is needed to ensure faster refresh for compiler diagnostics while typing.

            RoslynDebug.Assert(_compilationWithAnalyzers != null);

            var span = AnalysisScope.Span;
            var document = (Document)AnalysisScope.TextDocument;
            if (isCompilerAnalyzer)
            {
#if DEBUG
                await VerifySpanBasedCompilerDiagnosticsAsync().ConfigureAwait(false);
#endif

                var adjustedSpan = await GetAdjustedSpanForCompilerAnalyzerAsync().ConfigureAwait(false);
                return await GetCompilerAnalyzerDiagnosticsAsync(analyzer, adjustedSpan, cancellationToken).ConfigureAwait(false);
            }

            if (_lazySemanticDiagnostics == null)
            {
                using var _ = TelemetryLogging.LogBlockTimeAggregated(FunctionId.RequestDiagnostics_Summary, $"{nameof(GetSemanticDiagnosticsAsync)}.{nameof(GetAnalysisResultAsync)}");

                var analysisScope = AnalysisScope.WithAnalyzers(_compilationBasedAnalyzersInAnalysisScope);
                var semanticDiagnostics = await GetAnalysisResultAsync(analysisScope, cancellationToken).ConfigureAwait(false);
                Interlocked.CompareExchange(ref _lazySemanticDiagnostics, semanticDiagnostics, null);
            }

            return _lazySemanticDiagnostics.TryGetValue(analyzer, out var diagnosticAnalysisResult)
                ? diagnosticAnalysisResult.GetDocumentDiagnostics(AnalysisScope.TextDocument.Id, AnalysisScope.Kind)
                : [];

            async Task<TextSpan?> GetAdjustedSpanForCompilerAnalyzerAsync()
            {
                // This method is to workaround a bug (https://github.com/dotnet/roslyn/issues/1557)
                // once that bug is fixed, we should be able to use given span as it is.

                Debug.Assert(isCompilerAnalyzer);

                if (!span.HasValue)
                {
                    return null;
                }

                var service = document.GetRequiredLanguageService<ISyntaxFactsService>();
                var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
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
            async Task VerifySpanBasedCompilerDiagnosticsAsync()
            {
                if (!span.HasValue)
                {
                    return;
                }

                // make sure what we got from range is same as what we got from whole diagnostics
                var model = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var rangeDeclaractionDiagnostics = model.GetDeclarationDiagnostics(span.Value, cancellationToken).ToArray();
                var rangeMethodBodyDiagnostics = model.GetMethodBodyDiagnostics(span.Value, cancellationToken).ToArray();
                var rangeDiagnostics = rangeDeclaractionDiagnostics.Concat(rangeMethodBodyDiagnostics).Where(shouldInclude).ToArray();

                var wholeDeclarationDiagnostics = model.GetDeclarationDiagnostics(cancellationToken: cancellationToken).ToArray();
                var wholeMethodBodyDiagnostics = model.GetMethodBodyDiagnostics(cancellationToken: cancellationToken).ToArray();
                var wholeDiagnostics = wholeDeclarationDiagnostics.Concat(wholeMethodBodyDiagnostics).Where(shouldInclude).ToArray();

                if (!AreEquivalent(rangeDiagnostics, wholeDiagnostics))
                {
                    // otherwise, report non-fatal watson so that we can fix those cases
                    FatalError.ReportAndCatch(new Exception("Bug in GetDiagnostics"));

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

        private static async Task<ImmutableArray<DiagnosticData>> RemapDiagnosticLocationsIfRequiredAsync(
            TextDocument textDocument,
            ImmutableArray<DiagnosticData> diagnostics,
            CancellationToken cancellationToken)
        {
            if (diagnostics.IsEmpty)
            {
                return diagnostics;
            }

            // Check if IWorkspaceVenusSpanMappingService is present for remapping.
            var diagnosticSpanMappingService = textDocument.Project.Solution.Services.GetService<IWorkspaceVenusSpanMappingService>();
            if (diagnosticSpanMappingService == null)
            {
                return diagnostics;
            }

            // Round tripping the diagnostics should ensure they get correctly remapped.
            using var _ = ArrayBuilder<DiagnosticData>.GetInstance(diagnostics.Length, out var builder);
            foreach (var diagnosticData in diagnostics)
            {
                var diagnostic = await diagnosticData.ToDiagnosticAsync(textDocument.Project, cancellationToken).ConfigureAwait(false);
                builder.Add(DiagnosticData.Create(diagnostic, textDocument));
            }

            return builder.ToImmutableAndClear();
        }
    }
}
