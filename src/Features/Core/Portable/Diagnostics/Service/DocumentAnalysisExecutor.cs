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
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Telemetry;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal sealed partial class DiagnosticAnalyzerService
{
    /// <summary>
    /// Executes analyzers on a document for computing local syntax/semantic/additional file diagnostics for a specific <see cref="DocumentAnalysisScope"/>.
    /// </summary>
    private sealed partial class DocumentAnalysisExecutor
    {
        private readonly DiagnosticAnalyzerService _diagnosticAnalyzerService;
        private readonly CompilationWithAnalyzers? _compilationWithAnalyzers;
        private readonly bool _logPerformanceInfo;
        private readonly Action? _onAnalysisException;

        private readonly ImmutableArray<DiagnosticAnalyzer> _compilationBasedAnalyzersInAnalysisScope;

        private ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult>? _lazySyntaxDiagnostics;
        private ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult>? _lazySemanticDiagnostics;

        public DocumentAnalysisExecutor(
            DiagnosticAnalyzerService diagnosticAnalyzerService,
            DocumentAnalysisScope analysisScope,
            CompilationWithAnalyzers? compilationWithAnalyzers,
            bool logPerformanceInfo,
            Action? onAnalysisException = null)
        {
            AnalysisScope = analysisScope;
            _diagnosticAnalyzerService = diagnosticAnalyzerService;
            _compilationWithAnalyzers = compilationWithAnalyzers;
            _logPerformanceInfo = logPerformanceInfo;
            _onAnalysisException = onAnalysisException;

            var compilationBasedProjectAnalyzers = compilationWithAnalyzers?.Analyzers.ToImmutableHashSet();
            _compilationBasedAnalyzersInAnalysisScope = compilationBasedProjectAnalyzers != null
                ? analysisScope.Analyzers.WhereAsArray(compilationBasedProjectAnalyzers.Contains)
                : [];
        }

        public DocumentAnalysisScope AnalysisScope { get; }

        public DocumentAnalysisExecutor With(DocumentAnalysisScope analysisScope)
            => new(_diagnosticAnalyzerService, analysisScope, _compilationWithAnalyzers, _logPerformanceInfo, _onAnalysisException);

        /// <summary>
        /// Return all local diagnostics (syntax, semantic) that belong to given document for the given analyzer by calculating them.
        /// </summary>
        public async ValueTask<ImmutableArray<DiagnosticData>> ComputeDiagnosticsInProcessAsync(
            DiagnosticAnalyzer analyzer, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(AnalysisScope.Analyzers.Contains(analyzer));

            var textDocument = AnalysisScope.TextDocument;
            var span = AnalysisScope.Span;
            var kind = AnalysisScope.Kind;

            var document = textDocument as Document;

            if (analyzer is DocumentDiagnosticAnalyzer documentAnalyzer)
            {
                // DocumentDiagnosticAnalyzer is a host-only analyzer
                var tree = document is null
                    ? null
                    : await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                var documentDiagnostics = await ComputeDocumentDiagnosticAnalyzerDiagnosticsAsync(
                    documentAnalyzer, textDocument, kind, _compilationWithAnalyzers?.Compilation, tree, cancellationToken).ConfigureAwait(false);

                return Extensions.ConvertToLocalDiagnostics(documentDiagnostics, textDocument, span);
            }

            // quick optimization to reduce allocations.
            if (_compilationWithAnalyzers == null || !analyzer.SupportAnalysisKind(kind))
            {
                if (kind == AnalysisKind.Syntax)
                {
                    Logger.Log(FunctionId.Diagnostics_SyntaxDiagnostic,
                        static (r, d, a, k) => $"Driver: {r != null}, {d.Id}, {d.Project.Id}, {a}, {k}", _compilationWithAnalyzers, textDocument, analyzer, kind);
                }

                return [];
            }

            // if project is not loaded successfully then, we disable semantic errors for compiler analyzers
            var isCompilerAnalyzer = analyzer.IsCompilerAnalyzer();
            if (kind != AnalysisKind.Syntax && isCompilerAnalyzer)
            {
                var isEnabled = await textDocument.Project.HasSuccessfullyLoadedAsync(cancellationToken).ConfigureAwait(false);

                Logger.Log(FunctionId.Diagnostics_SemanticDiagnostic, static (a, d, e) => $"{a}, ({d.Id}, {d.Project.Id}), Enabled:{e}", analyzer, textDocument, isEnabled);

                if (!isEnabled)
                    return [];
            }

            // We currently support document analysis only for source documents and additional documents.
            if (document == null && textDocument is not AdditionalDocument)
                return [];

            var diagnostics = kind switch
            {
                AnalysisKind.Syntax => await GetSyntaxDiagnosticsInProcessAsync().ConfigureAwait(false),
                AnalysisKind.Semantic => await GetSemanticDiagnosticsInProcessAsync().ConfigureAwait(false),
                _ => throw ExceptionUtilities.UnexpectedValue(kind),
            };

            if (diagnostics.IsEmpty)
                return [];

            // Remap diagnostic locations, if required.
            diagnostics = await RemapDiagnosticLocationsIfRequiredAsync(diagnostics).ConfigureAwait(false);

            if (span.HasValue)
            {
                var sourceText = await textDocument.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

                // TODO: Unclear if using the unmapped span here is correct.  It does feel somewhat appropriate as the
                // caller should be asking about diagnostics in an actual document, and not where they were remapped to.
                diagnostics = diagnostics.WhereAsArray(
                    static (d, args) =>
                    {
                        var (span, sourceText) = args;
                        return d.DocumentId is null || span.IntersectsWith(d.DataLocation.UnmappedFileSpan.GetClampedTextSpan(sourceText));
                    },
                    (span.Value, sourceText));
            }

#if DEBUG
            var diags = await diagnostics.ToDiagnosticsAsync(textDocument.Project, cancellationToken).ConfigureAwait(false);
            var compilation = _compilationWithAnalyzers.Compilation;
            RoslynDebug.AssertNotNull(compilation);
            Debug.Assert(diags.Length == CompilationWithAnalyzers.GetEffectiveDiagnostics(diags, compilation).Count());
            Debug.Assert(diagnostics.Length == Extensions.ConvertToLocalDiagnostics(diags, textDocument, span).Length);
#endif

            return diagnostics;

            async ValueTask<ImmutableDictionary<DiagnosticAnalyzer, DiagnosticAnalysisResult>> GetAnalysisResultInProcessAsync(
                DocumentAnalysisScope analysisScope)
            {
                RoslynDebug.Assert(_compilationWithAnalyzers != null);

                try
                {
                    var resultAndTelemetry = await _diagnosticAnalyzerService.AnalyzeInProcessAsync(
                        analysisScope, analysisScope.TextDocument.Project, _compilationWithAnalyzers, _logPerformanceInfo, getTelemetryInfo: false, cancellationToken).ConfigureAwait(false);
                    return resultAndTelemetry.AnalysisResult;
                }
                catch when (_onAnalysisException != null)
                {
                    _onAnalysisException.Invoke();
                    throw;
                }
            }

            async ValueTask<ImmutableArray<DiagnosticData>> GetCompilerAnalyzerDiagnosticsInProcessAsync(TextSpan? span)
            {
                Contract.ThrowIfFalse(analyzer.IsCompilerAnalyzer());
                Contract.ThrowIfNull(_compilationWithAnalyzers);
                Contract.ThrowIfFalse(_compilationBasedAnalyzersInAnalysisScope.Contains(analyzer));
                Contract.ThrowIfFalse(AnalysisScope.TextDocument is Document);

                var analysisScope = AnalysisScope.WithAnalyzers([analyzer]).WithSpan(span);
                var analysisResult = await GetAnalysisResultInProcessAsync(analysisScope).ConfigureAwait(false);

                return analysisResult.TryGetValue(analyzer, out var result)
                    ? result.GetDocumentDiagnostics(analysisScope.TextDocument.Id, analysisScope.Kind)
                    : [];
            }

            async ValueTask<ImmutableArray<DiagnosticData>> GetSyntaxDiagnosticsInProcessAsync()
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
                        return [];

                    return await GetCompilerAnalyzerDiagnosticsInProcessAsync(AnalysisScope.Span).ConfigureAwait(false);
                }

                if (_lazySyntaxDiagnostics == null)
                {
                    using var _ = TelemetryLogging.LogBlockTimeAggregatedHistogram(FunctionId.RequestDiagnostics_Summary, $"{nameof(GetSyntaxDiagnosticsInProcessAsync)}.{nameof(GetAnalysisResultInProcessAsync)}");

                    var analysisScope = AnalysisScope.WithAnalyzers(_compilationBasedAnalyzersInAnalysisScope);
                    var syntaxDiagnostics = await GetAnalysisResultInProcessAsync(analysisScope).ConfigureAwait(false);
                    Interlocked.CompareExchange(ref _lazySyntaxDiagnostics, syntaxDiagnostics, null);
                }

                return _lazySyntaxDiagnostics.TryGetValue(analyzer, out var diagnosticAnalysisResult)
                    ? diagnosticAnalysisResult.GetDocumentDiagnostics(AnalysisScope.TextDocument.Id, AnalysisScope.Kind)
                    : [];
            }

            async ValueTask<ImmutableArray<DiagnosticData>> GetSemanticDiagnosticsInProcessAsync()
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
                    await VerifySpanBasedCompilerDiagnosticsAsync(document).ConfigureAwait(false);
#endif

                    var adjustedSpan = await GetAdjustedSpanForCompilerAnalyzerAsync(document).ConfigureAwait(false);
                    return await GetCompilerAnalyzerDiagnosticsInProcessAsync(adjustedSpan).ConfigureAwait(false);
                }

                if (_lazySemanticDiagnostics == null)
                {
                    using var _ = TelemetryLogging.LogBlockTimeAggregatedHistogram(FunctionId.RequestDiagnostics_Summary, $"{nameof(GetSemanticDiagnosticsInProcessAsync)}.{nameof(GetAnalysisResultInProcessAsync)}");

                    var analysisScope = AnalysisScope.WithAnalyzers(_compilationBasedAnalyzersInAnalysisScope);
                    var semanticDiagnostics = await GetAnalysisResultInProcessAsync(analysisScope).ConfigureAwait(false);
                    Interlocked.CompareExchange(ref _lazySemanticDiagnostics, semanticDiagnostics, null);
                }

                return _lazySemanticDiagnostics.TryGetValue(analyzer, out var diagnosticAnalysisResult)
                    ? diagnosticAnalysisResult.GetDocumentDiagnostics(AnalysisScope.TextDocument.Id, AnalysisScope.Kind)
                    : [];
            }

            async ValueTask<TextSpan?> GetAdjustedSpanForCompilerAnalyzerAsync(Document document)
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
            async ValueTask VerifySpanBasedCompilerDiagnosticsAsync(Document document)
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

            async ValueTask<ImmutableArray<DiagnosticData>> RemapDiagnosticLocationsIfRequiredAsync(
               ImmutableArray<DiagnosticData> diagnostics)
            {
                if (diagnostics.IsEmpty)
                    return diagnostics;

                // Check if IWorkspaceVenusSpanMappingService is present for remapping.
                var diagnosticSpanMappingService = textDocument.Project.Solution.Services.GetService<IWorkspaceVenusSpanMappingService>();
                if (diagnosticSpanMappingService == null)
                    return diagnostics;

                // Round tripping the diagnostics should ensure they get correctly remapped.
                var builder = new FixedSizeArrayBuilder<DiagnosticData>(diagnostics.Length);
                foreach (var diagnosticData in diagnostics)
                {
                    var diagnostic = await diagnosticData.ToDiagnosticAsync(textDocument.Project, cancellationToken).ConfigureAwait(false);
                    builder.Add(DiagnosticData.Create(diagnostic, textDocument));
                }

                return builder.MoveToImmutable();
            }
        }
    }
}
