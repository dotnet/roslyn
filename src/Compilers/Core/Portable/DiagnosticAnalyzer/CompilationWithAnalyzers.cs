// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;
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
        /// Contains the partial analysis state per-analyzer. It tracks:
        /// 1. Global set of pending compilation events. This is used to populate the event queue for analyzer execution.
        /// 2. Per-analyzer set of pending compilation events, symbols, declarations, etc. Each of these pending entities has a <see cref="AnalysisState.AnalyzerStateData"/> state object to track partial analysis.
        /// </summary>
        private readonly AnalysisState _analysisState;

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
        /// Lock to track the set of active tasks computing tree diagnostics and task computing compilation diagnostics.
        /// </summary>
        private readonly object _executingTasksLock = new object();
        private readonly Dictionary<SyntaxTree, Tuple<Task, CancellationTokenSource>> _executingConcurrentTreeTasksOpt;
        private Tuple<Task, CancellationTokenSource> _executingCompilationOrNonConcurrentTreeTask;

        /// <summary>
        /// Used to generate a unique token for each tree diagnostics request.
        /// The token is used to determine the priority of each request.
        /// Each new tree diagnostic request gets an incremented token value and has higher priority over other requests for the same tree.
        /// Compilation diagnostics requests always have the lowest priority.
        /// </summary>
        private int _currentToken = 0;

        /// <summary>
        /// Map from active tasks computing tree diagnostics to their token number.
        /// </summary>
        private readonly Dictionary<Task, int> _concurrentTreeTaskTokensOpt;

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
        public CompilationWithAnalyzers(Compilation compilation, ImmutableArray<DiagnosticAnalyzer> analyzers, AnalyzerOptions options, CancellationToken cancellationToken)
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
                .WithEventQueue(new AsyncQueue<CompilationEvent>());
            _compilation = compilation;
            _analyzers = analyzers;
            _analysisOptions = analysisOptions;
            _cancellationToken = cancellationToken;

            _compilationData = new CompilationData(_compilation);
            _analysisState = new AnalysisState(analyzers, _compilationData, _compilation.Options);
            _analysisResultBuilder = new AnalysisResultBuilder(analysisOptions.LogAnalyzerExecutionTime, analyzers);
            _analyzerManager = new AnalyzerManager(analyzers);
            _driverPool = new ObjectPool<AnalyzerDriver>(() => _compilation.AnalyzerForLanguage(analyzers, _analyzerManager));
            _executingConcurrentTreeTasksOpt = analysisOptions.ConcurrentAnalysis ? new Dictionary<SyntaxTree, Tuple<Task, CancellationTokenSource>>() : null;
            _concurrentTreeTaskTokensOpt = analysisOptions.ConcurrentAnalysis ? new Dictionary<Task, int>() : null;
            _executingCompilationOrNonConcurrentTreeTask = null;
        }

        private void AddExceptionDiagnostic(Exception exception, DiagnosticAnalyzer analyzer, Diagnostic diagnostic)
        {
            _analysisOptions.OnAnalyzerException?.Invoke(exception, analyzer, diagnostic);

            _exceptionDiagnostics.Add(diagnostic);
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

            if (analyzers.Any(a => a == null))
            {
                throw new ArgumentException(CodeAnalysisResources.ArgumentElementCannotBeNull, nameof(analyzers));
            }

            if (analyzers.Distinct().Length != analyzers.Length)
            {
                // Has duplicate analyzer instances.
                throw new ArgumentException(CodeAnalysisResources.DuplicateAnalyzerInstances, nameof(analyzers));
            }
        }

        private void VerifyAnalyzersArgument(ImmutableArray<DiagnosticAnalyzer> analyzers)
        {
            VerifyAnalyzersArgumentForStaticApis(analyzers);

            if (analyzers.Any(a => !_analyzers.Contains(a)))
            {
                throw new ArgumentException(CodeAnalysisResources.UnsupportedAnalyzerInstance, nameof(analyzers));
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

            if (analyzers.Any(a => !_analyzers.Contains(a)))
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

        #endregion

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
            var diagnostics = await GetAllDiagnosticsWithoutStateTrackingAsync(Analyzers, cancellationToken: cancellationToken).ConfigureAwait(false);
            return diagnostics.AddRange(_exceptionDiagnostics);
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
            // Wait for all active tasks to complete.
            await WaitForActiveAnalysisTasksAsync(waitForTreeTasks: true, waitForCompilationOrNonConcurrentTask: true, cancellationToken: cancellationToken).ConfigureAwait(false);

            var diagnostics = ImmutableArray<Diagnostic>.Empty;
            var analysisScope = new AnalysisScope(_compilation, analyzers, _analysisOptions.ConcurrentAnalysis, categorizeDiagnostics: true);
            Func<ImmutableArray<CompilationEvent>> getPendingEvents = () =>
                _analysisState.GetPendingEvents(analyzers, includeSourceEvents: true, includeNonSourceEvents: true, cancellationToken);

            // Compute the analyzer diagnostics for the given analysis scope.
            await ComputeAnalyzerDiagnosticsAsync(analysisScope, getPendingEvents, newTaskToken: 0, cancellationToken: cancellationToken).ConfigureAwait(false);

            // Return computed non-local diagnostics for the given analysis scope.
            return _analysisResultBuilder.GetDiagnostics(analysisScope, getLocalDiagnostics: false, getNonLocalDiagnostics: true);
        }

        private async Task<AnalysisResult> GetAnalysisResultWithoutStateTrackingAsync(ImmutableArray<DiagnosticAnalyzer> analyzers, CancellationToken cancellationToken)
        {
            // PERF: Compute whole compilation diagnostics without doing any partial analysis state tracking.
            await ComputeAnalyzerDiagnosticsWithoutStateTrackingAsync(cancellationToken).ConfigureAwait(false);

            return _analysisResultBuilder.ToAnalysisResult(analyzers, cancellationToken);
        }

        private async Task<ImmutableArray<Diagnostic>> GetAnalyzerDiagnosticsWithoutStateTrackingAsync(ImmutableArray<DiagnosticAnalyzer> analyzers, CancellationToken cancellationToken)
        {
            // PERF: Compute whole compilation diagnostics without doing any partial analysis state tracking.
            await ComputeAnalyzerDiagnosticsWithoutStateTrackingAsync(cancellationToken).ConfigureAwait(false);

            // Get analyzer diagnostics for the given analysis scope.
            var analysisScope = new AnalysisScope(_compilation, analyzers, concurrentAnalysis: _analysisOptions.ConcurrentAnalysis, categorizeDiagnostics: true);
            return _analysisResultBuilder.GetDiagnostics(analysisScope, getLocalDiagnostics: true, getNonLocalDiagnostics: true);
        }

        private async Task ComputeAnalyzerDiagnosticsWithoutStateTrackingAsync(CancellationToken cancellationToken)
        {
            // Exclude analyzers that have fully executed.
            var analyzers = _analysisResultBuilder.GetPendingAnalyzers(this.Analyzers);
            if (analyzers.IsEmpty)
            {
                return;
            }

            AsyncQueue<CompilationEvent> eventQueue = _eventQueuePool.Allocate();
            AnalyzerDriver driver = null;

            try
            {
                // Clone the compilation with new event queue.
                var compilation = _compilation.WithEventQueue(eventQueue);
                var compilationData = new CompilationData(compilation);

                // Create and attach the driver to compilation.
                var categorizeDiagnostics = true;
                driver = compilation.AnalyzerForLanguage(analyzers, _analyzerManager);
                driver.Initialize(compilation, _analysisOptions, compilationData, categorizeDiagnostics, cancellationToken);
                var analysisScope = new AnalysisScope(compilation, analyzers, concurrentAnalysis: _analysisOptions.ConcurrentAnalysis, categorizeDiagnostics: categorizeDiagnostics);
                driver.AttachQueueAndStartProcessingEvents(compilation.EventQueue, analysisScope, cancellationToken);

                // Force compilation diagnostics and wait for analyzer execution to complete.
                var compDiags = compilation.GetDiagnostics(cancellationToken);
                await driver.WhenCompletedTask.ConfigureAwait(false);

                // Get analyzer action counts.
                var analyzerActionCounts = new Dictionary<DiagnosticAnalyzer, AnalyzerActionCounts>(analyzers.Length);
                foreach (var analyzer in analyzers)
                {
                    var actionCounts = await driver.GetAnalyzerActionCountsAsync(analyzer, compilation.Options, cancellationToken).ConfigureAwait(false);
                    analyzerActionCounts.Add(analyzer, actionCounts);
                }
                Func<DiagnosticAnalyzer, AnalyzerActionCounts> getAnalyzerActionCounts = analyzer => analyzerActionCounts[analyzer];

                _analysisResultBuilder.ApplySuppressionsAndStoreAnalysisResult(analysisScope, driver, compilation, getAnalyzerActionCounts, fullAnalysisResultForAnalyzersInScope: true);
            }
            finally
            {
                driver?.Dispose();
                FreeEventQueue(eventQueue, _eventQueuePool);
            }
        }

        private async Task<ImmutableArray<Diagnostic>> GetAllDiagnosticsWithoutStateTrackingAsync(ImmutableArray<DiagnosticAnalyzer> analyzers, CancellationToken cancellationToken)
        {
            AsyncQueue<CompilationEvent> eventQueue = _eventQueuePool.Allocate();
            AnalyzerDriver driver = null;

            try
            {
                // Clone the compilation with new event queue.
                var compilation = _compilation.WithEventQueue(eventQueue);
                var compilationData = new CompilationData(compilation);

                // Create and attach the driver to compilation.
                var categorizeDiagnostics = false;
                driver = compilation.AnalyzerForLanguage(analyzers, _analyzerManager);
                driver.Initialize(compilation, _analysisOptions, compilationData, categorizeDiagnostics, cancellationToken);
                var analysisScope = new AnalysisScope(compilation, analyzers, concurrentAnalysis: _analysisOptions.ConcurrentAnalysis, categorizeDiagnostics: categorizeDiagnostics);
                driver.AttachQueueAndStartProcessingEvents(compilation.EventQueue, analysisScope, cancellationToken);

                // Force compilation diagnostics and wait for analyzer execution to complete.
                var compDiags = compilation.GetDiagnostics(cancellationToken);
                var analyzerDiags = await driver.GetDiagnosticsAsync(compilation).ConfigureAwait(false);
                var reportedDiagnostics = compDiags.AddRange(analyzerDiags);
                return driver.ApplyProgrammaticSuppressions(reportedDiagnostics, compilation);
            }
            finally
            {
                driver?.Dispose();
                FreeEventQueue(eventQueue, _eventQueuePool);
            }
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
            try
            {
                VerifyTree(tree);
                VerifyExistingAnalyzersArgument(analyzers);

                return await GetAnalyzerSyntaxDiagnosticsCoreAsync(tree, analyzers, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private async Task<ImmutableArray<Diagnostic>> GetAnalyzerSyntaxDiagnosticsCoreAsync(SyntaxTree tree, ImmutableArray<DiagnosticAnalyzer> analyzers, CancellationToken cancellationToken)
        {
            try
            {
                var taskToken = Interlocked.Increment(ref _currentToken);

                var analysisScope = new AnalysisScope(analyzers, tree, filterSpan: null, syntaxAnalysis: true, concurrentAnalysis: _analysisOptions.ConcurrentAnalysis, categorizeDiagnostics: true);

                var pendingAnalyzers = _analysisResultBuilder.GetPendingAnalyzers(analyzers);
                if (pendingAnalyzers.Length > 0)
                {
                    var pendingAnalysisScope = pendingAnalyzers.Length < analyzers.Length ? analysisScope.WithAnalyzers(pendingAnalyzers) : analysisScope;

                    // Compute the analyzer diagnostics for the pending analysis scope.
                    await ComputeAnalyzerDiagnosticsAsync(pendingAnalysisScope, getPendingEventsOpt: null, taskToken, cancellationToken).ConfigureAwait(false);
                }

                // Return computed analyzer diagnostics for the given analysis scope.
                return _analysisResultBuilder.GetDiagnostics(analysisScope, getLocalDiagnostics: true, getNonLocalDiagnostics: false);
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        /// <summary>
        /// Returns semantic diagnostics produced by all <see cref="Analyzers"/> from analyzing the given <paramref name="model"/>, optionally scoped to a <paramref name="filterSpan"/>.
        /// Depending on analyzers' behavior, returned diagnostics can have locations outside the tree,
        /// and some diagnostics that would be reported for the tree by an analysis of the complete compilation
        /// can be absent.
        /// </summary>
        /// <param name="model">Semantic model representing the syntax tree to analyze.</param>
        /// <param name="filterSpan">An optional span within the tree to scope analysis.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public async Task<ImmutableArray<Diagnostic>> GetAnalyzerSemanticDiagnosticsAsync(SemanticModel model, TextSpan? filterSpan, CancellationToken cancellationToken)
        {
            try
            {
                VerifyModel(model);

                return await GetAnalyzerSemanticDiagnosticsCoreAsync(model, filterSpan, Analyzers, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        /// <summary>
        /// Returns semantic diagnostics produced by all <see cref="Analyzers"/> from analyzing the given <paramref name="model"/>, optionally scoped to a <paramref name="filterSpan"/>.
        /// Depending on analyzers' behavior, returned diagnostics can have locations outside the tree,
        /// and some diagnostics that would be reported for the tree by an analysis of the complete compilation
        /// can be absent.
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

        private async Task<ImmutableArray<Diagnostic>> GetAnalyzerSemanticDiagnosticsCoreAsync(SemanticModel model, TextSpan? filterSpan, ImmutableArray<DiagnosticAnalyzer> analyzers, CancellationToken cancellationToken, bool forceCompletePartialTrees = true)
        {
            try
            {
                var taskToken = Interlocked.Increment(ref _currentToken);

                var analysisScope = new AnalysisScope(analyzers, model.SyntaxTree, filterSpan, syntaxAnalysis: false, concurrentAnalysis: _analysisOptions.ConcurrentAnalysis, categorizeDiagnostics: true);

                var pendingAnalyzers = _analysisResultBuilder.GetPendingAnalyzers(analyzers);
                if (pendingAnalyzers.Length > 0)
                {
                    var pendingAnalysisScope = pendingAnalyzers.Length < analyzers.Length ? analysisScope.WithAnalyzers(pendingAnalyzers) : analysisScope;

                    Func<ImmutableArray<CompilationEvent>> getPendingEvents = () => _analysisState.GetPendingEvents(analyzers, model.SyntaxTree, cancellationToken);

                    // Compute the analyzer diagnostics for the given analysis scope.
                    // We need to loop till symbol analysis is complete for any partial symbols being processed for other tree diagnostic requests.
                    do
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        (ImmutableArray<CompilationEvent> compilationEvents, bool hasSymbolStartActions) = await ComputeAnalyzerDiagnosticsAsync(pendingAnalysisScope, getPendingEvents, taskToken, cancellationToken).ConfigureAwait(false);
                        if (hasSymbolStartActions && forceCompletePartialTrees)
                        {
                            await processPartialSymbolLocationsAsync(compilationEvents, analysisScope).ConfigureAwait(false);
                        }
                    } while (_analysisOptions.ConcurrentAnalysis && _analysisState.HasPendingSymbolAnalysis(pendingAnalysisScope, cancellationToken));

                    if (_analysisOptions.ConcurrentAnalysis)
                    {
                        // Wait for all active tree tasks as they might still be reporting diagnostics for partial symbols defined in this tree.
                        await WaitForActiveAnalysisTasksAsync(waitForTreeTasks: true, waitForCompilationOrNonConcurrentTask: false, cancellationToken: cancellationToken).ConfigureAwait(false);
                    }
                }

                // Return computed analyzer diagnostics for the given analysis scope.
                return _analysisResultBuilder.GetDiagnostics(analysisScope, getLocalDiagnostics: true, getNonLocalDiagnostics: false);
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }

            async Task processPartialSymbolLocationsAsync(ImmutableArray<CompilationEvent> compilationEvents, AnalysisScope analysisScope)
            {
                if (compilationEvents.IsDefaultOrEmpty)
                {
                    return;
                }

                if (analysisScope.FilterSpanOpt.HasValue && !analysisScope.ContainsSpan(model.SyntaxTree.GetRoot(cancellationToken).Span))
                {
                    // Analyzing part of the tree.
                    return;
                }

                HashSet<SyntaxTree> partialTrees = null;
                foreach (var compilationEvent in compilationEvents)
                {
                    // Force complete partial declarations, except namespace declarations.
                    if (compilationEvent is SymbolDeclaredCompilationEvent symbolDeclaredEvent &&
                        symbolDeclaredEvent.Symbol.Kind != SymbolKind.Namespace &&
                        symbolDeclaredEvent.Symbol.Locations.Length > 1)
                    {
                        foreach (var location in symbolDeclaredEvent.Symbol.Locations)
                        {
                            if (location.SourceTree != null && location.SourceTree != model.SyntaxTree)
                            {
                                partialTrees = partialTrees ?? new HashSet<SyntaxTree>();
                                partialTrees.Add(location.SourceTree);
                            }
                        }
                    }
                }

                if (partialTrees != null)
                {
                    if (AnalysisOptions.ConcurrentAnalysis)
                    {
                        await Task.WhenAll(partialTrees.Select(tree =>
                            Task.Run(() =>
                            {
                                var treeModel = _compilationData.GetOrCreateCachedSemanticModel(tree, _compilation, cancellationToken);
                                return GetAnalyzerSemanticDiagnosticsCoreAsync(treeModel, filterSpan: null, analysisScope.Analyzers, cancellationToken, forceCompletePartialTrees: false);
                            }, cancellationToken))).ConfigureAwait(false);
                    }
                    else
                    {
                        foreach (var tree in partialTrees)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            var treeModel = _compilationData.GetOrCreateCachedSemanticModel(tree, _compilation, cancellationToken);
                            await GetAnalyzerSemanticDiagnosticsCoreAsync(treeModel, filterSpan: null, analysisScope.Analyzers, cancellationToken, forceCompletePartialTrees: false).ConfigureAwait(false);
                        }
                    }
                }
            }
        }

        private async Task<(ImmutableArray<CompilationEvent> events, bool hasSymbolStartActions)> ComputeAnalyzerDiagnosticsAsync(AnalysisScope analysisScope, Func<ImmutableArray<CompilationEvent>> getPendingEventsOpt, int newTaskToken, CancellationToken cancellationToken)
        {
            try
            {
                AnalyzerDriver driver = null;
                Task computeTask = null;
                CancellationTokenSource cts;

                try
                {
                    // Get the analyzer driver to execute analysis.
                    driver = await GetAnalyzerDriverAsync(cancellationToken).ConfigureAwait(false);

                    // Driver must have been initialized.
                    Debug.Assert(driver.WhenInitializedTask != null);
                    Debug.Assert(!driver.WhenInitializedTask.IsCanceled);

                    cancellationToken.ThrowIfCancellationRequested();

                    await GenerateCompilationEventsAndPopulateEventsCacheAsync(analysisScope, driver, cancellationToken).ConfigureAwait(false);

                    // Track if this task was suspended by another tree diagnostics request for the same tree.
                    // If so, we wait for the high priority requests to complete before restarting analysis.
                    bool suspended;
                    var pendingEvents = ImmutableArray<CompilationEvent>.Empty;
                    do
                    {
                        suspended = false;

                        // Create a new cancellation source to allow higher priority requests to suspend our analysis.
                        using (cts = new CancellationTokenSource())
                        {
                            // Link the cancellation source with client supplied cancellation source, so the public API callee can also cancel analysis.
                            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cts.Token, cancellationToken))
                            {
                                try
                                {
                                    // Fetch the cancellation token here to avoid capturing linkedCts in the getComputeTask lambda as the task may run after linkedCts has been disposed due to cancellation.
                                    var linkedCancellationToken = linkedCts.Token;

                                    // Core task to compute analyzer diagnostics.
                                    Func<Tuple<Task, CancellationTokenSource>> getComputeTask = () => Tuple.Create(
                                        Task.Run(async () =>
                                        {
                                            try
                                            {
                                                AsyncQueue<CompilationEvent> eventQueue = s_EmptyEventQueue;
                                                try
                                                {
                                                    // Get event queue with pending events to analyze.
                                                    if (getPendingEventsOpt != null)
                                                    {
                                                        pendingEvents = getPendingEventsOpt();
                                                        eventQueue = CreateEventsQueue(pendingEvents);
                                                    }

                                                    linkedCancellationToken.ThrowIfCancellationRequested();

                                                    // Execute analyzer driver on the given analysis scope with the given event queue.
                                                    await ComputeAnalyzerDiagnosticsCoreAsync(driver, eventQueue, analysisScope, cancellationToken: linkedCancellationToken).ConfigureAwait(false);
                                                }
                                                finally
                                                {
                                                    FreeEventQueue(eventQueue, _eventQueuePool);
                                                }
                                            }
                                            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
                                            {
                                                throw ExceptionUtilities.Unreachable;
                                            }
                                        },
                                            linkedCancellationToken),
                                        cts);

                                    // Wait for higher priority tree document tasks to complete.
                                    computeTask = await SetActiveAnalysisTaskAsync(getComputeTask, analysisScope.FilterTreeOpt, newTaskToken, cancellationToken).ConfigureAwait(false);

                                    cancellationToken.ThrowIfCancellationRequested();

                                    await computeTask.ConfigureAwait(false);
                                }
                                catch (OperationCanceledException)
                                {
                                    cancellationToken.ThrowIfCancellationRequested();
                                    if (!cts.IsCancellationRequested)
                                    {
                                        throw;
                                    }

                                    suspended = true;
                                }
                                finally
                                {
                                    ClearExecutingTask(computeTask, analysisScope.FilterTreeOpt);
                                    computeTask = null;
                                }
                            }
                        }
                    } while (suspended);

                    return (pendingEvents, hasSymbolStartActions: driver?.HasSymbolStartedActions(analysisScope) ?? false);
                }
                finally
                {
                    FreeDriver(driver);
                }
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private async Task GenerateCompilationEventsAndPopulateEventsCacheAsync(AnalysisScope analysisScope, AnalyzerDriver driver, CancellationToken cancellationToken)
        {
            GenerateCompilationEvents(analysisScope, cancellationToken);
            await PopulateEventsCacheAsync(cancellationToken).ConfigureAwait(false);
        }

        private void GenerateCompilationEvents(AnalysisScope analysisScope, CancellationToken cancellationToken)
        {
            // Invoke GetDiagnostics to populate CompilationEvent queue for the given analysis scope.
            // Discard the returned diagnostics.
            if (analysisScope.FilterTreeOpt == null)
            {
                var unused = _compilation.GetDiagnostics(cancellationToken);
            }
            else if (!analysisScope.IsSyntaxOnlyTreeAnalysis)
            {
                var mappedModel = _compilationData.GetOrCreateCachedSemanticModel(analysisScope.FilterTreeOpt, _compilation, cancellationToken);
                var unused = mappedModel.GetDiagnostics(cancellationToken: cancellationToken);
            }
        }

        private async Task PopulateEventsCacheAsync(CancellationToken cancellationToken)
        {
            if (_compilation.EventQueue.Count > 0)
            {
                AnalyzerDriver driver = null;
                try
                {
                    driver = await GetAnalyzerDriverAsync(cancellationToken).ConfigureAwait(false);
                    cancellationToken.ThrowIfCancellationRequested();

                    var compilationEvents = DequeueGeneratedCompilationEvents();
                    await _analysisState.OnCompilationEventsGeneratedAsync(compilationEvents, driver, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    FreeDriver(driver);
                }
            }
        }

        private ImmutableArray<CompilationEvent> DequeueGeneratedCompilationEvents()
        {
            var builder = ImmutableArray.CreateBuilder<CompilationEvent>();

            CompilationEvent compilationEvent;
            while (_compilation.EventQueue.TryDequeue(out compilationEvent))
            {
                builder.Add(compilationEvent);
            }

            return builder.ToImmutable();
        }

        private async Task<AnalyzerDriver> GetAnalyzerDriverAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Get instance of analyzer driver from the driver pool.
            AnalyzerDriver driver = _driverPool.Allocate();

            try
            {
                // Start the initialization task, if required.
                if (driver.WhenInitializedTask == null)
                {
                    driver.Initialize(_compilation, _analysisOptions, _compilationData, categorizeDiagnostics: true, cancellationToken: cancellationToken);
                }

                // Wait for driver initialization to complete: this executes the Initialize and CompilationStartActions to compute all registered actions per-analyzer.
                await driver.WhenInitializedTask.ConfigureAwait(false);

                return driver;
            }
            catch (OperationCanceledException)
            {
                FreeDriver(driver);
                throw;
            }
        }

        private void FreeDriver(AnalyzerDriver driver)
        {
            if (driver != null)
            {
                // Throw away the driver instance if the initialization didn't succeed.
                if (driver.WhenInitializedTask == null || driver.WhenInitializedTask.IsCanceled)
                {
                    _driverPool.ForgetTrackedObject(driver);
                }
                else
                {
                    _driverPool.Free(driver);
                }
            }
        }

        /// <summary>
        /// Core method for executing analyzers.
        /// </summary>
        private async Task ComputeAnalyzerDiagnosticsCoreAsync(AnalyzerDriver driver, AsyncQueue<CompilationEvent> eventQueue, AnalysisScope analysisScope, CancellationToken cancellationToken)
        {
            try
            {
                Debug.Assert(!driver.WhenInitializedTask.IsCanceled);

                if (eventQueue.Count > 0 || _analysisState.HasPendingSyntaxAnalysis(analysisScope))
                {
                    try
                    {
                        // Perform analysis to compute new diagnostics.
                        Debug.Assert(!eventQueue.IsCompleted);
                        await driver.AttachQueueAndProcessAllEventsAsync(eventQueue, analysisScope, _analysisState, cancellationToken: cancellationToken).ConfigureAwait(false);
                    }
                    finally
                    {
                        // Update the diagnostic results based on the diagnostics reported on the driver.
                        _analysisResultBuilder.ApplySuppressionsAndStoreAnalysisResult(analysisScope, driver, _compilation, _analysisState.GetAnalyzerActionCounts, fullAnalysisResultForAnalyzersInScope: false);
                    }
                }
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private Task<Task> SetActiveAnalysisTaskAsync(Func<Tuple<Task, CancellationTokenSource>> getNewAnalysisTask, SyntaxTree treeOpt, int newTaskToken, CancellationToken cancellationToken)
        {
            if (treeOpt != null)
            {
                return SetActiveTreeAnalysisTaskAsync(getNewAnalysisTask, treeOpt, newTaskToken, cancellationToken);
            }
            else
            {
                return SetActiveCompilationAnalysisTaskAsync(getNewAnalysisTask, cancellationToken);
            }
        }

        private async Task<Task> SetActiveCompilationAnalysisTaskAsync(Func<Tuple<Task, CancellationTokenSource>> getNewCompilationTask, CancellationToken cancellationToken)
        {
            while (true)
            {
                // Wait for all active tasks, compilation analysis tasks have lowest priority.
                await WaitForActiveAnalysisTasksAsync(waitForTreeTasks: true, waitForCompilationOrNonConcurrentTask: true, cancellationToken: cancellationToken).ConfigureAwait(false);

                lock (_executingTasksLock)
                {
                    if ((_executingConcurrentTreeTasksOpt == null || _executingConcurrentTreeTasksOpt.Count == 0) &&
                        _executingCompilationOrNonConcurrentTreeTask == null)
                    {
                        _executingCompilationOrNonConcurrentTreeTask = getNewCompilationTask();
                        return _executingCompilationOrNonConcurrentTreeTask.Item1;
                    }
                }
            }
        }

        private async Task WaitForActiveAnalysisTasksAsync(bool waitForTreeTasks, bool waitForCompilationOrNonConcurrentTask, CancellationToken cancellationToken)
        {
            Debug.Assert(waitForTreeTasks || waitForCompilationOrNonConcurrentTask);

            var executingTasks = ArrayBuilder<Tuple<Task, CancellationTokenSource>>.GetInstance();

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                lock (_executingTasksLock)
                {
                    if (waitForTreeTasks && _executingConcurrentTreeTasksOpt?.Count > 0)
                    {
                        executingTasks.AddRange(_executingConcurrentTreeTasksOpt.Values);
                    }

                    if (waitForCompilationOrNonConcurrentTask && _executingCompilationOrNonConcurrentTreeTask != null)
                    {
                        executingTasks.Add(_executingCompilationOrNonConcurrentTreeTask);
                    }
                }

                if (executingTasks.Count == 0)
                {
                    executingTasks.Free();
                    return;
                }

                foreach (var task in executingTasks)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await task.Item1.ConfigureAwait(false);
                }

                executingTasks.Clear();
            }
        }

        private async Task<Task> SetActiveTreeAnalysisTaskAsync(Func<Tuple<Task, CancellationTokenSource>> getNewTreeAnalysisTask, SyntaxTree tree, int newTaskToken, CancellationToken cancellationToken)
        {
            try
            {
                while (true)
                {
                    // For concurrent analysis, we must wait for any executing tree task with higher tokens.
                    Tuple<Task, CancellationTokenSource> executingTreeTask = null;

                    lock (_executingTasksLock)
                    {
                        if (!_analysisOptions.ConcurrentAnalysis)
                        {
                            // For non-concurrent analysis, just suspend the executing task, if any.
                            if (_executingCompilationOrNonConcurrentTreeTask != null)
                            {
                                SuspendAnalysis_NoLock(_executingCompilationOrNonConcurrentTreeTask.Item1, _executingCompilationOrNonConcurrentTreeTask.Item2);
                                _executingCompilationOrNonConcurrentTreeTask = null;
                            }

                            var newTask = getNewTreeAnalysisTask();
                            _executingCompilationOrNonConcurrentTreeTask = newTask;
                            return newTask.Item1;
                        }

                        Debug.Assert(_executingConcurrentTreeTasksOpt != null);
                        Debug.Assert(_concurrentTreeTaskTokensOpt != null);

                        if (!_executingConcurrentTreeTasksOpt.TryGetValue(tree, out executingTreeTask) ||
                            _concurrentTreeTaskTokensOpt[executingTreeTask.Item1] < newTaskToken)
                        {
                            if (executingTreeTask != null)
                            {
                                SuspendAnalysis_NoLock(executingTreeTask.Item1, executingTreeTask.Item2);
                            }

                            if (_executingCompilationOrNonConcurrentTreeTask != null)
                            {
                                SuspendAnalysis_NoLock(_executingCompilationOrNonConcurrentTreeTask.Item1, _executingCompilationOrNonConcurrentTreeTask.Item2);
                                _executingCompilationOrNonConcurrentTreeTask = null;
                            }

                            var newTask = getNewTreeAnalysisTask();
                            _concurrentTreeTaskTokensOpt[newTask.Item1] = newTaskToken;
                            _executingConcurrentTreeTasksOpt[tree] = newTask;
                            return newTask.Item1;
                        }
                    }

                    await executingTreeTask.Item1.ConfigureAwait(false);
                }
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private void SuspendAnalysis_NoLock(Task computeTask, CancellationTokenSource cts)
        {
            if (!computeTask.IsCompleted)
            {
                // Suspend analysis.
                cts.Cancel();
            }
        }

        private void ClearExecutingTask(Task computeTask, SyntaxTree treeOpt)
        {
            if (computeTask != null)
            {
                lock (_executingTasksLock)
                {
                    Tuple<Task, CancellationTokenSource> executingTask;
                    if (treeOpt != null && _analysisOptions.ConcurrentAnalysis)
                    {
                        Debug.Assert(_executingConcurrentTreeTasksOpt != null);
                        Debug.Assert(_concurrentTreeTaskTokensOpt != null);

                        if (_executingConcurrentTreeTasksOpt.TryGetValue(treeOpt, out executingTask) &&
                            executingTask.Item1 == computeTask)
                        {
                            _executingConcurrentTreeTasksOpt.Remove(treeOpt);
                        }

                        if (_concurrentTreeTaskTokensOpt.ContainsKey(computeTask))
                        {
                            _concurrentTreeTaskTokensOpt.Remove(computeTask);
                        }
                    }
                    else if (_executingCompilationOrNonConcurrentTreeTask?.Item1 == computeTask)
                    {
                        _executingCompilationOrNonConcurrentTreeTask = null;
                    }
                }
            }
        }

        private AsyncQueue<CompilationEvent> CreateEventsQueue(ImmutableArray<CompilationEvent> compilationEvents)
        {
            if (compilationEvents.IsEmpty)
            {
                return s_EmptyEventQueue;
            }

            var eventQueue = _eventQueuePool.Allocate();
            Debug.Assert(!eventQueue.IsCompleted);
            Debug.Assert(eventQueue.Count == 0);

            foreach (var compilationEvent in compilationEvents)
            {
                eventQueue.TryEnqueue(compilationEvent);
            }

            return eventQueue;
        }

        private static void FreeEventQueue(AsyncQueue<CompilationEvent> eventQueue, ObjectPool<AsyncQueue<CompilationEvent>> eventQueuePool)
        {
            if (eventQueue == null || ReferenceEquals(eventQueue, s_EmptyEventQueue))
            {
                return;
            }

            if (eventQueue.Count > 0)
            {
                CompilationEvent discarded;
                while (eventQueue.TryDequeue(out discarded)) ;
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
            var suppressMessageState = new SuppressMessageAttributeState(compilation);
            foreach (var diagnostic in diagnostics)
            {
                if (diagnostic != null)
                {
                    var effectiveDiagnostic = compilation.Options.FilterDiagnostic(diagnostic);
                    if (effectiveDiagnostic != null)
                    {
                        yield return suppressMessageState.ApplySourceSuppressions(effectiveDiagnostic);
                    }
                }
            }
        }

        /// <summary>
        /// Returns true if all the diagnostics that can be produced by this analyzer are suppressed through options.
        /// <param name="analyzer">Analyzer to be checked for suppression.</param>
        /// <param name="options">Compilation options.</param>
        /// <param name="onAnalyzerException">
        /// Optional delegate which is invoked when an analyzer throws an exception.
        /// Delegate can do custom tasks such as report the given analyzer exception diagnostic, report a non-fatal watson for the exception, etc.
        /// </param>
        /// </summary>
        public static bool IsDiagnosticAnalyzerSuppressed(
            DiagnosticAnalyzer analyzer,
            CompilationOptions options,
            Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException = null)
        {
            VerifyAnalyzerArgumentForStaticApis(analyzer);

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var analyzerManager = new AnalyzerManager(analyzer);
            var analyzerExecutor = AnalyzerExecutor.CreateForSupportedDiagnostics(onAnalyzerException, analyzerManager);
            return AnalyzerDriver.IsDiagnosticAnalyzerSuppressed(analyzer, options, analyzerManager, analyzerExecutor);
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
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        /// <summary>
        /// Gets the count of registered actions for the analyzer.
        /// </summary>
        private async Task<AnalyzerActionCounts> GetAnalyzerActionCountsAsync(DiagnosticAnalyzer analyzer, CancellationToken cancellationToken)
        {
            AnalyzerDriver driver = null;
            try
            {
                driver = await GetAnalyzerDriverAsync(cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                return await _analysisState.GetOrComputeAnalyzerActionCountsAsync(analyzer, driver, cancellationToken).ConfigureAwait(false);
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
                return default(TimeSpan);
            }

            return _analysisResultBuilder.GetAnalyzerExecutionTime(analyzer);
        }
    }
}
