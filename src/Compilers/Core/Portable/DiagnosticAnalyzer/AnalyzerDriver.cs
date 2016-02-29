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
        private readonly Func<SyntaxTree, CancellationToken, bool> _isGeneratedCode;

        // Lazy fields
        private CancellationTokenRegistration _queueRegistration;
        protected AnalyzerExecutor analyzerExecutor;
        protected CompilationData compilationData;
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
        /// Map from analyzers to their <see cref="GeneratedCodeAnalysisFlags"/> setting. 
        /// </summary>
        private ImmutableDictionary<DiagnosticAnalyzer, GeneratedCodeAnalysisFlags> _generatedCodeAnalysisFlagsMap;

        /// <summary>
        /// True if all analyzers need to analyze and report diagnostics in generated code - we can assume all code to be non-generated code.
        /// </summary>
        private bool _treatAllCodeAsNonGeneratedCode;

        /// <summary>
        /// True if no analyzer needs generated code analysis - we can skip all analysis on a generated code symbol/tree.
        /// </summary>
        private bool _doNotAnalyzeGeneratedCode;

        /// <summary>
        /// Lazily populated dictionary indicating whether a source file is a generated code file or not.
        /// </summary>
        private Dictionary<SyntaxTree, bool> _lazyGeneratedCodeFilesMap;

        /// <summary>
        /// Symbol for <see cref="System.CodeDom.Compiler.GeneratedCodeAttribute"/>.
        /// </summary>
        private INamedTypeSymbol _generatedCodeAttribute;

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
        /// <param name="isComment">Delegate to identify if the given trivia is a comment.</param>
        protected AnalyzerDriver(ImmutableArray<DiagnosticAnalyzer> analyzers, AnalyzerManager analyzerManager, Func<SyntaxTrivia, bool> isComment)
        {
            this.analyzers = analyzers;
            this.analyzerManager = analyzerManager;
            _isGeneratedCode = (tree, ct) => GeneratedCodeUtilities.IsGeneratedCode(tree, isComment, ct);
        }

        /// <summary>
        /// Initializes the <see cref="analyzerActions"/> and related actions maps for the analyzer driver.
        /// It kicks off the <see cref="WhenInitializedTask"/> task for initialization.
        /// Note: This method must be invoked exactly once on the driver.
        /// </summary>
        private void Initialize(AnalyzerExecutor analyzerExecutor, DiagnosticQueue diagnosticQueue, CompilationData compilationData, CancellationToken cancellationToken)
        {
            try
            {
                Debug.Assert(_initializeTask == null);

                this.analyzerExecutor = analyzerExecutor;
                this.compilationData = compilationData;
                this.DiagnosticQueue = diagnosticQueue;

                // Compute the set of effective actions based on suppression, and running the initial analyzers
                _initializeTask = Task.Run(async () =>
                {
                    var unsuppressedAnalyzers = GetUnsuppressedAnalyzers(analyzers, analyzerManager, analyzerExecutor);
                    this.analyzerActions = await GetAnalyzerActionsAsync(unsuppressedAnalyzers, analyzerManager, analyzerExecutor).ConfigureAwait(false);
                    _analyzerGateMap = await GetAnalyzerGateMapAsync(unsuppressedAnalyzers, analyzerManager, analyzerExecutor).ConfigureAwait(false);

                    _generatedCodeAnalysisFlagsMap = await GetGeneratedCodeAnalysisFlagsAsync(unsuppressedAnalyzers, analyzerManager, analyzerExecutor).ConfigureAwait(false);
                    _doNotAnalyzeGeneratedCode = ShouldSkipAnalysisOnGeneratedCode(unsuppressedAnalyzers);
                    _treatAllCodeAsNonGeneratedCode = ShouldTreatAllCodeAsNonGeneratedCode(unsuppressedAnalyzers, _generatedCodeAnalysisFlagsMap);
                    _lazyGeneratedCodeFilesMap = _treatAllCodeAsNonGeneratedCode ? null : new Dictionary<SyntaxTree, bool>();
                    _generatedCodeAttribute = analyzerExecutor.Compilation?.GetTypeByMetadataName("System.CodeDom.Compiler.GeneratedCodeAttribute");

                    _symbolActionsByKind = MakeSymbolActionsByKind();
                    _semanticModelActionsMap = MakeSemanticModelActionsByAnalyzer();
                    _syntaxTreeActionsMap = MakeSyntaxTreeActionsByAnalyzer();
                    _compilationActionsMap = MakeCompilationActionsByAnalyzer(this.analyzerActions.CompilationActions);
                    _compilationEndActionsMap = MakeCompilationActionsByAnalyzer(this.analyzerActions.CompilationEndActions);
                }, cancellationToken);

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
           CompilationData compilationData,
           bool categorizeDiagnostics,
           CancellationToken cancellationToken)
        {
            Debug.Assert(_initializeTask == null);

            var diagnosticQueue = DiagnosticQueue.Create(categorizeDiagnostics);

            Action<Diagnostic> addNotCategorizedDiagnosticOpt = null;
            Action<Diagnostic, DiagnosticAnalyzer, bool> addCategorizedLocalDiagnosticOpt = null;
            Action<Diagnostic, DiagnosticAnalyzer> addCategorizedNonLocalDiagnosticOpt = null;
            if (categorizeDiagnostics)
            {
                addCategorizedLocalDiagnosticOpt = GetDiagnosticSink(diagnosticQueue.EnqueueLocal, compilation);
                addCategorizedNonLocalDiagnosticOpt = GetDiagnosticSink(diagnosticQueue.EnqueueNonLocal, compilation);
            }
            else
            {
                addNotCategorizedDiagnosticOpt = GetDiagnosticSink(diagnosticQueue.Enqueue, compilation);
            }

            // Wrap onAnalyzerException to pass in filtered diagnostic.
            Action<Exception, DiagnosticAnalyzer, Diagnostic> newOnAnalyzerException = (ex, analyzer, diagnostic) =>
            {
                var filteredDiagnostic = GetFilteredDiagnostic(diagnostic, compilation);
                if (filteredDiagnostic != null)
                {
                    if (analysisOptions.OnAnalyzerException != null)
                    {
                        analysisOptions.OnAnalyzerException(ex, analyzer, filteredDiagnostic);
                    }
                    else if (categorizeDiagnostics)
                    {
                        addCategorizedNonLocalDiagnosticOpt(filteredDiagnostic, analyzer);
                    }
                    else
                    {
                        addNotCategorizedDiagnosticOpt(filteredDiagnostic);
                    }
                }
            };

            if (analysisOptions.LogAnalyzerExecutionTime)
            {
                // If we are reporting detailed analyzer performance numbers, then do a dummy invocation of Compilation.GetTypeByMetadataName API upfront.
                // This API seems to cause a severe hit for the first analyzer invoking it and hence introduces lot of noise in the computed analyzer execution times.
                var unused = compilation.GetTypeByMetadataName("System.Object");
            }

            var analyzerExecutor = AnalyzerExecutor.Create(
                compilation, analysisOptions.Options ?? AnalyzerOptions.Empty, addNotCategorizedDiagnosticOpt, newOnAnalyzerException, analysisOptions.AnalyzerExceptionFilter,
                IsCompilerAnalyzer, analyzerManager, ShouldSkipAnalysisOnGeneratedCode, ShouldSuppressGeneratedCodeDiagnostic, GetAnalyzerGate,
                analysisOptions.LogAnalyzerExecutionTime, addCategorizedLocalDiagnosticOpt, addCategorizedNonLocalDiagnosticOpt, cancellationToken);

            Initialize(analyzerExecutor, diagnosticQueue, compilationData, cancellationToken);
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

        private bool ShouldSkipAnalysisOnGeneratedCode(ImmutableArray<DiagnosticAnalyzer> analyzers)
        {
            foreach (var analyzer in analyzers)
            {
                if (!ShouldSkipAnalysisOnGeneratedCode(analyzer))
                {
                    return false;
                }
            }

            return true;
        }

        private bool ShouldTreatAllCodeAsNonGeneratedCode(ImmutableArray<DiagnosticAnalyzer> analyzers, ImmutableDictionary<DiagnosticAnalyzer, GeneratedCodeAnalysisFlags> generatedCodeAnalysisFlagsMap)
        {
            foreach (var analyzer in analyzers)
            {
                var flags = generatedCodeAnalysisFlagsMap[analyzer];
                var analyze = (flags & GeneratedCodeAnalysisFlags.Analyze) != 0;
                var report = (flags & GeneratedCodeAnalysisFlags.ReportDiagnostics) != 0;
                if (!analyze || !report)
                {
                    return false;
                }
            }

            return true;
        }

        private bool ShouldSkipAnalysisOnGeneratedCode(DiagnosticAnalyzer analyzer)
        {
            if (_treatAllCodeAsNonGeneratedCode)
            {
                return false;
            }

            var mode = _generatedCodeAnalysisFlagsMap[analyzer];
            return (mode & GeneratedCodeAnalysisFlags.Analyze) == 0;
        }

        private bool ShouldSuppressGeneratedCodeDiagnostic(Diagnostic diagnostic, DiagnosticAnalyzer analyzer, Compilation compilation, CancellationToken cancellationToken)
        {
            if (_treatAllCodeAsNonGeneratedCode)
            {
                return false;
            }

            var generatedCodeAnalysisFlags = _generatedCodeAnalysisFlagsMap[analyzer];
            var suppressInGeneratedCode = (generatedCodeAnalysisFlags & GeneratedCodeAnalysisFlags.ReportDiagnostics) == 0;
            return suppressInGeneratedCode && IsInGeneratedCode(diagnostic.Location, compilation, cancellationToken);
        }

        /// <summary>
        /// Attaches a pre-populated event queue to the driver and processes all events in the queue.
        /// </summary>
        /// <param name="eventQueue">Compilation events to analyze.</param>
        /// <param name="analysisScope">Scope of analysis.</param>
        /// <param name="analysisStateOpt">An optional object to track partial analysis state.</param>
        /// <param name="cancellationToken">Cancellation token to abort analysis.</param>
        /// <remarks>Driver must be initialized before invoking this method, i.e. <see cref="Initialize(AnalyzerExecutor, DiagnosticQueue, CompilationData, CancellationToken)"/> method must have been invoked and <see cref="WhenInitializedTask"/> must be non-null.</remarks>
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
        /// <remarks>Driver must be initialized before invoking this method, i.e. <see cref="Initialize(AnalyzerExecutor, DiagnosticQueue, CompilationData, CancellationToken)"/> method must have been invoked and <see cref="WhenInitializedTask"/> must be non-null.</remarks>
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

        private void ExecuteSyntaxTreeActions(AnalysisScope analysisScope, AnalysisState analysisStateOpt, CancellationToken cancellationToken)
        {
            if (analysisScope.IsTreeAnalysis && !analysisScope.IsSyntaxOnlyTreeAnalysis)
            {
                // For partial analysis, only execute syntax tree actions if performing syntax analysis.
                return;
            }

            foreach (var tree in analysisScope.SyntaxTrees)
            {
                var isGeneratedCode = IsGeneratedCode(tree);
                if (isGeneratedCode && DoNotAnalyzeGeneratedCode)
                {
                    analysisStateOpt?.MarkSyntaxAnalysisComplete(tree, analysisScope.Analyzers);
                    continue;
                }

                foreach (var analyzer in analysisScope.Analyzers)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    ImmutableArray<SyntaxTreeAnalyzerAction> syntaxTreeActions;
                    if (_syntaxTreeActionsMap.TryGetValue(analyzer, out syntaxTreeActions))
                    {
                        // Execute actions for a given analyzer sequentially.
                        analyzerExecutor.ExecuteSyntaxTreeActions(syntaxTreeActions, analyzer, tree, analysisScope, analysisStateOpt, isGeneratedCode);
                    }
                    else
                    {
                        analysisStateOpt?.MarkSyntaxAnalysisComplete(tree, analyzer);
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

            Func<Exception, bool> nullFilter = null;
            return CreateAndAttachToCompilation(compilation, analyzers, options, analyzerManager, onAnalyzerException, nullFilter, reportAnalyzer, out newCompilation, cancellationToken: cancellationToken);
        }

        // internal for testing purposes
        internal static AnalyzerDriver CreateAndAttachToCompilation(
            Compilation compilation,
            ImmutableArray<DiagnosticAnalyzer> analyzers,
            AnalyzerOptions options,
            AnalyzerManager analyzerManager,
            Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException,
            Func<Exception, bool> analyzerExceptionFilter,
            bool reportAnalyzer,
            out Compilation newCompilation,
            CancellationToken cancellationToken)
        {
            AnalyzerDriver analyzerDriver = compilation.AnalyzerForLanguage(analyzers, analyzerManager);
            newCompilation = compilation.WithEventQueue(new AsyncQueue<CompilationEvent>());

            var categorizeDiagnostics = false;
            var analysisOptions = new CompilationWithAnalyzersOptions(options, onAnalyzerException, analyzerExceptionFilter, concurrentAnalysis: true, logAnalyzerExecutionTime: reportAnalyzer);
            analyzerDriver.Initialize(newCompilation, analysisOptions, new CompilationData(newCompilation), categorizeDiagnostics, cancellationToken);

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

            var suppressMessageState = compilationData.SuppressMessageAttributeState;
            var reportSuppressedDiagnostics = compilation.Options.ReportSuppressedDiagnostics;
            Diagnostic d;
            while (DiagnosticQueue.TryDequeue(out d))
            {
                d = suppressMessageState.ApplySourceSuppressions(d);
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
            return FilterDiagnosticsSuppressedInSource(diagnostics, compilation, compilationData.SuppressMessageAttributeState);
        }

        public ImmutableArray<Diagnostic> DequeueNonLocalDiagnostics(DiagnosticAnalyzer analyzer, Compilation compilation)
        {
            var diagnostics = DiagnosticQueue.DequeueNonLocalDiagnostics(analyzer);
            return FilterDiagnosticsSuppressedInSource(diagnostics, compilation, compilationData.SuppressMessageAttributeState);
        }

        private ImmutableArray<Diagnostic> FilterDiagnosticsSuppressedInSource(ImmutableArray<Diagnostic> diagnostics, Compilation compilation, SuppressMessageAttributeState suppressMessageState)
        {
            if (diagnostics.IsEmpty)
            {
                return diagnostics;
            }

            var reportSuppressedDiagnostics = compilation.Options.ReportSuppressedDiagnostics;
            var builder = ImmutableArray.CreateBuilder<Diagnostic>();
            for (var i = 0; i < diagnostics.Length; i++)
            {
#if DEBUG
                // We should have ignored diagnostics with invalid locations and reported analyzer exception diagnostic for the same.
                DiagnosticAnalysisContextHelpers.VerifyDiagnosticLocationsInCompilation(diagnostics[i], compilation);
#endif

                var diagnostic = suppressMessageState.ApplySourceSuppressions(diagnostics[i]);
                if (!reportSuppressedDiagnostics && diagnostic.IsSuppressed)
                {
                    // Diagnostic suppressed in source.
                    continue;
                }

                builder.Add(diagnostic);
            }

            return builder.ToImmutable();
        }

        private bool IsInGeneratedCode(Location location, Compilation compilation, CancellationToken cancellationToken)
        {
            if (_treatAllCodeAsNonGeneratedCode || !location.IsInSource)
            {
                return false;
            }

            if (IsGeneratedCode(location.SourceTree))
            {
                return true;
            }

            if (_generatedCodeAttribute != null)
            {
                var model = compilation.GetSemanticModel(location.SourceTree);
                for (var node = location.SourceTree.GetRoot(cancellationToken).FindNode(location.SourceSpan, getInnermostNodeForTie: true);
                    node != null;
                    node = node.Parent)
                {
                    var declaredSymbols = model.GetDeclaredSymbolsForNode(node, cancellationToken);
                    Debug.Assert(declaredSymbols != null);

                    foreach (var symbol in declaredSymbols)
                    {
                        if (GeneratedCodeUtilities.IsGeneratedSymbolWithGeneratedCodeAttribute(symbol, _generatedCodeAttribute))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
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
                        // Create separate worker tasks to process all compilation events - we do not want to process any events on the main thread.
                        workerTasks[i] = Task.Run(async () => await ProcessCompilationEventsCoreAsync(analysisScope, analysisStateOpt, prePopulatedEventQueue, cancellationToken).ConfigureAwait(false));
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    // Kick off tasks to execute syntax tree actions.
                    var syntaxTreeActionsTask = Task.Run(() => ExecuteSyntaxTreeActions(analysisScope, analysisStateOpt, cancellationToken));

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

                    ExecuteSyntaxTreeActions(analysisScope, analysisStateOpt, cancellationToken);
                }

                // Finally process the compilation completed event, if any.
                if (completedEvent != null)
                {
                    ProcessEvent(completedEvent, analysisScope, analysisStateOpt, cancellationToken);
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

                    ProcessEvent(e, analysisScope, analysisStateOpt, cancellationToken);
                }

                return completedEvent;
            }
            catch (Exception e) when (FatalError.ReportUnlessCanceled(e))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private void ProcessEvent(CompilationEvent e, AnalysisScope analysisScope, AnalysisState analysisStateOpt, CancellationToken cancellationToken)
        {
            ProcessEventCore(e, analysisScope, analysisStateOpt, cancellationToken);
            analysisStateOpt?.OnCompilationEventProcessed(e, analysisScope);
        }

        private void ProcessEventCore(CompilationEvent e, AnalysisScope analysisScope, AnalysisState analysisStateOpt, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var symbolEvent = e as SymbolDeclaredCompilationEvent;
            if (symbolEvent != null)
            {
                ProcessSymbolDeclared(symbolEvent, analysisScope, analysisStateOpt, cancellationToken);
                return;
            }

            var completedEvent = e as CompilationUnitCompletedEvent;
            if (completedEvent != null)
            {
                ProcessCompilationUnitCompleted(completedEvent, analysisScope, analysisStateOpt, cancellationToken);
                return;
            }

            var endEvent = e as CompilationCompletedEvent;
            if (endEvent != null)
            {
                ProcessCompilationCompleted(endEvent, analysisScope, analysisStateOpt, cancellationToken);
                return;
            }

            var startedEvent = e as CompilationStartedEvent;
            if (startedEvent != null)
            {
                ProcessCompilationStarted(startedEvent, analysisScope, analysisStateOpt, cancellationToken);
                return;
            }

            throw new InvalidOperationException("Unexpected compilation event of type " + e.GetType().Name);
        }

        private void ProcessSymbolDeclared(SymbolDeclaredCompilationEvent symbolEvent, AnalysisScope analysisScope, AnalysisState analysisStateOpt, CancellationToken cancellationToken)
        {
            try
            {
                // Execute all analyzer actions.
                var symbol = symbolEvent.Symbol;
                var isGeneratedCodeSymbol = IsGeneratedCodeSymbol(symbol);
                if (!AnalysisScope.ShouldSkipSymbolAnalysis(symbolEvent))
                {
                    ExecuteSymbolActions(symbolEvent, analysisScope, analysisStateOpt, isGeneratedCodeSymbol, cancellationToken);
                }

                if (!AnalysisScope.ShouldSkipDeclarationAnalysis(symbol))
                {
                    ExecuteDeclaringReferenceActions(symbolEvent, analysisScope, analysisStateOpt, isGeneratedCodeSymbol, cancellationToken);
                }
            }
            finally
            {
                symbolEvent.FlushCache();
            }
        }

        private void ExecuteSymbolActions(SymbolDeclaredCompilationEvent symbolEvent, AnalysisScope analysisScope, AnalysisState analysisStateOpt, bool isGeneratedCodeSymbol, CancellationToken cancellationToken)
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
                    analyzerExecutor.ExecuteSymbolActions(actionsByKind[(int)symbol.Kind], analyzer, symbolEvent, GetTopmostNodeForAnalysis, analysisScope, analysisStateOpt, isGeneratedCodeSymbol);
                }
                else
                {
                    analysisStateOpt?.MarkSymbolComplete(symbol, analyzer);
                }
            }
        }

        private static SyntaxNode GetTopmostNodeForAnalysis(ISymbol symbol, SyntaxReference syntaxReference, Compilation compilation)
        {
            var model = compilation.GetSemanticModel(syntaxReference.SyntaxTree);
            return model.GetTopmostNodeForDiagnosticAnalysis(symbol, syntaxReference.GetSyntax());
        }

        protected abstract void ExecuteDeclaringReferenceActions(SymbolDeclaredCompilationEvent symbolEvent, AnalysisScope analysisScope, AnalysisState analysisStateOpt, bool isGeneratedCodeSymbol, CancellationToken cancellationToken);

        private void ProcessCompilationUnitCompleted(CompilationUnitCompletedEvent completedEvent, AnalysisScope analysisScope, AnalysisState analysisStateOpt, CancellationToken cancellationToken)
        {
            // When the compiler is finished with a compilation unit, we can run user diagnostics which
            // might want to ask the compiler for all the diagnostics in the source file, for example
            // to get information about unnecessary usings.

            var semanticModel = analysisStateOpt != null ?
                compilationData.GetOrCreateCachedSemanticModel(completedEvent.CompilationUnit, completedEvent.Compilation, cancellationToken) :
                completedEvent.SemanticModel;

            if (!analysisScope.ShouldAnalyze(semanticModel.SyntaxTree))
            {
                return;
            }

            var isGeneratedCode = IsGeneratedCode(semanticModel.SyntaxTree);
            if (isGeneratedCode && DoNotAnalyzeGeneratedCode)
            {
                analysisStateOpt?.MarkEventComplete(completedEvent, analysisScope.Analyzers);
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
                        analyzerExecutor.ExecuteSemanticModelActions(semanticModelActions, analyzer, semanticModel, completedEvent, analysisScope, analysisStateOpt, isGeneratedCode);
                    }
                    else
                    {
                        analysisStateOpt?.MarkEventComplete(completedEvent, analyzer);
                    }
                }
            }
            finally
            {
                completedEvent.FlushCache();
            }
        }

        private void ProcessCompilationStarted(CompilationStartedEvent startedEvent, AnalysisScope analysisScope, AnalysisState analysisStateOpt, CancellationToken cancellationToken)
        {
            ExecuteCompilationActions(_compilationActionsMap, startedEvent, analysisScope, analysisStateOpt, cancellationToken);
        }

        private void ProcessCompilationCompleted(CompilationCompletedEvent endEvent, AnalysisScope analysisScope, AnalysisState analysisStateOpt, CancellationToken cancellationToken)
        {
            ExecuteCompilationActions(_compilationEndActionsMap, endEvent, analysisScope, analysisStateOpt, cancellationToken);
        }

        private void ExecuteCompilationActions(
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
                        analyzerExecutor.ExecuteCompilationActions(compilationActions, analyzer, compilationEvent, analysisScope, analysisStateOpt);
                    }
                    else
                    {
                        analysisStateOpt?.MarkEventComplete(compilationEvent, analyzer);
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

        internal static Action<Diagnostic, DiagnosticAnalyzer> GetDiagnosticSink(Action<Diagnostic, DiagnosticAnalyzer> addDiagnosticCore, Compilation compilation)
        {
            return (diagnostic, analyzer) =>
            {
                var filteredDiagnostic = GetFilteredDiagnostic(diagnostic, compilation);
                if (filteredDiagnostic != null)
                {
                    addDiagnosticCore(filteredDiagnostic, analyzer);
                }
            };
        }

        private static Diagnostic GetFilteredDiagnostic(Diagnostic diagnostic, Compilation compilation)
        {
            return compilation.Options.FilterDiagnostic(diagnostic);
        }

        private static ImmutableArray<DiagnosticAnalyzer> GetUnsuppressedAnalyzers(
            ImmutableArray<DiagnosticAnalyzer> analyzers,
            AnalyzerManager analyzerManager,
            AnalyzerExecutor analyzerExecutor)
        {
            var builder = ImmutableArray.CreateBuilder<DiagnosticAnalyzer>();
            foreach (var analyzer in analyzers)
            {
                if (!IsDiagnosticAnalyzerSuppressed(analyzer, analyzerExecutor.Compilation.Options, analyzerManager, analyzerExecutor))
                {
                    builder.Add(analyzer);
                }
            }

            return builder.ToImmutable();
        }

        private static async Task<AnalyzerActions> GetAnalyzerActionsAsync(
            ImmutableArray<DiagnosticAnalyzer> analyzers,
            AnalyzerManager analyzerManager,
            AnalyzerExecutor analyzerExecutor)
        {
            var allAnalyzerActions = new AnalyzerActions();
            foreach (var analyzer in analyzers)
            {
                Debug.Assert(!IsDiagnosticAnalyzerSuppressed(analyzer, analyzerExecutor.Compilation.Options, analyzerManager, analyzerExecutor));

                var analyzerActions = await analyzerManager.GetAnalyzerActionsAsync(analyzer, analyzerExecutor).ConfigureAwait(false);
                if (analyzerActions != null)
                {
                    allAnalyzerActions = allAnalyzerActions.Append(analyzerActions);
                }
            }

            return allAnalyzerActions;
        }

        private static async Task<ImmutableDictionary<DiagnosticAnalyzer, SemaphoreSlim>> GetAnalyzerGateMapAsync(
            ImmutableArray<DiagnosticAnalyzer> analyzers,
            AnalyzerManager analyzerManager,
            AnalyzerExecutor analyzerExecutor)
        {
            var builder = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, SemaphoreSlim>();
            foreach (var analyzer in analyzers)
            {
                Debug.Assert(!IsDiagnosticAnalyzerSuppressed(analyzer, analyzerExecutor.Compilation.Options, analyzerManager, analyzerExecutor));

                var isConcurrent = await analyzerManager.IsConcurrentAnalyzerAsync(analyzer, analyzerExecutor).ConfigureAwait(false);
                if (!isConcurrent)
                {
                    // Non-concurrent analyzers need their action callbacks from the analyzer driver to be guarded by a gate.
                    var gate = new SemaphoreSlim(initialCount: 1);
                    builder.Add(analyzer, gate);
                }
            }

            return builder.ToImmutable();
        }

        private static async Task<ImmutableDictionary<DiagnosticAnalyzer, GeneratedCodeAnalysisFlags>> GetGeneratedCodeAnalysisFlagsAsync(
            ImmutableArray<DiagnosticAnalyzer> analyzers,
            AnalyzerManager analyzerManager,
            AnalyzerExecutor analyzerExecutor)
        {
            var builder = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, GeneratedCodeAnalysisFlags>();
            foreach (var analyzer in analyzers)
            {
                Debug.Assert(!IsDiagnosticAnalyzerSuppressed(analyzer, analyzerExecutor.Compilation.Options, analyzerManager, analyzerExecutor));

                var generatedCodeAnalysisFlags = await analyzerManager.GetGeneratedCodeAnalysisFlagsAsync(analyzer, analyzerExecutor).ConfigureAwait(false);
                builder.Add(analyzer, generatedCodeAnalysisFlags);
            }

            return builder.ToImmutable();
        }

        private bool IsGeneratedCodeSymbol(ISymbol symbol)
        {
            if (_treatAllCodeAsNonGeneratedCode)
            {
                return false;
            }

            if (_generatedCodeAttribute != null && GeneratedCodeUtilities.IsGeneratedSymbolWithGeneratedCodeAttribute(symbol, _generatedCodeAttribute))
            {
                return true;
            }

            foreach (var declaringRef in symbol.DeclaringSyntaxReferences)
            {
                if (!IsGeneratedCode(declaringRef.SyntaxTree))
                {
                    return false;
                }
            }

            return true;
        }

        protected bool IsGeneratedCode(SyntaxTree tree)
        {
            if (_treatAllCodeAsNonGeneratedCode)
            {
                return false;
            }

            Debug.Assert(_lazyGeneratedCodeFilesMap != null);

            lock (_lazyGeneratedCodeFilesMap)
            {
                bool isGenerated;
                if (!_lazyGeneratedCodeFilesMap.TryGetValue(tree, out isGenerated))
                {
                    isGenerated = _isGeneratedCode(tree, analyzerExecutor.CancellationToken);
                    _lazyGeneratedCodeFilesMap.Add(tree, isGenerated);
                }

                return isGenerated;
            }
        }

        protected bool DoNotAnalyzeGeneratedCode => _doNotAnalyzeGeneratedCode;

        internal async Task<AnalyzerActionCounts> GetAnalyzerActionCountsAsync(DiagnosticAnalyzer analyzer, CancellationToken cancellationToken)
        {
            var executor = analyzerExecutor.WithCancellationToken(cancellationToken);
            var analyzerActions = await analyzerManager.GetAnalyzerActionsAsync(analyzer, executor).ConfigureAwait(false);
            return AnalyzerActionCounts.Create(analyzerActions);
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
        /// <param name="isComment">Delegate to identify if the given trivia is a comment.</param>
        internal AnalyzerDriver(ImmutableArray<DiagnosticAnalyzer> analyzers, Func<SyntaxNode, TLanguageKindEnum> getKind, AnalyzerManager analyzerManager, Func<SyntaxTrivia, bool> isComment)
            : base(analyzers, analyzerManager, isComment)
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

        protected override void ExecuteDeclaringReferenceActions(
            SymbolDeclaredCompilationEvent symbolEvent,
            AnalysisScope analysisScope,
            AnalysisState analysisStateOpt,
            bool isGeneratedCodeSymbol,
            CancellationToken cancellationToken)
        {
            var symbol = symbolEvent.Symbol;
            var executeSyntaxNodeActions = ShouldExecuteSyntaxNodeActions(analysisScope);
            var executeCodeBlockActions = ShouldExecuteCodeBlockActions(analysisScope, symbol);
            var executeOperationActions = ShouldExecuteOperationActions(analysisScope);
            var executeOperationBlockActions = ShouldExecuteOperationBlockActions(analysisScope, symbol);

            if (executeSyntaxNodeActions || executeOperationActions || executeCodeBlockActions || executeOperationBlockActions)
            {
                var declaringReferences = symbolEvent.DeclaringSyntaxReferences;
                for (var i = 0; i < declaringReferences.Length; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var decl = declaringReferences[i];
                    if (analysisScope.FilterTreeOpt != null && analysisScope.FilterTreeOpt != decl.SyntaxTree)
                    {
                        continue;
                    }

                    var isInGeneratedCode = isGeneratedCodeSymbol || IsGeneratedCode(decl.SyntaxTree);
                    if (isInGeneratedCode && DoNotAnalyzeGeneratedCode)
                    {
                        analysisStateOpt?.MarkDeclarationComplete(symbol, i, analysisScope.Analyzers);
                        continue;
                    }

                    ExecuteDeclaringReferenceActions(decl, i, symbolEvent, analysisScope, analysisStateOpt, executeSyntaxNodeActions, executeOperationActions, executeCodeBlockActions, executeOperationBlockActions, isInGeneratedCode, cancellationToken);
                }
            }
            else if (analysisStateOpt != null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                analysisStateOpt.MarkDeclarationsComplete(symbol, analysisScope.Analyzers);

                var declaringReferences = symbolEvent.DeclaringSyntaxReferences;
                for (var i = 0; i < declaringReferences.Length; i++)
                {
                    var decl = declaringReferences[i];
                    ClearCachedAnalysisDataIfAnalyzed(decl, symbol, i, analysisStateOpt);
                }
            }
        }

        private void ClearCachedAnalysisDataIfAnalyzed(SyntaxReference declaration, ISymbol symbol, int declarationIndex, AnalysisState analysisState)
        {
            Debug.Assert(analysisState != null);

            if (!analysisState.IsDeclarationComplete(symbol, declarationIndex))
            {
                return;
            }

            compilationData.ClearDeclarationAnalysisData(declaration);
        }

        private DeclarationAnalysisData ComputeDeclarationAnalysisData(
            ISymbol symbol,
            SyntaxReference declaration,
            SemanticModel semanticModel,
            bool shouldExecuteSyntaxNodeActions,
            AnalysisScope analysisScope,
            Func<DeclarationAnalysisData> allocateData,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var declarationAnalysisData = allocateData();
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

        private void ExecuteDeclaringReferenceActions(
            SyntaxReference decl,
            int declarationIndex,
            SymbolDeclaredCompilationEvent symbolEvent,
            AnalysisScope analysisScope,
            AnalysisState analysisStateOpt,
            bool shouldExecuteSyntaxNodeActions,
            bool shouldExecuteOperationActions,
            bool shouldExecuteCodeBlockActions,
            bool shouldExecuteOperationBlockActions,
            bool isInGeneratedCode,
            CancellationToken cancellationToken)
        {
            Debug.Assert(shouldExecuteSyntaxNodeActions || shouldExecuteOperationActions || shouldExecuteCodeBlockActions || shouldExecuteOperationBlockActions);
            Debug.Assert(!isInGeneratedCode || !DoNotAnalyzeGeneratedCode);

            var symbol = symbolEvent.Symbol;

            SemanticModel semanticModel = analysisStateOpt != null ?
                compilationData.GetOrCreateCachedSemanticModel(decl.SyntaxTree, symbolEvent.Compilation, cancellationToken) :
                symbolEvent.SemanticModel(decl);

            var cacheAnalysisData = analysisScope.Analyzers.Length < analyzers.Length &&
                (!analysisScope.FilterSpanOpt.HasValue || analysisScope.FilterSpanOpt.Value.Length >= decl.SyntaxTree.GetRoot(cancellationToken).Span.Length);

            var declarationAnalysisData = compilationData.GetOrComputeDeclarationAnalysisData(
                decl,
                allocateData => ComputeDeclarationAnalysisData(symbol, decl, semanticModel, shouldExecuteSyntaxNodeActions, analysisScope, allocateData, cancellationToken),
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
                        analyzerExecutor.ExecuteSyntaxNodeActions(nodesToAnalyze, nodeActionsByKind,
                            analyzer, semanticModel, _getKind, declarationAnalysisData.TopmostNodeForAnalysis.FullSpan,
                            decl, declarationIndex, symbol, analysisScope, analysisStateOpt, isInGeneratedCode);
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
                                                analyzerExecutor.ExecuteOperationActions(operationsToAnalyze, operationActionsByKind,
                                                    analyzer, semanticModel, declarationAnalysisData.TopmostNodeForAnalysis.FullSpan,
                                                    decl, declarationIndex, symbol, analysisScope, analysisStateOpt, isInGeneratedCode);
                                            }
                                        }
                                    }

                                    if (shouldExecuteOperationBlockActions)
                                    {
                                        foreach (var analyzerActions in codeBlockActions)
                                        {
                                            analyzerExecutor.ExecuteOperationBlockActions(
                                                analyzerActions.OperationBlockStartActions, analyzerActions.OperationBlockActions,
                                                analyzerActions.OpererationBlockEndActions, analyzerActions.Analyzer, declarationAnalysisData.TopmostNodeForAnalysis, symbol,
                                                operationBlocksToAnalyze, operationsToAnalyze, semanticModel, decl, declarationIndex, analysisScope, analysisStateOpt, isInGeneratedCode);
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
                        analyzerExecutor.ExecuteCodeBlockActions(
                            analyzerActions.CodeBlockStartActions, analyzerActions.CodeBlockActions,
                            analyzerActions.CodeBlockEndActions, analyzerActions.Analyzer, declarationAnalysisData.TopmostNodeForAnalysis, symbol,
                            executableCodeBlocks, semanticModel, _getKind, decl, declarationIndex, analysisScope, analysisStateOpt, isInGeneratedCode);
                    }
                }
            }

            // Mark completion only if we are analyzing a span containing the entire syntax node.
            if (analysisStateOpt != null && !declarationAnalysisData.IsPartialAnalysis)
            {
                foreach (var analyzer in analysisScope.Analyzers)
                {
                    analysisStateOpt.MarkDeclarationComplete(symbol, declarationIndex, analyzer);
                }

                if (cacheAnalysisData)
                {
                    ClearCachedAnalysisDataIfAnalyzed(decl, symbol, declarationIndex, analysisStateOpt);
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
                    // Might be:
                    // (1) A field declaration statement with multiple fields declared.
                    //     If so, we execute syntax node analysis for entire field declaration (and its descendants)
                    //     if we processing the first field and skip syntax actions for remaining fields in the declaration.
                    // (2) A namespace declaration statement with qualified name "namespace A.B { }"
                    if (IsEquivalentSymbol(declaredSymbol, declInNode.DeclaredSymbol))
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

        private static bool IsEquivalentSymbol(ISymbol declaredSymbol, ISymbol otherSymbol)
        {
            if (declaredSymbol.Equals(otherSymbol))
            {
                return true;
            }

            // GetSymbolInfo(name syntax) for "A" in "namespace A.B { }" sometimes returns a symbol which doesn't match
            // the symbol declared in the compilation. So we do an equivalence check for such namespace symbols.
            return otherSymbol != null &&
                declaredSymbol.Kind == SymbolKind.Namespace &&
                otherSymbol.Kind == SymbolKind.Namespace &&
                declaredSymbol.Name == otherSymbol.Name &&
                declaredSymbol.ToDisplayString() == otherSymbol.ToDisplayString();
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
