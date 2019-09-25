// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
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

        // Cache delegates for static methods
        private static readonly Func<DiagnosticAnalyzer, bool> s_IsCompilerAnalyzerFunc = IsCompilerAnalyzer;

        private readonly Func<SyntaxTree, CancellationToken, bool> _isGeneratedCode;

        /// <summary>
        /// Set of diagnostic suppressions that are suppressed via analyzer suppression actions. 
        /// </summary>
        private readonly ConcurrentSet<Suppression> _programmaticSuppressions;

        /// <summary>
        /// Set of diagnostics that have already been processed for application of programmatic suppressions. 
        /// </summary>
        private readonly ConcurrentSet<Diagnostic> _diagnosticsProcessedForProgrammaticSuppressions;

        /// <summary>
        /// Flag indicating if the <see cref="Analyzers"/> include any <see cref="DiagnosticSuppressor"/>
        /// which can suppress reported analyzer/compiler diagnostics.
        /// </summary>
        private readonly bool _hasDiagnosticSuppressors;

        // Lazy fields/properties
        private CancellationTokenRegistration _queueRegistration;
        protected ImmutableArray<DiagnosticAnalyzer> Analyzers { get; }
        protected AnalyzerManager AnalyzerManager { get; }
        protected AnalyzerExecutor AnalyzerExecutor { get; private set; }
        protected CompilationData CurrentCompilationData { get; private set; }
        protected AnalyzerActions AnalyzerActions { get; private set; }

        /// <summary>
        /// Cache of additional analyzer actions to be executed per symbol per analyzer, which are registered in symbol start actions.
        /// We cache the tuple:
        ///   1. myActions: analyzer actions registered in the symbol start actions of containing namespace/type, which are to be executed for this symbol
        ///   2. childActions: analyzer actions registered in this symbol's start actions, which are to be executed for member symbols.
        /// </summary>
        private ConcurrentDictionary<(ISymbol, DiagnosticAnalyzer), AnalyzerActions> _perSymbolAnalyzerActionsCache;

        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<ImmutableArray<SymbolAnalyzerAction>>> _symbolActionsByKind;
        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<SemanticModelAnalyzerAction>> _semanticModelActionsMap;
        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<SyntaxTreeAnalyzerAction>> _syntaxTreeActionsMap;
        // Compilation actions and compilation end actions have separate maps so that it is easy to
        // execute the compilation actions before the compilation end actions.
        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<CompilationAnalyzerAction>> _compilationActionsMap;
        private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<CompilationAnalyzerAction>> _compilationEndActionsMap;

        /// <summary>
        /// Default analysis mode for generated code.
        /// </summary>
        /// <remarks>
        /// This mode should always guarantee that analyzer action callbacks are enabled for generated code, i.e. <see cref="GeneratedCodeAnalysisFlags.Analyze"/> is set.
        /// However, the default diagnostic reporting mode is liable to change in future.
        /// </remarks>
        internal const GeneratedCodeAnalysisFlags DefaultGeneratedCodeAnalysisFlags = GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics;

        /// <summary>
        /// Map from non-concurrent analyzers to the gate guarding callback into the analyzer. 
        /// </summary>
        private ImmutableDictionary<DiagnosticAnalyzer, SemaphoreSlim> _analyzerGateMap = ImmutableDictionary<DiagnosticAnalyzer, SemaphoreSlim>.Empty;

        /// <summary>
        /// Map from analyzers to their <see cref="GeneratedCodeAnalysisFlags"/> setting. 
        /// </summary>
        private ImmutableDictionary<DiagnosticAnalyzer, GeneratedCodeAnalysisFlags> _generatedCodeAnalysisFlagsMap;

        /// <summary>
        /// Set of unsuppressed analyzers that need to be executed. 
        /// </summary>
        private ImmutableHashSet<DiagnosticAnalyzer> _unsuppressedAnalyzers;

        /// <summary>
        /// True if all analyzers need to analyze and report diagnostics in generated code - we can assume all code to be non-generated code.
        /// </summary>
        private bool _treatAllCodeAsNonGeneratedCode;

        /// <summary>
        /// True if no analyzer needs generated code analysis - we can skip all analysis on a generated code symbol/tree.
        /// </summary>
        private bool _doNotAnalyzeGeneratedCode;

        /// <summary>
        /// Lazily populated dictionary indicating whether a source file is a generated code file or not - we populate it lazily to avoid realizing all syntax trees in the compilation upfront.
        /// </summary>
        private ConcurrentDictionary<SyntaxTree, bool> _lazyGeneratedCodeFilesMap;

        /// <summary>
        /// Lazily populated dictionary from tree to declared symbols with GeneratedCodeAttribute.
        /// </summary>
        private Dictionary<SyntaxTree, ImmutableHashSet<ISymbol>> _lazyGeneratedCodeSymbolsForTreeMap;

        /// <summary>
        /// Lazily populated dictionary from symbol to a bool indicating if it is a generated code symbol.
        /// </summary>
        private ConcurrentDictionary<ISymbol, bool> _lazyIsGeneratedCodeSymbolMap;

        /// <summary>
        /// Lazily populated dictionary indicating whether a source file has any hidden regions - we populate it lazily to avoid realizing all syntax trees in the compilation upfront.
        /// </summary>
        private ConcurrentDictionary<SyntaxTree, bool> _lazyTreesWithHiddenRegionsMap;

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
            this.Analyzers = analyzers;
            this.AnalyzerManager = analyzerManager;
            _isGeneratedCode = (tree, ct) => GeneratedCodeUtilities.IsGeneratedCode(tree, isComment, ct);
            _hasDiagnosticSuppressors = this.Analyzers.Any(a => a is DiagnosticSuppressor);
            _programmaticSuppressions = _hasDiagnosticSuppressors ? new ConcurrentSet<Suppression>() : null;
            _diagnosticsProcessedForProgrammaticSuppressions = _hasDiagnosticSuppressors ? new ConcurrentSet<Diagnostic>(ReferenceEqualityComparer.Instance) : null;
        }

        /// <summary>
        /// Initializes the <see cref="AnalyzerActions"/> and related actions maps for the analyzer driver.
        /// It kicks off the <see cref="WhenInitializedTask"/> task for initialization.
        /// Note: This method must be invoked exactly once on the driver.
        /// </summary>
        private void Initialize(AnalyzerExecutor analyzerExecutor, DiagnosticQueue diagnosticQueue, CompilationData compilationData, CancellationToken cancellationToken)
        {
            try
            {
                Debug.Assert(_initializeTask == null);

                this.AnalyzerExecutor = analyzerExecutor;
                this.CurrentCompilationData = compilationData;
                this.DiagnosticQueue = diagnosticQueue;

                // Compute the set of effective actions based on suppression, and running the initial analyzers
                _initializeTask = Task.Run(async () =>
                {
                    (AnalyzerActions, _unsuppressedAnalyzers) = await GetAnalyzerActionsAsync(Analyzers, AnalyzerManager, analyzerExecutor).ConfigureAwait(false);
                    _analyzerGateMap = await CreateAnalyzerGateMapAsync(_unsuppressedAnalyzers, AnalyzerManager, analyzerExecutor).ConfigureAwait(false);
                    _generatedCodeAnalysisFlagsMap = await CreateGeneratedCodeAnalysisFlagsMapAsync(_unsuppressedAnalyzers, AnalyzerManager, analyzerExecutor).ConfigureAwait(false);
                    _doNotAnalyzeGeneratedCode = ComputeShouldSkipAnalysisOnGeneratedCode(_unsuppressedAnalyzers);
                    _treatAllCodeAsNonGeneratedCode = ComputeShouldTreatAllCodeAsNonGeneratedCode(_unsuppressedAnalyzers, _generatedCodeAnalysisFlagsMap);
                    _lazyGeneratedCodeFilesMap = _treatAllCodeAsNonGeneratedCode ? null : new ConcurrentDictionary<SyntaxTree, bool>();
                    _lazyGeneratedCodeSymbolsForTreeMap = _treatAllCodeAsNonGeneratedCode ? null : new Dictionary<SyntaxTree, ImmutableHashSet<ISymbol>>();
                    _lazyIsGeneratedCodeSymbolMap = _treatAllCodeAsNonGeneratedCode ? null : new ConcurrentDictionary<ISymbol, bool>();
                    _lazyTreesWithHiddenRegionsMap = _treatAllCodeAsNonGeneratedCode ? null : new ConcurrentDictionary<SyntaxTree, bool>();
                    _generatedCodeAttribute = analyzerExecutor.Compilation?.GetTypeByMetadataName("System.CodeDom.Compiler.GeneratedCodeAttribute");

                    _symbolActionsByKind = MakeSymbolActionsByKind(this.AnalyzerActions);
                    _semanticModelActionsMap = MakeSemanticModelActionsByAnalyzer(this.AnalyzerActions);
                    _syntaxTreeActionsMap = MakeSyntaxTreeActionsByAnalyzer(this.AnalyzerActions);
                    _compilationActionsMap = MakeCompilationActionsByAnalyzer(this.AnalyzerActions.CompilationActions);
                    _compilationEndActionsMap = MakeCompilationActionsByAnalyzer(this.AnalyzerActions.CompilationEndActions);

                    if (this.AnalyzerActions.SymbolStartActionsCount > 0)
                    {
                        _perSymbolAnalyzerActionsCache = new ConcurrentDictionary<(ISymbol, DiagnosticAnalyzer), AnalyzerActions>();
                    }

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

            var analyzerExecutor = AnalyzerExecutor.Create(
                compilation, analysisOptions.Options ?? AnalyzerOptions.Empty, addNotCategorizedDiagnosticOpt, newOnAnalyzerException, analysisOptions.AnalyzerExceptionFilter,
                IsCompilerAnalyzer, AnalyzerManager, ShouldSkipAnalysisOnGeneratedCode, ShouldSuppressGeneratedCodeDiagnostic, IsGeneratedOrHiddenCodeLocation, GetAnalyzerGate,
                getSemanticModel: tree => CurrentCompilationData.GetOrCreateCachedSemanticModel(tree, compilation, cancellationToken),
                analysisOptions.LogAnalyzerExecutionTime, addCategorizedLocalDiagnosticOpt, addCategorizedNonLocalDiagnosticOpt, s => _programmaticSuppressions.Add(s), cancellationToken);

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

        private bool ComputeShouldSkipAnalysisOnGeneratedCode(ImmutableHashSet<DiagnosticAnalyzer> analyzers)
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

        /// <summary>
        /// Returns true if all analyzers need to analyze and report diagnostics in generated code - we can assume all code to be non-generated code.
        /// </summary>
        private static bool ComputeShouldTreatAllCodeAsNonGeneratedCode(ImmutableHashSet<DiagnosticAnalyzer> analyzers, ImmutableDictionary<DiagnosticAnalyzer, GeneratedCodeAnalysisFlags> generatedCodeAnalysisFlagsMap)
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
                OnDriverException(WhenInitializedTask, this.AnalyzerExecutor, analysisScope.Analyzers);
            }
            else if (!WhenInitializedTask.IsCanceled)
            {
                this.AnalyzerExecutor = this.AnalyzerExecutor.WithCancellationToken(cancellationToken);

                await ProcessCompilationEventsAsync(analysisScope, analysisStateOpt, usingPrePopulatedEventQueue, cancellationToken).ConfigureAwait(false);

                // If not using pre-populated event queue (batch mode), then verify all symbol end actions were processed.
                if (!usingPrePopulatedEventQueue)
                {
                    AnalyzerManager.VerifyAllSymbolEndActionsExecuted();
                }
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
                        AnalyzerExecutor.TryExecuteSyntaxTreeActions(syntaxTreeActions, analyzer, tree, analysisScope, analysisStateOpt, isGeneratedCode);
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
            var analysisOptions = new CompilationWithAnalyzersOptions(options, onAnalyzerException, analyzerExceptionFilter: analyzerExceptionFilter, concurrentAnalysis: true, logAnalyzerExecutionTime: reportAnalyzer, reportSuppressedDiagnostics: false);
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
                    OnDriverException(this.WhenCompletedTask, this.AnalyzerExecutor, this.Analyzers);
                }
            }

            var suppressMessageState = CurrentCompilationData.SuppressMessageAttributeState;
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

        public void ApplyProgrammaticSuppressions(DiagnosticBag reportedDiagnostics, Compilation compilation)
        {
            Debug.Assert(!reportedDiagnostics.IsEmptyWithoutResolution);
            if (!_hasDiagnosticSuppressors)
            {
                return;
            }

            var newDiagnostics = ApplyProgrammaticSuppressionsCore(reportedDiagnostics.ToReadOnly(), compilation);
            reportedDiagnostics.Clear();
            reportedDiagnostics.AddRange(newDiagnostics);
        }

        public ImmutableArray<Diagnostic> ApplyProgrammaticSuppressions(ImmutableArray<Diagnostic> reportedDiagnostics, Compilation compilation)
        {
            if (reportedDiagnostics.IsEmpty ||
                !_hasDiagnosticSuppressors)
            {
                return reportedDiagnostics;
            }

            return ApplyProgrammaticSuppressionsCore(reportedDiagnostics, compilation);
        }

        private ImmutableArray<Diagnostic> ApplyProgrammaticSuppressionsCore(ImmutableArray<Diagnostic> reportedDiagnostics, Compilation compilation)
        {
            Debug.Assert(_hasDiagnosticSuppressors);
            Debug.Assert(!reportedDiagnostics.IsEmpty);
            Debug.Assert(_programmaticSuppressions != null);
            Debug.Assert(_diagnosticsProcessedForProgrammaticSuppressions != null);

            try
            {
                // We do not allow analyzer based suppressions for following category of diagnostics:
                //  1. Diagnostics which are already suppressed in source via pragma/suppress message attribute.
                //  2. Diagnostics explicitly tagged as not configurable by analyzer authors - this includes compiler error diagnostics.
                //  3. Diagnostics which are marked as error by default by diagnostic authors.
                var suppressableDiagnostics = reportedDiagnostics.Where(d => !d.IsSuppressed &&
                                                                             !d.IsNotConfigurable() &&
                                                                             d.DefaultSeverity != DiagnosticSeverity.Error &&
                                                                             !_diagnosticsProcessedForProgrammaticSuppressions.Contains(d));

                if (suppressableDiagnostics.IsEmpty())
                {
                    return reportedDiagnostics;
                }

                executeSuppressionActions(suppressableDiagnostics, concurrent: compilation.Options.ConcurrentBuild);
                if (_programmaticSuppressions.IsEmpty)
                {
                    return reportedDiagnostics;
                }

                var builder = ArrayBuilder<Diagnostic>.GetInstance(reportedDiagnostics.Length);
                ImmutableDictionary<Diagnostic, ProgrammaticSuppressionInfo> programmaticSuppressionsByDiagnostic = createProgrammaticSuppressionsByDiagnosticMap(_programmaticSuppressions);
                foreach (var diagnostic in reportedDiagnostics)
                {
                    if (programmaticSuppressionsByDiagnostic.TryGetValue(diagnostic, out var programmaticSuppressionInfo))
                    {
                        Debug.Assert(suppressableDiagnostics.Contains(diagnostic));
                        Debug.Assert(!diagnostic.IsSuppressed);
                        var suppressedDiagnostic = diagnostic.WithProgrammaticSuppression(programmaticSuppressionInfo);
                        Debug.Assert(suppressedDiagnostic.IsSuppressed);
                        builder.Add(suppressedDiagnostic);
                    }
                    else
                    {
                        builder.Add(diagnostic);
                    }
                }

                return builder.ToImmutableAndFree();
            }
            finally
            {
                // Mark the reported diagnostics as processed for programmatic suppressions to avoid duplicate callbacks to suppressors for same diagnostics.
                _diagnosticsProcessedForProgrammaticSuppressions.AddRange(reportedDiagnostics);
            }

            void executeSuppressionActions(IEnumerable<Diagnostic> reportedDiagnostics, bool concurrent)
            {
                var suppressors = this.Analyzers.OfType<DiagnosticSuppressor>();
                if (concurrent)
                {
                    Parallel.ForEach(suppressors, suppressor =>
                    {
                        AnalyzerExecutor.ExecuteSuppressionAction(suppressor, getSuppressableDiagnostics(suppressor));
                    });
                }
                else
                {
                    foreach (var suppressor in suppressors)
                    {
                        AnalyzerExecutor.ExecuteSuppressionAction(suppressor, getSuppressableDiagnostics(suppressor));
                    }
                }

                return;

                ImmutableArray<Diagnostic> getSuppressableDiagnostics(DiagnosticSuppressor suppressor)
                {
                    var supportedSuppressions = AnalyzerManager.GetSupportedSuppressionDescriptors(suppressor, AnalyzerExecutor);
                    if (supportedSuppressions.IsEmpty)
                    {
                        return ImmutableArray<Diagnostic>.Empty;
                    }

                    var builder = ArrayBuilder<Diagnostic>.GetInstance();
                    foreach (var diagnostic in reportedDiagnostics)
                    {
                        if (supportedSuppressions.Contains(s => s.SuppressedDiagnosticId == diagnostic.Id))
                        {
                            builder.Add(diagnostic);
                        }
                    }

                    return builder.ToImmutableAndFree();
                }
            }

            static ImmutableDictionary<Diagnostic, ProgrammaticSuppressionInfo> createProgrammaticSuppressionsByDiagnosticMap(ConcurrentSet<Suppression> programmaticSuppressions)
            {
                var programmaticSuppressionsBuilder = PooledDictionary<Diagnostic, ImmutableHashSet<(string, LocalizableString)>.Builder>.GetInstance();
                foreach (var programmaticSuppression in programmaticSuppressions)
                {
                    if (!programmaticSuppressionsBuilder.TryGetValue(programmaticSuppression.SuppressedDiagnostic, out var set))
                    {
                        set = ImmutableHashSet.CreateBuilder<(string, LocalizableString)>();
                        programmaticSuppressionsBuilder.Add(programmaticSuppression.SuppressedDiagnostic, set);
                    }

                    set.Add((programmaticSuppression.Descriptor.Id, programmaticSuppression.Descriptor.Justification));
                }

                var mapBuilder = ImmutableDictionary.CreateBuilder<Diagnostic, ProgrammaticSuppressionInfo>();
                foreach (var (diagnostic, set) in programmaticSuppressionsBuilder)
                {
                    mapBuilder.Add(diagnostic, new ProgrammaticSuppressionInfo(set.ToImmutable()));
                }

                return mapBuilder.ToImmutable();
            }
        }

        public ImmutableArray<Diagnostic> DequeueLocalDiagnosticsAndApplySuppressions(DiagnosticAnalyzer analyzer, bool syntax, Compilation compilation)
        {
            var diagnostics = syntax ? DiagnosticQueue.DequeueLocalSyntaxDiagnostics(analyzer) : DiagnosticQueue.DequeueLocalSemanticDiagnostics(analyzer);
            return FilterDiagnosticsSuppressedInSourceOrByAnalyzers(diagnostics, compilation);
        }

        public ImmutableArray<Diagnostic> DequeueNonLocalDiagnosticsAndApplySuppressions(DiagnosticAnalyzer analyzer, Compilation compilation)
        {
            var diagnostics = DiagnosticQueue.DequeueNonLocalDiagnostics(analyzer);
            return FilterDiagnosticsSuppressedInSourceOrByAnalyzers(diagnostics, compilation);
        }

        private ImmutableArray<Diagnostic> FilterDiagnosticsSuppressedInSourceOrByAnalyzers(ImmutableArray<Diagnostic> diagnostics, Compilation compilation)
        {
            diagnostics = FilterDiagnosticsSuppressedInSource(diagnostics, compilation, CurrentCompilationData.SuppressMessageAttributeState);
            return ApplyProgrammaticSuppressions(diagnostics, compilation);
        }

        private static ImmutableArray<Diagnostic> FilterDiagnosticsSuppressedInSource(ImmutableArray<Diagnostic> diagnostics, Compilation compilation, SuppressMessageAttributeState suppressMessageState)
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

            // Check if this is a generated code location.
            if (IsGeneratedOrHiddenCodeLocation(location.SourceTree, location.SourceSpan))
            {
                return true;
            }

            // Check if the file has generated code definitions (i.e. symbols with GeneratedCodeAttribute).
            if (_generatedCodeAttribute != null && _lazyGeneratedCodeSymbolsForTreeMap != null)
            {
                var generatedCodeSymbolsInTree = GetOrComputeGeneratedCodeSymbolsInTree(location.SourceTree, compilation, cancellationToken);
                if (generatedCodeSymbolsInTree.Count > 0)
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
                            if (generatedCodeSymbolsInTree.Contains(symbol))
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        private ImmutableHashSet<ISymbol> GetOrComputeGeneratedCodeSymbolsInTree(SyntaxTree tree, Compilation compilation, CancellationToken cancellationToken)
        {
            Debug.Assert(_lazyGeneratedCodeSymbolsForTreeMap != null);

            ImmutableHashSet<ISymbol> generatedCodeSymbols;
            lock (_lazyGeneratedCodeSymbolsForTreeMap)
            {
                if (_lazyGeneratedCodeSymbolsForTreeMap.TryGetValue(tree, out generatedCodeSymbols))
                {
                    return generatedCodeSymbols;
                }
            }

            generatedCodeSymbols = ComputeGeneratedCodeSymbolsInTree(tree, compilation, cancellationToken);

            lock (_lazyGeneratedCodeSymbolsForTreeMap)
            {
                ImmutableHashSet<ISymbol> existingGeneratedCodeSymbols;
                if (!_lazyGeneratedCodeSymbolsForTreeMap.TryGetValue(tree, out existingGeneratedCodeSymbols))
                {
                    _lazyGeneratedCodeSymbolsForTreeMap.Add(tree, generatedCodeSymbols);
                }
                else
                {
                    Debug.Assert(existingGeneratedCodeSymbols.SetEquals(generatedCodeSymbols));
                }
            }

            return generatedCodeSymbols;
        }

        private ImmutableHashSet<ISymbol> ComputeGeneratedCodeSymbolsInTree(SyntaxTree tree, Compilation compilation, CancellationToken cancellationToken)
        {
            // PERF: Bail out early if file doesn't have "GeneratedCode" text.
            var text = tree.GetText(cancellationToken).ToString();
            if (!text.Contains("GeneratedCode"))
            {
                return ImmutableHashSet<ISymbol>.Empty;
            }

            var model = compilation.GetSemanticModel(tree);
            var root = tree.GetRoot(cancellationToken);
            var span = root.FullSpan;
            var declarationInfoBuilder = ArrayBuilder<DeclarationInfo>.GetInstance();
            model.ComputeDeclarationsInSpan(span, getSymbol: true, builder: declarationInfoBuilder, cancellationToken: cancellationToken);

            ImmutableHashSet<ISymbol>.Builder generatedSymbolsBuilderOpt = null;
            foreach (var declarationInfo in declarationInfoBuilder)
            {
                var symbol = declarationInfo.DeclaredSymbol;
                if (symbol != null &&
                    GeneratedCodeUtilities.IsGeneratedSymbolWithGeneratedCodeAttribute(symbol, _generatedCodeAttribute))
                {
                    generatedSymbolsBuilderOpt = generatedSymbolsBuilderOpt ?? ImmutableHashSet.CreateBuilder<ISymbol>();
                    generatedSymbolsBuilderOpt.Add(symbol);
                }
            }

            declarationInfoBuilder.Free();
            return generatedSymbolsBuilderOpt != null ? generatedSymbolsBuilderOpt.ToImmutable() : ImmutableHashSet<ISymbol>.Empty;
        }

        /// <summary>
        /// Return a task that completes when the driver is initialized.
        /// </summary>
        public Task WhenInitializedTask => _initializeTask;

        /// <summary>
        /// Return a task that completes when the driver is done producing diagnostics.
        /// </summary>
        public Task WhenCompletedTask => _primaryTask;

        internal ImmutableDictionary<DiagnosticAnalyzer, TimeSpan> AnalyzerExecutionTimes => AnalyzerExecutor.AnalyzerExecutionTimes;
        internal TimeSpan ResetAnalyzerExecutionTime(DiagnosticAnalyzer analyzer) => AnalyzerExecutor.ResetAnalyzerExecutionTime(analyzer);

        private static ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<ImmutableArray<SymbolAnalyzerAction>>> MakeSymbolActionsByKind(AnalyzerActions analyzerActions)
        {
            var builder = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, ImmutableArray<ImmutableArray<SymbolAnalyzerAction>>>();
            var actionsByAnalyzers = analyzerActions.SymbolActions.GroupBy(action => action.Analyzer);
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

        private static ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<SyntaxTreeAnalyzerAction>> MakeSyntaxTreeActionsByAnalyzer(AnalyzerActions analyzerActions)
        {
            var builder = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, ImmutableArray<SyntaxTreeAnalyzerAction>>();
            var actionsByAnalyzers = analyzerActions.SyntaxTreeActions.GroupBy(action => action.Analyzer);
            foreach (var analyzerAndActions in actionsByAnalyzers)
            {
                builder.Add(analyzerAndActions.Key, analyzerAndActions.ToImmutableArray());
            }

            return builder.ToImmutable();
        }

        private static ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<SemanticModelAnalyzerAction>> MakeSemanticModelActionsByAnalyzer(AnalyzerActions analyzerActions)
        {
            var builder = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, ImmutableArray<SemanticModelAnalyzerAction>>();
            var actionsByAnalyzers = analyzerActions.SemanticModelActions.GroupBy(action => action.Analyzer);
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

                    // NOTE: IsCompleted guarantees that Count will not increase
                    //       the reverse is not true, so we need to check IsCompleted first and then check the Count
                    if ((prePopulatedEventQueue || CompilationEventQueue.IsCompleted) &&
                        CompilationEventQueue.Count == 0)
                    {
                        break;
                    }

                    CompilationEvent e;
                    try
                    {
                        if (!CompilationEventQueue.TryDequeue(out e))
                        {
                            if (!prePopulatedEventQueue)
                            {
                                e = await CompilationEventQueue.DequeueAsync(cancellationToken).ConfigureAwait(false);
                            }
                            else
                            {
                                return completedEvent;
                            }
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
            EventProcessedState eventProcessedState = await TryProcessEventCoreAsync(e, analysisScope, analysisStateOpt, cancellationToken).ConfigureAwait(false);

            ImmutableArray<DiagnosticAnalyzer> processedAnalyzers;
            switch (eventProcessedState.Kind)
            {
                case EventProcessedStateKind.Processed:
                    processedAnalyzers = analysisScope.Analyzers;
                    break;

                case EventProcessedStateKind.PartiallyProcessed:
                    processedAnalyzers = eventProcessedState.SubsetProcessedAnalyzers;
                    break;

                default:
                    return;
            }

            await OnEventProcessedCoreAsync(e, processedAnalyzers, analysisStateOpt, cancellationToken).ConfigureAwait(false);
        }

        private async Task OnEventProcessedCoreAsync(CompilationEvent e, ImmutableArray<DiagnosticAnalyzer> processedAnalyzers, AnalysisState analysisStateOpt, CancellationToken cancellationToken)
        {
            if (analysisStateOpt != null)
            {
                await analysisStateOpt.OnCompilationEventProcessedAsync(e, processedAnalyzers, onSymbolAndMembersProcessedAsync).ConfigureAwait(false);
            }
            else if (AnalyzerActions.SymbolStartActionsCount > 0 &&
                e is SymbolDeclaredCompilationEvent symbolDeclaredEvent)
            {
                foreach (var analyzer in processedAnalyzers)
                {
                    await onSymbolAndMembersProcessedAsync(symbolDeclaredEvent.Symbol, analyzer).ConfigureAwait(false);
                }
            }

            async Task onSymbolAndMembersProcessedAsync(ISymbol symbol, DiagnosticAnalyzer analyzer)
            {
                if (AnalyzerActions.SymbolStartActionsCount == 0 || symbol.IsImplicitlyDeclared)
                {
                    return;
                }

                _perSymbolAnalyzerActionsCache.TryRemove((symbol, analyzer), out _);

                await processContainerOnMemberCompletedAsync(symbol.ContainingNamespace, symbol, analyzer).ConfigureAwait(false);
                await processContainerOnMemberCompletedAsync(symbol.ContainingType, symbol, analyzer).ConfigureAwait(false);
            }

            async Task processContainerOnMemberCompletedAsync(INamespaceOrTypeSymbol containerSymbol, ISymbol processedMemberSymbol, DiagnosticAnalyzer analyzer)
            {
                if (containerSymbol != null &&
                    AnalyzerExecutor.TryExecuteSymbolEndActionsForContainer(containerSymbol, processedMemberSymbol,
                        analyzer, GetTopmostNodeForAnalysis, analysisStateOpt, out SymbolDeclaredCompilationEvent processedContainerEvent))
                {
                    await OnEventProcessedCoreAsync(processedContainerEvent, ImmutableArray.Create(analyzer), analysisStateOpt, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private async Task<EventProcessedState> TryProcessEventCoreAsync(CompilationEvent e, AnalysisScope analysisScope, AnalysisState analysisStateOpt, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var symbolEvent = e as SymbolDeclaredCompilationEvent;
            if (symbolEvent != null)
            {
                return await TryProcessSymbolDeclaredAsync(symbolEvent, analysisScope, analysisStateOpt, cancellationToken).ConfigureAwait(false);
            }

            var completedEvent = e as CompilationUnitCompletedEvent;
            if (completedEvent != null)
            {
                return TryProcessCompilationUnitCompleted(completedEvent, analysisScope, analysisStateOpt, cancellationToken) ?
                    EventProcessedState.Processed :
                    EventProcessedState.NotProcessed;
            }

            var endEvent = e as CompilationCompletedEvent;
            if (endEvent != null)
            {
                return TryProcessCompilationCompleted(endEvent, analysisScope, analysisStateOpt, cancellationToken) ?
                    EventProcessedState.Processed :
                    EventProcessedState.NotProcessed;
            }

            var startedEvent = e as CompilationStartedEvent;
            if (startedEvent != null)
            {
                return TryProcessCompilationStarted(startedEvent, analysisScope, analysisStateOpt, cancellationToken) ?
                    EventProcessedState.Processed :
                    EventProcessedState.NotProcessed;
            }

            throw new InvalidOperationException("Unexpected compilation event of type " + e.GetType().Name);
        }

        /// <summary>
        /// Tries to execute symbol action, symbol start/end actions and declaration actions for the given symbol.
        /// </summary>
        /// <returns>
        /// <see cref="EventProcessedState"/> indicating the current state of processing of the given compilation event.
        /// </returns>
        private async Task<EventProcessedState> TryProcessSymbolDeclaredAsync(SymbolDeclaredCompilationEvent symbolEvent, AnalysisScope analysisScope, AnalysisState analysisStateOpt, CancellationToken cancellationToken)
        {
            try
            {
                // Attempt to execute all analyzer actions.
                var processedState = EventProcessedState.Processed;
                var symbol = symbolEvent.Symbol;
                var isGeneratedCodeSymbol = IsGeneratedCodeSymbol(symbol);

                var skipSymbolAnalysis = AnalysisScope.ShouldSkipSymbolAnalysis(symbolEvent);
                var skipDeclarationAnalysis = AnalysisScope.ShouldSkipDeclarationAnalysis(symbol);
                var hasPerSymbolActions = AnalyzerActions.SymbolStartActionsCount > 0 && (!skipSymbolAnalysis || !skipDeclarationAnalysis);

                AnalyzerActions perSymbolActions = hasPerSymbolActions ?
                    await GetPerSymbolAnalyzerActionsAsync(symbol, analysisScope, analysisStateOpt, cancellationToken).ConfigureAwait(false) :
                    null;

                if (!skipSymbolAnalysis &&
                    !TryExecuteSymbolActions(symbolEvent, analysisScope, analysisStateOpt, isGeneratedCodeSymbol, cancellationToken))
                {
                    processedState = EventProcessedState.NotProcessed;
                }

                if (!skipDeclarationAnalysis &&
                    !TryExecuteDeclaringReferenceActions(symbolEvent, analysisScope, analysisStateOpt, isGeneratedCodeSymbol, perSymbolActions, cancellationToken))
                {
                    processedState = EventProcessedState.NotProcessed;
                }

                ImmutableArray<DiagnosticAnalyzer> subsetProcessedAnalyzers = default;
                if (processedState.Kind == EventProcessedStateKind.Processed &&
                    hasPerSymbolActions &&
                    !TryExecuteSymbolEndActions(perSymbolActions, symbolEvent, analysisScope, analysisStateOpt, cancellationToken, out subsetProcessedAnalyzers))
                {
                    processedState = subsetProcessedAnalyzers.IsDefaultOrEmpty ? EventProcessedState.NotProcessed : EventProcessedState.CreatePartiallyProcessed(subsetProcessedAnalyzers);
                }

                return processedState;
            }
            finally
            {
                symbolEvent.FlushCache();
            }
        }

        /// <summary>
        /// Tries to execute symbol actions.
        /// </summary>
        /// <returns>
        /// True, if successfully executed the actions for the given analysis scope OR no actions were required to be executed for the given analysis scope.
        /// False, otherwise.
        /// </returns>
        private bool TryExecuteSymbolActions(SymbolDeclaredCompilationEvent symbolEvent, AnalysisScope analysisScope, AnalysisState analysisStateOpt, bool isGeneratedCodeSymbol, CancellationToken cancellationToken)
        {
            var symbol = symbolEvent.Symbol;
            if (!analysisScope.ShouldAnalyze(symbol))
            {
                return true;
            }

            var success = true;
            foreach (var analyzer in analysisScope.Analyzers)
            {
                // Invoke symbol analyzers only for source symbols.
                ImmutableArray<ImmutableArray<SymbolAnalyzerAction>> actionsByKind;
                if (_symbolActionsByKind.TryGetValue(analyzer, out actionsByKind) && (int)symbol.Kind < actionsByKind.Length)
                {
                    if (!AnalyzerExecutor.TryExecuteSymbolActions(actionsByKind[(int)symbol.Kind], analyzer, symbolEvent, GetTopmostNodeForAnalysis, analysisScope, analysisStateOpt, isGeneratedCodeSymbol))
                    {
                        success = false;
                    }
                }
                else
                {
                    analysisStateOpt?.MarkSymbolComplete(symbol, analyzer);
                }
            }

            return success;
        }

        private bool TryExecuteSymbolEndActions(
            AnalyzerActions perSymbolActions,
            SymbolDeclaredCompilationEvent symbolEvent,
            AnalysisScope analysisScope,
            AnalysisState analysisStateOpt,
            CancellationToken cancellationToken,
            out ImmutableArray<DiagnosticAnalyzer> subsetProcessedAnalyzers)
        {
            Debug.Assert(AnalyzerActions.SymbolStartActionsCount > 0);
            subsetProcessedAnalyzers = default;

            var symbol = symbolEvent.Symbol;
            var symbolEndActions = perSymbolActions?.SymbolEndActions ?? ImmutableArray<SymbolEndAnalyzerAction>.Empty;
            if (!analysisScope.ShouldAnalyze(symbol) || symbolEndActions.IsEmpty)
            {
                analysisStateOpt?.MarkSymbolEndAnalysisComplete(symbol, analysisScope.Analyzers);
                return true;
            }

            var success = true;
            ArrayBuilder<DiagnosticAnalyzer> subsetProcessedAnalyzersBuilderOpt = null;
            for (int i = 0; i < analysisScope.Analyzers.Length; i++)
            {
                var analyzer = analysisScope.Analyzers[i];
                var analyzerSuccess = true;
                var symbolEndActionsForAnalyzer = symbolEndActions.Where(a => a.Analyzer == analyzer).ToImmutableArrayOrEmpty();
                if (!symbolEndActionsForAnalyzer.IsEmpty)
                {
                    if (!AnalyzerExecutor.TryExecuteSymbolEndActions(symbolEndActionsForAnalyzer, analyzer, symbolEvent, GetTopmostNodeForAnalysis, analysisStateOpt))
                    {
                        if (subsetProcessedAnalyzersBuilderOpt == null)
                        {
                            subsetProcessedAnalyzersBuilderOpt = ArrayBuilder<DiagnosticAnalyzer>.GetInstance();
                            subsetProcessedAnalyzersBuilderOpt.AddRange(analysisScope.Analyzers, i);
                        }

                        analyzerSuccess = false;
                        success = false;
                    }
                }

                if (analyzerSuccess)
                {
                    AnalyzerExecutor.MarkSymbolEndAnalysisComplete(symbol, analyzer, analysisStateOpt);
                    subsetProcessedAnalyzersBuilderOpt?.Add(analyzer);
                }
            }

            if (subsetProcessedAnalyzersBuilderOpt != null)
            {
                Debug.Assert(!success);
                Debug.Assert(subsetProcessedAnalyzersBuilderOpt.Count < analysisScope.Analyzers.Length);

                if (subsetProcessedAnalyzersBuilderOpt.Count > 0)
                {
                    subsetProcessedAnalyzers = subsetProcessedAnalyzersBuilderOpt.ToImmutableAndFree();
                }
                else
                {
                    subsetProcessedAnalyzersBuilderOpt.Free();
                }
            }

            return success;
        }

        private static SyntaxNode GetTopmostNodeForAnalysis(ISymbol symbol, SyntaxReference syntaxReference, Compilation compilation)
        {
            var model = compilation.GetSemanticModel(syntaxReference.SyntaxTree);
            return model.GetTopmostNodeForDiagnosticAnalysis(symbol, syntaxReference.GetSyntax());
        }

        protected abstract bool TryExecuteDeclaringReferenceActions(
            SymbolDeclaredCompilationEvent symbolEvent,
            AnalysisScope analysisScope,
            AnalysisState analysisStateOpt,
            bool isGeneratedCodeSymbol,
            AnalyzerActions additionalPerSymbolActions,
            CancellationToken cancellationToken);

        /// <summary>
        /// Tries to execute compilation unit actions.
        /// </summary>
        /// <returns>
        /// True, if successfully executed the actions for the given analysis scope OR no actions were required to be executed for the given analysis scope.
        /// False, otherwise.
        /// </returns>
        private bool TryProcessCompilationUnitCompleted(CompilationUnitCompletedEvent completedEvent, AnalysisScope analysisScope, AnalysisState analysisStateOpt, CancellationToken cancellationToken)
        {
            // When the compiler is finished with a compilation unit, we can run user diagnostics which
            // might want to ask the compiler for all the diagnostics in the source file, for example
            // to get information about unnecessary usings.

            var semanticModel = analysisStateOpt != null ?
                CurrentCompilationData.GetOrCreateCachedSemanticModel(completedEvent.CompilationUnit, completedEvent.Compilation, cancellationToken) :
                completedEvent.SemanticModel;

            if (!analysisScope.ShouldAnalyze(semanticModel.SyntaxTree))
            {
                return true;
            }

            var isGeneratedCode = IsGeneratedCode(semanticModel.SyntaxTree);
            if (isGeneratedCode && DoNotAnalyzeGeneratedCode)
            {
                analysisStateOpt?.MarkEventComplete(completedEvent, analysisScope.Analyzers);
                return true;
            }

            try
            {
                var success = true;
                foreach (var analyzer in analysisScope.Analyzers)
                {
                    ImmutableArray<SemanticModelAnalyzerAction> semanticModelActions;
                    if (_semanticModelActionsMap.TryGetValue(analyzer, out semanticModelActions))
                    {
                        // Execute actions for a given analyzer sequentially.
                        if (!AnalyzerExecutor.TryExecuteSemanticModelActions(semanticModelActions, analyzer, semanticModel, completedEvent, analysisScope, analysisStateOpt, isGeneratedCode))
                        {
                            success = false;
                        }
                    }
                    else
                    {
                        analysisStateOpt?.MarkEventComplete(completedEvent, analyzer);
                    }
                }

                return success;
            }
            finally
            {
                completedEvent.FlushCache();
            }
        }

        /// <summary>
        /// Tries to execute compilation started actions.
        /// </summary>
        /// <returns>
        /// True, if successfully executed the actions for the given analysis scope OR no actions were required to be executed for the given analysis scope.
        /// False, otherwise.
        /// </returns>
        private bool TryProcessCompilationStarted(CompilationStartedEvent startedEvent, AnalysisScope analysisScope, AnalysisState analysisStateOpt, CancellationToken cancellationToken)
        {
            return TryExecuteCompilationActions(_compilationActionsMap, startedEvent, analysisScope, analysisStateOpt, cancellationToken);
        }

        /// <summary>
        /// Tries to execute compilation completed actions.
        /// </summary>
        /// <returns>
        /// True, if successfully executed the actions for the given analysis scope OR no actions were required to be executed for the given analysis scope.
        /// False, otherwise.
        /// </returns>
        private bool TryProcessCompilationCompleted(CompilationCompletedEvent endEvent, AnalysisScope analysisScope, AnalysisState analysisStateOpt, CancellationToken cancellationToken)
        {
            return TryExecuteCompilationActions(_compilationEndActionsMap, endEvent, analysisScope, analysisStateOpt, cancellationToken);
        }

        /// <summary>
        /// Tries to execute compilation actions.
        /// </summary>
        /// <returns>
        /// True, if successfully executed the actions for the given analysis scope OR no actions were required to be executed for the given analysis scope.
        /// False, otherwise.
        /// </returns>
        private bool TryExecuteCompilationActions(
            ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<CompilationAnalyzerAction>> compilationActionsMap,
            CompilationEvent compilationEvent,
            AnalysisScope analysisScope,
            AnalysisState analysisStateOpt,
            CancellationToken cancellationToken)
        {
            Debug.Assert(compilationEvent is CompilationStartedEvent || compilationEvent is CompilationCompletedEvent);

            try
            {
                var success = true;
                foreach (var analyzer in analysisScope.Analyzers)
                {
                    ImmutableArray<CompilationAnalyzerAction> compilationActions;
                    if (compilationActionsMap.TryGetValue(analyzer, out compilationActions))
                    {
                        if (!AnalyzerExecutor.TryExecuteCompilationActions(compilationActions, analyzer, compilationEvent, analysisScope, analysisStateOpt))
                        {
                            success = false;
                        }
                    }
                    else
                    {
                        analysisStateOpt?.MarkEventComplete(compilationEvent, analyzer);
                    }
                }

                return success;
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

        private static async Task<(AnalyzerActions actions, ImmutableHashSet<DiagnosticAnalyzer> unsuppressedAnalyzers)> GetAnalyzerActionsAsync(
            ImmutableArray<DiagnosticAnalyzer> analyzers,
            AnalyzerManager analyzerManager,
            AnalyzerExecutor analyzerExecutor)
        {
            var allAnalyzerActions = new AnalyzerActions();
            var unsuppressedAnalyzersBuilder = PooledHashSet<DiagnosticAnalyzer>.GetInstance();
            foreach (var analyzer in analyzers)
            {
                if (!IsDiagnosticAnalyzerSuppressed(analyzer, analyzerExecutor.Compilation.Options, analyzerManager, analyzerExecutor))
                {
                    unsuppressedAnalyzersBuilder.Add(analyzer);

                    var analyzerActions = await analyzerManager.GetAnalyzerActionsAsync(analyzer, analyzerExecutor).ConfigureAwait(false);
                    if (analyzerActions != null)
                    {
                        allAnalyzerActions = allAnalyzerActions.Append(analyzerActions);
                    }
                }
            }

            var unsuppressedAnalyzers = unsuppressedAnalyzersBuilder.ToImmutableHashSet();
            unsuppressedAnalyzersBuilder.Free();
            return (allAnalyzerActions, unsuppressedAnalyzers);
        }

        public bool HasSymbolStartedActions(AnalysisScope analysisScope)
        {
            if (this.AnalyzerActions.SymbolStartActionsCount == 0)
            {
                return false;
            }

            // Perform simple checks for when we are executing all analyzers (batch compilation mode) OR
            // executing just a single analyzer (IDE open file analysis).
            if (analysisScope.Analyzers.Length == this.Analyzers.Length)
            {
                // We are executing all analyzers, so at least one analyzer in analysis scope must have a symbol start action.
                return true;
            }
            else if (analysisScope.Analyzers.Length == 1)
            {
                // We are executing a single analyzer.
                var analyzer = analysisScope.Analyzers[0];
                foreach (var action in this.AnalyzerActions.SymbolStartActions)
                {
                    if (action.Analyzer == analyzer)
                    {
                        return true;
                    }
                }

                return false;
            }

            // Slow check when we are executing more than one analyzer, but it is still a strict subset of all analyzers.
            var symbolStartAnalyzers = PooledHashSet<DiagnosticAnalyzer>.GetInstance();
            try
            {
                foreach (var action in this.AnalyzerActions.SymbolStartActions)
                {
                    symbolStartAnalyzers.Add(action.Analyzer);
                }

                foreach (var analyzer in analysisScope.Analyzers)
                {
                    if (symbolStartAnalyzers.Contains(analyzer))
                    {
                        return true;
                    }
                }

                return false;
            }
            finally
            {
                symbolStartAnalyzers.Free();
            }
        }

        private async Task<AnalyzerActions> GetPerSymbolAnalyzerActionsAsync(
            ISymbol symbol,
            AnalysisScope analysisScope,
            AnalysisState analysisStateOpt,
            CancellationToken cancellationToken)
        {
            if (AnalyzerActions.SymbolStartActionsCount == 0 || symbol.IsImplicitlyDeclared)
            {
                return default;
            }

            var allActions = new AnalyzerActions();
            foreach (var analyzer in analysisScope.Analyzers)
            {
                var analyzerActions = await GetPerSymbolAnalyzerActionsAsync(symbol, analyzer, analysisStateOpt, cancellationToken).ConfigureAwait(false);
                if (analyzerActions != null)
                {
                    allActions = allActions.Append(analyzerActions);
                }
            }

            return allActions;
        }

        private async Task<AnalyzerActions> GetPerSymbolAnalyzerActionsAsync(
            ISymbol symbol,
            DiagnosticAnalyzer analyzer,
            AnalysisState analysisStateOpt,
            CancellationToken cancellationToken)
        {
            Debug.Assert(AnalyzerActions.SymbolStartActionsCount > 0);

            if (symbol.IsImplicitlyDeclared)
            {
                return null;
            }

            if (_perSymbolAnalyzerActionsCache.TryGetValue((symbol, analyzer), out var actions))
            {
                return actions;
            }

            // Compute additional inherited actions for this symbol by running the containing symbol's start actions.
            AnalyzerActions inheritedActions = await getInheritedActionsAsync().ConfigureAwait(false);

            // Execute the symbol start actions for this symbol to compute additional actions for its members.
            AnalyzerActions myActions = await getSymbolActionsCoreAsync().ConfigureAwait(false);
            AnalyzerActions allActions = myActions != null ? inheritedActions.Append(myActions) : inheritedActions;
            return _perSymbolAnalyzerActionsCache.GetOrAdd((symbol, analyzer), allActions);

            async Task<AnalyzerActions> getInheritedActionsAsync()
            {
                if (symbol.ContainingSymbol != null)
                {
                    // Get container symbol's per-symbol actions, which also forces its start actions to execute.
                    var containerActions = await GetPerSymbolAnalyzerActionsAsync(symbol.ContainingSymbol, analyzer, analysisStateOpt, cancellationToken).ConfigureAwait(false);
                    if (containerActions != null)
                    {
                        // Don't inherit actions for nested type and namespace from its containing type and namespace respectively.
                        // However, note that we bail out **after** computing container's per-symbol actions above.
                        // This is done to ensure that we have executed symbol started actions for the container before our start actions are executed.
                        if (symbol.ContainingSymbol.Kind != symbol.Kind)
                        {
                            // Don't inherit the symbol start and symbol end actions.
                            return new AnalyzerActions().Append(containerActions, appendSymbolStartAndSymbolEndActions: false);
                        }
                    }
                }

                return new AnalyzerActions();
            }

            async Task<AnalyzerActions> getSymbolActionsCoreAsync()
            {
                if (!_unsuppressedAnalyzers.Contains(analyzer) ||
                    IsGeneratedCodeSymbol(symbol) && ShouldSkipAnalysisOnGeneratedCode(analyzer))
                {
                    return null;
                }
                else
                {
                    return await AnalyzerManager.GetPerSymbolAnalyzerActionsAsync(symbol, analyzer, AnalyzerExecutor).ConfigureAwait(false);
                }
            }
        }

        private static async Task<ImmutableDictionary<DiagnosticAnalyzer, SemaphoreSlim>> CreateAnalyzerGateMapAsync(
            ImmutableHashSet<DiagnosticAnalyzer> analyzers,
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

        private static async Task<ImmutableDictionary<DiagnosticAnalyzer, GeneratedCodeAnalysisFlags>> CreateGeneratedCodeAnalysisFlagsMapAsync(
            ImmutableHashSet<DiagnosticAnalyzer> analyzers,
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

        [PerformanceSensitive(
            "https://github.com/dotnet/roslyn/pull/23637",
            AllowLocks = false)]
        private bool IsGeneratedCodeSymbol(ISymbol symbol)
        {
            if (_treatAllCodeAsNonGeneratedCode)
            {
                return false;
            }

            return _lazyIsGeneratedCodeSymbolMap.TryGetValue(symbol, out bool isGeneratedCodeSymbol) ?
                isGeneratedCodeSymbol :
                _lazyIsGeneratedCodeSymbolMap.GetOrAdd(symbol, computeIsGeneratedCodeSymbol());

            bool computeIsGeneratedCodeSymbol()
            {
                if (_generatedCodeAttribute != null && GeneratedCodeUtilities.IsGeneratedSymbolWithGeneratedCodeAttribute(symbol, _generatedCodeAttribute))
                {
                    return true;
                }

                foreach (var declaringRef in symbol.DeclaringSyntaxReferences)
                {
                    if (!IsGeneratedOrHiddenCodeLocation(declaringRef.SyntaxTree, declaringRef.Span))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        [PerformanceSensitive(
            "https://github.com/dotnet/roslyn/pull/23637",
            AllowLocks = false)]
        protected bool IsGeneratedCode(SyntaxTree tree)
        {
            if (_treatAllCodeAsNonGeneratedCode)
            {
                return false;
            }

            Debug.Assert(_lazyGeneratedCodeFilesMap != null);

            bool isGenerated;
            if (!_lazyGeneratedCodeFilesMap.TryGetValue(tree, out isGenerated))
            {
                isGenerated = computeIsGeneratedCode();
                _lazyGeneratedCodeFilesMap.TryAdd(tree, isGenerated);
            }

            return isGenerated;

            bool computeIsGeneratedCode()
            {
                // First check for explicit user configuration for generated code.
                //     generated_code = true | false
                var options = AnalyzerExecutor.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(tree);
                if (options.TryGetValue("generated_code", out string optionValue) &&
                    bool.TryParse(optionValue, out var boolValue))
                {
                    return boolValue;
                }

                // Either no explicit user configuration or we don't recognize the option value.
                // Compute isGeneratedCode using our generated code heuristic.
                return _isGeneratedCode(tree, AnalyzerExecutor.CancellationToken);
            }
        }

        protected bool DoNotAnalyzeGeneratedCode => _doNotAnalyzeGeneratedCode;

        // Location is in generated code if either the containing tree is a generated code file OR if it is a hidden source location.
        protected bool IsGeneratedOrHiddenCodeLocation(SyntaxTree syntaxTree, TextSpan span)
            => IsGeneratedCode(syntaxTree) || IsHiddenSourceLocation(syntaxTree, span);

        protected bool IsHiddenSourceLocation(SyntaxTree syntaxTree, TextSpan span)
            => HasHiddenRegions(syntaxTree) &&
               syntaxTree.IsHiddenPosition(span.Start);

        [PerformanceSensitive(
            "https://github.com/dotnet/roslyn/pull/23637",
            AllowLocks = false)]
        private bool HasHiddenRegions(SyntaxTree tree)
        {
            Debug.Assert(tree != null);

            if (_lazyTreesWithHiddenRegionsMap == null)
            {
                return false;
            }

            bool hasHiddenRegions;
            if (!_lazyTreesWithHiddenRegionsMap.TryGetValue(tree, out hasHiddenRegions))
            {
                hasHiddenRegions = tree.HasHiddenRegions();
                _lazyTreesWithHiddenRegionsMap.TryAdd(tree, hasHiddenRegions);
            }

            return hasHiddenRegions;
        }

        internal async Task<AnalyzerActionCounts> GetAnalyzerActionCountsAsync(DiagnosticAnalyzer analyzer, CompilationOptions compilationOptions, CancellationToken cancellationToken)
        {
            var executor = AnalyzerExecutor.WithCancellationToken(cancellationToken);
            if (IsDiagnosticAnalyzerSuppressed(analyzer, compilationOptions, AnalyzerManager, executor))
            {
                return AnalyzerActionCounts.Empty;
            }

            var analyzerActions = await AnalyzerManager.GetAnalyzerActionsAsync(analyzer, executor).ConfigureAwait(false);
            return new AnalyzerActionCounts(analyzerActions);
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
            return analyzerManager.IsDiagnosticAnalyzerSuppressed(analyzer, options, s_IsCompilerAnalyzerFunc, analyzerExecutor);
        }

        private static bool IsCompilerAnalyzer(DiagnosticAnalyzer analyzer)
        {
            return analyzer is CompilerDiagnosticAnalyzer;
        }

        public void Dispose()
        {
            this.CompilationEventQueue?.TryComplete();
            this.DiagnosticQueue?.TryComplete();
            _queueRegistration.Dispose();
        }
    }

    /// <summary>
    /// Driver to execute diagnostic analyzers for a given compilation.
    /// It uses a <see cref="AsyncQueue{TElement}"/> of <see cref="CompilationEvent"/>s to drive its analysis.
    /// </summary>
    internal partial class AnalyzerDriver<TLanguageKindEnum> : AnalyzerDriver where TLanguageKindEnum : struct
    {
        private readonly Func<SyntaxNode, TLanguageKindEnum> _getKind;
        private GroupedAnalyzerActions _lazyCoreActions;

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

        private GroupedAnalyzerActions CoreActions
        {
            get
            {
                if (_lazyCoreActions == null)
                {
                    Interlocked.CompareExchange(ref _lazyCoreActions, GroupedAnalyzerActions.Create(base.AnalyzerActions), null);
                }

                return _lazyCoreActions;
            }
        }

        private static bool ShouldExecuteSyntaxNodeActions(GroupedAnalyzerActions coreActions, GroupedAnalyzerActions additionalActions, AnalysisScope analysisScope)
        {
            return coreActions.ShouldExecuteSyntaxNodeActions(analysisScope) || additionalActions.ShouldExecuteSyntaxNodeActions(analysisScope);
        }

        private static bool ShouldExecuteCodeBlockActions(GroupedAnalyzerActions coreActions, GroupedAnalyzerActions additionalActions, AnalysisScope analysisScope, ISymbol symbol)
        {
            return coreActions.ShouldExecuteCodeBlockActions(analysisScope, symbol) || additionalActions.ShouldExecuteCodeBlockActions(analysisScope, symbol);
        }

        private static bool ShouldExecuteOperationActions(GroupedAnalyzerActions coreActions, GroupedAnalyzerActions additionalActions, AnalysisScope analysisScope)
        {
            return coreActions.ShouldExecuteOperationActions(analysisScope) || additionalActions.ShouldExecuteOperationActions(analysisScope);
        }

        private static bool ShouldExecuteOperationBlockActions(GroupedAnalyzerActions coreActions, GroupedAnalyzerActions additionalActions, AnalysisScope analysisScope, ISymbol symbol)
        {
            return coreActions.ShouldExecuteOperationBlockActions(analysisScope, symbol) || additionalActions.ShouldExecuteOperationBlockActions(analysisScope, symbol);
        }

        /// <summary>
        /// Tries to execute syntax node, code block and operation actions for all declarations for the given symbol.
        /// </summary>
        /// <returns>
        /// True, if successfully executed the actions for the given analysis scope OR no actions were required to be executed for the given analysis scope.
        /// False, otherwise.
        /// </returns>
        protected override bool TryExecuteDeclaringReferenceActions(
            SymbolDeclaredCompilationEvent symbolEvent,
            AnalysisScope analysisScope,
            AnalysisState analysisStateOpt,
            bool isGeneratedCodeSymbol,
            AnalyzerActions additionalPerSymbolActionsOpt,
            CancellationToken cancellationToken)
        {
            var symbol = symbolEvent.Symbol;
            var additionalGroupedPerSymbolActions = GroupedAnalyzerActions.Create(additionalPerSymbolActionsOpt);

            var executeSyntaxNodeActions = ShouldExecuteSyntaxNodeActions(CoreActions, additionalGroupedPerSymbolActions, analysisScope);
            var executeCodeBlockActions = ShouldExecuteCodeBlockActions(CoreActions, additionalGroupedPerSymbolActions, analysisScope, symbol);
            var executeOperationActions = ShouldExecuteOperationActions(CoreActions, additionalGroupedPerSymbolActions, analysisScope);
            var executeOperationBlockActions = ShouldExecuteOperationBlockActions(CoreActions, additionalGroupedPerSymbolActions, analysisScope, symbol);

            var success = true;
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

                    var isInGeneratedCode = isGeneratedCodeSymbol || IsGeneratedOrHiddenCodeLocation(decl.SyntaxTree, decl.Span);
                    if (isInGeneratedCode && DoNotAnalyzeGeneratedCode)
                    {
                        analysisStateOpt?.MarkDeclarationComplete(symbol, i, analysisScope.Analyzers);
                        continue;
                    }

                    if (!TryExecuteDeclaringReferenceActions(decl, i, symbolEvent, analysisScope, analysisStateOpt, additionalGroupedPerSymbolActions,
                        executeSyntaxNodeActions, executeOperationActions, executeCodeBlockActions, executeOperationBlockActions, isInGeneratedCode, cancellationToken))
                    {
                        success = false;
                    }
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

            return success;
        }

        private void ClearCachedAnalysisDataIfAnalyzed(SyntaxReference declaration, ISymbol symbol, int declarationIndex, AnalysisState analysisState)
        {
            Debug.Assert(analysisState != null);

            if (!analysisState.IsDeclarationComplete(symbol, declarationIndex))
            {
                return;
            }

            CurrentCompilationData.ClearDeclarationAnalysisData(declaration);
        }

        private DeclarationAnalysisData ComputeDeclarationAnalysisData(
            ISymbol symbol,
            SyntaxReference declaration,
            SemanticModel semanticModel,
            AnalysisScope analysisScope,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var builder = ArrayBuilder<DeclarationInfo>.GetInstance();
            SyntaxNode declaringReferenceSyntax = declaration.GetSyntax(cancellationToken);
            SyntaxNode topmostNodeForAnalysis = semanticModel.GetTopmostNodeForDiagnosticAnalysis(symbol, declaringReferenceSyntax);
            ComputeDeclarationsInNode(semanticModel, symbol, declaringReferenceSyntax, topmostNodeForAnalysis, builder, cancellationToken);
            ImmutableArray<DeclarationInfo> declarationInfos = builder.ToImmutableAndFree();

            bool isPartialDeclAnalysis = analysisScope.FilterSpanOpt.HasValue && !analysisScope.ContainsSpan(topmostNodeForAnalysis.FullSpan);
            ImmutableArray<SyntaxNode> nodesToAnalyze = GetSyntaxNodesToAnalyze(topmostNodeForAnalysis, symbol, declarationInfos, analysisScope, isPartialDeclAnalysis, semanticModel, AnalyzerExecutor);
            return new DeclarationAnalysisData(declaringReferenceSyntax, topmostNodeForAnalysis, declarationInfos, nodesToAnalyze, isPartialDeclAnalysis);
        }

        private static void ComputeDeclarationsInNode(SemanticModel semanticModel, ISymbol declaredSymbol, SyntaxNode declaringReferenceSyntax, SyntaxNode topmostNodeForAnalysis, ArrayBuilder<DeclarationInfo> builder, CancellationToken cancellationToken)
        {
            // We only care about the top level symbol declaration and its immediate member declarations.
            int? levelsToCompute = 2;
            var getSymbol = topmostNodeForAnalysis != declaringReferenceSyntax || declaredSymbol.Kind == SymbolKind.Namespace;
            semanticModel.ComputeDeclarationsInNode(topmostNodeForAnalysis, getSymbol, builder, cancellationToken, levelsToCompute);
        }

        /// <summary>
        /// Tries to execute syntax node, code block and operation actions for the given declaration.
        /// </summary>
        /// <returns>
        /// True, if successfully executed the actions for the given analysis scope OR no actions were required to be executed for the given analysis scope.
        /// False, otherwise.
        /// </returns>
        private bool TryExecuteDeclaringReferenceActions(
            SyntaxReference decl,
            int declarationIndex,
            SymbolDeclaredCompilationEvent symbolEvent,
            AnalysisScope analysisScope,
            AnalysisState analysisStateOpt,
            GroupedAnalyzerActions additionalPerSymbolActions,
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
                CurrentCompilationData.GetOrCreateCachedSemanticModel(decl.SyntaxTree, symbolEvent.Compilation, cancellationToken) :
                symbolEvent.SemanticModel(decl);

            var cacheAnalysisData = analysisScope.Analyzers.Length < Analyzers.Length &&
                (!analysisScope.FilterSpanOpt.HasValue || analysisScope.FilterSpanOpt.Value.Length >= decl.SyntaxTree.GetRoot(cancellationToken).Span.Length);

            var declarationAnalysisData = CurrentCompilationData.GetOrComputeDeclarationAnalysisData(
                decl,
                computeDeclarationAnalysisData: () => ComputeDeclarationAnalysisData(symbol, decl, semanticModel, analysisScope, cancellationToken),
                cacheAnalysisData: cacheAnalysisData);

            if (!analysisScope.ShouldAnalyze(declarationAnalysisData.TopmostNodeForAnalysis))
            {
                return true;
            }

            var success = true;

            // Execute stateless syntax node actions.
            executeNodeActions();

            // Execute actions in executable code: code block actions, operation actions and operation block actions.
            executeExecutableCodeActions();

            // Mark completion if we successfully executed all actions and only if we are analyzing a span containing the entire syntax node.
            if (success && analysisStateOpt != null && !declarationAnalysisData.IsPartialAnalysis)
            {
                // Ensure that we do not mark declaration complete/clear state if cancellation was requested.
                // Other thread(s) might still be executing analysis, and clearing state could lead to corrupt execution
                // or unknown exceptions.
                cancellationToken.ThrowIfCancellationRequested();

                foreach (var analyzer in analysisScope.Analyzers)
                {
                    analysisStateOpt.MarkDeclarationComplete(symbol, declarationIndex, analyzer);
                }

                if (cacheAnalysisData)
                {
                    ClearCachedAnalysisDataIfAnalyzed(decl, symbol, declarationIndex, analysisStateOpt);
                }
            }

            return success;

            void executeNodeActions()
            {
                if (shouldExecuteSyntaxNodeActions)
                {
                    var nodesToAnalyze = declarationAnalysisData.DescendantNodesToAnalyze;
                    foreach (var analyzer in analysisScope.Analyzers)
                    {
                        executeNodeActionsByKind(analyzer, nodesToAnalyze, CoreActions);
                        executeNodeActionsByKind(analyzer, nodesToAnalyze, additionalPerSymbolActions);
                    }
                }
            }

            void executeNodeActionsByKind(DiagnosticAnalyzer analyzer, ImmutableArray<SyntaxNode> nodesToAnalyze, GroupedAnalyzerActions groupedActions)
            {
                if (groupedActions.NodeActionsByAnalyzerAndKind.TryGetValue(analyzer, out var nodeActionsByKind) &&
                    !nodeActionsByKind.IsEmpty)
                {
                    if (!AnalyzerExecutor.TryExecuteSyntaxNodeActions(nodesToAnalyze, nodeActionsByKind,
                        analyzer, semanticModel, _getKind, declarationAnalysisData.TopmostNodeForAnalysis.FullSpan,
                        decl, declarationIndex, symbol, analysisScope, analysisStateOpt, isInGeneratedCode))
                    {
                        success = false;
                    }
                }
            }

            void executeExecutableCodeActions()
            {
                if (!shouldExecuteCodeBlockActions && !shouldExecuteOperationActions && !shouldExecuteOperationBlockActions)
                {
                    return;
                }

                // Compute the executable code blocks of interest.
                var executableCodeBlocks = ImmutableArray<SyntaxNode>.Empty;
                IEnumerable<CodeBlockAnalyzerActions> codeBlockActions = null;
                foreach (var declInNode in declarationAnalysisData.DeclarationsInNode)
                {
                    if (declInNode.DeclaredNode == declarationAnalysisData.TopmostNodeForAnalysis || declInNode.DeclaredNode == declarationAnalysisData.DeclaringReferenceSyntax)
                    {
                        executableCodeBlocks = declInNode.ExecutableCodeBlocks;
                        if (!executableCodeBlocks.IsEmpty)
                        {
                            if (shouldExecuteCodeBlockActions || shouldExecuteOperationBlockActions)
                            {
                                codeBlockActions = CoreActions.GetCodeBlockActions(analysisScope)
                                    .Concat(additionalPerSymbolActions.GetCodeBlockActions(analysisScope));
                            }

                            // Execute operation actions.
                            if (shouldExecuteOperationActions || shouldExecuteOperationBlockActions)
                            {
                                var operationBlocksToAnalyze = GetOperationBlocksToAnalyze(executableCodeBlocks, semanticModel, cancellationToken);
                                var operationsToAnalyze = getOperationsToAnalyzeWithStackGuard(operationBlocksToAnalyze);

                                if (!operationsToAnalyze.IsEmpty)
                                {
                                    executeOperationsActions(operationsToAnalyze);
                                    executeOperationsBlockActions(operationBlocksToAnalyze, operationsToAnalyze, codeBlockActions);
                                }
                            }

                            break;
                        }
                    }
                }

                executeCodeBlockActions(executableCodeBlocks, codeBlockActions);
            }

            ImmutableArray<IOperation> getOperationsToAnalyzeWithStackGuard(ImmutableArray<IOperation> operationBlocksToAnalyze)
            {
                try
                {
                    return GetOperationsToAnalyze(operationBlocksToAnalyze);
                }
                catch (Exception ex) when (ex is InsufficientExecutionStackException || FatalError.ReportWithoutCrashUnlessCanceled(ex))
                {
                    // the exception filter will short-circuit if `ex` is `InsufficientExecutionStackException` (from OperationWalker)
                    // and no non-fatal-watson will be logged as a result.
                    var diagnostic = AnalyzerExecutor.CreateDriverExceptionDiagnostic(ex);
                    var analyzer = this.Analyzers[0];

                    AnalyzerExecutor.OnAnalyzerException(ex, analyzer, diagnostic);
                    return ImmutableArray<IOperation>.Empty;
                }
            }

            void executeOperationsActions(ImmutableArray<IOperation> operationsToAnalyze)
            {
                if (shouldExecuteOperationActions)
                {
                    foreach (var analyzer in analysisScope.Analyzers)
                    {
                        executeOperationsActionsByKind(analyzer, operationsToAnalyze, CoreActions);
                        executeOperationsActionsByKind(analyzer, operationsToAnalyze, additionalPerSymbolActions);
                    }
                }
            }

            void executeOperationsActionsByKind(DiagnosticAnalyzer analyzer, ImmutableArray<IOperation> operationsToAnalyze, GroupedAnalyzerActions groupedActions)
            {
                if (groupedActions.OperationActionsByAnalyzerAndKind.TryGetValue(analyzer, out var operationActionsByKind) &&
                    !operationActionsByKind.IsEmpty)
                {
                    if (!AnalyzerExecutor.TryExecuteOperationActions(operationsToAnalyze, operationActionsByKind,
                        analyzer, semanticModel, declarationAnalysisData.TopmostNodeForAnalysis.FullSpan,
                        decl, declarationIndex, symbol, analysisScope, analysisStateOpt, isInGeneratedCode))
                    {
                        success = false;
                    }
                }
            }

            void executeOperationsBlockActions(ImmutableArray<IOperation> operationBlocksToAnalyze, ImmutableArray<IOperation> operationsToAnalyze, IEnumerable<CodeBlockAnalyzerActions> codeBlockActions)
            {
                if (!shouldExecuteOperationBlockActions)
                {
                    return;
                }

                foreach (var analyzerActions in codeBlockActions)
                {
                    if (analyzerActions.OperationBlockStartActions.IsEmpty &&
                        analyzerActions.OperationBlockActions.IsEmpty &&
                        analyzerActions.OperationBlockEndActions.IsEmpty)
                    {
                        continue;
                    }

                    if (!AnalyzerExecutor.TryExecuteOperationBlockActions(
                        analyzerActions.OperationBlockStartActions, analyzerActions.OperationBlockActions,
                        analyzerActions.OperationBlockEndActions, analyzerActions.Analyzer, declarationAnalysisData.TopmostNodeForAnalysis, symbol,
                        operationBlocksToAnalyze, operationsToAnalyze, semanticModel, decl, declarationIndex, analysisScope, analysisStateOpt, isInGeneratedCode))
                    {
                        success = false;
                    }
                }
            }

            void executeCodeBlockActions(ImmutableArray<SyntaxNode> executableCodeBlocks, IEnumerable<CodeBlockAnalyzerActions> codeBlockActions)
            {
                if (executableCodeBlocks.IsEmpty || !shouldExecuteCodeBlockActions)
                {
                    return;
                }

                foreach (var analyzerActions in codeBlockActions)
                {
                    if (analyzerActions.CodeBlockStartActions.IsEmpty &&
                        analyzerActions.CodeBlockActions.IsEmpty &&
                        analyzerActions.CodeBlockEndActions.IsEmpty)
                    {
                        continue;
                    }

                    if (!AnalyzerExecutor.TryExecuteCodeBlockActions(
                        analyzerActions.CodeBlockStartActions, analyzerActions.CodeBlockActions,
                        analyzerActions.CodeBlockEndActions, analyzerActions.Analyzer, declarationAnalysisData.TopmostNodeForAnalysis, symbol,
                        executableCodeBlocks, semanticModel, _getKind, decl, declarationIndex, analysisScope, analysisStateOpt, isInGeneratedCode))
                    {
                        success = false;
                    }
                }
            }
        }

        private static ImmutableArray<SyntaxNode> GetSyntaxNodesToAnalyze(
            SyntaxNode declaredNode,
            ISymbol declaredSymbol,
            ImmutableArray<DeclarationInfo> declarationsInNode,
            AnalysisScope analysisScope,
            bool isPartialDeclAnalysis,
            SemanticModel semanticModel,
            AnalyzerExecutor analyzerExecutor)
        {
            // Eliminate descendant member declarations within declarations.
            // There will be separate symbols declared for the members.
            HashSet<SyntaxNode> descendantDeclsToSkipOpt = null;
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

                    descendantDeclsToSkipOpt = descendantDeclsToSkipOpt ?? new HashSet<SyntaxNode>();
                    descendantDeclsToSkipOpt.Add(declarationNodeToSkip);
                }

                first = false;
            }

            bool shouldAddNode(SyntaxNode node) => descendantDeclsToSkipOpt == null || !descendantDeclsToSkipOpt.Contains(node);
            var nodeBuilder = ArrayBuilder<SyntaxNode>.GetInstance();
            foreach (var node in declaredNode.DescendantNodesAndSelf(descendIntoChildren: shouldAddNode, descendIntoTrivia: true))
            {
                if (shouldAddNode(node) &&
                    (!isPartialDeclAnalysis || analysisScope.ShouldAnalyze(node)))
                {
                    nodeBuilder.Add(node);
                }
            }

            return nodeBuilder.ToImmutableAndFree();
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
            var checkParent = true;

            foreach (IOperation operationBlock in operationBlocks)
            {
                if (checkParent)
                {
                    // Special handling for IMethodBodyOperation and IConstructorBodyOperation.
                    // These are newly added root operation nodes for C# method and constructor bodies.
                    // However, to avoid a breaking change for existing operation block analyzers,
                    // we have decided to retain the current behavior of making operation block callbacks with the contained
                    // method body and/or constructor initializer operation nodes.
                    // Hence we detect here if the operation block is parented by IMethodBodyOperation or IConstructorBodyOperation and
                    // add them to 'operationsToAnalyze' so that analyzers that explicitly register for these operation kinds
                    // can get callbacks for these nodes.
                    if (operationBlock.Parent != null)
                    {
                        switch (operationBlock.Parent.Kind)
                        {
                            case OperationKind.MethodBody:
                            case OperationKind.ConstructorBody:
                                operationsToAnalyze.Add(operationBlock.Parent);
                                break;

                            default:
                                Debug.Fail($"Expected operation with kind '{operationBlock.Kind}' to be the root operation with null 'Parent', but instead it has a non-null Parent with kind '{operationBlock.Parent.Kind}'");
                                break;
                        }

                        checkParent = false;
                    }
                }

                operationsToAnalyze.AddRange(operationBlock.DescendantsAndSelf());
            }

            Debug.Assert(operationsToAnalyze.ToImmutableHashSet().Count == operationsToAnalyze.Count);
            return operationsToAnalyze.ToImmutableAndFree();
        }
    }
}
