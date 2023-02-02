// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.Diagnostics.AnalyzerDriver;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    public class CompilationWithAnalyzers
    {
        private readonly Compilation _compilation;
        private readonly CompilationData _compilationData;
        private readonly ImmutableArray<DiagnosticAnalyzer> _analyzers;
        private readonly CompilationWithAnalyzersOptions _analysisOptions;
        private readonly CancellationToken _cancellationToken;
        private readonly AnalyzerManager _analyzerManager;

        /// <summary>
        /// Pool of <see cref="AnalyzerDriver"/>s used for analyzer execution.
        /// </summary>
        private readonly ObjectPool<AnalyzerDriver> _driverPool;

        /// <summary>
        /// Builder for storing current, possibly partial, analysis results:
        /// 1. Diagnostics reported by analyzers.
        /// 2. AnalyzerTelemetryInfo.
        /// </summary>
        private readonly AnalysisResultBuilder _analysisResultBuilder;

        /// <summary>
        /// Set of exception diagnostics reported for exceptions thrown by the analyzers.
        /// </summary>
        private readonly ConcurrentSet<Diagnostic> _exceptionDiagnostics = new ConcurrentSet<Diagnostic>();

        /// <summary>
        /// Pool of event queues to serve each diagnostics request.
        /// </summary>
        private readonly ObjectPool<AsyncQueue<CompilationEvent>> _eventQueuePool = new ObjectPool<AsyncQueue<CompilationEvent>>(() => new AsyncQueue<CompilationEvent>());
        private static readonly AsyncQueue<CompilationEvent> s_EmptyEventQueue = new AsyncQueue<CompilationEvent>();

        /// <summary>
        /// Underlying <see cref="Compilation"/> with a non-null <see cref="Compilation.EventQueue"/>, used to drive analyzer execution.
        /// </summary>
        public Compilation Compilation => _compilation;

        /// <summary>
        /// Analyzers to execute on the compilation.
        /// </summary>
        public ImmutableArray<DiagnosticAnalyzer> Analyzers => _analyzers;

        /// <summary>
        /// Options to configure analyzer execution.
        /// </summary>
        public CompilationWithAnalyzersOptions AnalysisOptions => _analysisOptions;

        /// <summary>
        /// An optional cancellation token which can be used to cancel analysis.
        /// Note: This token is only used if the API invoked to get diagnostics doesn't provide a cancellation token.
        /// </summary>
        public CancellationToken CancellationToken => _cancellationToken;

        /// <summary>
        /// Creates a new compilation by attaching diagnostic analyzers to an existing compilation.
        /// </summary>
        /// <param name="compilation">The original compilation.</param>
        /// <param name="analyzers">The set of analyzers to include in future analyses.</param>
        /// <param name="options">Options that are passed to analyzers.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to abort analysis.</param>
        public CompilationWithAnalyzers(Compilation compilation, ImmutableArray<DiagnosticAnalyzer> analyzers, AnalyzerOptions? options, CancellationToken cancellationToken)
            : this(compilation, analyzers, new CompilationWithAnalyzersOptions(options, onAnalyzerException: null, analyzerExceptionFilter: null, concurrentAnalysis: true, logAnalyzerExecutionTime: true, reportSuppressedDiagnostics: false), cancellationToken)
        {
        }

        /// <summary>
        /// Creates a new compilation by attaching diagnostic analyzers to an existing compilation.
        /// </summary>
        /// <param name="compilation">The original compilation.</param>
        /// <param name="analyzers">The set of analyzers to include in future analyses.</param>
        /// <param name="analysisOptions">Options to configure analyzer execution.</param>
        public CompilationWithAnalyzers(Compilation compilation, ImmutableArray<DiagnosticAnalyzer> analyzers, CompilationWithAnalyzersOptions analysisOptions)
            : this(compilation, analyzers, analysisOptions, cancellationToken: CancellationToken.None)
        {
        }

        private CompilationWithAnalyzers(Compilation compilation, ImmutableArray<DiagnosticAnalyzer> analyzers, CompilationWithAnalyzersOptions analysisOptions, CancellationToken cancellationToken)
        {
            VerifyArguments(compilation, analyzers, analysisOptions);

            compilation = compilation
                .WithOptions(compilation.Options.WithReportSuppressedDiagnostics(analysisOptions.ReportSuppressedDiagnostics))
                .WithSemanticModelProvider(new CachingSemanticModelProvider())
                .WithEventQueue(new AsyncQueue<CompilationEvent>());
            _compilation = compilation;
            _analyzers = analyzers;
            _analysisOptions = analysisOptions;
            _cancellationToken = cancellationToken;

            _compilationData = new CompilationData(_compilation);
            _analysisResultBuilder = new AnalysisResultBuilder(analysisOptions.LogAnalyzerExecutionTime, analyzers, _analysisOptions.Options?.AdditionalFiles ?? ImmutableArray<AdditionalText>.Empty);
            _analyzerManager = new AnalyzerManager(analyzers);
            _driverPool = new ObjectPool<AnalyzerDriver>(() => _compilation.CreateAnalyzerDriver(analyzers, _analyzerManager, severityFilter: SeverityFilter.None));
        }

        #region Helper methods for public API argument validation

        private static void VerifyArguments(Compilation compilation, ImmutableArray<DiagnosticAnalyzer> analyzers, CompilationWithAnalyzersOptions analysisOptions)
        {
            if (compilation == null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }

            if (analysisOptions == null)
            {
                throw new ArgumentNullException(nameof(analysisOptions));
            }

            VerifyAnalyzersArgumentForStaticApis(analyzers);
        }

        private static void VerifyAnalyzersArgumentForStaticApis(ImmutableArray<DiagnosticAnalyzer> analyzers, bool allowDefaultOrEmpty = false)
        {
            if (analyzers.IsDefaultOrEmpty)
            {
                if (allowDefaultOrEmpty)
                {
                    return;
                }

                throw new ArgumentException(CodeAnalysisResources.ArgumentCannotBeEmpty, nameof(analyzers));
            }

            if (analyzers.Any(static a => a == null))
            {
                throw new ArgumentException(CodeAnalysisResources.ArgumentElementCannotBeNull, nameof(analyzers));
            }

            if (analyzers.Distinct().Length != analyzers.Length)
            {
                // Has duplicate analyzer instances.
                throw new ArgumentException(CodeAnalysisResources.DuplicateAnalyzerInstances, nameof(analyzers));
            }
        }

        private void VerifyAnalyzerArgument(DiagnosticAnalyzer analyzer)
        {
            VerifyAnalyzerArgumentForStaticApis(analyzer);

            if (!_analyzers.Contains(analyzer))
            {
                throw new ArgumentException(CodeAnalysisResources.UnsupportedAnalyzerInstance, nameof(analyzer));
            }
        }

        private static void VerifyAnalyzerArgumentForStaticApis(DiagnosticAnalyzer analyzer)
        {
            if (analyzer == null)
            {
                throw new ArgumentException(CodeAnalysisResources.ArgumentCannotBeEmpty, nameof(analyzer));
            }
        }

        private void VerifyExistingAnalyzersArgument(ImmutableArray<DiagnosticAnalyzer> analyzers)
        {
            VerifyAnalyzersArgumentForStaticApis(analyzers);

            if (analyzers.Any(static (a, self) => !self._analyzers.Contains(a), this))
            {
                throw new ArgumentException(CodeAnalysisResources.UnsupportedAnalyzerInstance, nameof(_analyzers));
            }

            if (analyzers.Distinct().Length != analyzers.Length)
            {
                // Has duplicate analyzer instances.
                throw new ArgumentException(CodeAnalysisResources.DuplicateAnalyzerInstances, nameof(analyzers));
            }
        }

        private void VerifyModel(SemanticModel model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            if (!_compilation.ContainsSyntaxTree(model.SyntaxTree))
            {
                throw new ArgumentException(CodeAnalysisResources.InvalidTree, nameof(model));
            }
        }

        private void VerifyTree(SyntaxTree tree)
        {
            if (tree == null)
            {
                throw new ArgumentNullException(nameof(tree));
            }

            if (!_compilation.ContainsSyntaxTree(tree))
            {
                throw new ArgumentException(CodeAnalysisResources.InvalidTree, nameof(tree));
            }
        }

        private void VerifyAdditionalFile(AdditionalText file)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            if (!AdditionalFiles.Contains(file))
            {
                throw new ArgumentException(CodeAnalysisResources.InvalidAdditionalFile, nameof(file));
            }
        }

        #endregion

        private ImmutableArray<AdditionalText> AdditionalFiles => _analysisOptions.Options?.AdditionalFiles ?? ImmutableArray<AdditionalText>.Empty;

        /// <summary>
        /// Returns diagnostics produced by all <see cref="Analyzers"/>.
        /// </summary>
        public Task<ImmutableArray<Diagnostic>> GetAnalyzerDiagnosticsAsync()
        {
            return GetAnalyzerDiagnosticsAsync(_cancellationToken);
        }

        /// <summary>
        /// Returns diagnostics produced by all <see cref="Analyzers"/>.
        /// </summary>
        public async Task<ImmutableArray<Diagnostic>> GetAnalyzerDiagnosticsAsync(CancellationToken cancellationToken)
        {
            return await GetAnalyzerDiagnosticsWithoutStateTrackingAsync(Analyzers, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns diagnostics produced by given <paramref name="analyzers"/>.
        /// </summary>
        /// <param name="analyzers">Analyzers whose diagnostics are required. All the given analyzers must be from the analyzers passed into the constructor of <see cref="CompilationWithAnalyzers"/>.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task<ImmutableArray<Diagnostic>> GetAnalyzerDiagnosticsAsync(ImmutableArray<DiagnosticAnalyzer> analyzers, CancellationToken cancellationToken)
        {
            VerifyExistingAnalyzersArgument(analyzers);

            return await GetAnalyzerDiagnosticsWithoutStateTrackingAsync(analyzers, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes all <see cref="Analyzers"/> and returns the corresponding <see cref="AnalysisResult"/> with all diagnostics and telemetry info.
        /// </summary>
        public async Task<AnalysisResult> GetAnalysisResultAsync(CancellationToken cancellationToken)
        {
            return await GetAnalysisResultWithoutStateTrackingAsync(Analyzers, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the given <paramref name="analyzers"/> and returns the corresponding <see cref="AnalysisResult"/> with all diagnostics and telemetry info.
        /// </summary>
        /// <param name="analyzers">Analyzers whose analysis results are required. All the given analyzers must be from the analyzers passed into the constructor of <see cref="CompilationWithAnalyzers"/>.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task<AnalysisResult> GetAnalysisResultAsync(ImmutableArray<DiagnosticAnalyzer> analyzers, CancellationToken cancellationToken)
        {
            VerifyExistingAnalyzersArgument(analyzers);

            return await GetAnalysisResultWithoutStateTrackingAsync(analyzers, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns all diagnostics produced by compilation and by all <see cref="Analyzers"/>.
        /// </summary>
        public Task<ImmutableArray<Diagnostic>> GetAllDiagnosticsAsync()
        {
            return GetAllDiagnosticsAsync(_cancellationToken);
        }

        /// <summary>
        /// Returns all diagnostics produced by compilation and by all <see cref="Analyzers"/>.
        /// </summary>
        public async Task<ImmutableArray<Diagnostic>> GetAllDiagnosticsAsync(CancellationToken cancellationToken)
        {
            var diagnostics = await getAllDiagnosticsWithoutStateTrackingAsync(Analyzers, cancellationToken: cancellationToken).ConfigureAwait(false);
            return diagnostics.AddRange(_exceptionDiagnostics);

            async Task<ImmutableArray<Diagnostic>> getAllDiagnosticsWithoutStateTrackingAsync(ImmutableArray<DiagnosticAnalyzer> analyzers, CancellationToken cancellationToken)
            {
                AsyncQueue<CompilationEvent> eventQueue = _eventQueuePool.Allocate();
                AnalyzerDriver? driver = null;

                try
                {
                    // Clone the compilation with new event queue.
                    var compilation = _compilation.WithEventQueue(eventQueue);
                    var compilationData = new CompilationData(compilation);

                    // Create and attach the driver to compilation.
                    var categorizeDiagnostics = false;
                    driver = CreateDriverForComputingDiagnosticsWithoutStateTracking(compilation, analyzers);
                    driver.Initialize(compilation, _analysisOptions, compilationData, categorizeDiagnostics, trackSuppressedDiagnosticIds: false, cancellationToken);
                    var hasAllAnalyzers = analyzers.Length == Analyzers.Length;
                    var analysisScope = new AnalysisScope(compilation, _analysisOptions.Options, analyzers, hasAllAnalyzers, concurrentAnalysis: _analysisOptions.ConcurrentAnalysis, categorizeDiagnostics: categorizeDiagnostics);
                    driver.AttachQueueAndStartProcessingEvents(compilation.EventQueue!, analysisScope, usingPrePopulatedEventQueue: false, cancellationToken);

                    // Force compilation diagnostics and wait for analyzer execution to complete.
                    var compDiags = compilation.GetDiagnostics(cancellationToken);
                    var analyzerDiags = await driver.GetDiagnosticsAsync(compilation).ConfigureAwait(false);
                    var reportedDiagnostics = compDiags.AddRange(analyzerDiags);
                    return driver.ApplyProgrammaticSuppressionsAndFilterDiagnostics(reportedDiagnostics, compilation);
                }
                finally
                {
                    driver?.Dispose();
                    FreeEventQueue(eventQueue, _eventQueuePool);
                }
            }
        }

        /// <summary>
        /// Returns diagnostics produced by compilation actions of all <see cref="Analyzers"/>.
        /// </summary>
        [Obsolete("This API was found to have performance issues and hence has been deprecated. Instead, invoke the API 'GetAnalysisResultAsync' and access the property 'CompilationDiagnostics' on the returned 'AnalysisResult' to fetch the compilation diagnostics.")]
        public async Task<ImmutableArray<Diagnostic>> GetAnalyzerCompilationDiagnosticsAsync(CancellationToken cancellationToken)
        {
            return await GetAnalyzerCompilationDiagnosticsCoreAsync(Analyzers, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns diagnostics produced by compilation actions of given <paramref name="analyzers"/>.
        /// </summary>
        /// <param name="analyzers">Analyzers whose diagnostics are required. All the given analyzers must be from the analyzers passed into the constructor of <see cref="CompilationWithAnalyzers"/>.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        [Obsolete("This API was found to have performance issues and hence has been deprecated. Instead, invoke the API 'GetAnalysisResultAsync' and access the property 'CompilationDiagnostics' on the returned 'AnalysisResult' to fetch the compilation diagnostics.")]
        public async Task<ImmutableArray<Diagnostic>> GetAnalyzerCompilationDiagnosticsAsync(ImmutableArray<DiagnosticAnalyzer> analyzers, CancellationToken cancellationToken)
        {
            VerifyExistingAnalyzersArgument(analyzers);

            return await GetAnalyzerCompilationDiagnosticsCoreAsync(analyzers, cancellationToken).ConfigureAwait(false);
        }

        private async Task<ImmutableArray<Diagnostic>> GetAnalyzerCompilationDiagnosticsCoreAsync(ImmutableArray<DiagnosticAnalyzer> analyzers, CancellationToken cancellationToken)
        {
            var hasAllAnalyzers = analyzers.Length == Analyzers.Length;
            var analysisScope = new AnalysisScope(_compilation, _analysisOptions.Options, analyzers, hasAllAnalyzers, _analysisOptions.ConcurrentAnalysis, categorizeDiagnostics: true);
            await ComputeAnalyzerDiagnosticsAsync(analysisScope, cancellationToken).ConfigureAwait(false);
            return _analysisResultBuilder.GetDiagnostics(analysisScope, getLocalDiagnostics: false, getNonLocalDiagnostics: true);
        }

        private async Task<AnalysisResult> GetAnalysisResultWithoutStateTrackingAsync(ImmutableArray<DiagnosticAnalyzer> analyzers, CancellationToken cancellationToken)
        {
            var hasAllAnalyzers = analyzers.Length == Analyzers.Length;
            var analysisScope = new AnalysisScope(_compilation, _analysisOptions.Options, analyzers, hasAllAnalyzers, concurrentAnalysis: _analysisOptions.ConcurrentAnalysis, categorizeDiagnostics: true);
            await ComputeAnalyzerDiagnosticsAsync(analysisScope, cancellationToken).ConfigureAwait(false);
            return _analysisResultBuilder.ToAnalysisResult(analyzers, analysisScope, cancellationToken);
        }

        private async Task<ImmutableArray<Diagnostic>> GetAnalyzerDiagnosticsWithoutStateTrackingAsync(ImmutableArray<DiagnosticAnalyzer> analyzers, CancellationToken cancellationToken)
        {
            var hasAllAnalyzers = analyzers.Length == Analyzers.Length;
            var analysisScope = new AnalysisScope(_compilation, _analysisOptions.Options, analyzers, hasAllAnalyzers, concurrentAnalysis: _analysisOptions.ConcurrentAnalysis, categorizeDiagnostics: true);
            await ComputeAnalyzerDiagnosticsAsync(analysisScope, cancellationToken).ConfigureAwait(false);
            return _analysisResultBuilder.GetDiagnostics(analysisScope, getLocalDiagnostics: true, getNonLocalDiagnostics: true);
        }

        private static AnalyzerDriver CreateDriverForComputingDiagnosticsWithoutStateTracking(Compilation compilation, ImmutableArray<DiagnosticAnalyzer> analyzers)
        {
            // Create and attach a new driver instance to compilation, do not used a pooled instance from '_driverPool'.
            // Additionally, use a new AnalyzerManager and don't use the analyzer manager instance
            // stored onto the field '_analyzerManager'.
            // Pooled _driverPool and shared _analyzerManager is used for partial/state-based analysis, and
            // we want to compute diagnostics without any state tracking here.

            return compilation.CreateAnalyzerDriver(analyzers, new AnalyzerManager(analyzers), severityFilter: SeverityFilter.None);
        }

        /// <summary>
        /// Returns syntax diagnostics produced by all <see cref="Analyzers"/> from analyzing the given <paramref name="tree"/>.
        /// Depending on analyzers' behavior, returned diagnostics can have locations outside the tree,
        /// and some diagnostics that would be reported for the tree by an analysis of the complete compilation
        /// can be absent.
        /// </summary>
        /// <param name="tree">Syntax tree to analyze.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task<ImmutableArray<Diagnostic>> GetAnalyzerSyntaxDiagnosticsAsync(SyntaxTree tree, CancellationToken cancellationToken)
        {
            VerifyTree(tree);

            return await GetAnalyzerSyntaxDiagnosticsCoreAsync(tree, Analyzers, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns syntax diagnostics produced by given <paramref name="analyzers"/> from analyzing the given <paramref name="tree"/>.
        /// Depending on analyzers' behavior, returned diagnostics can have locations outside the tree,
        /// and some diagnostics that would be reported for the tree by an analysis of the complete compilation
        /// can be absent.
        /// </summary>
        /// <param name="tree">Syntax tree to analyze.</param>
        /// <param name="analyzers">Analyzers whose diagnostics are required. All the given analyzers must be from the analyzers passed into the constructor of <see cref="CompilationWithAnalyzers"/>.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task<ImmutableArray<Diagnostic>> GetAnalyzerSyntaxDiagnosticsAsync(SyntaxTree tree, ImmutableArray<DiagnosticAnalyzer> analyzers, CancellationToken cancellationToken)
        {
            VerifyTree(tree);
            VerifyExistingAnalyzersArgument(analyzers);

            return await GetAnalyzerSyntaxDiagnosticsCoreAsync(tree, analyzers, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns an <see cref="AnalysisResult"/> populated with <see cref="AnalysisResult.SyntaxDiagnostics"/> produced by all <see cref="Analyzers"/> from analyzing the given <paramref name="tree"/>.
        /// Depending on analyzers' behavior, some diagnostics that would be reported for the tree by an analysis of the complete compilation can be absent.
        /// </summary>
        /// <param name="tree">Syntax tree to analyze.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public Task<AnalysisResult> GetAnalysisResultAsync(SyntaxTree tree, CancellationToken cancellationToken)
        {
            VerifyTree(tree);

            return GetAnalysisResultCoreAsync(new SourceOrAdditionalFile(tree), Analyzers, cancellationToken);
        }

        /// <summary>
        /// Returns an <see cref="AnalysisResult"/> populated with <see cref="AnalysisResult.SyntaxDiagnostics"/> produced by given <paramref name="analyzers"/> from analyzing the given <paramref name="tree"/>.
        /// Depending on analyzers' behavior, some diagnostics that would be reported for the tree by an analysis of the complete compilation can be absent.
        /// </summary>
        /// <param name="tree">Syntax tree to analyze.</param>
        /// <param name="analyzers">Analyzers whose diagnostics are required. All the given analyzers must be from the analyzers passed into the constructor of <see cref="CompilationWithAnalyzers"/>.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public Task<AnalysisResult> GetAnalysisResultAsync(SyntaxTree tree, ImmutableArray<DiagnosticAnalyzer> analyzers, CancellationToken cancellationToken)
        {
            VerifyTree(tree);
            VerifyExistingAnalyzersArgument(analyzers);

            return GetAnalysisResultCoreAsync(new SourceOrAdditionalFile(tree), analyzers, cancellationToken);
        }

        /// <summary>
        /// Returns an <see cref="AnalysisResult"/> populated with <see cref="AnalysisResult.AdditionalFileDiagnostics"/> produced by all <see cref="Analyzers"/> from analyzing the given additional <paramref name="file"/>.
        /// The given <paramref name="file"/> must be part of <see cref="AnalyzerOptions.AdditionalFiles"/> for the <see cref="AnalysisOptions"/> for this CompilationWithAnalyzers instance.
        /// Depending on analyzers' behavior, some diagnostics that would be reported for the file by an analysis of the complete compilation can be absent.
        /// </summary>
        /// <param name="file">Additional file to analyze.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task<AnalysisResult> GetAnalysisResultAsync(AdditionalText file, CancellationToken cancellationToken)
        {
            VerifyAdditionalFile(file);

            return await GetAnalysisResultCoreAsync(new SourceOrAdditionalFile(file), Analyzers, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns an <see cref="AnalysisResult"/> populated with <see cref="AnalysisResult.AdditionalFileDiagnostics"/> produced by given <paramref name="analyzers"/> from analyzing the given additional <paramref name="file"/>.
        /// The given <paramref name="file"/> must be part of <see cref="AnalyzerOptions.AdditionalFiles"/> for the <see cref="AnalysisOptions"/> for this CompilationWithAnalyzers instance.
        /// Depending on analyzers' behavior, some diagnostics that would be reported for the file by an analysis of the complete compilation can be absent.
        /// </summary>
        /// <param name="file">Additional file to analyze.</param>
        /// <param name="analyzers">Analyzers whose diagnostics are required. All the given analyzers must be from the analyzers passed into the constructor of <see cref="CompilationWithAnalyzers"/>.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task<AnalysisResult> GetAnalysisResultAsync(AdditionalText file, ImmutableArray<DiagnosticAnalyzer> analyzers, CancellationToken cancellationToken)
        {
            VerifyAdditionalFile(file);
            VerifyExistingAnalyzersArgument(analyzers);

            return await GetAnalysisResultCoreAsync(new SourceOrAdditionalFile(file), analyzers, cancellationToken).ConfigureAwait(false);
        }

        private async Task<AnalysisResult> GetAnalysisResultCoreAsync(SourceOrAdditionalFile file, ImmutableArray<DiagnosticAnalyzer> analyzers, CancellationToken cancellationToken)
        {
            var analysisScope = new AnalysisScope(analyzers, file, filterSpan: null, isSyntacticSingleFileAnalysis: true, concurrentAnalysis: _analysisOptions.ConcurrentAnalysis, categorizeDiagnostics: true);
            await ComputeAnalyzerDiagnosticsAsync(analysisScope, cancellationToken).ConfigureAwait(false);
            return _analysisResultBuilder.ToAnalysisResult(analyzers, analysisScope, cancellationToken);
        }

        private async Task<ImmutableArray<Diagnostic>> GetAnalyzerSyntaxDiagnosticsCoreAsync(SyntaxTree tree, ImmutableArray<DiagnosticAnalyzer> analyzers, CancellationToken cancellationToken)
        {
            var analysisScope = new AnalysisScope(analyzers, new SourceOrAdditionalFile(tree), filterSpan: null, isSyntacticSingleFileAnalysis: true, concurrentAnalysis: _analysisOptions.ConcurrentAnalysis, categorizeDiagnostics: true);
            await ComputeAnalyzerDiagnosticsAsync(analysisScope, cancellationToken).ConfigureAwait(false);
            return _analysisResultBuilder.GetDiagnostics(analysisScope, getLocalDiagnostics: true, getNonLocalDiagnostics: false);
        }

        /// <summary>
        /// Returns semantic diagnostics produced by all <see cref="Analyzers"/> from analyzing the given <paramref name="model"/>, optionally scoped to a <paramref name="filterSpan"/>.
        /// Depending on analyzers' behavior, some diagnostics that would be reported for the tree by an analysis of the complete compilation can be absent.
        /// </summary>
        /// <param name="model">Semantic model representing the syntax tree to analyze.</param>
        /// <param name="filterSpan">An optional span within the tree to scope analysis.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task<ImmutableArray<Diagnostic>> GetAnalyzerSemanticDiagnosticsAsync(SemanticModel model, TextSpan? filterSpan, CancellationToken cancellationToken)
        {
            VerifyModel(model);

            return await GetAnalyzerSemanticDiagnosticsCoreAsync(model, filterSpan, Analyzers, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns semantic diagnostics produced by the given <paramref name="analyzers"/> from analyzing the given <paramref name="model"/>, optionally scoped to a <paramref name="filterSpan"/>.
        /// Depending on analyzers' behavior, some diagnostics that would be reported for the tree by an analysis of the complete compilation can be absent.
        /// </summary>
        /// <param name="model">Semantic model representing the syntax tree to analyze.</param>
        /// <param name="filterSpan">An optional span within the tree to scope analysis.</param>
        /// <param name="analyzers">Analyzers whose diagnostics are required. All the given analyzers must be from the analyzers passed into the constructor of <see cref="CompilationWithAnalyzers"/>.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task<ImmutableArray<Diagnostic>> GetAnalyzerSemanticDiagnosticsAsync(SemanticModel model, TextSpan? filterSpan, ImmutableArray<DiagnosticAnalyzer> analyzers, CancellationToken cancellationToken)
        {
            VerifyModel(model);
            VerifyExistingAnalyzersArgument(analyzers);

            return await GetAnalyzerSemanticDiagnosticsCoreAsync(model, filterSpan, analyzers, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Returns an <see cref="AnalysisResult"/> populated with <see cref="AnalysisResult.SemanticDiagnostics"/> produced by all <see cref="Analyzers"/> from analyzing the given <paramref name="model"/>, optionally scoped to a <paramref name="filterSpan"/>.
        /// Depending on analyzers' behavior, some diagnostics that would be reported for the tree by an analysis of the complete compilation can be absent.
        /// </summary>
        /// <param name="model">Semantic model representing the syntax tree to analyze.</param>
        /// <param name="filterSpan">An optional span within the tree to scope analysis.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public Task<AnalysisResult> GetAnalysisResultAsync(SemanticModel model, TextSpan? filterSpan, CancellationToken cancellationToken)
        {
            VerifyModel(model);

            return GetAnalysisResultCoreAsync(model, filterSpan, this.Analyzers, cancellationToken);
        }

        /// <summary>
        /// Returns an <see cref="AnalysisResult"/> populated with <see cref="AnalysisResult.SemanticDiagnostics"/> produced by the given <paramref name="analyzers"/> from analyzing the given <paramref name="model"/>, optionally scoped to a <paramref name="filterSpan"/>.
        /// Depending on analyzers' behavior, some diagnostics that would be reported for the tree by an analysis of the complete compilation can be absent.
        /// </summary>
        /// <param name="model">Semantic model representing the syntax tree to analyze.</param>
        /// <param name="filterSpan">An optional span within the tree to scope analysis.</param>
        /// <param name="analyzers">Analyzers whose diagnostics are required. All the given analyzers must be from the analyzers passed into the constructor of <see cref="CompilationWithAnalyzers"/>.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public Task<AnalysisResult> GetAnalysisResultAsync(SemanticModel model, TextSpan? filterSpan, ImmutableArray<DiagnosticAnalyzer> analyzers, CancellationToken cancellationToken)
        {
            VerifyModel(model);
            VerifyExistingAnalyzersArgument(analyzers);

            return GetAnalysisResultCoreAsync(model, filterSpan, analyzers, cancellationToken);
        }

        private async Task<AnalysisResult> GetAnalysisResultCoreAsync(SemanticModel model, TextSpan? filterSpan, ImmutableArray<DiagnosticAnalyzer> analyzers, CancellationToken cancellationToken)
        {
            var analysisScope = new AnalysisScope(analyzers, new SourceOrAdditionalFile(model.SyntaxTree), filterSpan, isSyntacticSingleFileAnalysis: false, concurrentAnalysis: _analysisOptions.ConcurrentAnalysis, categorizeDiagnostics: true);
            await ComputeAnalyzerDiagnosticsAsync(analysisScope, cancellationToken).ConfigureAwait(false);
            return _analysisResultBuilder.ToAnalysisResult(analyzers, analysisScope, cancellationToken);
        }

        private async Task<ImmutableArray<Diagnostic>> GetAnalyzerSemanticDiagnosticsCoreAsync(SemanticModel model, TextSpan? filterSpan, ImmutableArray<DiagnosticAnalyzer> analyzers, CancellationToken cancellationToken)
        {
            var analysisScope = new AnalysisScope(analyzers, new SourceOrAdditionalFile(model.SyntaxTree), filterSpan, isSyntacticSingleFileAnalysis: false, concurrentAnalysis: _analysisOptions.ConcurrentAnalysis, categorizeDiagnostics: true);
            await ComputeAnalyzerDiagnosticsAsync(analysisScope, cancellationToken).ConfigureAwait(false);
            return _analysisResultBuilder.GetDiagnostics(analysisScope, getLocalDiagnostics: true, getNonLocalDiagnostics: false);
        }

        /// <summary>
        /// Core method to compute analyzer diagnostics for the given <paramref name="analysisScope"/>.
        /// This method is used to compute diagnostics for the entire compilation or a specific file.
        /// It executes the required analyzers and stores the reported analyzer diagnostics into
        /// <see cref="_analysisResultBuilder"/>.
        /// </summary>
        /// <remarks>
        /// PERF: We re-use the underlying <see cref="_compilation"/> for the below cases:
        ///     1. If the given analysis scope only includes the <see cref="CompilerDiagnosticAnalyzer"/>.
        ///     2. If we are only computing syntax diagnostics.
        /// For rest of the cases, we always fork the underlying <see cref="_compilation"/> with a
        /// new compilation event queue, execute the analyzers on this forked compilation and then
        /// discard this compilation. Using a forked compilation allows us to avoid performing expensive
        /// partial analysis state tracking for analyzer execution. It is the responsibility of the
        /// CompilationWithAnalyzers host to club the analyzer diagnostics requests into minimal number of
        /// calls into CompilationWithAnalyzers to get the optimum performance by minimize compilation forking.
        /// See https://github.com/dotnet/roslyn/issues/66714 for more details.
        /// </remarks>
        private async Task ComputeAnalyzerDiagnosticsAsync(AnalysisScope? analysisScope, CancellationToken cancellationToken)
        {
            Debug.Assert(analysisScope != null);

            var originalAnalyzers = analysisScope.Analyzers;
            Compilation compilation;
            AnalyzerDriver? driver = null;
            bool driverAllocatedFromPool = false;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                analysisScope = GetPendingAnalysisScope(analysisScope, _analysisResultBuilder);
                if (analysisScope == null)
                    return;

                (compilation, driver, driverAllocatedFromPool) = await getCompilationAndDriverAsync().ConfigureAwait(false);

                // Driver must have been initialized.
                Debug.Assert(driver.IsInitialized);
                Debug.Assert(!driver.WhenInitializedTask.IsCanceled);

                (var analyzerActionCounts, var hasAnyActionsRequiringCompilationEvents) = await getAnalyzerActionCountsAsync(
                    originalAnalyzers, driver, compilation, cancellationToken).ConfigureAwait(false);
                Func<DiagnosticAnalyzer, AnalyzerActionCounts> getAnalyzerActionCounts = analyzer => analyzerActionCounts[analyzer];

                // We use different analyzer execution models based on whether we are analyzing the full compilation or not.
                if (!analysisScope.IsSingleFileAnalysis)
                {
                    // We are performing full compilation analysis.
                    // PERF: For improved performance, we first attach the event queue to the driver and then
                    //       invoke compilation.GetDiagnostics() to ensure that we generate and process the
                    //       compilation events concurrently.

                    // Attach the driver to compilation and start processing events.
                    // If we do not have any analyzer actions that are driven by compilation events,
                    // we are only going to enqueue a CompilationStartedEvent, so ensure we
                    // pass 'usingPrePopulatedEventQueue = true' for that case.
                    driver.AttachQueueAndStartProcessingEvents(compilation.EventQueue!, analysisScope, usingPrePopulatedEventQueue: !hasAnyActionsRequiringCompilationEvents, cancellationToken);

                    // If we have any analyzer actions that are driven by compilation events, we need to
                    // force compilation diagnostics to populate the compilation event queue and force it to be completed.
                    if (hasAnyActionsRequiringCompilationEvents)
                        _ = compilation.GetDiagnostics(cancellationToken);

                    // Wait for analyzer execution to complete.
                    await driver.WhenCompletedTask.ConfigureAwait(false);

                    _analysisResultBuilder.ApplySuppressionsAndStoreAnalysisResult(analysisScope, driver, compilation, getAnalyzerActionCounts);
                }
                else
                {
                    // We are performing analysis for a single file in the compilation, with or without a filter span.
                    // First fetch the compilation events to drive this partial analysis.
                    var compilationEvents = GetCompilationEventsForSingleFileAnalysis(compilation, analysisScope, AdditionalFiles, hasAnyActionsRequiringCompilationEvents, cancellationToken);

                    var builder = ArrayBuilder<(AnalysisScope, ImmutableArray<CompilationEvent>)>.GetInstance();
                    builder.Add((analysisScope, compilationEvents));

                    // If the analysis scope has symbol declared events, and we have any analyzers with SymbolStart/End actions,
                    // then we need to analyze entire symbol declaration, including partial declarations, to compute the
                    // required SymbolEnd diagnostics for such symbolStartAnalyzers.
                    if (compilationEvents.Any(e => e is SymbolDeclaredCompilationEvent) &&
                        driver.HasSymbolStartedActions(analysisScope))
                    {
                        Debug.Assert(!analysisScope.IsSyntacticSingleFileAnalysis);
                        Debug.Assert(hasAnyActionsRequiringCompilationEvents);

                        var (symbolStartAnalyzers, otherAnalyzers) = getSymbolStartAnalyzers(analysisScope.Analyzers, analyzerActionCounts);
                        Debug.Assert(!symbolStartAnalyzers.IsEmpty);

                        // We separate out the execution of symbol start analyzers and rest of the analyzers.
                        // This is due to the fact that symbol start analyzers need to execute on the entire
                        // symbol declarations, not just the filter span, and hence have different analysis scope.
                        builder.Clear();
                        if (!otherAnalyzers.IsEmpty)
                        {
                            var otherAnalyzersAnalysisScope = analysisScope.WithAnalyzers(otherAnalyzers, hasAllAnalyzers: false);
                            builder.Add((otherAnalyzersAnalysisScope, compilationEvents));
                        }

                        var tree = analysisScope.FilterFileOpt!.Value.SourceTree!;
                        processSymbolStartAnalyzers(tree, compilationEvents, symbolStartAnalyzers, compilation,
                            _analysisResultBuilder, builder, AdditionalFiles, analysisScope.ConcurrentAnalysis, cancellationToken);
                    }

                    await attachQueueAndProcessAllEventsAsync(builder, driver, _eventQueuePool, cancellationToken).ConfigureAwait(false);

                    // Update the diagnostic results based on the diagnostics reported on the driver.
                    foreach (var (scope, _) in builder)
                    {
                        _analysisResultBuilder.ApplySuppressionsAndStoreAnalysisResult(scope, driver, compilation, getAnalyzerActionCounts);
                    }
                }
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable();
            }
            finally
            {
                if (driverAllocatedFromPool)
                    FreeDriver(driver);
            }

            async Task<(Compilation compilation, AnalyzerDriver driver, bool driverAllocatedFromPool)> getCompilationAndDriverAsync()
            {
                Debug.Assert(analysisScope != null);

                Compilation compilation;
                AnalyzerDriver driver;
                bool driverAllocatedFromPool;

                // We reuse the root "_compilation" when performing purely syntactic analysis or computing semantic diagnostics
                // only for the compiler analyzer.
                // For rest of the cases, we use a cloned compilation with a new event queue to allow us to execute analyzers on this
                // compilation without any partial state tracking and subsequently discard this compilation.
                if (analysisScope.IsSyntacticSingleFileAnalysis ||
                    analysisScope.IsSemanticSingleFileAnalysisForCompilerAnalyzer)
                {
                    compilation = _compilation;
                    driver = await GetAnalyzerDriverFromPoolAsync(cancellationToken).ConfigureAwait(false);
                    driverAllocatedFromPool = true;
                }
                else
                {
                    // Clone the compilation with new event queue and semantic model provider.
                    compilation = _compilation.WithSemanticModelProvider(new CachingSemanticModelProvider()).WithEventQueue(new AsyncQueue<CompilationEvent>());

                    // Get the analyzer driver to execute analysis.
                    driver = CreateDriverForComputingDiagnosticsWithoutStateTracking(compilation, originalAnalyzers);
                    driverAllocatedFromPool = false;

                    // Initialize the driver.
                    driver.Initialize(compilation, _analysisOptions, new CompilationData(compilation), categorizeDiagnostics: true, trackSuppressedDiagnosticIds: false, cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();

                    await driver.WhenInitializedTask.ConfigureAwait(false);
                }

                return (compilation, driver, driverAllocatedFromPool);
            }

            static async Task attachQueueAndProcessAllEventsAsync(
                ArrayBuilder<(AnalysisScope, ImmutableArray<CompilationEvent>)> builder,
                AnalyzerDriver driver,
                ObjectPool<AsyncQueue<CompilationEvent>> eventQueuePool,
                CancellationToken cancellationToken)
            {
                foreach (var (analysisScope, compilationEvents) in builder)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    AsyncQueue<CompilationEvent>? eventQueue = null;

                    try
                    {
                        eventQueue = CreateEventsQueue(compilationEvents, eventQueuePool);

                        // Perform analysis to compute new diagnostics.
                        await driver.AttachQueueAndProcessAllEventsAsync(eventQueue, analysisScope, cancellationToken).ConfigureAwait(false);
                    }
                    finally
                    {
                        FreeEventQueue(eventQueue, eventQueuePool);
                    }
                }
            }

            static async Task<(ImmutableDictionary<DiagnosticAnalyzer, AnalyzerActionCounts> analyzerActionCounts, bool hasAnyActionsRequiringCompilationEvents)> getAnalyzerActionCountsAsync(
                ImmutableArray<DiagnosticAnalyzer> analyzers,
                AnalyzerDriver driver,
                Compilation compilation,
                CancellationToken cancellationToken)
            {
                Debug.Assert(driver.IsInitialized);

                // Get analyzer action counts.
                var builder = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, AnalyzerActionCounts>();
                var hasAnyActionsRequiringCompilationEvents = false;
                foreach (var analyzer in analyzers)
                {
                    var actionCounts = await driver.GetAnalyzerActionCountsAsync(analyzer, compilation.Options, cancellationToken).ConfigureAwait(false);
                    builder.Add(analyzer, actionCounts);

                    if (actionCounts.HasAnyActionsRequiringCompilationEvents)
                        hasAnyActionsRequiringCompilationEvents = true;
                }

                return (builder.ToImmutable(), hasAnyActionsRequiringCompilationEvents);
            }

            static (ImmutableArray<DiagnosticAnalyzer> symbolStartAnalyzers, ImmutableArray<DiagnosticAnalyzer> otherAnalyzers) getSymbolStartAnalyzers(
                ImmutableArray<DiagnosticAnalyzer> analyzers,
                ImmutableDictionary<DiagnosticAnalyzer, AnalyzerActionCounts> analyzerActionCounts)
            {
                var symbolStartAnalyzersBuilder = ArrayBuilder<DiagnosticAnalyzer>.GetInstance();
                var otherAnalyzersBuilder = ArrayBuilder<DiagnosticAnalyzer>.GetInstance();

                foreach (var analyzer in analyzers)
                {
                    if (analyzerActionCounts[analyzer].SymbolStartActionsCount > 0)
                    {
                        symbolStartAnalyzersBuilder.Add(analyzer);
                    }
                    else
                    {
                        otherAnalyzersBuilder.Add(analyzer);
                    }
                }

                return (symbolStartAnalyzersBuilder.ToImmutableAndFree(), otherAnalyzersBuilder.ToImmutableAndFree());
            }

            static void processSymbolStartAnalyzers(
                SyntaxTree tree,
                ImmutableArray<CompilationEvent> compilationEventsForTree,
                ImmutableArray<DiagnosticAnalyzer> analyzers,
                Compilation compilation,
                AnalysisResultBuilder analysisResultBuilder,
                ArrayBuilder<(AnalysisScope, ImmutableArray<CompilationEvent>)> builder,
                ImmutableArray<AdditionalText> additionalFiles,
                bool concurrentAnalysis,
                CancellationToken cancellationToken)
            {
                var partialTrees = PooledHashSet<SyntaxTree>.GetInstance();
                partialTrees.Add(tree);

                try
                {
                    // Gather all trees with symbol declarations events in original analysis scope, except namespace symbols.
                    foreach (var compilationEvent in compilationEventsForTree)
                    {
                        if (compilationEvent is SymbolDeclaredCompilationEvent symbolDeclaredEvent &&
                            symbolDeclaredEvent.Symbol.Kind != SymbolKind.Namespace)
                        {
                            foreach (var location in symbolDeclaredEvent.Symbol.Locations)
                            {
                                if (location.SourceTree != null)
                                {
                                    partialTrees.Add(location.SourceTree);
                                }
                            }
                        }
                    }

                    // Process all trees sequentially: this is required to ensure the appropriate
                    // compilation events are mapped to the tree for which they are generated.
                    foreach (var partialTree in partialTrees)
                    {
                        if (tryProcessTree(partialTree, out var analysisScopeAndEvents))
                        {
                            builder.Add((analysisScopeAndEvents.Value.scope, analysisScopeAndEvents.Value.events));
                        }
                    }
                }
                finally
                {
                    partialTrees.Free();
                }

                bool tryProcessTree(SyntaxTree partialTree, [NotNullWhen(true)] out (AnalysisScope scope, ImmutableArray<CompilationEvent> events)? scopeAndEvents)
                {
                    scopeAndEvents = null;

                    var file = new SourceOrAdditionalFile(partialTree);
                    var analysisScope = new AnalysisScope(analyzers, file, filterSpan: null,
                        isSyntacticSingleFileAnalysis: false, concurrentAnalysis, categorizeDiagnostics: true);

                    analysisScope = GetPendingAnalysisScope(analysisScope, analysisResultBuilder);
                    if (analysisScope == null)
                        return false;

                    var compilationEvents = GetCompilationEventsForSingleFileAnalysis(compilation, analysisScope, additionalFiles, hasAnyActionsRequiringCompilationEvents: true, cancellationToken);

                    // Include the already generated compilations events for the primary tree.
                    if (partialTree == tree)
                    {
                        compilationEvents = compilationEventsForTree.AddRange(compilationEvents);

                        // We shouldn't have any duplicate events.
                        Debug.Assert(compilationEvents.Distinct().Length == compilationEvents.Length);
                    }

                    scopeAndEvents = (analysisScope, compilationEvents);
                    return true;
                }
            }
        }

        private static AnalysisScope? GetPendingAnalysisScope(AnalysisScope analysisScope, AnalysisResultBuilder analysisResultBuilder)
        {
            (SourceOrAdditionalFile file, bool syntax)? filterScope = analysisScope.FilterFileOpt.HasValue ?
                (analysisScope.FilterFileOpt.Value, analysisScope.IsSyntacticSingleFileAnalysis) :
                null;
            var pendingAnalyzers = analysisResultBuilder.GetPendingAnalyzers(analysisScope.Analyzers, filterScope);
            if (pendingAnalyzers.IsEmpty)
            {
                // All analyzers have already executed on the requested scope.
                return null;
            }

            return pendingAnalyzers.Length < analysisScope.Analyzers.Length ?
                analysisScope.WithAnalyzers(pendingAnalyzers, hasAllAnalyzers: false) :
                analysisScope;
        }

        private static ImmutableArray<CompilationEvent> GetCompilationEventsForSingleFileAnalysis(
            Compilation compilation,
            AnalysisScope analysisScope,
            ImmutableArray<AdditionalText> additionalFiles,
            bool hasAnyActionsRequiringCompilationEvents,
            CancellationToken cancellationToken)
        {
            Debug.Assert(analysisScope.IsSingleFileAnalysis);

            if (analysisScope.IsSyntacticSingleFileAnalysis || !hasAnyActionsRequiringCompilationEvents)
            {
                return ImmutableArray<CompilationEvent>.Empty;
            }

            if (analysisScope.IsSemanticSingleFileAnalysisForCompilerAnalyzer)
            {
                // PERF: We are computing only compiler semantic diagnostics for this file via the CompilerDiagnosticAnalyzer.
                //       CompilerDiagnosticAnalyzer reports semantic diagnostics for each tree in SemanticModelAction callbacks,
                //       which are driven with CompilationUnitCompleted events.
                //       We just need CompilationStarted event and CompilationUnitCompleted event for the tree to drive all analysis.
                var compilationStartedEvent = new CompilationStartedEvent(compilation);
                if (!additionalFiles.IsEmpty)
                {
                    compilationStartedEvent = compilationStartedEvent.WithAdditionalFiles(additionalFiles);
                }

                var compilationUnitCompletedEvent = new CompilationUnitCompletedEvent(compilation, analysisScope.FilterFileOpt!.Value.SourceTree!, analysisScope.FilterSpanOpt);
                return ImmutableArray.Create<CompilationEvent>(compilationStartedEvent, compilationUnitCompletedEvent);
            }

            generateCompilationEvents(compilation, analysisScope, cancellationToken);

            return dequeueAndFilterCompilationEvents(compilation, analysisScope, additionalFiles, cancellationToken);

            static void generateCompilationEvents(Compilation compilation, AnalysisScope analysisScope, CancellationToken cancellationToken)
            {
                // Invoke GetDiagnostics to populate CompilationEvent queue for the given analysis scope.
                // Discard the returned diagnostics.
                if (analysisScope.FilterFileOpt == null)
                {
                    _ = compilation.GetDiagnostics(cancellationToken);
                }
                else if (!analysisScope.IsSyntacticSingleFileAnalysis)
                {
                    // Get the mapped model and invoke GetDiagnostics for the given filter span, if any.
                    // Limiting the GetDiagnostics scope to the filter span ensures we only generate compilation events
                    // for the required symbols whose declaration intersects with this span, instead of all symbols in the tree.
                    var mappedModel = compilation.GetSemanticModel(analysisScope.FilterFileOpt!.Value.SourceTree!);
                    _ = mappedModel.GetDiagnostics(analysisScope.FilterSpanOpt, cancellationToken);
                }
            }

            static ImmutableArray<CompilationEvent> dequeueAndFilterCompilationEvents(
                Compilation compilation,
                AnalysisScope analysisScope,
                ImmutableArray<AdditionalText> additionalFiles,
                CancellationToken cancellationToken)
            {
                Debug.Assert(analysisScope.IsSingleFileAnalysis);
                Debug.Assert(!analysisScope.IsSyntacticSingleFileAnalysis);

                var eventQueue = compilation.EventQueue!;
                if (eventQueue.Count == 0)
                {
                    return ImmutableArray<CompilationEvent>.Empty;
                }

                cancellationToken.ThrowIfCancellationRequested();

                // We synthesize a span-based CompilationUnitCompletedEvent for improved performance for computing semantic diagnostics
                // of compiler diagnostic analyzer. See https://github.com/dotnet/roslyn/issues/56843 for more details.
                var needsSpanBasedCompilationUnitCompletedEvent = analysisScope.FilterSpanOpt.HasValue;

                var tree = analysisScope.FilterFileOpt!.Value.SourceTree!;
                var builder = ArrayBuilder<CompilationEvent>.GetInstance();
                while (eventQueue.TryDequeue(out CompilationEvent compilationEvent))
                {
                    switch (compilationEvent)
                    {
                        case CompilationStartedEvent compilationStartedEvent:
                            if (!additionalFiles.IsEmpty)
                            {
                                compilationEvent = compilationStartedEvent.WithAdditionalFiles(additionalFiles);
                            }

                            break;

                        case CompilationCompletedEvent:
                            break;

                        case CompilationUnitCompletedEvent compilationUnitCompletedEvent:
                            if (tree != compilationUnitCompletedEvent.CompilationUnit)
                                continue;

                            // We don't need to synthesize a span-based CompilationUnitCompletedEvent if the event queue already
                            // has a CompilationUnitCompletedEvent for the entire source tree.
                            needsSpanBasedCompilationUnitCompletedEvent = false;
                            break;

                        case SymbolDeclaredCompilationEvent symbolDeclaredCompilationEvent:
                            var hasLocationInTree = false;
                            foreach (var location in symbolDeclaredCompilationEvent.Symbol.Locations)
                            {
                                if (tree == location.SourceTree)
                                {
                                    hasLocationInTree = true;
                                    break;
                                }
                            }

                            if (!hasLocationInTree)
                                continue;

                            break;

                        default:
                            throw ExceptionUtilities.UnexpectedValue(compilationEvent.GetType().ToString());
                    }

                    builder.Add(compilationEvent);
                }

                if (needsSpanBasedCompilationUnitCompletedEvent)
                {
                    builder.Add(new CompilationUnitCompletedEvent(compilation, tree, analysisScope.FilterSpanOpt));
                }

                return builder.ToImmutableAndFree();
            }
        }

        private async Task<AnalyzerDriver> GetAnalyzerDriverFromPoolAsync(CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Get instance of analyzer driver from the driver pool.
                AnalyzerDriver driver = _driverPool.Allocate();

                bool success = false;
                try
                {
                    // Start the initialization task, if required.
                    if (!driver.IsInitialized)
                    {
                        driver.Initialize(_compilation, _analysisOptions, _compilationData, categorizeDiagnostics: true, trackSuppressedDiagnosticIds: false, cancellationToken: cancellationToken);
                    }

                    // Wait for driver initialization to complete: this executes the Initialize and CompilationStartActions to compute all registered actions per-analyzer.
                    await driver.WhenInitializedTask.ConfigureAwait(false);

                    success = true;
                    return driver;
                }
                finally
                {
                    if (!success)
                    {
                        FreeDriver(driver);
                    }
                }
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        private void FreeDriver(AnalyzerDriver? driver)
        {
            if (driver != null)
            {
                // Throw away the driver instance if the initialization didn't succeed.
                if (!driver.IsInitialized || driver.WhenInitializedTask.IsCanceled)
                {
                    _driverPool.ForgetTrackedObject(driver);
                }
                else
                {
                    _driverPool.Free(driver);
                }
            }
        }

        private static AsyncQueue<CompilationEvent> CreateEventsQueue(ImmutableArray<CompilationEvent> compilationEvents, ObjectPool<AsyncQueue<CompilationEvent>> eventQueuePool)
        {
            if (compilationEvents.IsEmpty)
            {
                return s_EmptyEventQueue;
            }

            var eventQueue = eventQueuePool.Allocate();
            Debug.Assert(!eventQueue.IsCompleted);
            Debug.Assert(eventQueue.Count == 0);

            foreach (var compilationEvent in compilationEvents)
            {
                eventQueue.TryEnqueue(compilationEvent);
            }

            return eventQueue;
        }

        private static void FreeEventQueue(AsyncQueue<CompilationEvent>? eventQueue, ObjectPool<AsyncQueue<CompilationEvent>> eventQueuePool)
        {
            if (eventQueue == null || ReferenceEquals(eventQueue, s_EmptyEventQueue))
            {
                return;
            }

            if (eventQueue.Count > 0)
            {
                while (eventQueue.TryDequeue(out _)) ;
            }

            if (!eventQueue.IsCompleted)
            {
                eventQueuePool.Free(eventQueue);
            }
            else
            {
                eventQueuePool.ForgetTrackedObject(eventQueue);
            }
        }

        /// <summary>
        /// Given a set of compiler or <see cref="DiagnosticAnalyzer"/> generated <paramref name="diagnostics"/>, returns the effective diagnostics after applying the below filters:
        /// 1) <see cref="CompilationOptions.SpecificDiagnosticOptions"/> specified for the given <paramref name="compilation"/>.
        /// 2) <see cref="CompilationOptions.GeneralDiagnosticOption"/> specified for the given <paramref name="compilation"/>.
        /// 3) Diagnostic suppression through applied <see cref="System.Diagnostics.CodeAnalysis.SuppressMessageAttribute"/>.
        /// 4) Pragma directives for the given <paramref name="compilation"/>.
        /// </summary>
        public static IEnumerable<Diagnostic> GetEffectiveDiagnostics(IEnumerable<Diagnostic> diagnostics, Compilation compilation)
            => GetEffectiveDiagnostics(diagnostics.AsImmutableOrNull(), compilation);

        /// <summary>
        /// Given a set of compiler or <see cref="DiagnosticAnalyzer"/> generated <paramref name="diagnostics"/>, returns the effective diagnostics after applying the below filters:
        /// 1) <see cref="CompilationOptions.SpecificDiagnosticOptions"/> specified for the given <paramref name="compilation"/>.
        /// 2) <see cref="CompilationOptions.GeneralDiagnosticOption"/> specified for the given <paramref name="compilation"/>.
        /// 3) Diagnostic suppression through applied <see cref="System.Diagnostics.CodeAnalysis.SuppressMessageAttribute"/>.
        /// 4) Pragma directives for the given <paramref name="compilation"/>.
        /// </summary>
        public static IEnumerable<Diagnostic> GetEffectiveDiagnostics(ImmutableArray<Diagnostic> diagnostics, Compilation compilation)
        {
            if (diagnostics.IsDefault)
            {
                throw new ArgumentNullException(nameof(diagnostics));
            }

            if (compilation == null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }

            return GetEffectiveDiagnosticsImpl(diagnostics, compilation);
        }

        private static IEnumerable<Diagnostic> GetEffectiveDiagnosticsImpl(ImmutableArray<Diagnostic> diagnostics, Compilation compilation)
        {
            if (diagnostics.IsEmpty)
            {
                yield break;
            }

            if (compilation.SemanticModelProvider == null)
            {
                compilation = compilation.WithSemanticModelProvider(new CachingSemanticModelProvider());
            }

            var suppressMessageState = new SuppressMessageAttributeState(compilation);
            foreach (var diagnostic in diagnostics)
            {
                if (diagnostic != null)
                {
                    var effectiveDiagnostic = compilation.Options.FilterDiagnostic(diagnostic, CancellationToken.None);
                    if (effectiveDiagnostic != null)
                    {
                        yield return suppressMessageState.ApplySourceSuppressions(effectiveDiagnostic);
                    }
                }
            }
        }

        /// <summary>
        /// Returns true if all the diagnostics that can be produced by this analyzer are suppressed through options.
        /// </summary>
        /// <param name="analyzer">Analyzer to be checked for suppression.</param>
        /// <param name="options">Compilation options.</param>
        /// <param name="onAnalyzerException">
        /// Optional delegate which is invoked when an analyzer throws an exception.
        /// Delegate can do custom tasks such as report the given analyzer exception diagnostic, report a non-fatal watson for the exception, etc.
        /// </param>        
        public static bool IsDiagnosticAnalyzerSuppressed(
            DiagnosticAnalyzer analyzer,
            CompilationOptions options,
            Action<Exception, DiagnosticAnalyzer, Diagnostic>? onAnalyzerException = null)
        {
            VerifyAnalyzerArgumentForStaticApis(analyzer);

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var analyzerManager = new AnalyzerManager(analyzer);
            var analyzerExecutor = AnalyzerExecutor.CreateForSupportedDiagnostics(onAnalyzerException, analyzerManager);
            return AnalyzerDriver.IsDiagnosticAnalyzerSuppressed(analyzer, options, analyzerManager, analyzerExecutor, severityFilter: SeverityFilter.None);
        }

        /// <summary>
        /// This method should be invoked when the analyzer host is disposing off the given <paramref name="analyzers"/>.
        /// It clears the cached internal state (supported descriptors, registered actions, exception handlers, etc.) for analyzers.
        /// </summary>
        /// <param name="analyzers">Analyzers whose state needs to be cleared.</param>
        [Obsolete("This API is no longer required to be invoked. Analyzer state is automatically cleaned up when CompilationWithAnalyzers instance is released.")]
        public static void ClearAnalyzerState(ImmutableArray<DiagnosticAnalyzer> analyzers)
        {
        }

        /// <summary>
        /// Gets telemetry info for the given analyzer, such as count of registered actions, the total execution time (if <see cref="CompilationWithAnalyzersOptions.LogAnalyzerExecutionTime"/> is true), etc.
        /// </summary>
        public async Task<AnalyzerTelemetryInfo> GetAnalyzerTelemetryInfoAsync(DiagnosticAnalyzer analyzer, CancellationToken cancellationToken)
        {
            VerifyAnalyzerArgument(analyzer);

            try
            {
                var actionCounts = await GetAnalyzerActionCountsAsync(analyzer, cancellationToken).ConfigureAwait(false);
                var suppressionActionCounts = analyzer is DiagnosticSuppressor ? 1 : 0;
                var executionTime = GetAnalyzerExecutionTime(analyzer);
                return new AnalyzerTelemetryInfo(actionCounts, suppressionActionCounts, executionTime);
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        /// <summary>
        /// Gets the count of registered actions for the analyzer.
        /// </summary>
        private async Task<AnalyzerActionCounts> GetAnalyzerActionCountsAsync(DiagnosticAnalyzer analyzer, CancellationToken cancellationToken)
        {
            AnalyzerDriver? driver = null;
            try
            {
                driver = await GetAnalyzerDriverFromPoolAsync(cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                return await driver.GetAnalyzerActionCountsAsync(analyzer, _compilation.Options, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                FreeDriver(driver);
            }
        }

        /// <summary>
        /// Gets the execution time for the given analyzer.
        /// </summary>
        private TimeSpan GetAnalyzerExecutionTime(DiagnosticAnalyzer analyzer)
        {
            if (!_analysisOptions.LogAnalyzerExecutionTime)
            {
                return default;
            }

            return _analysisResultBuilder.GetAnalyzerExecutionTime(analyzer);
        }
    }
}
