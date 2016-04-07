// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics.Log;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV1
{
    internal class DiagnosticAnalyzerDriver
    {
        private readonly Project _project;
        private readonly Document _document;

        // The root of the document.  May be null for documents without a root.
        private readonly SyntaxNode _root;

        // The span of the documents we want diagnostics for.  If null, then we want diagnostics 
        // for the entire file.
        private readonly TextSpan? _span;

        private readonly DiagnosticIncrementalAnalyzer _owner;
        private readonly IEnumerable<DiagnosticAnalyzer> _analyzers;

        private readonly bool _concurrentAnalysis;
        private readonly bool _reportSuppressedDiagnostics;
        private readonly CancellationToken _cancellationToken;

        private CompilationWithAnalyzers _lazyCompilationWithAnalyzers;

        public DiagnosticAnalyzerDriver(
            Document document,
            TextSpan? span,
            SyntaxNode root,
            DiagnosticIncrementalAnalyzer owner,
            IEnumerable<DiagnosticAnalyzer> analyzers,
            bool concurrentAnalysis,
            bool reportSuppressedDiagnostics,
            CancellationToken cancellationToken)
            : this(document, span, root, owner, analyzers, concurrentAnalysis, reportSuppressedDiagnostics,
                   cachedCompilationWithAnalyzersOpt: null, cancellationToken: cancellationToken)
        {
        }

        public DiagnosticAnalyzerDriver(
            Document document,
            TextSpan? span,
            SyntaxNode root,
            DiagnosticIncrementalAnalyzer owner,
            IEnumerable<DiagnosticAnalyzer> analyzers,
            bool concurrentAnalysis,
            bool reportSuppressedDiagnostics,
            CompilationWithAnalyzers cachedCompilationWithAnalyzersOpt,
            CancellationToken cancellationToken)
            : this(document.Project, owner, analyzers, concurrentAnalysis, reportSuppressedDiagnostics,
                   cachedCompilationWithAnalyzersOpt, cancellationToken)
        {
            _document = document;
            _span = span;
            _root = root;
        }

        public DiagnosticAnalyzerDriver(
            Project project,
            DiagnosticIncrementalAnalyzer owner,
            IEnumerable<DiagnosticAnalyzer> analyzers,
            bool concurrentAnalysis,
            bool reportSuppressedDiagnostics,
            CancellationToken cancellationToken)
            : this(project, owner, analyzers, concurrentAnalysis, reportSuppressedDiagnostics,
                   cachedCompilationWithAnalyzersOpt: null, cancellationToken: cancellationToken)
        {
        }

        public DiagnosticAnalyzerDriver(
            Project project,
            DiagnosticIncrementalAnalyzer owner,
            IEnumerable<DiagnosticAnalyzer> analyzers,
            bool concurrentAnalysis,
            bool reportSuppressedDiagnostics,
            CompilationWithAnalyzers cachedCompilationWithAnalyzersOpt,
            CancellationToken cancellationToken)
        {
            _project = project;
            _owner = owner;
            _analyzers = analyzers;

            _concurrentAnalysis = concurrentAnalysis;
            _reportSuppressedDiagnostics = reportSuppressedDiagnostics;
            _cancellationToken = cancellationToken;
            _lazyCompilationWithAnalyzers = cachedCompilationWithAnalyzersOpt;

#if DEBUG
            // this is a bit wierd, but if both analyzers and compilationWithAnalyzers are given,
            // make sure both are same.
            // We also need to handle the fact that compilationWithAnalyzers is created with all non-supprssed analyzers.
            if (_lazyCompilationWithAnalyzers != null)
            {
                var filteredAnalyzers = _analyzers
                    .Where(a => !CompilationWithAnalyzers.IsDiagnosticAnalyzerSuppressed(a, _lazyCompilationWithAnalyzers.Compilation.Options, _lazyCompilationWithAnalyzers.AnalysisOptions.OnAnalyzerException))
                    .Distinct();
                Contract.ThrowIfFalse(_lazyCompilationWithAnalyzers.Analyzers.SetEquals(filteredAnalyzers));
            }
#endif
        }

        public Document Document
        {
            get
            {
                Contract.ThrowIfNull(_document);
                return _document;
            }
        }

        public TextSpan? Span
        {
            get
            {
                Contract.ThrowIfNull(_document);
                return _span;
            }
        }

        public Project Project
        {
            get
            {
                Contract.ThrowIfNull(_project);
                return _project;
            }
        }

        public CancellationToken CancellationToken
        {
            get
            {
                return _cancellationToken;
            }
        }

        private CompilationWithAnalyzers GetCompilationWithAnalyzers(Compilation compilation)
        {
            Contract.ThrowIfFalse(_project.SupportsCompilation);

            if (_lazyCompilationWithAnalyzers == null)
            {
                Interlocked.CompareExchange(
                    ref _lazyCompilationWithAnalyzers,
                    _owner.GetCompilationWithAnalyzers(_project, _analyzers, compilation, _concurrentAnalysis, _reportSuppressedDiagnostics),
                    null);
            }

            return _lazyCompilationWithAnalyzers;
        }

        public async Task<ImmutableArray<Diagnostic>> GetSyntaxDiagnosticsAsync(DiagnosticAnalyzer analyzer)
        {
            try
            {
                var compilation = _document.Project.SupportsCompilation ? await _document.Project.GetCompilationAsync(_cancellationToken).ConfigureAwait(false) : null;

                Contract.ThrowIfNull(_document);

                var documentAnalyzer = analyzer as DocumentDiagnosticAnalyzer;
                if (documentAnalyzer != null)
                {
                    using (var pooledObject = SharedPools.Default<List<Diagnostic>>().GetPooledObject())
                    {
                        var diagnostics = pooledObject.Object;
                        _cancellationToken.ThrowIfCancellationRequested();

                        try
                        {
                            await documentAnalyzer.AnalyzeSyntaxAsync(_document, diagnostics.Add, _cancellationToken).ConfigureAwait(false);
                            return GetFilteredDocumentDiagnostics(diagnostics, compilation).ToImmutableArray();
                        }
                        catch (Exception e) when (!IsCanceled(e, _cancellationToken))
                        {
                            OnAnalyzerException(e, analyzer, compilation);
                            return ImmutableArray<Diagnostic>.Empty;
                        }
                    }
                }

                if (!_document.SupportsSyntaxTree || compilation == null)
                {
                    return ImmutableArray<Diagnostic>.Empty;
                }

                var compilationWithAnalyzers = GetCompilationWithAnalyzers(compilation);
                var syntaxDiagnostics = await compilationWithAnalyzers.GetAnalyzerSyntaxDiagnosticsAsync(_root.SyntaxTree, ImmutableArray.Create(analyzer), _cancellationToken).ConfigureAwait(false);
                await UpdateAnalyzerTelemetryDataAsync(analyzer, compilationWithAnalyzers).ConfigureAwait(false);
                return GetFilteredDocumentDiagnostics(syntaxDiagnostics, compilation, onlyLocationFiltering: true).ToImmutableArray();
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private IEnumerable<Diagnostic> GetFilteredDocumentDiagnostics(IEnumerable<Diagnostic> diagnostics, Compilation compilation, bool onlyLocationFiltering = false)
        {
            if (_root == null)
            {
                return diagnostics;
            }

            return GetFilteredDocumentDiagnosticsCore(diagnostics, compilation, onlyLocationFiltering);
        }

        private IEnumerable<Diagnostic> GetFilteredDocumentDiagnosticsCore(IEnumerable<Diagnostic> diagnostics, Compilation compilation, bool onlyLocationFiltering)
        {
            var diagsFilteredByLocation = diagnostics.Where(diagnostic => (diagnostic.Location == Location.None) ||
                        (diagnostic.Location.SourceTree == _root.SyntaxTree &&
                         (_span == null || diagnostic.Location.SourceSpan.IntersectsWith(_span.Value))));

            return compilation == null || onlyLocationFiltering
                ? diagsFilteredByLocation
                : CompilationWithAnalyzers.GetEffectiveDiagnostics(diagsFilteredByLocation, compilation);
        }

        internal void OnAnalyzerException(Exception ex, DiagnosticAnalyzer analyzer, Compilation compilation)
        {
            var exceptionDiagnostic = AnalyzerHelper.CreateAnalyzerExceptionDiagnostic(analyzer, ex);

            if (compilation != null)
            {
                exceptionDiagnostic = CompilationWithAnalyzers.GetEffectiveDiagnostics(ImmutableArray.Create(exceptionDiagnostic), compilation).SingleOrDefault();
            }

            var onAnalyzerException = _owner.GetOnAnalyzerException(_project.Id);
            onAnalyzerException(ex, analyzer, exceptionDiagnostic);
        }

        public async Task<ImmutableArray<Diagnostic>> GetSemanticDiagnosticsAsync(DiagnosticAnalyzer analyzer)
        {
            try
            {
                var model = await _document.GetSemanticModelAsync(_cancellationToken).ConfigureAwait(false);
                var compilation = model?.Compilation;

                Contract.ThrowIfNull(_document);

                var documentAnalyzer = analyzer as DocumentDiagnosticAnalyzer;
                if (documentAnalyzer != null)
                {
                    using (var pooledObject = SharedPools.Default<List<Diagnostic>>().GetPooledObject())
                    {
                        var diagnostics = pooledObject.Object;
                        _cancellationToken.ThrowIfCancellationRequested();

                        try
                        {
                            await documentAnalyzer.AnalyzeSemanticsAsync(_document, diagnostics.Add, _cancellationToken).ConfigureAwait(false);
                        }
                        catch (Exception e) when (!IsCanceled(e, _cancellationToken))
                        {
                            OnAnalyzerException(e, analyzer, compilation);
                            return ImmutableArray<Diagnostic>.Empty;
                        }

                        return GetFilteredDocumentDiagnostics(diagnostics, compilation).ToImmutableArray();
                    }
                }

                if (!_document.SupportsSyntaxTree || compilation == null)
                {
                    return ImmutableArray<Diagnostic>.Empty;
                }

                var compilationWithAnalyzers = GetCompilationWithAnalyzers(compilation);
                var semanticDiagnostics = await compilationWithAnalyzers.GetAnalyzerSemanticDiagnosticsAsync(model, _span, ImmutableArray.Create(analyzer), _cancellationToken).ConfigureAwait(false);
                await UpdateAnalyzerTelemetryDataAsync(analyzer, compilationWithAnalyzers).ConfigureAwait(false);
                return semanticDiagnostics;
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        public async Task<ImmutableArray<Diagnostic>> GetProjectDiagnosticsAsync(DiagnosticAnalyzer analyzer)
        {
            try
            {
                Contract.ThrowIfNull(_project);
                Contract.ThrowIfFalse(_document == null);

                using (var diagnostics = SharedPools.Default<List<Diagnostic>>().GetPooledObject())
                {
                    if (_project.SupportsCompilation)
                    {
                        await this.GetCompilationDiagnosticsAsync(analyzer, diagnostics.Object).ConfigureAwait(false);
                    }

                    await this.GetProjectDiagnosticsWorkerAsync(analyzer, diagnostics.Object).ConfigureAwait(false);

                    return diagnostics.Object.ToImmutableArray();
                }
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private async Task GetProjectDiagnosticsWorkerAsync(DiagnosticAnalyzer analyzer, List<Diagnostic> diagnostics)
        {
            try
            {
                var projectAnalyzer = analyzer as ProjectDiagnosticAnalyzer;
                if (projectAnalyzer == null)
                {
                    return;
                }

                try
                {
                    await projectAnalyzer.AnalyzeProjectAsync(_project, diagnostics.Add, _cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e) when (!IsCanceled(e, _cancellationToken))
                {
                    var compilation = await _project.GetCompilationAsync(_cancellationToken).ConfigureAwait(false);
                    OnAnalyzerException(e, analyzer, compilation);
                }
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private async Task GetCompilationDiagnosticsAsync(DiagnosticAnalyzer analyzer, List<Diagnostic> diagnostics)
        {
            try
            {
                Contract.ThrowIfFalse(_project.SupportsCompilation);

                var compilation = await _project.GetCompilationAsync(_cancellationToken).ConfigureAwait(false);
                var compilationWithAnalyzers = GetCompilationWithAnalyzers(compilation);
                var compilationDiagnostics = await compilationWithAnalyzers.GetAnalyzerCompilationDiagnosticsAsync(ImmutableArray.Create(analyzer), _cancellationToken).ConfigureAwait(false);
                await UpdateAnalyzerTelemetryDataAsync(analyzer, compilationWithAnalyzers).ConfigureAwait(false);
                diagnostics.AddRange(compilationDiagnostics);
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private async Task UpdateAnalyzerTelemetryDataAsync(DiagnosticAnalyzer analyzer, CompilationWithAnalyzers compilationWithAnalyzers)
        {
            try
            {
                var analyzerTelemetryInfo = await compilationWithAnalyzers.GetAnalyzerTelemetryInfoAsync(analyzer, _cancellationToken).ConfigureAwait(false);
                DiagnosticAnalyzerLogger.UpdateAnalyzerTypeCount(analyzer, analyzerTelemetryInfo, _project, _owner.DiagnosticLogAggregator);
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private static bool IsCanceled(Exception ex, CancellationToken cancellationToken)
        {
            return (ex as OperationCanceledException)?.CancellationToken == cancellationToken;
        }
    }
}
