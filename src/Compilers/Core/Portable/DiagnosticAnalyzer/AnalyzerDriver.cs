// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Semantics;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Driver to execute diagnostic analyzers for a given compilation.
    /// It uses a <see cref="AsyncQueue{TElement}"/> of <see cref="CompilationEvent"/>s to drive its analysis.
    /// </summary>
    internal abstract partial class AnalyzerDriver : IDisposable
    {
        // Protect against vicious analyzers that provide large values for SymbolKind.
        private const int MaxSymbolKind = 100;

        protected readonly ImmutableArray<DiagnosticAnalyzer> analyzers;
        protected readonly AnalyzerManager analyzerManager;

        // Lazy fields
        private CancellationTokenRegistration _queueRegistration;
        protected AnalyzerExecutor analyzerExecutor;
        internal AnalyzerActions analyzerActions;
        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<ImmutableArray<SymbolAnalyzerAction>>> _symbolActionsByKind;
        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<SemanticModelAnalyzerAction>> _semanticModelActionsMap;
        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<SyntaxTreeAnalyzerAction>> _syntaxTreeActionsMap;
        // Compilation actions and compilation end actions have separate maps so that it is easy to
        // execute the compilation actions before the compilation end actions.
        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<CompilationAnalyzerAction>> _compilationActionsMap;
        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<CompilationAnalyzerAction>> _compilationEndActionsMap;

        /// <summary>
        /// Map from non-concurrent analyzers to the gate guarding callback into the analyzer. 
        /// </summary>
        private ImmutableDictionary<DiagnosticAnalyzer, SemaphoreSlim> _analyzerGateMap = ImmutableDictionary<DiagnosticAnalyzer, SemaphoreSlim>.Empty;

        /// <summary>
        /// Driver task which initializes all analyzers.
        /// This task is initialized and executed only once at start of analysis.
        /// </summary>
        private Task _initializeTask;

        /// <summary>
        /// Flag to indicate if the <see cref="_initializeTask"/> was successfully started.
        /// </summary>
        private bool _initializeSucceeded = false;

        /// <summary>
        /// Primary driver task which processes all <see cref="CompilationEventQueue"/> events, runs analyzer actions and signals completion of <see cref="DiagnosticQueue"/> at the end.
        /// </summary>
        private Task _primaryTask;

        /// <summary>
        /// Number of worker tasks processing compilation events and executing analyzer actions.
        /// </summary>
        private readonly int _workerCount = Environment.ProcessorCount;

        /// <summary>
        /// Events queue for analyzer execution.
        /// </summary>
        public AsyncQueue<CompilationEvent> CompilationEventQueue { get; private set; }

        /// <summary>
        /// <see cref="DiagnosticQueue"/> that is fed the diagnostics as they are computed.
        /// </summary>
        public DiagnosticQueue DiagnosticQueue { get; private set; }

        /// <summary>
        /// Create an analyzer driver.
        /// </summary>
        /// <param name="analyzers">The set of analyzers to include in the analysis</param>
        /// <param name="analyzerManager">AnalyzerManager to manage analyzers for analyzer host's lifetime.</param>
        protected AnalyzerDriver(ImmutableArray<DiagnosticAnalyzer> analyzers, AnalyzerManager analyzerManager)
        {
            this.analyzers = analyzers;
            this.analyzerManager = analyzerManager;
        }

        /// <summary>
        /// Initializes the <see cref="analyzerActions"/> and related actions maps for the analyzer driver.
        /// It kicks off the <see cref="WhenInitializedTask"/> task for initialization.
        /// Note: This method must be invoked exactly once on the driver.
        /// </summary>
        private void Initialize(AnalyzerExecutor analyzerExecutor, DiagnosticQueue diagnosticQueue, CancellationToken cancellationToken)
        {
            try
            {
                Debug.Assert(_initializeTask == null);

                this.analyzerExecutor = analyzerExecutor;
                this.DiagnosticQueue = diagnosticQueue;

                // Compute the set of effective actions based on suppression, and running the initial analyzers
                var analyzerActionsTask = GetAnalyzerActionsAsync(analyzers, analyzerManager, analyzerExecutor);
                _initializeTask = analyzerActionsTask.ContinueWith(t =>
                {
                    this.analyzerActions = t.Result.Item1;
                    this._analyzerGateMap = t.Result.Item2;
                    _symbolActionsByKind = MakeSymbolActionsByKind();
                    _semanticModelActionsMap = MakeSemanticModelActionsByAnalyzer();
                    _syntaxTreeActionsMap = MakeSyntaxTreeActionsByAnalyzer();
                    _compilationActionsMap = MakeCompilationActionsByAnalyzer(this.analyzerActions.CompilationActions);
                    _compilationEndActionsMap = MakeCompilationActionsByAnalyzer(this.analyzerActions.CompilationEndActions);
                }, cancellationToken, TaskContinuationOptions.None, TaskScheduler.Default);

                // create the primary driver task.
                cancellationToken.ThrowIfCancellationRequested();

                _initializeSucceeded = true;
            }
            finally
            {
                if (_initializeTask == null)
                {
                    // Set initializeTask to be a cancelled task.
                    var tcs = new TaskCompletionSource<int>();
                    tcs.SetCanceled();
                    _initializeTask = tcs.Task;

                    // Set primaryTask to be a cancelled task.
                    tcs = new TaskCompletionSource<int>();
                    tcs.SetCanceled();
                    _primaryTask = tcs.Task;

                    // Try to set the DiagnosticQueue to be complete.
                    this.DiagnosticQueue.TryComplete();
                }
            }
        }

        internal void Initialize(
           Compilation compilation,
           CompilationWithAnalyzersOptions analysisOptions,
           bool categorizeDiagnostics,
           CancellationToken cancellationToken)
        {
            Debug.Assert(_initializeTask == null);

            var diagnosticQueue = DiagnosticQueue.Create(categorizeDiagnostics);
            var addDiagnostic = GetDiagnosticSink(diagnosticQueue.Enqueue, compilation);
            var addLocalDiagnosticOpt = categorizeDiagnostics ? GetDiagnosticSink(diagnosticQueue.EnqueueLocal, compilation) : null;
            var addNonLocalDiagnosticOpt = categorizeDiagnostics ? GetDiagnosticSink(diagnosticQueue.EnqueueNonLocal, compilation) : null;

            Action<Exception, DiagnosticAnalyzer, Diagnostic> newOnAnalyzerException;
            if (analysisOptions.OnAnalyzerException != null)
            {
                // Wrap onAnalyzerException to pass in filtered diagnostic.
                var comp = compilation;
                newOnAnalyzerException = (ex, analyzer, diagnostic) =>
                    analysisOptions.OnAnalyzerException(ex, analyzer, GetFilteredDiagnostic(diagnostic, comp));
            }
            else
            {
                // Add exception diagnostic to regular diagnostic bag.
                newOnAnalyzerException = (ex, analyzer, diagnostic) => addDiagnostic(diagnostic);
            }

            if (analysisOptions.LogAnalyzerExecutionTime)
            {
                // If we are reporting detailed analyzer performance numbers, then do a dummy invocation of Compilation.GetTypeByMetadataName API upfront.
                // This API seems to cause a severe hit for the first analyzer invoking it and hence introduces lot of noise in the computed analyzer execution times.
                var unused = compilation.GetTypeByMetadataName("System.Object");
            }

            var analyzerExecutor = AnalyzerExecutor.Create(compilation, analysisOptions.Options ?? AnalyzerOptions.Empty, addDiagnostic, newOnAnalyzerException, IsCompilerAnalyzer,
                analyzerManager, GetAnalyzerGate, analysisOptions.LogAnalyzerExecutionTime, addLocalDiagnosticOpt, addNonLocalDiagnosticOpt, cancellationToken);

            Initialize(analyzerExecutor, diagnosticQueue, cancellationToken);
        }

        private SemaphoreSlim GetAnalyzerGate(DiagnosticAnalyzer analyzer)
        {
            SemaphoreSlim gate;
            if (_analyzerGateMap.TryGetValue(analyzer, out gate))
            {
                // Non-concurrent analyzer, needs all the callbacks guarded by a gate.
                Debug.Assert(gate != null);
                return gate;
            }

            // Concurrent analyzer.
            return null;
        }

        /// <summary>
        /// Attaches a pre-populated event queue to the driver and processes all events in the queue.
        /// </summary>
        /// <param name="eventQueue">Compilation events to analyze.</param>
        /// <param name="analysisScope">Scope of analysis.</param>
        /// <param name="analysisStateOpt">An optional object to track partial analysis state.</param>
        /// <param name="cancellationToken">Cancellation token to abort analysis.</param>
        /// <remarks>Driver must be initialized before invoking this method, i.e. <see cref="Initialize(AnalyzerExecutor, DiagnosticQueue, CancellationToken)"/> method must have been invoked and <see cref="WhenInitializedTask"/> must be non-null.</remarks>
        internal async Task AttachQueueAndProcessAllEventsAsync(AsyncQueue<CompilationEvent> eventQueue, AnalysisScope analysisScope, AnalysisState analysisStateOpt, CancellationToken cancellationToken)
        {
            try
            {
                if (_initializeSucceeded)
                {
                    this.CompilationEventQueue = eventQueue;
                    _queueRegistration = default(CancellationTokenRegistration);

                    await ExecutePrimaryAnalysisTaskAsync(analysisScope, analysisStateOpt, usingPrePopulatedEventQueue: true, cancellationToken: cancellationToken).ConfigureAwait(false);

                    _primaryTask = Task.FromResult(true);
                }
            }
            finally
            {
                if (_primaryTask == null)
                {
                    // Set primaryTask to be a cancelled task.
                    var tcs = new TaskCompletionSource<int>();
                    tcs.SetCanceled();
                    _primaryTask = tcs.Task;
                }
            }
        }

        /// <summary>
        /// Attaches event queue to the driver and start processing all events pertaining to the given analysis scope.
        /// </summary>
        /// <param name="eventQueue">Compilation events to analyze.</param>
        /// <param name="analysisScope">Scope of analysis.</param>
        /// <param name="cancellationToken">Cancellation token to abort analysis.</param>
        /// <remarks>Driver must be initialized before invoking this method, i.e. <see cref="Initialize(AnalyzerExecutor, DiagnosticQueue, CancellationToken)"/> method must have been invoked and <see cref="WhenInitializedTask"/> must be non-null.</remarks>
        internal void AttachQueueAndStartProcessingEvents(AsyncQueue<CompilationEvent> eventQueue, AnalysisScope analysisScope, CancellationToken cancellationToken)
        {
            try
            {
                if (_initializeSucceeded)
                {
                    this.CompilationEventQueue = eventQueue;
                    _queueRegistration = cancellationToken.Register(() =>
                    {
                        this.CompilationEventQueue.TryComplete();
                        this.DiagnosticQueue.TryComplete();
                    });

                    _primaryTask = ExecutePrimaryAnalysisTaskAsync(analysisScope, analysisStateOpt: null, usingPrePopulatedEventQueue: false, cancellationToken: cancellationToken)
                        .ContinueWith(c => DiagnosticQueue.TryComplete(), cancellationToken, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                }
            }
            finally
            {
                if (_primaryTask == null)
                {
                    // Set primaryTask to be a cancelled task.
                    var tcs = new TaskCompletionSource<int>();
                    tcs.SetCanceled();
                    _primaryTask = tcs.Task;

                    // Try to set the DiagnosticQueue to be complete.
                    this.DiagnosticQueue.TryComplete();
                }
            }
        }

        private async Task ExecutePrimaryAnalysisTaskAsync(AnalysisScope analysisScope, AnalysisState analysisStateOpt, bool usingPrePopulatedEventQueue, CancellationToken cancellationToken)
        {
            Debug.Assert(analysisScope != null);
            Debug.Assert(WhenInitializedTask != null);

            await WhenInitializedTask.ConfigureAwait(false);

            if (WhenInitializedTask.IsFaulted)
            {
                OnDriverException(WhenInitializedTask, this.analyzerExecutor, analysisScope.Analyzers);
            }
            else if (!WhenInitializedTask.IsCanceled)
            {
                this.analyzerExecutor = this.analyzerExecutor.WithCancellationToken(cancellationToken);

                await ProcessCompilationEventsAsync(analysisScope, analysisStateOpt, usingPrePopulatedEventQueue, cancellationToken).ConfigureAwait(false);
            }
        }

        private static void OnDriverException(Task faultedTask, AnalyzerExecutor analyzerExecutor, ImmutableArray<DiagnosticAnalyzer> analyzers)
        {
            Debug.Assert(faultedTask.IsFaulted);

            var innerException = faultedTask.Exception?.InnerException;
            if (innerException == null || innerException is OperationCanceledException)
            {
                return;
            }

            var diagnostic = AnalyzerExecutor.CreateDriverExceptionDiagnostic(innerException);

            // Just pick the first analyzer from the scope for the onAnalyzerException callback.
            // The exception diagnostic's message and description will not include the analyzer, but explicitly state its a driver exception.
            var analyzer = analyzers[0];

            analyzerExecutor.OnAnalyzerException(innerException, analyzer, diagnostic);
        }

        private async Task ExecuteSyntaxTreeActionsAsync(AnalysisScope analysisScope, AnalysisState analysisStateOpt, CancellationToken cancellationToken)
        {
            if (analysisScope.IsTreeAnalysis && !analysisScope.IsSyntaxOnlyTreeAnalysis)
            {
                // For partial analysis, only execute syntax tree actions if performing syntax analysis.
                return;
            }

            foreach (var tree in analysisScope.SyntaxTrees)
            {
                foreach (var analyzer in analysisScope.Analyzers)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    ImmutableArray<SyntaxTreeAnalyzerAction> syntaxTreeActions;
                    if (_syntaxTreeActionsMap.TryGetValue(analyzer, out syntaxTreeActions))
                    {
                        // Execute actions for a given analyzer sequentially.
                        await analyzerExecutor.ExecuteSyntaxTreeActionsAsync(syntaxTreeActions, analyzer, tree, analysisScope, analysisStateOpt).ConfigureAwait(false);
                    }
                    else if (analysisStateOpt != null)
                    {
                        await analysisStateOpt.MarkSyntaxAnalysisCompleteAsync(tree, analyzer, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
        }

        /// <summary>
        /// Create an <see cref="AnalyzerDriver"/> and attach it to the given compilation. 
        /// </summary>
        /// <param name="compilation">The compilation to which the new driver should be attached.</param>
        /// <param name="analyzers">The set of analyzers to include in the analysis.</param>
        /// <param name="options">Options that are passed to analyzers.</param>
        /// <param name="analyzerManager">AnalyzerManager to manage analyzers for the lifetime of analyzer host.</param>
        /// <param name="addExceptionDiagnostic">Delegate to add diagnostics generated for exceptions from third party analyzers.</param>
        /// <param name="reportAnalyzer">Report additional information related to analyzers, such as analyzer execution time.</param>
        /// <param name="newCompilation">The new compilation with the analyzer driver attached.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to abort analysis.</param>
        /// <returns>A newly created analyzer driver</returns>
        /// <remarks>
        /// Note that since a compilation is immutable, the act of creating a driver and attaching it produces
        /// a new compilation. Any further actions on the compilation should use the new compilation.
        /// </remarks>
        public static AnalyzerDriver CreateAndAttachToCompilation(
            Compilation compilation,
            ImmutableArray<DiagnosticAnalyzer> analyzers,
            AnalyzerOptions options,
            AnalyzerManager analyzerManager,
            Action<Diagnostic> addExceptionDiagnostic,
            bool reportAnalyzer,
            out Compilation newCompilation,
            CancellationToken cancellationToken)
        {
            Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException =
                (ex, analyzer, diagnostic) => addExceptionDiagnostic?.Invoke(diagnostic);

            return CreateAndAttachToCompilation(compilation, analyzers, options, analyzerManager, onAnalyzerException, reportAnalyzer, out newCompilation, cancellationToken: cancellationToken);
        }

        // internal for testing purposes
        internal static AnalyzerDriver CreateAndAttachToCompilation(
            Compilation compilation,
            ImmutableArray<DiagnosticAnalyzer> analyzers,
            AnalyzerOptions options,
            AnalyzerManager analyzerManager,
            Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException,
            bool reportAnalyzer,
            out Compilation newCompilation,
            CancellationToken cancellationToken)
        {
            AnalyzerDriver analyzerDriver = compilation.AnalyzerForLanguage(analyzers, analyzerManager);
            newCompilation = compilation.WithEventQueue(new AsyncQueue<CompilationEvent>());

            var categorizeDiagnostics = false;
            var analysisOptions = new CompilationWithAnalyzersOptions(options, onAnalyzerException, concurrentAnalysis: true, logAnalyzerExecutionTime: reportAnalyzer);
            analyzerDriver.Initialize(newCompilation, analysisOptions, categorizeDiagnostics, cancellationToken);

            var analysisScope = new AnalysisScope(newCompilation, analyzers, concurrentAnalysis: newCompilation.Options.ConcurrentBuild, categorizeDiagnostics: categorizeDiagnostics);
            analyzerDriver.AttachQueueAndStartProcessingEvents(newCompilation.EventQueue, analysisScope, cancellationToken: cancellationToken);
            return analyzerDriver;
        }

        /// <summary>
        /// Returns all diagnostics computed by the analyzers since the last time this was invoked.
        /// If <see cref="CompilationEventQueue"/> has been completed with all compilation events, then it waits for
        /// <see cref="WhenCompletedTask"/> task for the driver to finish processing all events and generate remaining analyzer diagnostics.
        /// </summary>
        public async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(Compilation compilation)
        {
            var allDiagnostics = DiagnosticBag.GetInstance();
            if (CompilationEventQueue.IsCompleted)
            {
                await this.WhenCompletedTask.ConfigureAwait(false);

                if (this.WhenCompletedTask.IsFaulted)
                {
                    OnDriverException(this.WhenCompletedTask, this.analyzerExecutor, this.analyzers);
                }
            }

            var suppressMessageState = GetOrCreateCachedCompilationData(compilation).SuppressMessageAttributeState;
            var reportSuppressedDiagnostics = compilation.Options.ReportSuppressedDiagnostics;
            Diagnostic d;
            while (DiagnosticQueue.TryDequeue(out d))
            {
                d = SuppressMessageAttributeState.ApplySourceSuppressions(d, compilation);
                if (reportSuppressedDiagnostics || !d.IsSuppressed)
                {
                    allDiagnostics.Add(d);
                }
            }

            return allDiagnostics.ToReadOnlyAndFree();
        }

        public ImmutableArray<Diagnostic> DequeueLocalDiagnostics(DiagnosticAnalyzer analyzer, bool syntax, Compilation compilation)
        {
            var diagnostics = syntax ? DiagnosticQueue.DequeueLocalSyntaxDiagnostics(analyzer) : DiagnosticQueue.DequeueLocalSemanticDiagnostics(analyzer);
            return FilterDiagnosticsSuppressedInSource(diagnostics, compilation);
        }

        public ImmutableArray<Diagnostic> DequeueNonLocalDiagnostics(DiagnosticAnalyzer analyzer, Compilation compilation)
        {
            var diagnostics = DiagnosticQueue.DequeueNonLocalDiagnostics(analyzer);
            return FilterDiagnosticsSuppressedInSource(diagnostics, compilation);
        }

        private static ImmutableArray<Diagnostic> FilterDiagnosticsSuppressedInSource(ImmutableArray<Diagnostic> diagnostics, Compilation compilation)
        {
            if (diagnostics.IsEmpty)
            {
                return diagnostics;
            }

            var suppressMessageState = GetOrCreateCachedCompilationData(compilation).SuppressMessageAttributeState;
            var reportSuppressedDiagnostics = compilation.Options.ReportSuppressedDiagnostics;
            var builder = ImmutableArray.CreateBuilder<Diagnostic>();
            for (var i = 0; i < diagnostics.Length; i++)
            {
                var diagnostic = SuppressMessageAttributeState.ApplySourceSuppressions(diagnostics[i], compilation);
                if (reportSuppressedDiagnostics || !diagnostic.IsSuppressed)
                {
                    builder.Add(diagnostic);
                }
            }

            return builder.ToImmutable();
        }

        /// <summary>
        /// Return a task that completes when the driver is initialized.
        /// </summary>
        public Task WhenInitializedTask => _initializeTask;

        /// <summary>
        /// Return a task that completes when the driver is done producing diagnostics.
        /// </summary>
        public Task WhenCompletedTask => _primaryTask;

        internal ImmutableDictionary<DiagnosticAnalyzer, TimeSpan> AnalyzerExecutionTimes => analyzerExecutor.AnalyzerExecutionTimes;
        internal TimeSpan ResetAnalyzerExecutionTime(DiagnosticAnalyzer analyzer) => analyzerExecutor.ResetAnalyzerExecutionTime(analyzer);

        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<ImmutableArray<SymbolAnalyzerAction>>> MakeSymbolActionsByKind()
        {
            var builder = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, ImmutableArray<ImmutableArray<SymbolAnalyzerAction>>>();
            var actionsByAnalyzers = this.analyzerActions.SymbolActions.GroupBy(action => action.Analyzer);
            foreach (var analyzerAndActions in actionsByAnalyzers)
            {
                var actionsByKindBuilder = new List<ArrayBuilder<SymbolAnalyzerAction>>();
                foreach (var symbolAction in analyzerAndActions)
                {
                    var kinds = symbolAction.Kinds;
                    foreach (int kind in kinds.Distinct())
                    {
                        if (kind > MaxSymbolKind) continue; // protect against vicious analyzers
                        while (kind >= actionsByKindBuilder.Count)
                        {
                            actionsByKindBuilder.Add(ArrayBuilder<SymbolAnalyzerAction>.GetInstance());
                        }

                        actionsByKindBuilder[kind].Add(symbolAction);
                    }
                }

                var actionsByKind = actionsByKindBuilder.Select(a => a.ToImmutableAndFree()).ToImmutableArray();
                builder.Add(analyzerAndActions.Key, actionsByKind);
            }

            return builder.ToImmutable();
        }

        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<SyntaxTreeAnalyzerAction>> MakeSyntaxTreeActionsByAnalyzer()
        {
            var builder = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, ImmutableArray<SyntaxTreeAnalyzerAction>>();
            var actionsByAnalyzers = this.analyzerActions.SyntaxTreeActions.GroupBy(action => action.Analyzer);
            foreach (var analyzerAndActions in actionsByAnalyzers)
            {
                builder.Add(analyzerAndActions.Key, analyzerAndActions.ToImmutableArray());
            }

            return builder.ToImmutable();
        }

        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<SemanticModelAnalyzerAction>> MakeSemanticModelActionsByAnalyzer()
        {
            var builder = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, ImmutableArray<SemanticModelAnalyzerAction>>();
            var actionsByAnalyzers = this.analyzerActions.SemanticModelActions.GroupBy(action => action.Analyzer);
            foreach (var analyzerAndActions in actionsByAnalyzers)
            {
                builder.Add(analyzerAndActions.Key, analyzerAndActions.ToImmutableArray());
            }

            return builder.ToImmutable();
        }

        private static ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<CompilationAnalyzerAction>> MakeCompilationActionsByAnalyzer(ImmutableArray<CompilationAnalyzerAction> compilationActions)
        {
            var builder = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, ImmutableArray<CompilationAnalyzerAction>>();
            var actionsByAnalyzers = compilationActions.GroupBy(action => action.Analyzer);
            foreach (var analyzerAndActions in actionsByAnalyzers)
            {
                builder.Add(analyzerAndActions.Key, analyzerAndActions.ToImmutableArray());
            }

            return builder.ToImmutable();
        }

        private async Task ProcessCompilationEventsAsync(AnalysisScope analysisScope, AnalysisState analysisStateOpt, bool prePopulatedEventQueue, CancellationToken cancellationToken)
        {
            try
            {
                CompilationCompletedEvent completedEvent = null;

                if (analysisScope.ConcurrentAnalysis)
                {
                    // Kick off worker tasks to process all compilation events (except the compilation end event) in parallel.
                    // Compilation end event must be processed after all other events.

                    var workerCount = prePopulatedEventQueue ? Math.Min(CompilationEventQueue.Count, _workerCount) : _workerCount;

                    var workerTasks = new Task<CompilationCompletedEvent>[workerCount];
                    for (int i = 0; i < workerCount; i++)
                    {
                        workerTasks[i] = Task.Run(async() => await ProcessCompilationEventsCoreAsync(analysisScope, analysisStateOpt, prePopulatedEventQueue, cancellationToken).ConfigureAwait(false));
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    // Kick off tasks to execute syntax tree actions.
                    var syntaxTreeActionsTask = ExecuteSyntaxTreeActionsAsync(analysisScope, analysisStateOpt, cancellationToken);

                    // Wait for all worker threads to complete processing events.
                    await Task.WhenAll(workerTasks.Concat(syntaxTreeActionsTask)).ConfigureAwait(false);

                    for (int i = 0; i < workerCount; i++)
                    {
                        if (workerTasks[i].Status == TaskStatus.RanToCompletion && workerTasks[i].Result != null)
                        {
                            completedEvent = workerTasks[i].Result;
                            break;
                        }
                    }
                }
                else
                {
                    completedEvent = await ProcessCompilationEventsCoreAsync(analysisScope, analysisStateOpt, prePopulatedEventQueue, cancellationToken).ConfigureAwait(false);

                    await ExecuteSyntaxTreeActionsAsync(analysisScope, analysisStateOpt, cancellationToken).ConfigureAwait(false);
                }

                // Finally process the compilation completed event, if any.
                if (completedEvent != null)
                {
                    await ProcessEventAsync(completedEvent, analysisScope, analysisStateOpt, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private async Task<CompilationCompletedEvent> ProcessCompilationEventsCoreAsync(AnalysisScope analysisScope, AnalysisState analysisStateOpt, bool prePopulatedEventQueue, CancellationToken cancellationToken)
        {
            try
            {
                CompilationCompletedEvent completedEvent = null;

                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (CompilationEventQueue.Count == 0 &&
                        (prePopulatedEventQueue || CompilationEventQueue.IsCompleted))
                    {
                        break;
                    }

                    CompilationEvent e;
                    try
                    {
                        if (!prePopulatedEventQueue)
                        {
                            e = await CompilationEventQueue.DequeueAsync(cancellationToken).ConfigureAwait(false);
                        }
                        else if (!CompilationEventQueue.TryDequeue(out e))
                        {
                            return completedEvent;
                        }
                    }
                    catch (TaskCanceledException) when (!prePopulatedEventQueue)
                    {
                        // When the queue is completed with a pending DequeueAsync return then a 
                        // TaskCanceledException will be thrown.  This just signals the queue is 
                        // complete and we should finish processing it.

                        // This failure is being tracked by https://github.com/dotnet/roslyn/issues/5962
                        // Debug.Assert(CompilationEventQueue.IsCompleted, "DequeueAsync should never throw unless the AsyncQueue<T> is completed.");
                        break;
                    }

                    // Don't process the compilation completed event as other worker threads might still be processing other compilation events.
                    // The caller will wait for all workers to complete and finally process this event.
                    var compilationCompletedEvent = e as CompilationCompletedEvent;
                    if (compilationCompletedEvent != null)
                    {
                        completedEvent = compilationCompletedEvent;
                        continue;
                    }

                    await ProcessEventAsync(e, analysisScope, analysisStateOpt, cancellationToken).ConfigureAwait(false);
                }

                return completedEvent;
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private async Task ProcessEventAsync(CompilationEvent e, AnalysisScope analysisScope, AnalysisState analysisStateOpt, CancellationToken cancellationToken)
        {
            await ProcessEventCoreAsync(e, analysisScope, analysisStateOpt, cancellationToken).ConfigureAwait(false);

            if (analysisStateOpt != null)
            {
                await analysisStateOpt.OnCompilationEventProcessedAsync(e, analysisScope, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task ProcessEventCoreAsync(CompilationEvent e, AnalysisScope analysisScope, AnalysisState analysisStateOpt, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var symbolEvent = e as SymbolDeclaredCompilationEvent;
            if (symbolEvent != null)
            {
                await ProcessSymbolDeclaredAsync(symbolEvent, analysisScope, analysisStateOpt, cancellationToken).ConfigureAwait(false);
                return;
            }

            var completedEvent = e as CompilationUnitCompletedEvent;
            if (completedEvent != null)
            {
                await ProcessCompilationUnitCompletedAsync(completedEvent, analysisScope, analysisStateOpt, cancellationToken).ConfigureAwait(false);
                return;
            }

            var endEvent = e as CompilationCompletedEvent;
            if (endEvent != null)
            {
                await ProcessCompilationCompletedAsync(endEvent, analysisScope, analysisStateOpt, cancellationToken).ConfigureAwait(false);
                return;
            }

            var startedEvent = e as CompilationStartedEvent;
            if (startedEvent != null)
            {
                await ProcessCompilationStartedAsync(startedEvent, analysisScope, analysisStateOpt, cancellationToken).ConfigureAwait(false);
                return;
            }

            throw new InvalidOperationException("Unexpected compilation event of type " + e.GetType().Name);
        }

        private async Task ProcessSymbolDeclaredAsync(SymbolDeclaredCompilationEvent symbolEvent, AnalysisScope analysisScope, AnalysisState analysisStateOpt, CancellationToken cancellationToken)
        {
            try
            {
                // Execute all analyzer actions.
                var symbol = symbolEvent.Symbol;
                var references = symbolEvent.DeclaringSyntaxReferences;
                if (!AnalysisScope.ShouldSkipSymbolAnalysis(symbolEvent))
                {
                    await ExecuteSymbolActionsAsync(symbolEvent, analysisScope, analysisStateOpt, cancellationToken).ConfigureAwait(false);
                }

                if (!AnalysisScope.ShouldSkipDeclarationAnalysis(symbol))
                {
                    await ExecuteDeclaringReferenceActionsAsync(symbolEvent, analysisScope, analysisStateOpt, cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                symbolEvent.FlushCache();
            }
        }

        private async Task ExecuteSymbolActionsAsync(SymbolDeclaredCompilationEvent symbolEvent, AnalysisScope analysisScope, AnalysisState analysisStateOpt, CancellationToken cancellationToken)
        {
            var symbol = symbolEvent.Symbol;
            if (!analysisScope.ShouldAnalyze(symbol))
            {
                return;
            }

            foreach (var analyzer in analysisScope.Analyzers)
            {
                // Invoke symbol analyzers only for source symbols.
                ImmutableArray<ImmutableArray<SymbolAnalyzerAction>> actionsByKind;
                if (_symbolActionsByKind.TryGetValue(analyzer, out actionsByKind) && (int)symbol.Kind < actionsByKind.Length)
                {
                    await analyzerExecutor.ExecuteSymbolActionsAsync(actionsByKind[(int)symbol.Kind], analyzer, symbol, GetTopmostNodeForAnalysis, analysisScope, analysisStateOpt).ConfigureAwait(false);
                }
                else if (analysisStateOpt != null)
                {
                    await analysisStateOpt.MarkSymbolCompleteAsync(symbol, analyzer, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private static SyntaxNode GetTopmostNodeForAnalysis(ISymbol symbol, SyntaxReference syntaxReference, Compilation compilation)
        {
            var model = compilation.GetSemanticModel(syntaxReference.SyntaxTree);
            return model.GetTopmostNodeForDiagnosticAnalysis(symbol, syntaxReference.GetSyntax());
        }

        protected abstract Task ExecuteDeclaringReferenceActionsAsync(SymbolDeclaredCompilationEvent symbolEvent, AnalysisScope analysisScope, AnalysisState analysisStateOpt, CancellationToken cancellationToken);

        private async Task ProcessCompilationUnitCompletedAsync(CompilationUnitCompletedEvent completedEvent, AnalysisScope analysisScope, AnalysisState analysisStateOpt, CancellationToken cancellationToken)
        {
            // When the compiler is finished with a compilation unit, we can run user diagnostics which
            // might want to ask the compiler for all the diagnostics in the source file, for example
            // to get information about unnecessary usings.

            var semanticModel = analysisStateOpt != null ?
                GetOrCreateCachedSemanticModel(completedEvent.CompilationUnit, completedEvent.Compilation, cancellationToken) :
                completedEvent.SemanticModel;

            if (!analysisScope.ShouldAnalyze(semanticModel.SyntaxTree))
            {
                return;
            }

            try
            {
                foreach (var analyzer in analysisScope.Analyzers)
                {
                    ImmutableArray<SemanticModelAnalyzerAction> semanticModelActions;
                    if (_semanticModelActionsMap.TryGetValue(analyzer, out semanticModelActions))
                    {
                        // Execute actions for a given analyzer sequentially.
                        await analyzerExecutor.ExecuteSemanticModelActionsAsync(semanticModelActions, analyzer, semanticModel, completedEvent, analysisScope, analysisStateOpt).ConfigureAwait(false);
                    }
                    else if (analysisStateOpt != null)
                    {
                        await analysisStateOpt.MarkEventCompleteAsync(completedEvent, analyzer, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                completedEvent.FlushCache();
            }
        }

        private Task ProcessCompilationStartedAsync(CompilationStartedEvent startedEvent, AnalysisScope analysisScope, AnalysisState analysisStateOpt, CancellationToken cancellationToken)
        {
            return ExecuteCompilationActionsAsync(_compilationActionsMap, startedEvent, analysisScope, analysisStateOpt, cancellationToken);
        }

        private Task ProcessCompilationCompletedAsync(CompilationCompletedEvent endEvent, AnalysisScope analysisScope, AnalysisState analysisStateOpt, CancellationToken cancellationToken)
        {
            return ExecuteCompilationActionsAsync(_compilationEndActionsMap, endEvent, analysisScope, analysisStateOpt, cancellationToken);
        }

        private async Task ExecuteCompilationActionsAsync(
            ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<CompilationAnalyzerAction>> compilationActionsMap,
            CompilationEvent compilationEvent,
            AnalysisScope analysisScope,
            AnalysisState analysisStateOpt,
            CancellationToken cancellationToken)
        {
            Debug.Assert(compilationEvent is CompilationStartedEvent || compilationEvent is CompilationCompletedEvent);

            try
            {
                foreach (var analyzer in analysisScope.Analyzers)
                {
                    ImmutableArray<CompilationAnalyzerAction> compilationActions;
                    if (compilationActionsMap.TryGetValue(analyzer, out compilationActions))
                    {
                        await analyzerExecutor.ExecuteCompilationActionsAsync(compilationActions, analyzer, compilationEvent, analysisScope, analysisStateOpt).ConfigureAwait(false);
                    }
                    else if (analysisStateOpt != null)
                    {
                        await analysisStateOpt.MarkEventCompleteAsync(compilationEvent, analyzer, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                compilationEvent.FlushCache();
            }
        }

        internal static Action<Diagnostic> GetDiagnosticSink(Action<Diagnostic> addDiagnosticCore, Compilation compilation)
        {
            return diagnostic =>
            {
                var filteredDiagnostic = GetFilteredDiagnostic(diagnostic, compilation);
                if (filteredDiagnostic != null)
                {
                    addDiagnosticCore(filteredDiagnostic);
                }
            };
        }

        internal static Action<Diagnostic, DiagnosticAnalyzer, bool> GetDiagnosticSink(Action<Diagnostic, DiagnosticAnalyzer, bool> addLocalDiagnosticCore, Compilation compilation)
        {
            return (diagnostic, analyzer, isSyntaxDiagnostic) =>
            {
                var filteredDiagnostic = GetFilteredDiagnostic(diagnostic, compilation);
                if (filteredDiagnostic != null)
                {
                    addLocalDiagnosticCore(filteredDiagnostic, analyzer, isSyntaxDiagnostic);
                }
            };
        }

        internal static Action<Diagnostic, DiagnosticAnalyzer> GetDiagnosticSink(Action<Diagnostic, DiagnosticAnalyzer> addNonLocalDiagnosticCore, Compilation compilation)
        {
            return (diagnostic, analyzer) =>
            {
                var filteredDiagnostic = GetFilteredDiagnostic(diagnostic, compilation);
                if (filteredDiagnostic != null)
                {
                    addNonLocalDiagnosticCore(filteredDiagnostic, analyzer);
                }
            };
        }

        private static Diagnostic GetFilteredDiagnostic(Diagnostic diagnostic, Compilation compilation)
        {
            return compilation.Options.FilterDiagnostic(diagnostic);
        }

        private static Task<Tuple<AnalyzerActions, ImmutableDictionary<DiagnosticAnalyzer, SemaphoreSlim>>> GetAnalyzerActionsAsync(
            ImmutableArray<DiagnosticAnalyzer> analyzers,
            AnalyzerManager analyzerManager,
            AnalyzerExecutor analyzerExecutor)
        {
            return Task.Run(async () =>
            {
                var allAnalyzerActions = new AnalyzerActions();
                var concurrentAnalyzers = new HashSet<DiagnosticAnalyzer>();
                foreach (var analyzer in analyzers)
                {
                    if (!IsDiagnosticAnalyzerSuppressed(analyzer, analyzerExecutor.Compilation.Options, analyzerManager, analyzerExecutor))
                    {
                        var tuple = await analyzerManager.GetAnalyzerActionsAsync(analyzer, analyzerExecutor).ConfigureAwait(false);
                        var analyzerActions = tuple.Item1;
                        if (analyzerActions != null)
                        {
                            allAnalyzerActions = allAnalyzerActions.Append(analyzerActions);

                            var isConcurrentAnalyzer = tuple.Item2;
                            if (isConcurrentAnalyzer)
                            {
                                concurrentAnalyzers.Add(analyzer);
                            }
                        }
                    }
                }

                var analyzerGateMap = GetAnalyzerGateMap(analyzers, concurrentAnalyzers);
                return Tuple.Create(allAnalyzerActions, analyzerGateMap);
            }, analyzerExecutor.CancellationToken);
        }

        private static ImmutableDictionary<DiagnosticAnalyzer, SemaphoreSlim> GetAnalyzerGateMap(ImmutableArray<DiagnosticAnalyzer> allAnalyzers, HashSet<DiagnosticAnalyzer> concurrentAnalyzers)
        {
            // Non-concurrent analyzers need their action callbacks from the analyzer drive to be guarded by a gate.
            if (allAnalyzers.Length == concurrentAnalyzers.Count)
            {
                // All concurrent analyzers, so we need no gates.
                return ImmutableDictionary<DiagnosticAnalyzer, SemaphoreSlim>.Empty;
            }

            var builder = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, SemaphoreSlim>();
            foreach (var analyzer in allAnalyzers)
            {
                if (!concurrentAnalyzers.Contains(analyzer))
                {
                    var gate = new SemaphoreSlim(initialCount: 1);
                    builder.Add(analyzer, gate);
                }
            }

            return builder.ToImmutable();
        }

        internal async Task<AnalyzerActionCounts> GetAnalyzerActionCountsAsync(DiagnosticAnalyzer analyzer, CancellationToken cancellationToken)
        {
            var executor = analyzerExecutor.WithCancellationToken(cancellationToken);
            var analyzerActions = await analyzerManager.GetAnalyzerActionsAsync(analyzer, executor).ConfigureAwait(false);
            return AnalyzerActionCounts.Create(analyzerActions.Item1);
        }

        /// <summary>
        /// Returns true if all the diagnostics that can be produced by this analyzer are suppressed through options.
        /// </summary>
        internal static bool IsDiagnosticAnalyzerSuppressed(
            DiagnosticAnalyzer analyzer,
            CompilationOptions options,
            AnalyzerManager analyzerManager,
            AnalyzerExecutor analyzerExecutor)
        {
            return analyzerManager.IsDiagnosticAnalyzerSuppressed(analyzer, options, IsCompilerAnalyzer, analyzerExecutor);
        }

        private static bool IsCompilerAnalyzer(DiagnosticAnalyzer analyzer)
        {
            return analyzer is CompilerDiagnosticAnalyzer;
        }

        public void Dispose()
        {
            this.CompilationEventQueue.TryComplete();
            this.DiagnosticQueue.TryComplete();
            _queueRegistration.Dispose();
        }
    }

    /// <summary>
    /// Driver to execute diagnostic analyzers for a given compilation.
    /// It uses a <see cref="AsyncQueue{TElement}"/> of <see cref="CompilationEvent"/>s to drive its analysis.
    /// </summary>
    internal class AnalyzerDriver<TLanguageKindEnum> : AnalyzerDriver where TLanguageKindEnum : struct
    {
        private readonly Func<SyntaxNode, TLanguageKindEnum> _getKind;
        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableDictionary<TLanguageKindEnum, ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>>> _lazyNodeActionsByKind;
        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableDictionary<OperationKind, ImmutableArray<OperationAnalyzerAction>>> _lazyOperationActionsByKind;
        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<CodeBlockStartAnalyzerAction<TLanguageKindEnum>>> _lazyCodeBlockStartActionsByAnalyzer;
        // Code block actions and code block end actions are kept separate so that it is easy to
        // execute the code block actions before the code block end actions.
        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<CodeBlockAnalyzerAction>> _lazyCodeBlockEndActionsByAnalyzer;
        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<CodeBlockAnalyzerAction>> _lazyCodeBlockActionsByAnalyzer;
        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<OperationBlockStartAnalyzerAction>> _lazyOperationBlockStartActionsByAnalyzer;
        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<OperationBlockAnalyzerAction>> _lazyOperationBlockActionsByAnalyzer;
        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<OperationBlockAnalyzerAction>> _lazyOperationBlockEndActionsByAnalyzer;

        private static readonly ObjectPool<DeclarationAnalysisData> s_declarationAnalysisDataPool = new ObjectPool<DeclarationAnalysisData>(() => new DeclarationAnalysisData());

        /// <summary>
        /// Create an analyzer driver.
        /// </summary>
        /// <param name="analyzers">The set of analyzers to include in the analysis</param>
        /// <param name="getKind">A delegate that returns the language-specific kind for a given syntax node</param>
        /// <param name="analyzerManager">AnalyzerManager to manage analyzers for the lifetime of analyzer host.</param>
        internal AnalyzerDriver(ImmutableArray<DiagnosticAnalyzer> analyzers, Func<SyntaxNode, TLanguageKindEnum> getKind, AnalyzerManager analyzerManager) : base(analyzers, analyzerManager)
        {
            _getKind = getKind;
        }

        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableDictionary<TLanguageKindEnum, ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>>> NodeActionsByAnalyzerAndKind
        {
            get
            {
                if (_lazyNodeActionsByKind == null)
                {
                    var nodeActions = this.analyzerActions.GetSyntaxNodeActions<TLanguageKindEnum>();
                    ImmutableDictionary<DiagnosticAnalyzer, ImmutableDictionary<TLanguageKindEnum, ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>>> analyzerActionsByKind;
                    if (nodeActions.Any())
                    {
                        var nodeActionsByAnalyzers = nodeActions.GroupBy(a => a.Analyzer);
                        var builder = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, ImmutableDictionary<TLanguageKindEnum, ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>>>();
                        foreach (var analyzerAndActions in nodeActionsByAnalyzers)
                        {
                            ImmutableDictionary<TLanguageKindEnum, ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>> actionsByKind;
                            if (analyzerAndActions.Any())
                            {
                                actionsByKind = AnalyzerExecutor.GetNodeActionsByKind(analyzerAndActions);
                            }
                            else
                            {
                                actionsByKind = ImmutableDictionary<TLanguageKindEnum, ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>>.Empty;
                            }

                            builder.Add(analyzerAndActions.Key, actionsByKind);
                        }

                        analyzerActionsByKind = builder.ToImmutable();
                    }
                    else
                    {
                        analyzerActionsByKind = ImmutableDictionary<DiagnosticAnalyzer, ImmutableDictionary<TLanguageKindEnum, ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>>>.Empty;
                    }

                    Interlocked.CompareExchange(ref _lazyNodeActionsByKind, analyzerActionsByKind, null);
                }

                return _lazyNodeActionsByKind;
            }
        }

        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableDictionary<OperationKind, ImmutableArray<OperationAnalyzerAction>>> OperationActionsByAnalyzerAndKind
        {
            get
            {
                if (_lazyOperationActionsByKind == null)
                {
                    var operationActions = this.analyzerActions.OperationActions;
                    ImmutableDictionary<DiagnosticAnalyzer, ImmutableDictionary<OperationKind, ImmutableArray<OperationAnalyzerAction>>> analyzerActionsByKind;
                    if (operationActions.Any())
                    {
                        var operationActionsByAnalyzers = operationActions.GroupBy(a => a.Analyzer);
                        var builder = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, ImmutableDictionary<OperationKind, ImmutableArray<OperationAnalyzerAction>>>();
                        foreach (var analyzerAndActions in operationActionsByAnalyzers)
                        {
                            ImmutableDictionary<OperationKind, ImmutableArray<OperationAnalyzerAction>> actionsByKind;
                            if (analyzerAndActions.Any())
                            {
                                actionsByKind = AnalyzerExecutor.GetOperationActionsByKind(analyzerAndActions);
                            }
                            else
                            {
                                actionsByKind = ImmutableDictionary<OperationKind, ImmutableArray<OperationAnalyzerAction>>.Empty;
                            }

                            builder.Add(analyzerAndActions.Key, actionsByKind);
                        }

                        analyzerActionsByKind = builder.ToImmutable();
                    }
                    else
                    {
                        analyzerActionsByKind = ImmutableDictionary<DiagnosticAnalyzer, ImmutableDictionary<OperationKind, ImmutableArray<OperationAnalyzerAction>>>.Empty;
                    }

                    Interlocked.CompareExchange(ref _lazyOperationActionsByKind, analyzerActionsByKind, null);
                }

                return _lazyOperationActionsByKind;
            }
        }


        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<CodeBlockStartAnalyzerAction<TLanguageKindEnum>>> CodeBlockStartActionsByAnalyzer
        {
            get { return GetBlockActionsByAnalyzer(ref _lazyCodeBlockStartActionsByAnalyzer, this.analyzerActions.GetCodeBlockStartActions<TLanguageKindEnum>()); }
        }

        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<CodeBlockAnalyzerAction>> CodeBlockEndActionsByAnalyzer
        {
            get { return GetBlockActionsByAnalyzer(ref _lazyCodeBlockEndActionsByAnalyzer, this.analyzerActions.CodeBlockEndActions); }
        }

        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<CodeBlockAnalyzerAction>> CodeBlockActionsByAnalyzer
        {
            get { return GetBlockActionsByAnalyzer(ref _lazyCodeBlockActionsByAnalyzer, this.analyzerActions.CodeBlockActions); }
        }

        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<OperationBlockStartAnalyzerAction>> OperationBlockStartActionsByAnalyzer
        {
            get { return GetBlockActionsByAnalyzer(ref _lazyOperationBlockStartActionsByAnalyzer, this.analyzerActions.OperationBlockStartActions); }
        }

        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<OperationBlockAnalyzerAction>> OperationBlockEndActionsByAnalyzer
        {
            get { return GetBlockActionsByAnalyzer(ref _lazyOperationBlockEndActionsByAnalyzer, this.analyzerActions.OperationBlockEndActions); }
        }

        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<OperationBlockAnalyzerAction>> OperationBlockActionsByAnalyzer
        {
            get { return GetBlockActionsByAnalyzer(ref _lazyOperationBlockActionsByAnalyzer, this.analyzerActions.OperationBlockActions); }
        }

        private static ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<ActionType>> GetBlockActionsByAnalyzer<ActionType>(
            ref ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<ActionType>> lazyCodeBlockActionsByAnalyzer,
            ImmutableArray<ActionType> codeBlockActions) where ActionType : AnalyzerAction
        {
            if (lazyCodeBlockActionsByAnalyzer == null)
            {
                ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<ActionType>> codeBlockActionsByAnalyzer;
                if (!codeBlockActions.IsEmpty)
                {
                    var builder = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, ImmutableArray<ActionType>>();
                    var actionsByAnalyzer = codeBlockActions.GroupBy(action => action.Analyzer);
                    foreach (var analyzerAndActions in actionsByAnalyzer)
                    {
                        builder.Add(analyzerAndActions.Key, analyzerAndActions.ToImmutableArrayOrEmpty());
                    }

                    codeBlockActionsByAnalyzer = builder.ToImmutable();
                }
                else
                {
                    codeBlockActionsByAnalyzer = ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<ActionType>>.Empty;
                }

                Interlocked.CompareExchange(ref lazyCodeBlockActionsByAnalyzer, codeBlockActionsByAnalyzer, null);
            }

            return lazyCodeBlockActionsByAnalyzer;
        }

        private bool ShouldExecuteSyntaxNodeActions(AnalysisScope analysisScope)
        {
            foreach (var analyzer in analysisScope.Analyzers)
            {
                if (this.NodeActionsByAnalyzerAndKind.ContainsKey(analyzer))
                {
                    return true;
                }
            }

            return false;
        }

        private bool ShouldExecuteOperationActions(AnalysisScope analysisScope)
        {
            return analysisScope.Analyzers.Any(analyzer => this.OperationActionsByAnalyzerAndKind.ContainsKey(analyzer));
        }

        private bool ShouldExecuteBlockActions<T0, T1>(ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<T0>> blockStartActions, ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<T1>> blockActions, AnalysisScope analysisScope, ISymbol symbol)
        {
            if (AnalyzerExecutor.CanHaveExecutableCodeBlock(symbol))
            {
                foreach (var analyzer in analysisScope.Analyzers)
                {
                    if (blockStartActions.ContainsKey(analyzer) ||
                        blockActions.ContainsKey(analyzer))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool ShouldExecuteCodeBlockActions(AnalysisScope analysisScope, ISymbol symbol)
        {
            return ShouldExecuteBlockActions(this.CodeBlockStartActionsByAnalyzer, this.CodeBlockActionsByAnalyzer, analysisScope, symbol);
        }

        private bool ShouldExecuteOperationBlockActions(AnalysisScope analysisScope, ISymbol symbol)
        {
            return ShouldExecuteBlockActions(this.OperationBlockStartActionsByAnalyzer, this.OperationBlockActionsByAnalyzer, analysisScope, symbol);
        }

        protected override async Task ExecuteDeclaringReferenceActionsAsync(
            SymbolDeclaredCompilationEvent symbolEvent,
            AnalysisScope analysisScope,
            AnalysisState analysisStateOpt,
            CancellationToken cancellationToken)
        {
            var symbol = symbolEvent.Symbol;
            var executeSyntaxNodeActions = ShouldExecuteSyntaxNodeActions(analysisScope);
            var executeCodeBlockActions = ShouldExecuteCodeBlockActions(analysisScope, symbol);
            var executeOperationActions = ShouldExecuteOperationActions(analysisScope);
            var executeOperationBlockActions = ShouldExecuteOperationBlockActions(analysisScope, symbol);

            if (executeSyntaxNodeActions || executeOperationActions || executeCodeBlockActions || executeOperationBlockActions)
            {
                foreach (var decl in symbolEvent.DeclaringSyntaxReferences)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (analysisScope.FilterTreeOpt == null || analysisScope.FilterTreeOpt == decl.SyntaxTree)
                    {
                        await ExecuteDeclaringReferenceActionsAsync(decl, symbolEvent, analysisScope, analysisStateOpt, executeSyntaxNodeActions, executeOperationActions, executeCodeBlockActions, executeOperationBlockActions, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            else if (analysisStateOpt != null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await analysisStateOpt.MarkDeclarationsCompleteAsync(symbolEvent.DeclaringSyntaxReferences, analysisScope.Analyzers, cancellationToken).ConfigureAwait(false);

                foreach (var decl in symbolEvent.DeclaringSyntaxReferences)
                {
                    await ClearCachedAnalysisDataIfAnalyzedAsync(decl, decl.GetSyntax(), symbolEvent.Compilation, analysisStateOpt, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private DeclarationAnalysisData GetOrComputeDeclarationAnalysisData(
            SyntaxReference declaration,
            Func<DeclarationAnalysisData> computeDeclarationAnalysisData,
            Compilation compilation,
            bool cacheAnalysisData)
        {
            if (!cacheAnalysisData)
            {
                return computeDeclarationAnalysisData();
            }

            // NOTE: The driver guarantees that only a single thread will be performing analysis on individual declaration.
            // However, there might be multiple threads analyzing different trees at the same time, so we need to lock the map for read/write.

            var map = GetOrCreateCachedCompilationData(compilation).DeclarationAnalysisDataMap;

            DeclarationAnalysisData data;
            lock (map)
            {
                if (map.TryGetValue(declaration, out data))
                {
                    return data;
                }
            }

            data = computeDeclarationAnalysisData();

            lock (map)
            {
                map[declaration] = data;
            }

            return data;
        }

        private async Task ClearCachedAnalysisDataIfAnalyzedAsync(SyntaxReference declaration, SyntaxNode node, Compilation compilation, AnalysisState analysisState, CancellationToken cancellationToken)
        {
            Debug.Assert(analysisState != null);

            CompilationData compilationData;
            if (!s_compilationDataCache.TryGetValue(compilation, out compilationData) ||
                !(await analysisState.IsDeclarationCompleteAsync(node, cancellationToken).ConfigureAwait(false)))
            {
                return;
            }

            var map = compilationData.DeclarationAnalysisDataMap;
            DeclarationAnalysisData declarationData;
            lock (map)
            {
                if (!map.TryGetValue(declaration, out declarationData))
                {
                    return;
                }

                map.Remove(declaration);
            }

            declarationData.Free();
            s_declarationAnalysisDataPool.Free(declarationData);
        }

        private DeclarationAnalysisData ComputeDeclarationAnalysisData(
            ISymbol symbol,
            SyntaxReference declaration,
            SemanticModel semanticModel,
            bool shouldExecuteSyntaxNodeActions,
            AnalysisScope analysisScope,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var declarationAnalysisData = s_declarationAnalysisDataPool.Allocate();
            var builder = declarationAnalysisData.DeclarationsInNode;

            var declaringReferenceSyntax = declaration.GetSyntax(cancellationToken);
            var topmostNodeForAnalysis = semanticModel.GetTopmostNodeForDiagnosticAnalysis(symbol, declaringReferenceSyntax);
            ComputeDeclarationsInNode(semanticModel, symbol, declaringReferenceSyntax, topmostNodeForAnalysis, builder, cancellationToken);
            var isPartialDeclAnalysis = analysisScope.FilterSpanOpt.HasValue && !analysisScope.ContainsSpan(topmostNodeForAnalysis.FullSpan);
            var nodesToAnalyze = shouldExecuteSyntaxNodeActions ?
                    GetSyntaxNodesToAnalyze(topmostNodeForAnalysis, symbol, builder, analysisScope, isPartialDeclAnalysis, semanticModel, analyzerExecutor) :
                    ImmutableArray<SyntaxNode>.Empty;

            declarationAnalysisData.DeclaringReferenceSyntax = declaringReferenceSyntax;
            declarationAnalysisData.TopmostNodeForAnalysis = topmostNodeForAnalysis;
            declarationAnalysisData.DescendantNodesToAnalyze.AddRange(nodesToAnalyze);
            declarationAnalysisData.IsPartialAnalysis = isPartialDeclAnalysis;

            return declarationAnalysisData;
        }

        private static void ComputeDeclarationsInNode(SemanticModel semanticModel, ISymbol declaredSymbol, SyntaxNode declaringReferenceSyntax, SyntaxNode topmostNodeForAnalysis, List<DeclarationInfo> builder, CancellationToken cancellationToken)
        {
            // We only care about the top level symbol declaration and its immediate member declarations.
            int? levelsToCompute = 2;
            var getSymbol = topmostNodeForAnalysis != declaringReferenceSyntax || declaredSymbol.Kind == SymbolKind.Namespace;
            semanticModel.ComputeDeclarationsInNode(topmostNodeForAnalysis, getSymbol, builder, cancellationToken, levelsToCompute);
        }

        private async Task ExecuteDeclaringReferenceActionsAsync(
            SyntaxReference decl,
            SymbolDeclaredCompilationEvent symbolEvent,
            AnalysisScope analysisScope,
            AnalysisState analysisStateOpt,
            bool shouldExecuteSyntaxNodeActions,
            bool shouldExecuteOperationActions,
            bool shouldExecuteCodeBlockActions,
            bool shouldExecuteOperationBlockActions,
            CancellationToken cancellationToken)
        {
            Debug.Assert(shouldExecuteSyntaxNodeActions || shouldExecuteOperationActions || shouldExecuteCodeBlockActions || shouldExecuteOperationBlockActions);

            var symbol = symbolEvent.Symbol;
            SemanticModel semanticModel = analysisStateOpt != null ?
                GetOrCreateCachedSemanticModel(decl.SyntaxTree, symbolEvent.Compilation, cancellationToken) :
                symbolEvent.SemanticModel(decl);

            var cacheAnalysisData = analysisScope.Analyzers.Length < analyzers.Length &&
                (!analysisScope.FilterSpanOpt.HasValue || analysisScope.FilterSpanOpt.Value.Length >= decl.SyntaxTree.GetRoot(cancellationToken).Span.Length);

            var declarationAnalysisData = GetOrComputeDeclarationAnalysisData(
                decl,
                () => ComputeDeclarationAnalysisData(symbol, decl, semanticModel, shouldExecuteSyntaxNodeActions, analysisScope, cancellationToken),
                symbolEvent.Compilation,
                cacheAnalysisData);

            if (!analysisScope.ShouldAnalyze(declarationAnalysisData.TopmostNodeForAnalysis))
            {
                return;
            }

            // Execute stateless syntax node actions.
            if (shouldExecuteSyntaxNodeActions)
            {
                var nodesToAnalyze = declarationAnalysisData.DescendantNodesToAnalyze;
                foreach (var analyzer in analysisScope.Analyzers)
                {
                    ImmutableDictionary<TLanguageKindEnum, ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>> nodeActionsByKind;
                    if (this.NodeActionsByAnalyzerAndKind.TryGetValue(analyzer, out nodeActionsByKind))
                    {
                        await analyzerExecutor.ExecuteSyntaxNodeActionsAsync(nodesToAnalyze, nodeActionsByKind,
                            analyzer, semanticModel, _getKind, declarationAnalysisData.TopmostNodeForAnalysis.FullSpan, decl, analysisScope, analysisStateOpt).ConfigureAwait(false);
                    }
                }
            }

            // Execute code block actions.
            if (shouldExecuteCodeBlockActions || shouldExecuteOperationActions || shouldExecuteOperationBlockActions)
            {
                // Compute the executable code blocks of interest.
                var executableCodeBlocks = ImmutableArray<SyntaxNode>.Empty;
                IEnumerable<CodeBlockAnalyzerActions> codeBlockActions = null;
                foreach (var declInNode in declarationAnalysisData.DeclarationsInNode)
                {
                    if (declInNode.DeclaredNode == declarationAnalysisData.TopmostNodeForAnalysis || declInNode.DeclaredNode == declarationAnalysisData.DeclaringReferenceSyntax)
                    {
                        executableCodeBlocks = declInNode.ExecutableCodeBlocks;
                        if (executableCodeBlocks.Any())
                        {
                            if (shouldExecuteCodeBlockActions || shouldExecuteOperationBlockActions)
                            {
                                codeBlockActions = GetCodeBlockActions(analysisScope);
                            }

                            // Execute operation actions.
                            if (shouldExecuteOperationActions || shouldExecuteOperationBlockActions)
                            {
                                var operationBlocksToAnalyze = GetOperationBlocksToAnalyze(executableCodeBlocks, semanticModel, cancellationToken);
                                var operationsToAnalyze = GetOperationsToAnalyze(operationBlocksToAnalyze);

                                if (!operationsToAnalyze.IsEmpty)
                                {
                                    if (shouldExecuteOperationActions)
                                    {
                                        foreach (var analyzer in analysisScope.Analyzers)
                                        {
                                            ImmutableDictionary<OperationKind, ImmutableArray<OperationAnalyzerAction>> operationActionsByKind;
                                            if (this.OperationActionsByAnalyzerAndKind.TryGetValue(analyzer, out operationActionsByKind))
                                            {
                                                await analyzerExecutor.ExecuteOperationActionsAsync(operationsToAnalyze, operationActionsByKind,
                                                    analyzer, semanticModel, declarationAnalysisData.TopmostNodeForAnalysis.FullSpan, decl, analysisScope, analysisStateOpt).ConfigureAwait(false);
                                            }
                                        }
                                    }

                                    if (shouldExecuteOperationBlockActions)
                                    {
                                        foreach (var analyzerActions in codeBlockActions)
                                        {
                                            await analyzerExecutor.ExecuteOperationBlockActionsAsync(
                                                analyzerActions.OperationBlockStartActions, analyzerActions.OperationBlockActions,
                                                analyzerActions.OpererationBlockEndActions, analyzerActions.Analyzer, declarationAnalysisData.TopmostNodeForAnalysis, symbol,
                                                operationBlocksToAnalyze, operationsToAnalyze, semanticModel, decl, analysisScope, analysisStateOpt).ConfigureAwait(false);
                                        }
                                    }
                                }
                            }

                            break;
                        }
                    }
                }

                if (executableCodeBlocks.Any() && shouldExecuteCodeBlockActions)
                {
                    foreach (var analyzerActions in codeBlockActions)
                    {
                        await analyzerExecutor.ExecuteCodeBlockActionsAsync(
                            analyzerActions.CodeBlockStartActions, analyzerActions.CodeBlockActions,
                            analyzerActions.CodeBlockEndActions, analyzerActions.Analyzer, declarationAnalysisData.TopmostNodeForAnalysis, symbol,
                            executableCodeBlocks, semanticModel, _getKind, decl, analysisScope, analysisStateOpt).ConfigureAwait(false);
                    }
                }
            }

            // Mark completion only if we are analyzing a span containing the entire syntax node.
            if (analysisStateOpt != null && !declarationAnalysisData.IsPartialAnalysis)
            {
                foreach (var analyzer in analysisScope.Analyzers)
                {
                    await analysisStateOpt.MarkDeclarationCompleteAsync(decl, analyzer, cancellationToken).ConfigureAwait(false);
                }

                if (cacheAnalysisData)
                {
                    await ClearCachedAnalysisDataIfAnalyzedAsync(decl, declarationAnalysisData.DeclaringReferenceSyntax, symbolEvent.Compilation, analysisStateOpt, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        [StructLayout(LayoutKind.Auto)]
        private struct CodeBlockAnalyzerActions
        {
            public DiagnosticAnalyzer Analyzer;
            public ImmutableArray<CodeBlockStartAnalyzerAction<TLanguageKindEnum>> CodeBlockStartActions;
            public ImmutableArray<CodeBlockAnalyzerAction> CodeBlockActions;
            public ImmutableArray<CodeBlockAnalyzerAction> CodeBlockEndActions;
            public ImmutableArray<OperationBlockStartAnalyzerAction> OperationBlockStartActions;
            public ImmutableArray<OperationBlockAnalyzerAction> OperationBlockActions;
            public ImmutableArray<OperationBlockAnalyzerAction> OpererationBlockEndActions;
        }

        private IEnumerable<CodeBlockAnalyzerActions> GetCodeBlockActions(AnalysisScope analysisScope)
        {
            foreach (var analyzer in analysisScope.Analyzers)
            {
                ImmutableArray<CodeBlockStartAnalyzerAction<TLanguageKindEnum>> codeBlockStartActions;
                if (!this.CodeBlockStartActionsByAnalyzer.TryGetValue(analyzer, out codeBlockStartActions))
                {
                    codeBlockStartActions = ImmutableArray<CodeBlockStartAnalyzerAction<TLanguageKindEnum>>.Empty;
                }

                ImmutableArray<CodeBlockAnalyzerAction> codeBlockActions;
                if (!this.CodeBlockActionsByAnalyzer.TryGetValue(analyzer, out codeBlockActions))
                {
                    codeBlockActions = ImmutableArray<CodeBlockAnalyzerAction>.Empty;
                }

                ImmutableArray<CodeBlockAnalyzerAction> codeBlockEndActions;
                if (!this.CodeBlockEndActionsByAnalyzer.TryGetValue(analyzer, out codeBlockEndActions))
                {
                    codeBlockEndActions = ImmutableArray<CodeBlockAnalyzerAction>.Empty;
                }

                ImmutableArray<OperationBlockStartAnalyzerAction> operationBlockStartActions;
                if (!this.OperationBlockStartActionsByAnalyzer.TryGetValue(analyzer, out operationBlockStartActions))
                {
                    operationBlockStartActions = ImmutableArray<OperationBlockStartAnalyzerAction>.Empty;
                }

                ImmutableArray<OperationBlockAnalyzerAction> operationBlockActions;
                if (!this.OperationBlockActionsByAnalyzer.TryGetValue(analyzer, out operationBlockActions))
                {
                    operationBlockActions = ImmutableArray<OperationBlockAnalyzerAction>.Empty;
                }

                ImmutableArray<OperationBlockAnalyzerAction> operationBlockEndActions;
                if (!this.OperationBlockEndActionsByAnalyzer.TryGetValue(analyzer, out operationBlockEndActions))
                {
                    operationBlockEndActions = ImmutableArray<OperationBlockAnalyzerAction>.Empty;
                }

                if (!codeBlockStartActions.IsEmpty || !codeBlockActions.IsEmpty || !codeBlockEndActions.IsEmpty || !operationBlockStartActions.IsEmpty || !operationBlockActions.IsEmpty || !operationBlockEndActions.IsEmpty)
                {
                    yield return
                        new CodeBlockAnalyzerActions
                        {
                            Analyzer = analyzer,
                            CodeBlockStartActions = codeBlockStartActions,
                            CodeBlockActions = codeBlockActions,
                            CodeBlockEndActions = codeBlockEndActions,
                            OperationBlockStartActions = operationBlockStartActions,
                            OperationBlockActions = operationBlockActions,
                            OpererationBlockEndActions = operationBlockEndActions
                        };
                }
            }
        }

        private static IEnumerable<SyntaxNode> GetSyntaxNodesToAnalyze(
            SyntaxNode declaredNode,
            ISymbol declaredSymbol,
            IEnumerable<DeclarationInfo> declarationsInNode,
            AnalysisScope analysisScope,
            bool isPartialDeclAnalysis,
            SemanticModel semanticModel,
            AnalyzerExecutor analyzerExecutor)
        {
            // Eliminate descendant member declarations within declarations.
            // There will be separate symbols declared for the members.
            HashSet<SyntaxNode> descendantDeclsToSkip = null;
            bool first = true;
            foreach (var declInNode in declarationsInNode)
            {
                analyzerExecutor.CancellationToken.ThrowIfCancellationRequested();

                if (declInNode.DeclaredNode != declaredNode)
                {
                    // Might be a field declaration statement with multiple fields declared.
                    // If so, we execute syntax node analysis for entire field declaration (and its descendants)
                    // if we processing the first field and skip syntax actions for remaining fields in the declaration.
                    if (declInNode.DeclaredSymbol == declaredSymbol)
                    {
                        if (first)
                        {
                            break;
                        }

                        return ImmutableArray<SyntaxNode>.Empty;
                    }

                    // Compute the topmost node representing the syntax declaration for the member that needs to be skipped.
                    var declarationNodeToSkip = declInNode.DeclaredNode;
                    var declaredSymbolOfDeclInNode = declInNode.DeclaredSymbol ?? semanticModel.GetDeclaredSymbol(declInNode.DeclaredNode, analyzerExecutor.CancellationToken);
                    if (declaredSymbolOfDeclInNode != null)
                    {
                        declarationNodeToSkip = semanticModel.GetTopmostNodeForDiagnosticAnalysis(declaredSymbolOfDeclInNode, declInNode.DeclaredNode);
                    }

                    descendantDeclsToSkip = descendantDeclsToSkip ?? new HashSet<SyntaxNode>();
                    descendantDeclsToSkip.Add(declarationNodeToSkip);
                }

                first = false;
            }

            var nodesToAnalyze = descendantDeclsToSkip == null ?
                declaredNode.DescendantNodesAndSelf(descendIntoTrivia: true) :
                GetSyntaxNodesToAnalyze(declaredNode, descendantDeclsToSkip);

            if (isPartialDeclAnalysis)
            {
                nodesToAnalyze = nodesToAnalyze.Where(node => analysisScope.ShouldAnalyze(node));
            }

            return nodesToAnalyze;
        }

        private static ImmutableArray<IOperation> GetOperationBlocksToAnalyze(
            ImmutableArray<SyntaxNode> executableBlocks,
            SemanticModel semanticModel,
            CancellationToken cancellationToken)
        {
            ArrayBuilder<IOperation> operationBlocksToAnalyze = ArrayBuilder<IOperation>.GetInstance();

            foreach (SyntaxNode executableBlock in executableBlocks)
            {
                IOperation operation = semanticModel.GetOperation(executableBlock, cancellationToken);
                if (operation != null)
                {
                    operationBlocksToAnalyze.AddRange(operation);
                }
            }

            return operationBlocksToAnalyze.ToImmutableAndFree();
        }

        private static ImmutableArray<IOperation> GetOperationsToAnalyze(
            ImmutableArray<IOperation> operationBlocks)
        {
            ArrayBuilder<IOperation> operationsToAnalyze = ArrayBuilder<IOperation>.GetInstance();

            foreach (IOperation operationBlock in operationBlocks)
            {
                operationsToAnalyze.AddRange(operationBlock.DescendantsAndSelf());
            }

            return operationsToAnalyze.ToImmutableAndFree();
        }

        private static IEnumerable<SyntaxNode> GetSyntaxNodesToAnalyze(SyntaxNode declaredNode, HashSet<SyntaxNode> descendantDeclsToSkip)
        {
            Debug.Assert(declaredNode != null);
            Debug.Assert(descendantDeclsToSkip != null);

            foreach (var node in declaredNode.DescendantNodesAndSelf(n => !descendantDeclsToSkip.Contains(n), descendIntoTrivia: true))
            {
                if (!descendantDeclsToSkip.Contains(node))
                {
                    yield return node;
                }
            }
        }
    }
}
