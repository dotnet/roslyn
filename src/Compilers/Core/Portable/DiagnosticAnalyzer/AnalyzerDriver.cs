// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Diagnostics.Telemetry;
using Microsoft.CodeAnalysis.ErrorReporting;
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
        // NOTE: These methods are on hot paths and cause large allocation hit if removed.
        private static readonly Func<DiagnosticAnalyzer, bool> s_IsCompilerAnalyzerFunc = IsCompilerAnalyzer;
        private static readonly Func<ISymbol, SyntaxReference, Compilation, CancellationToken, SyntaxNode> s_getTopmostNodeForAnalysis = GetTopmostNodeForAnalysis;

        private readonly Func<SyntaxTree, CancellationToken, bool> _isGeneratedCode;

        /// <summary>
        /// Set of diagnostic suppressions that are suppressed via analyzer suppression actions. 
        /// </summary>
        private readonly ConcurrentSet<Suppression>? _programmaticSuppressions;

        /// <summary>
        /// Set of diagnostics that have already been processed for application of programmatic suppressions. 
        /// </summary>
        private readonly ConcurrentSet<Diagnostic>? _diagnosticsProcessedForProgrammaticSuppressions;

        /// <summary>
        /// Flag indicating if the <see cref="Analyzers"/> include any <see cref="DiagnosticSuppressor"/>
        /// which can suppress reported analyzer/compiler diagnostics.
        /// </summary>
        private readonly bool _hasDiagnosticSuppressors;

        /// <summary>
        /// Filtered diagnostic severities in the compilation, i.e. diagnostics with effective severity from this set should not be reported.
        /// PERF: If all supported diagnostics for an analyzer are from this set, we completely skip executing the analyzer.
        /// </summary>
        private readonly SeverityFilter _severityFilter;

        protected ImmutableArray<DiagnosticAnalyzer> Analyzers { get; }
        protected AnalyzerManager AnalyzerManager { get; }

        // Lazy fields/properties
        private CancellationTokenRegistration? _lazyQueueRegistration;

        private AnalyzerExecutor? _lazyAnalyzerExecutor;
        protected AnalyzerExecutor AnalyzerExecutor
        {
            get
            {
                Debug.Assert(_lazyAnalyzerExecutor != null);
                return _lazyAnalyzerExecutor;
            }
        }

        private CompilationData? _lazyCurrentCompilationData;
        protected CompilationData CurrentCompilationData
        {
            get
            {
                Debug.Assert(_lazyCurrentCompilationData != null);
                return _lazyCurrentCompilationData;
            }
        }

        protected CachingSemanticModelProvider SemanticModelProvider => CurrentCompilationData.SemanticModelProvider;

        protected ref readonly AnalyzerActions AnalyzerActions => ref _lazyAnalyzerActions;

        private ImmutableHashSet<DiagnosticAnalyzer>? _lazyUnsuppressedAnalyzers;

        /// <summary>
        /// Unsuppressed analyzers that need to be executed. 
        /// </summary>
        protected ImmutableHashSet<DiagnosticAnalyzer> UnsuppressedAnalyzers
        {
            get
            {
                Debug.Assert(_lazyUnsuppressedAnalyzers != null);
                return _lazyUnsuppressedAnalyzers;
            }
        }

        private ConcurrentDictionary<(INamespaceOrTypeSymbol, DiagnosticAnalyzer), IGroupedAnalyzerActions>? _lazyPerSymbolAnalyzerActionsCache;

        /// <summary>
        /// Cache of additional analyzer actions to be executed per symbol per analyzer, which are registered in symbol start actions.
        /// We cache the tuple:
        ///   1. myActions: analyzer actions registered in the symbol start actions of containing namespace/type, which are to be executed for this symbol
        ///   2. childActions: analyzer actions registered in this symbol's start actions, which are to be executed for member symbols.
        /// </summary>
        private ConcurrentDictionary<(INamespaceOrTypeSymbol, DiagnosticAnalyzer), IGroupedAnalyzerActions> PerSymbolAnalyzerActionsCache
        {
            get
            {
                Debug.Assert(_lazyPerSymbolAnalyzerActionsCache != null);
                return _lazyPerSymbolAnalyzerActionsCache;
            }
        }

        private ImmutableArray<(DiagnosticAnalyzer, ImmutableArray<ImmutableArray<SymbolAnalyzerAction>>)> _lazySymbolActionsByKind;
        private ImmutableArray<(DiagnosticAnalyzer, ImmutableArray<SemanticModelAnalyzerAction>)> _lazySemanticModelActions;
        private ImmutableArray<(DiagnosticAnalyzer, ImmutableArray<SyntaxTreeAnalyzerAction>)> _lazySyntaxTreeActions;
        private ImmutableArray<(DiagnosticAnalyzer, ImmutableArray<AdditionalFileAnalyzerAction>)> _lazyAdditionalFileActions;
        // Compilation actions and compilation end actions have separate maps so that it is easy to
        // execute the compilation actions before the compilation end actions.
        private ImmutableArray<(DiagnosticAnalyzer, ImmutableArray<CompilationAnalyzerAction>)> _lazyCompilationActions;
        private ImmutableArray<(DiagnosticAnalyzer, ImmutableArray<CompilationAnalyzerAction>)> _lazyCompilationEndActions;

        private ImmutableHashSet<DiagnosticAnalyzer>? _lazyCompilationEndAnalyzers;
        private ImmutableHashSet<DiagnosticAnalyzer> CompilationEndAnalyzers
        {
            get
            {
                Debug.Assert(_lazyCompilationEndAnalyzers != null);
                return _lazyCompilationEndAnalyzers;
            }
        }

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
        private ImmutableSegmentedDictionary<DiagnosticAnalyzer, SemaphoreSlim> _lazyAnalyzerGateMap;
        private ImmutableSegmentedDictionary<DiagnosticAnalyzer, SemaphoreSlim> AnalyzerGateMap
        {
            get
            {
                Debug.Assert(_lazyAnalyzerGateMap != null);
                return _lazyAnalyzerGateMap;
            }
        }

        private ImmutableSegmentedDictionary<DiagnosticAnalyzer, GeneratedCodeAnalysisFlags> _lazyGeneratedCodeAnalysisFlagsMap;

        /// <summary>
        /// Map from analyzers to their <see cref="GeneratedCodeAnalysisFlags"/> setting. 
        /// </summary>
        private ImmutableSegmentedDictionary<DiagnosticAnalyzer, GeneratedCodeAnalysisFlags> GeneratedCodeAnalysisFlagsMap
        {
            get
            {
                Debug.Assert(!_lazyGeneratedCodeAnalysisFlagsMap.IsDefault);
                return _lazyGeneratedCodeAnalysisFlagsMap;
            }
        }

        /// <summary>
        /// The set of registered analyzer actions.
        /// </summary>
        /// <seealso cref="AnalyzerActions"/>
        private AnalyzerActions _lazyAnalyzerActions;

        private ImmutableHashSet<DiagnosticAnalyzer>? _lazyNonConfigurableAnalyzers;

        /// <summary>
        /// Set of unsuppressed analyzers that report non-configurable diagnostics that cannot be suppressed with end user configuration. 
        /// </summary>
        private ImmutableHashSet<DiagnosticAnalyzer> NonConfigurableAnalyzers
        {
            get
            {
                Debug.Assert(_lazyNonConfigurableAnalyzers != null);
                return _lazyNonConfigurableAnalyzers;
            }
        }

        private ImmutableHashSet<DiagnosticAnalyzer>? _lazySymbolStartAnalyzers;

        /// <summary>
        /// Set of analyzers that have registered symbol start analyzer actions. 
        /// </summary>
        private ImmutableHashSet<DiagnosticAnalyzer> SymbolStartAnalyzers
        {
            get
            {
                Debug.Assert(_lazySymbolStartAnalyzers != null);
                return _lazySymbolStartAnalyzers;
            }
        }

        private bool? _lazyTreatAllCodeAsNonGeneratedCode;

        /// <summary>
        /// True if all analyzers need to analyze and report diagnostics in generated code - we can assume all code to be non-generated code.
        /// </summary>
        private bool TreatAllCodeAsNonGeneratedCode
        {
            get
            {
                Debug.Assert(_lazyTreatAllCodeAsNonGeneratedCode.HasValue);
                return _lazyTreatAllCodeAsNonGeneratedCode.Value;
            }
        }

        /// <summary>
        /// True if no analyzer needs generated code analysis - we can skip all analysis on a generated code symbol/tree.
        /// </summary>
        private bool? _lazyDoNotAnalyzeGeneratedCode;

        private ConcurrentDictionary<SyntaxTree, bool>? _lazyGeneratedCodeFilesMap;

        /// <summary>
        /// Lazily populated dictionary indicating whether a source file is a generated code file or not - we populate it lazily to avoid realizing all syntax trees in the compilation upfront.
        /// </summary>
        private ConcurrentDictionary<SyntaxTree, bool> GeneratedCodeFilesMap
        {
            get
            {
                Debug.Assert(_lazyGeneratedCodeFilesMap != null);
                return _lazyGeneratedCodeFilesMap;
            }
        }

        private Dictionary<SyntaxTree, ImmutableHashSet<ISymbol>>? _lazyGeneratedCodeSymbolsForTreeMap;

        /// <summary>
        /// Lazily populated dictionary from tree to declared symbols with GeneratedCodeAttribute.
        /// </summary>
        private Dictionary<SyntaxTree, ImmutableHashSet<ISymbol>> GeneratedCodeSymbolsForTreeMap
        {
            get
            {
                Debug.Assert(_lazyGeneratedCodeSymbolsForTreeMap != null);
                return _lazyGeneratedCodeSymbolsForTreeMap;
            }
        }

        private ConcurrentDictionary<SyntaxTree, ImmutableHashSet<DiagnosticAnalyzer>>? _lazySuppressedAnalyzersForTreeMap;

        /// <summary>
        /// Lazily populated dictionary from tree to analyzers that are suppressed on the entire tree.
        /// </summary>
        private ConcurrentDictionary<SyntaxTree, ImmutableHashSet<DiagnosticAnalyzer>> SuppressedAnalyzersForTreeMap
        {
            get
            {
                Debug.Assert(_lazySuppressedAnalyzersForTreeMap != null);
                return _lazySuppressedAnalyzersForTreeMap;
            }
        }

        private ConcurrentDictionary<ISymbol, bool>? _lazyIsGeneratedCodeSymbolMap;

        /// <summary>
        /// Lazily populated dictionary from symbol to a bool indicating if it is a generated code symbol.
        /// </summary>
        private ConcurrentDictionary<ISymbol, bool> IsGeneratedCodeSymbolMap
        {
            get
            {
                Debug.Assert(_lazyIsGeneratedCodeSymbolMap != null);
                return _lazyIsGeneratedCodeSymbolMap;
            }
        }

        /// <summary>
        /// Lazily populated dictionary indicating whether a source file has any hidden regions - we populate it lazily to avoid realizing all syntax trees in the compilation upfront.
        /// </summary>
        private ConcurrentDictionary<SyntaxTree, bool>? _lazyTreesWithHiddenRegionsMap;

        /// <summary>
        /// Symbol for <see cref="System.CodeDom.Compiler.GeneratedCodeAttribute"/>.
        /// </summary>
        private INamedTypeSymbol? _lazyGeneratedCodeAttribute;

        /// <summary>
        /// Driver task which initializes all analyzers.
        /// This task is initialized and executed only once at start of analysis.
        /// </summary>
        private Task? _lazyInitializeTask;

        /// <summary>
        /// Flag to indicate if the <see cref="_lazyInitializeTask"/> was successfully started.
        /// </summary>
        private bool _initializeSucceeded = false;

        /// <summary>
        /// Primary driver task which processes all <see cref="CompilationEventQueue"/> events, runs analyzer actions and signals completion of <see cref="DiagnosticQueue"/> at the end.
        /// </summary>
        private Task? _lazyPrimaryTask;

        /// <summary>
        /// Number of worker tasks processing compilation events and executing analyzer actions.
        /// </summary>
        private readonly int _workerCount = Environment.ProcessorCount;

        private AsyncQueue<CompilationEvent>? _lazyCompilationEventQueue;

        /// <summary>
        /// Events queue for analyzer execution.
        /// </summary>
        public AsyncQueue<CompilationEvent> CompilationEventQueue
        {
            get
            {
                Debug.Assert(_lazyCompilationEventQueue != null);
                return _lazyCompilationEventQueue;
            }
        }

        private DiagnosticQueue? _lazyDiagnosticQueue;

        /// <summary>
        /// <see cref="DiagnosticQueue"/> that is fed the diagnostics as they are computed.
        /// </summary>
        public DiagnosticQueue DiagnosticQueue
        {
            get
            {
                Debug.Assert(_lazyDiagnosticQueue != null);
                return _lazyDiagnosticQueue;
            }
        }

        /// <summary>
        /// Create an analyzer driver.
        /// </summary>
        /// <param name="analyzers">The set of analyzers to include in the analysis</param>
        /// <param name="analyzerManager">AnalyzerManager to manage analyzers for analyzer host's lifetime.</param>
        /// <param name="severityFilter">Filtered diagnostic severities in the compilation, i.e. diagnostics with effective severity from this set should not be reported.</param>
        /// <param name="isComment">Delegate to identify if the given trivia is a comment.</param>
        protected AnalyzerDriver(ImmutableArray<DiagnosticAnalyzer> analyzers, AnalyzerManager analyzerManager, SeverityFilter severityFilter, Func<SyntaxTrivia, bool> isComment)
        {
            Debug.Assert(!severityFilter.Contains(ReportDiagnostic.Suppress));
            Debug.Assert(!severityFilter.Contains(ReportDiagnostic.Default));

            this.Analyzers = analyzers;
            this.AnalyzerManager = analyzerManager;
            _isGeneratedCode = (tree, ct) => GeneratedCodeUtilities.IsGeneratedCode(tree, isComment, ct);
            _severityFilter = severityFilter;
            _hasDiagnosticSuppressors = this.Analyzers.Any(a => a is DiagnosticSuppressor);
            _programmaticSuppressions = _hasDiagnosticSuppressors ? new ConcurrentSet<Suppression>() : null;
            _diagnosticsProcessedForProgrammaticSuppressions = _hasDiagnosticSuppressors ? new ConcurrentSet<Diagnostic>(ReferenceEqualityComparer.Instance) : null;
            _lazyAnalyzerGateMap = ImmutableSegmentedDictionary<DiagnosticAnalyzer, SemaphoreSlim>.Empty;
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
                Debug.Assert(_lazyInitializeTask == null);

                _lazyAnalyzerExecutor = analyzerExecutor;
                _lazyCurrentCompilationData = compilationData;
                _lazyDiagnosticQueue = diagnosticQueue;

                // Compute the set of effective actions based on suppression, and running the initial analyzers
                _lazyInitializeTask = Task.Run(async () =>
                {
                    (_lazyAnalyzerActions, _lazyUnsuppressedAnalyzers) = await GetAnalyzerActionsAsync(Analyzers, AnalyzerManager, analyzerExecutor, _severityFilter).ConfigureAwait(false);
                    _lazyAnalyzerGateMap = await CreateAnalyzerGateMapAsync(UnsuppressedAnalyzers, AnalyzerManager, analyzerExecutor, _severityFilter).ConfigureAwait(false);
                    _lazyNonConfigurableAnalyzers = ComputeNonConfigurableAnalyzers(UnsuppressedAnalyzers);
                    _lazySymbolStartAnalyzers = ComputeSymbolStartAnalyzers(UnsuppressedAnalyzers);
                    _lazyGeneratedCodeAnalysisFlagsMap = await CreateGeneratedCodeAnalysisFlagsMapAsync(UnsuppressedAnalyzers, AnalyzerManager, analyzerExecutor, _severityFilter).ConfigureAwait(false);
                    _lazyTreatAllCodeAsNonGeneratedCode = ComputeShouldTreatAllCodeAsNonGeneratedCode(UnsuppressedAnalyzers, GeneratedCodeAnalysisFlagsMap);
                    _lazyDoNotAnalyzeGeneratedCode = ComputeShouldSkipAnalysisOnGeneratedCode(UnsuppressedAnalyzers, GeneratedCodeAnalysisFlagsMap, TreatAllCodeAsNonGeneratedCode);
                    _lazyGeneratedCodeFilesMap = TreatAllCodeAsNonGeneratedCode ? null : new ConcurrentDictionary<SyntaxTree, bool>();
                    _lazyGeneratedCodeSymbolsForTreeMap = TreatAllCodeAsNonGeneratedCode ? null : new Dictionary<SyntaxTree, ImmutableHashSet<ISymbol>>();
                    _lazyIsGeneratedCodeSymbolMap = TreatAllCodeAsNonGeneratedCode ? null : new ConcurrentDictionary<ISymbol, bool>();
                    _lazyTreesWithHiddenRegionsMap = TreatAllCodeAsNonGeneratedCode ? null : new ConcurrentDictionary<SyntaxTree, bool>();
                    _lazySuppressedAnalyzersForTreeMap = new ConcurrentDictionary<SyntaxTree, ImmutableHashSet<DiagnosticAnalyzer>>();
                    _lazyGeneratedCodeAttribute = analyzerExecutor.Compilation?.GetTypeByMetadataName("System.CodeDom.Compiler.GeneratedCodeAttribute");

                    _lazySymbolActionsByKind = MakeSymbolActionsByKind(in AnalyzerActions);
                    _lazySemanticModelActions = MakeActionsByAnalyzer(AnalyzerActions.SemanticModelActions);
                    _lazySyntaxTreeActions = MakeActionsByAnalyzer(AnalyzerActions.SyntaxTreeActions);
                    _lazyAdditionalFileActions = MakeActionsByAnalyzer(AnalyzerActions.AdditionalFileActions);
                    _lazyCompilationActions = MakeActionsByAnalyzer(this.AnalyzerActions.CompilationActions);
                    _lazyCompilationEndActions = MakeActionsByAnalyzer(this.AnalyzerActions.CompilationEndActions);
                    _lazyCompilationEndAnalyzers = MakeCompilationEndAnalyzers(_lazyCompilationEndActions);

                    if (this.AnalyzerActions.SymbolStartActionsCount > 0)
                    {
                        _lazyPerSymbolAnalyzerActionsCache = new ConcurrentDictionary<(INamespaceOrTypeSymbol, DiagnosticAnalyzer), IGroupedAnalyzerActions>();
                    }

                }, cancellationToken);

                // create the primary driver task. 
                cancellationToken.ThrowIfCancellationRequested();

                _initializeSucceeded = true;
            }
            finally
            {
                if (_lazyInitializeTask == null)
                {
                    // Set initializeTask to be a cancelled task.
                    _lazyInitializeTask = Task.FromCanceled(new CancellationToken(canceled: true));

                    // Set primaryTask to be a cancelled task.
                    _lazyPrimaryTask = Task.FromCanceled(new CancellationToken(canceled: true));

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
            Debug.Assert(_lazyInitializeTask == null);
            Debug.Assert(compilation.SemanticModelProvider != null);

            var diagnosticQueue = DiagnosticQueue.Create(categorizeDiagnostics);

            Action<Diagnostic>? addNotCategorizedDiagnostic = null;
            Action<Diagnostic, DiagnosticAnalyzer, bool>? addCategorizedLocalDiagnostic = null;
            Action<Diagnostic, DiagnosticAnalyzer>? addCategorizedNonLocalDiagnostic = null;
            if (categorizeDiagnostics)
            {
                addCategorizedLocalDiagnostic = GetDiagnosticSink(diagnosticQueue.EnqueueLocal, compilation, analysisOptions.Options, _severityFilter, cancellationToken);
                addCategorizedNonLocalDiagnostic = GetDiagnosticSink(diagnosticQueue.EnqueueNonLocal, compilation, analysisOptions.Options, _severityFilter, cancellationToken);
            }
            else
            {
                addNotCategorizedDiagnostic = GetDiagnosticSink(diagnosticQueue.Enqueue, compilation, analysisOptions.Options, _severityFilter, cancellationToken);
            }

            // Wrap onAnalyzerException to pass in filtered diagnostic.
            Action<Exception, DiagnosticAnalyzer, Diagnostic> newOnAnalyzerException = (ex, analyzer, diagnostic) =>
            {
                var filteredDiagnostic = GetFilteredDiagnostic(diagnostic, compilation, analysisOptions.Options, _severityFilter, cancellationToken);
                if (filteredDiagnostic != null)
                {
                    if (analysisOptions.OnAnalyzerException != null)
                    {
                        analysisOptions.OnAnalyzerException(ex, analyzer, filteredDiagnostic);
                    }
                    else if (categorizeDiagnostics)
                    {
                        addCategorizedNonLocalDiagnostic!(filteredDiagnostic, analyzer);
                    }
                    else
                    {
                        addNotCategorizedDiagnostic!(filteredDiagnostic);
                    }
                }
            };

            var analyzerExecutor = AnalyzerExecutor.Create(
                compilation, analysisOptions.Options ?? AnalyzerOptions.Empty, addNotCategorizedDiagnostic, newOnAnalyzerException, analysisOptions.AnalyzerExceptionFilter,
                IsCompilerAnalyzer, AnalyzerManager, ShouldSkipAnalysisOnGeneratedCode, ShouldSuppressGeneratedCodeDiagnostic, IsGeneratedOrHiddenCodeLocation, IsAnalyzerSuppressedForTree, GetAnalyzerGate,
                getSemanticModel: GetOrCreateSemanticModel,
                analysisOptions.LogAnalyzerExecutionTime, addCategorizedLocalDiagnostic, addCategorizedNonLocalDiagnostic, s => _programmaticSuppressions!.Add(s), cancellationToken);

            Initialize(analyzerExecutor, diagnosticQueue, compilationData, cancellationToken);
        }

        private SemaphoreSlim? GetAnalyzerGate(DiagnosticAnalyzer analyzer)
        {
            if (AnalyzerGateMap.TryGetValue(analyzer, out var gate))
            {
                // Non-concurrent analyzer, needs all the callbacks guarded by a gate.
                return gate;
            }

            // Concurrent analyzer.
            return null;
        }

        private ImmutableHashSet<DiagnosticAnalyzer> ComputeNonConfigurableAnalyzers(ImmutableHashSet<DiagnosticAnalyzer> unsuppressedAnalyzers)
        {
            var builder = ImmutableHashSet.CreateBuilder<DiagnosticAnalyzer>();
            foreach (var analyzer in unsuppressedAnalyzers)
            {
                var descriptors = AnalyzerManager.GetSupportedDiagnosticDescriptors(analyzer, AnalyzerExecutor);
                foreach (var descriptor in descriptors)
                {
                    if (descriptor.IsNotConfigurable())
                    {
                        builder.Add(analyzer);
                        break;
                    }
                }
            }

            return builder.ToImmutableHashSet();
        }

        private ImmutableHashSet<DiagnosticAnalyzer> ComputeSymbolStartAnalyzers(ImmutableHashSet<DiagnosticAnalyzer> unsuppressedAnalyzers)
        {
            var builder = ImmutableHashSet.CreateBuilder<DiagnosticAnalyzer>();
            foreach (var action in this.AnalyzerActions.SymbolStartActions)
            {
                if (unsuppressedAnalyzers.Contains(action.Analyzer))
                {
                    builder.Add(action.Analyzer);
                }
            }

            return builder.ToImmutableHashSet();
        }

        private static bool ComputeShouldSkipAnalysisOnGeneratedCode(
            ImmutableHashSet<DiagnosticAnalyzer> analyzers,
            ImmutableSegmentedDictionary<DiagnosticAnalyzer, GeneratedCodeAnalysisFlags> generatedCodeAnalysisFlagsMap,
            bool treatAllCodeAsNonGeneratedCode)
        {
            foreach (var analyzer in analyzers)
            {
                if (!ShouldSkipAnalysisOnGeneratedCode(analyzer, generatedCodeAnalysisFlagsMap, treatAllCodeAsNonGeneratedCode))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns true if all analyzers need to analyze and report diagnostics in generated code - we can assume all code to be non-generated code.
        /// </summary>
        private static bool ComputeShouldTreatAllCodeAsNonGeneratedCode(ImmutableHashSet<DiagnosticAnalyzer> analyzers, ImmutableSegmentedDictionary<DiagnosticAnalyzer, GeneratedCodeAnalysisFlags> generatedCodeAnalysisFlagsMap)
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
            => ShouldSkipAnalysisOnGeneratedCode(analyzer, GeneratedCodeAnalysisFlagsMap, TreatAllCodeAsNonGeneratedCode);

        private static bool ShouldSkipAnalysisOnGeneratedCode(
            DiagnosticAnalyzer analyzer,
            ImmutableSegmentedDictionary<DiagnosticAnalyzer, GeneratedCodeAnalysisFlags> generatedCodeAnalysisFlagsMap,
            bool treatAllCodeAsNonGeneratedCode)
        {
            if (treatAllCodeAsNonGeneratedCode)
            {
                return false;
            }

            var mode = generatedCodeAnalysisFlagsMap[analyzer];
            return (mode & GeneratedCodeAnalysisFlags.Analyze) == 0;
        }

        private bool ShouldSuppressGeneratedCodeDiagnostic(Diagnostic diagnostic, DiagnosticAnalyzer analyzer, Compilation compilation, CancellationToken cancellationToken)
        {
            if (TreatAllCodeAsNonGeneratedCode)
            {
                return false;
            }

            var generatedCodeAnalysisFlags = GeneratedCodeAnalysisFlagsMap[analyzer];
            var suppressInGeneratedCode = (generatedCodeAnalysisFlags & GeneratedCodeAnalysisFlags.ReportDiagnostics) == 0;
            return suppressInGeneratedCode && IsInGeneratedCode(diagnostic.Location, compilation, cancellationToken);
        }

        /// <summary>
        /// Attaches a pre-populated event queue to the driver and processes all events in the queue.
        /// </summary>
        /// <param name="eventQueue">Compilation events to analyze.</param>
        /// <param name="analysisScope">Scope of analysis.</param>
        /// <param name="analysisState">An optional object to track partial analysis state.</param>
        /// <param name="cancellationToken">Cancellation token to abort analysis.</param>
        /// <remarks>Driver must be initialized before invoking this method, i.e. <see cref="Initialize(AnalyzerExecutor, DiagnosticQueue, CompilationData, CancellationToken)"/> method must have been invoked and <see cref="WhenInitializedTask"/> must be non-null.</remarks>
        internal async Task AttachQueueAndProcessAllEventsAsync(AsyncQueue<CompilationEvent> eventQueue, AnalysisScope analysisScope, AnalysisState? analysisState, CancellationToken cancellationToken)
        {
            try
            {
                if (_initializeSucceeded)
                {
                    _lazyCompilationEventQueue = eventQueue;
                    _lazyQueueRegistration = default(CancellationTokenRegistration);

                    await ExecutePrimaryAnalysisTaskAsync(analysisScope, analysisState, usingPrePopulatedEventQueue: true, cancellationToken: cancellationToken).ConfigureAwait(false);

                    _lazyPrimaryTask = Task.FromResult(true);
                }
            }
            finally
            {
                if (_lazyPrimaryTask == null)
                {
                    // Set primaryTask to be a cancelled task.
                    _lazyPrimaryTask = Task.FromCanceled(new CancellationToken(canceled: true));
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
                    _lazyCompilationEventQueue = eventQueue;
                    _lazyQueueRegistration = cancellationToken.Register(() =>
                    {
                        this.CompilationEventQueue.TryComplete();
                        this.DiagnosticQueue.TryComplete();
                    });

                    _lazyPrimaryTask = ExecutePrimaryAnalysisTaskAsync(analysisScope, analysisState: null, usingPrePopulatedEventQueue: false, cancellationToken: cancellationToken)
                        .ContinueWith(c => DiagnosticQueue.TryComplete(), cancellationToken, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
                }
            }
            finally
            {
                if (_lazyPrimaryTask == null)
                {
                    // Set primaryTask to be a cancelled task.
                    _lazyPrimaryTask = Task.FromCanceled(new CancellationToken(canceled: true));

                    // Try to set the DiagnosticQueue to be complete.
                    this.DiagnosticQueue.TryComplete();
                }
            }
        }

        private async Task ExecutePrimaryAnalysisTaskAsync(AnalysisScope analysisScope, AnalysisState? analysisState, bool usingPrePopulatedEventQueue, CancellationToken cancellationToken)
        {
            Debug.Assert(analysisScope != null);

            await WhenInitializedTask.ConfigureAwait(false);

            if (WhenInitializedTask.IsFaulted)
            {
                OnDriverException(WhenInitializedTask, this.AnalyzerExecutor, analysisScope.Analyzers);
            }
            else if (!WhenInitializedTask.IsCanceled)
            {
                _lazyAnalyzerExecutor = this.AnalyzerExecutor.WithCancellationToken(cancellationToken);

                await ProcessCompilationEventsAsync(analysisScope, analysisState, usingPrePopulatedEventQueue, cancellationToken).ConfigureAwait(false);

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

        private void ExecuteSyntaxTreeActions(AnalysisScope analysisScope, AnalysisState? analysisState, CancellationToken cancellationToken)
        {
            if (analysisScope.IsSingleFileAnalysis && !analysisScope.IsSyntacticSingleFileAnalysis)
            {
                // For partial analysis, only execute syntax tree actions if performing syntax analysis.
                return;
            }

            foreach (var tree in analysisScope.SyntaxTrees)
            {
                var isGeneratedCode = IsGeneratedCode(tree);
                var file = new SourceOrAdditionalFile(tree);
                if (isGeneratedCode && DoNotAnalyzeGeneratedCode)
                {
                    analysisState?.MarkSyntaxAnalysisComplete(file, analysisScope.Analyzers);
                    continue;
                }

                var processedAnalyzers = analysisState != null ? PooledHashSet<DiagnosticAnalyzer>.GetInstance() : null;
                try
                {
                    foreach (var (analyzer, syntaxTreeActions) in _lazySyntaxTreeActions)
                    {
                        if (!analysisScope.Contains(analyzer))
                        {
                            continue;
                        }

                        cancellationToken.ThrowIfCancellationRequested();

                        // Execute actions for a given analyzer sequentially.
                        AnalyzerExecutor.TryExecuteSyntaxTreeActions(syntaxTreeActions, analyzer, file, analysisScope, analysisState, isGeneratedCode);

                        processedAnalyzers?.Add(analyzer);
                    }

                    analysisState?.MarkSyntaxAnalysisCompleteForUnprocessedAnalyzers(file, analysisScope, processedAnalyzers!);
                }
                finally
                {
                    processedAnalyzers?.Free();
                }
            }
        }

        private void ExecuteAdditionalFileActions(AnalysisScope analysisScope, AnalysisState? analysisState, CancellationToken cancellationToken)
        {
            if (analysisScope.IsSingleFileAnalysis && !analysisScope.IsSyntacticSingleFileAnalysis)
            {
                // For partial analysis, only execute additional file actions if performing syntactic single file analysis.
                return;
            }

            foreach (var additionalFile in analysisScope.AdditionalFiles)
            {
                var file = new SourceOrAdditionalFile(additionalFile);

                var processedAnalyzers = analysisState != null ? PooledHashSet<DiagnosticAnalyzer>.GetInstance() : null;
                try
                {
                    foreach (var (analyzer, additionalFileActions) in _lazyAdditionalFileActions)
                    {
                        if (!analysisScope.Contains(analyzer))
                        {
                            continue;
                        }

                        cancellationToken.ThrowIfCancellationRequested();

                        // Execute actions for a given analyzer sequentially.
                        AnalyzerExecutor.TryExecuteAdditionalFileActions(additionalFileActions, analyzer, file, analysisScope, analysisState);

                        processedAnalyzers?.Add(analyzer);
                    }

                    analysisState?.MarkSyntaxAnalysisCompleteForUnprocessedAnalyzers(file, analysisScope, processedAnalyzers!);
                }
                finally
                {
                    processedAnalyzers?.Free();
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
        /// <param name="severityFilter">Filtered diagnostic severities in the compilation, i.e. diagnostics with effective severity from this set should not be reported.</param>
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
            SeverityFilter severityFilter,
            out Compilation newCompilation,
            CancellationToken cancellationToken)
        {
            Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException =
                (ex, analyzer, diagnostic) => addExceptionDiagnostic?.Invoke(diagnostic);

            Func<Exception, bool>? nullFilter = null;
            return CreateAndAttachToCompilation(compilation, analyzers, options, analyzerManager, onAnalyzerException, nullFilter, reportAnalyzer, severityFilter, out newCompilation, cancellationToken: cancellationToken);
        }

        // internal for testing purposes
        internal static AnalyzerDriver CreateAndAttachToCompilation(
            Compilation compilation,
            ImmutableArray<DiagnosticAnalyzer> analyzers,
            AnalyzerOptions options,
            AnalyzerManager analyzerManager,
            Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException,
            Func<Exception, bool>? analyzerExceptionFilter,
            bool reportAnalyzer,
            SeverityFilter severityFilter,
            out Compilation newCompilation,
            CancellationToken cancellationToken)
        {
            AnalyzerDriver analyzerDriver = compilation.CreateAnalyzerDriver(analyzers, analyzerManager, severityFilter);
            newCompilation = compilation
                .WithSemanticModelProvider(new CachingSemanticModelProvider())
                .WithEventQueue(new AsyncQueue<CompilationEvent>());

            var categorizeDiagnostics = false;
            var analysisOptions = new CompilationWithAnalyzersOptions(options, onAnalyzerException, analyzerExceptionFilter: analyzerExceptionFilter, concurrentAnalysis: true, logAnalyzerExecutionTime: reportAnalyzer, reportSuppressedDiagnostics: false);
            analyzerDriver.Initialize(newCompilation, analysisOptions, new CompilationData(newCompilation), categorizeDiagnostics, cancellationToken);

            var analysisScope = new AnalysisScope(newCompilation, options, analyzers, hasAllAnalyzers: true, concurrentAnalysis: newCompilation.Options.ConcurrentBuild, categorizeDiagnostics: categorizeDiagnostics);
            analyzerDriver.AttachQueueAndStartProcessingEvents(newCompilation.EventQueue!, analysisScope, cancellationToken: cancellationToken);
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
            while (DiagnosticQueue.TryDequeue(out var diagnostic))
            {
                diagnostic = suppressMessageState.ApplySourceSuppressions(diagnostic);
                if (reportSuppressedDiagnostics || !diagnostic.IsSuppressed)
                {
                    allDiagnostics.Add(diagnostic);
                }
            }

            return allDiagnostics.ToReadOnlyAndFree();
        }

        private SemanticModel GetOrCreateSemanticModel(SyntaxTree tree)
            => GetOrCreateSemanticModel(tree, AnalyzerExecutor.Compilation);

        private SemanticModel GetOrCreateSemanticModel(SyntaxTree tree, Compilation compilation)
            => SemanticModelProvider.GetSemanticModel(tree, compilation);

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
                    // Kick off tasks to concurrently execute suppressors.
                    // Note that we avoid using Parallel.ForEach here to avoid wrapped exceptions.
                    // See https://github.com/dotnet/roslyn/issues/41713 for details.
                    var tasks = ArrayBuilder<Task>.GetInstance();
                    try
                    {
                        foreach (var suppressor in suppressors)
                        {
                            var task = Task.Run(
                                () => AnalyzerExecutor.ExecuteSuppressionAction(suppressor, getSuppressableDiagnostics(suppressor)),
                                AnalyzerExecutor.CancellationToken);
                            tasks.Add(task);
                        }

                        Task.WaitAll(tasks.ToArray(), AnalyzerExecutor.CancellationToken);
                    }
                    finally
                    {
                        tasks.Free();
                    }
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
            return ApplyProgrammaticSuppressionsAndFilterDiagnostics(diagnostics, compilation);
        }

        private static ImmutableArray<Diagnostic> FilterDiagnosticsSuppressedInSource(
            ImmutableArray<Diagnostic> diagnostics,
            Compilation compilation,
            SuppressMessageAttributeState suppressMessageState)
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

        internal ImmutableArray<Diagnostic> ApplyProgrammaticSuppressionsAndFilterDiagnostics(ImmutableArray<Diagnostic> reportedDiagnostics, Compilation compilation)
        {
            if (reportedDiagnostics.IsEmpty)
            {
                return reportedDiagnostics;
            }

            var diagnostics = ApplyProgrammaticSuppressions(reportedDiagnostics, compilation);
            if (compilation.Options.ReportSuppressedDiagnostics || diagnostics.All(d => !d.IsSuppressed))
            {
                return diagnostics;
            }

            return diagnostics.WhereAsArray(d => !d.IsSuppressed);
        }

        private bool IsInGeneratedCode(Location location, Compilation compilation, CancellationToken cancellationToken)
        {
            if (TreatAllCodeAsNonGeneratedCode || !location.IsInSource)
            {
                return false;
            }

            Debug.Assert(location.SourceTree != null);

            // Check if this is a generated code location.
            if (IsGeneratedOrHiddenCodeLocation(location.SourceTree, location.SourceSpan))
            {
                return true;
            }

            // Check if the file has generated code definitions (i.e. symbols with GeneratedCodeAttribute).
            if (_lazyGeneratedCodeAttribute != null)
            {
                var generatedCodeSymbolsInTree = getOrComputeGeneratedCodeSymbolsInTree(location.SourceTree, compilation, cancellationToken);
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

            ImmutableHashSet<ISymbol> getOrComputeGeneratedCodeSymbolsInTree(SyntaxTree tree, Compilation compilation, CancellationToken cancellationToken)
            {
                Debug.Assert(GeneratedCodeSymbolsForTreeMap != null);
                Debug.Assert(_lazyGeneratedCodeAttribute != null);

                ImmutableHashSet<ISymbol>? generatedCodeSymbols;
                lock (GeneratedCodeSymbolsForTreeMap)
                {
                    if (GeneratedCodeSymbolsForTreeMap.TryGetValue(tree, out generatedCodeSymbols))
                    {
                        return generatedCodeSymbols;
                    }
                }

                generatedCodeSymbols = computeGeneratedCodeSymbolsInTree(tree, compilation, _lazyGeneratedCodeAttribute, cancellationToken);

                lock (GeneratedCodeSymbolsForTreeMap)
                {
                    ImmutableHashSet<ISymbol>? existingGeneratedCodeSymbols;
                    if (!GeneratedCodeSymbolsForTreeMap.TryGetValue(tree, out existingGeneratedCodeSymbols))
                    {
                        GeneratedCodeSymbolsForTreeMap.Add(tree, generatedCodeSymbols);
                    }
                    else
                    {
                        Debug.Assert(existingGeneratedCodeSymbols.SetEquals(generatedCodeSymbols));
                    }
                }

                return generatedCodeSymbols;

                static ImmutableHashSet<ISymbol> computeGeneratedCodeSymbolsInTree(SyntaxTree tree, Compilation compilation, INamedTypeSymbol generatedCodeAttribute, CancellationToken cancellationToken)
                {
                    // PERF: Bail out early if file doesn't have "GeneratedCode" text.
                    var walker = new GeneratedCodeTokenWalker(cancellationToken);
                    walker.Visit(tree.GetRoot(cancellationToken));
                    if (!walker.HasGeneratedCodeIdentifier)
                    {
                        return ImmutableHashSet<ISymbol>.Empty;
                    }

                    var model = compilation.GetSemanticModel(tree);
                    var root = tree.GetRoot(cancellationToken);
                    var span = root.FullSpan;
                    var declarationInfoBuilder = ArrayBuilder<DeclarationInfo>.GetInstance();
                    model.ComputeDeclarationsInSpan(span, getSymbol: true, builder: declarationInfoBuilder, cancellationToken: cancellationToken);

                    ImmutableHashSet<ISymbol>.Builder? generatedSymbolsBuilder = null;
                    foreach (var declarationInfo in declarationInfoBuilder)
                    {
                        var symbol = declarationInfo.DeclaredSymbol;
                        if (symbol != null &&
                            GeneratedCodeUtilities.IsGeneratedSymbolWithGeneratedCodeAttribute(symbol, generatedCodeAttribute))
                        {
                            generatedSymbolsBuilder ??= ImmutableHashSet.CreateBuilder<ISymbol>();
                            generatedSymbolsBuilder.Add(symbol);
                        }
                    }

                    declarationInfoBuilder.Free();
                    return generatedSymbolsBuilder != null ? generatedSymbolsBuilder.ToImmutable() : ImmutableHashSet<ISymbol>.Empty;
                }
            }
        }

        private bool IsAnalyzerSuppressedForTree(DiagnosticAnalyzer analyzer, SyntaxTree tree, SyntaxTreeOptionsProvider? options)
        {
            if (!SuppressedAnalyzersForTreeMap.TryGetValue(tree, out var suppressedAnalyzers))
            {
                suppressedAnalyzers = SuppressedAnalyzersForTreeMap.GetOrAdd(tree, ComputeSuppressedAnalyzersForTree(tree, options));
            }

            return suppressedAnalyzers.Contains(analyzer);
        }

        private ImmutableHashSet<DiagnosticAnalyzer> ComputeSuppressedAnalyzersForTree(SyntaxTree tree, SyntaxTreeOptionsProvider? options)
        {
            if (options is null)
            {
                return ImmutableHashSet<DiagnosticAnalyzer>.Empty;
            }

            ImmutableHashSet<DiagnosticAnalyzer>.Builder? suppressedAnalyzersBuilder = null;
            foreach (var analyzer in UnsuppressedAnalyzers)
            {
                if (NonConfigurableAnalyzers.Contains(analyzer))
                {
                    // Analyzers reporting non-configurable diagnostics cannot be suppressed as user configuration is ignored for these analyzers.
                    continue;
                }

                if ((SymbolStartAnalyzers.Contains(analyzer) || CompilationEndAnalyzers.Contains(analyzer)) &&
                    !ShouldSkipAnalysisOnGeneratedCode(analyzer))
                {
                    // SymbolStart/End analyzers and CompilationStart/End analyzers that analyze generated code
                    // cannot have any of their callbacks suppressed as they need to analyze the entire compilation for correctness.
                    continue;
                }

                var descriptors = AnalyzerManager.GetSupportedDiagnosticDescriptors(analyzer, AnalyzerExecutor);
                var hasUnsuppressedDiagnostic = false;
                foreach (var descriptor in descriptors)
                {
                    _ = options.TryGetGlobalDiagnosticValue(descriptor.Id, AnalyzerExecutor.CancellationToken, out var configuredSeverity);
                    if (options.TryGetDiagnosticValue(tree, descriptor.Id, AnalyzerExecutor.CancellationToken, out var diagnosticSeverity))
                    {
                        configuredSeverity = diagnosticSeverity;
                    }

                    if (configuredSeverity != ReportDiagnostic.Suppress)
                    {
                        // Analyzer reports a diagnostic that is not suppressed by the diagnostic options for this tree.
                        hasUnsuppressedDiagnostic = true;
                        break;
                    }
                }

                if (!hasUnsuppressedDiagnostic)
                {
                    suppressedAnalyzersBuilder ??= ImmutableHashSet.CreateBuilder<DiagnosticAnalyzer>();
                    suppressedAnalyzersBuilder.Add(analyzer);
                }
            }

            return suppressedAnalyzersBuilder != null ? suppressedAnalyzersBuilder.ToImmutable() : ImmutableHashSet<DiagnosticAnalyzer>.Empty;
        }

        public bool IsInitialized => _lazyInitializeTask != null;

        /// <summary>
        /// Return a task that completes when the driver is initialized.
        /// </summary>
        public Task WhenInitializedTask
        {
            get
            {
                Debug.Assert(_lazyInitializeTask != null);
                return _lazyInitializeTask;
            }
        }

        /// <summary>
        /// Return a task that completes when the driver is done producing diagnostics.
        /// </summary>
        public Task WhenCompletedTask
        {
            get
            {
                Debug.Assert(_lazyPrimaryTask != null);
                return _lazyPrimaryTask;
            }
        }

        internal ImmutableDictionary<DiagnosticAnalyzer, TimeSpan> AnalyzerExecutionTimes => AnalyzerExecutor.AnalyzerExecutionTimes;
        internal TimeSpan ResetAnalyzerExecutionTime(DiagnosticAnalyzer analyzer) => AnalyzerExecutor.ResetAnalyzerExecutionTime(analyzer);

        private static ImmutableArray<(DiagnosticAnalyzer, ImmutableArray<ImmutableArray<SymbolAnalyzerAction>>)> MakeSymbolActionsByKind(in AnalyzerActions analyzerActions)
        {
            var builder = ArrayBuilder<(DiagnosticAnalyzer, ImmutableArray<ImmutableArray<SymbolAnalyzerAction>>)>.GetInstance();
            var actionsByAnalyzers = analyzerActions.SymbolActions.GroupBy(action => action.Analyzer);
            var actionsByKindBuilder = ArrayBuilder<ArrayBuilder<SymbolAnalyzerAction>>.GetInstance();
            foreach (var analyzerAndActions in actionsByAnalyzers)
            {
                actionsByKindBuilder.Clear();
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
                builder.Add((analyzerAndActions.Key, actionsByKind));
            }

            actionsByKindBuilder.Free();
            return builder.ToImmutableAndFree();
        }

        private static ImmutableArray<(DiagnosticAnalyzer, ImmutableArray<TAnalyzerAction>)> MakeActionsByAnalyzer<TAnalyzerAction>(in ImmutableArray<TAnalyzerAction> analyzerActions)
            where TAnalyzerAction : AnalyzerAction
        {
            var builder = ArrayBuilder<(DiagnosticAnalyzer, ImmutableArray<TAnalyzerAction>)>.GetInstance();
            var actionsByAnalyzers = analyzerActions.GroupBy(action => action.Analyzer);
            foreach (var analyzerAndActions in actionsByAnalyzers)
            {
                builder.Add((analyzerAndActions.Key, analyzerAndActions.ToImmutableArray()));
            }

            return builder.ToImmutableAndFree();
        }

        private static ImmutableHashSet<DiagnosticAnalyzer> MakeCompilationEndAnalyzers(ImmutableArray<(DiagnosticAnalyzer, ImmutableArray<CompilationAnalyzerAction>)> compilationEndActionsByAnalyzer)
        {
            var builder = ImmutableHashSet.CreateBuilder<DiagnosticAnalyzer>();
            foreach (var (analyzer, _) in compilationEndActionsByAnalyzer)
            {
                builder.Add(analyzer);
            }

            return builder.ToImmutable();
        }

        private async Task ProcessCompilationEventsAsync(AnalysisScope analysisScope, AnalysisState? analysisState, bool prePopulatedEventQueue, CancellationToken cancellationToken)
        {
            try
            {
                CompilationCompletedEvent? completedEvent = null;

                if (analysisScope.ConcurrentAnalysis)
                {
                    // Kick off worker tasks to process all compilation events (except the compilation end event) in parallel.
                    // Compilation end event must be processed after all other events.

                    var workerCount = prePopulatedEventQueue ? Math.Min(CompilationEventQueue.Count, _workerCount) : _workerCount;

                    var workerTasks = new Task<CompilationCompletedEvent?>[workerCount];
                    for (int i = 0; i < workerCount; i++)
                    {
                        // Create separate worker tasks to process all compilation events - we do not want to process any events on the main thread.
                        workerTasks[i] = Task.Run(async () => await ProcessCompilationEventsCoreAsync(analysisScope, analysisState, prePopulatedEventQueue, cancellationToken).ConfigureAwait(false));
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    // Kick off tasks to execute syntax tree actions.
                    var syntaxTreeActionsTask = Task.Run(() => ExecuteSyntaxTreeActions(analysisScope, analysisState, cancellationToken), cancellationToken);

                    // Kick off tasks to execute additional file actions.
                    var additionalFileActionsTask = Task.Run(() => ExecuteAdditionalFileActions(analysisScope, analysisState, cancellationToken), cancellationToken);

                    // Wait for all worker threads to complete processing events.
                    await Task.WhenAll(workerTasks.Concat(syntaxTreeActionsTask).Concat(additionalFileActionsTask)).ConfigureAwait(false);

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
                    completedEvent = await ProcessCompilationEventsCoreAsync(analysisScope, analysisState, prePopulatedEventQueue, cancellationToken).ConfigureAwait(false);

                    ExecuteSyntaxTreeActions(analysisScope, analysisState, cancellationToken);
                    ExecuteAdditionalFileActions(analysisScope, analysisState, cancellationToken);
                }

                // Finally process the compilation completed event, if any.
                if (completedEvent != null)
                {
                    await ProcessEventAsync(completedEvent, analysisScope, analysisState, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private async Task<CompilationCompletedEvent?> ProcessCompilationEventsCoreAsync(AnalysisScope analysisScope, AnalysisState? analysisState, bool prePopulatedEventQueue, CancellationToken cancellationToken)
        {
            try
            {
                CompilationCompletedEvent? completedEvent = null;

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

                    if (!CompilationEventQueue.TryDequeue(out var compilationEvent))
                    {
                        if (!prePopulatedEventQueue)
                        {
                            var optionalEvent = await CompilationEventQueue.TryDequeueAsync(cancellationToken).ConfigureAwait(false);
                            if (!optionalEvent.HasValue)
                            {
                                // When the queue is completed with a pending TryDequeueAsync return, the
                                // the Optional<T> will not have a value. This signals the queue has reached
                                // completion and no more items will be added to it.
                                Debug.Assert(CompilationEventQueue.IsCompleted, "TryDequeueAsync should provide a value unless the AsyncQueue<T> is completed.");
                                break;
                            }

                            compilationEvent = optionalEvent.Value;
                        }
                        else
                        {
                            return completedEvent;
                        }
                    }

                    // Don't process the compilation completed event as other worker threads might still be processing other compilation events.
                    // The caller will wait for all workers to complete and finally process this event.
                    if (compilationEvent is CompilationCompletedEvent compilationCompletedEvent)
                    {
                        completedEvent = compilationCompletedEvent;
                        continue;
                    }

                    await ProcessEventAsync(compilationEvent, analysisScope, analysisState, cancellationToken).ConfigureAwait(false);
                }

                return completedEvent;
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        private async Task ProcessEventAsync(CompilationEvent e, AnalysisScope analysisScope, AnalysisState? analysisState, CancellationToken cancellationToken)
        {
            EventProcessedState eventProcessedState = await TryProcessEventCoreAsync(e, analysisScope, analysisState, cancellationToken).ConfigureAwait(false);

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

            await OnEventProcessedCoreAsync(e, processedAnalyzers, analysisState, cancellationToken).ConfigureAwait(false);
        }

        private async Task OnEventProcessedCoreAsync(CompilationEvent compilationEvent, ImmutableArray<DiagnosticAnalyzer> processedAnalyzers, AnalysisState? analysisState, CancellationToken cancellationToken)
        {
            if (analysisState != null)
            {
                await analysisState.OnCompilationEventProcessedAsync(compilationEvent, processedAnalyzers, onSymbolAndMembersProcessedAsync).ConfigureAwait(false);
                return;
            }

            switch (compilationEvent)
            {
                case SymbolDeclaredCompilationEvent symbolDeclaredEvent:
                    if (AnalyzerActions.SymbolStartActionsCount > 0)
                    {
                        foreach (var analyzer in processedAnalyzers)
                        {
                            await onSymbolAndMembersProcessedAsync(symbolDeclaredEvent.Symbol, analyzer).ConfigureAwait(false);
                        }
                    }

                    break;

                case CompilationUnitCompletedEvent compilationUnitCompletedEvent when !compilationUnitCompletedEvent.FilterSpan.HasValue:
                    // Clear the semantic model cache only if we have completed analysis for the entire compilation unit,
                    // i.e. the event has a null filter span. Compilation unit completed event with a non-null filter span
                    // indicates a synthesized event for partial analysis of the tree and we avoid clearing the semantic model cache for that case. 
                    SemanticModelProvider.ClearCache(compilationUnitCompletedEvent.CompilationUnit, compilationUnitCompletedEvent.Compilation);
                    break;

                case CompilationCompletedEvent compilationCompletedEvent:
                    SemanticModelProvider.ClearCache(compilationCompletedEvent.Compilation);
                    break;
            }

            return;

            async Task onSymbolAndMembersProcessedAsync(ISymbol symbol, DiagnosticAnalyzer analyzer)
            {
                if (AnalyzerActions.SymbolStartActionsCount == 0 || symbol.IsImplicitlyDeclared)
                {
                    return;
                }

                if (symbol is INamespaceOrTypeSymbol namespaceOrType)
                {
                    PerSymbolAnalyzerActionsCache.TryRemove((namespaceOrType, analyzer), out _);
                }

                await processContainerOnMemberCompletedAsync(symbol.ContainingNamespace, symbol, analyzer).ConfigureAwait(false);
                await processContainerOnMemberCompletedAsync(symbol.ContainingType, symbol, analyzer).ConfigureAwait(false);
            }

            async Task processContainerOnMemberCompletedAsync(INamespaceOrTypeSymbol containerSymbol, ISymbol processedMemberSymbol, DiagnosticAnalyzer analyzer)
            {
                if (containerSymbol != null &&
                    AnalyzerExecutor.TryExecuteSymbolEndActionsForContainer(containerSymbol, processedMemberSymbol,
                        analyzer, s_getTopmostNodeForAnalysis, analysisState, out var processedContainerEvent))
                {
                    await OnEventProcessedCoreAsync(processedContainerEvent, ImmutableArray.Create(analyzer), analysisState, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        [PerformanceSensitive(
            "https://developercommunity.visualstudio.com/content/problem/805524/ctrl-suggestions-are-very-slow-and-produce-gatheri.html",
            OftenCompletesSynchronously = true)]
        private async ValueTask<EventProcessedState> TryProcessEventCoreAsync(CompilationEvent compilationEvent, AnalysisScope analysisScope, AnalysisState? analysisState, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            return compilationEvent switch
            {
                SymbolDeclaredCompilationEvent symbolEvent =>
                    await TryProcessSymbolDeclaredAsync(symbolEvent, analysisScope, analysisState, cancellationToken).ConfigureAwait(false),

                CompilationUnitCompletedEvent completedEvent =>
                    TryProcessCompilationUnitCompleted(completedEvent, analysisScope, analysisState, cancellationToken) ? EventProcessedState.Processed : EventProcessedState.NotProcessed,

                CompilationCompletedEvent endEvent =>
                    TryProcessCompilationCompleted(endEvent, analysisScope, analysisState) ? EventProcessedState.Processed : EventProcessedState.NotProcessed,

                CompilationStartedEvent startedEvent =>
                    TryProcessCompilationStarted(startedEvent, analysisScope, analysisState) ? EventProcessedState.Processed : EventProcessedState.NotProcessed,

                _ => throw new InvalidOperationException("Unexpected compilation event of type " + compilationEvent.GetType().Name)
            };
        }

        /// <summary>
        /// Tries to execute symbol action, symbol start/end actions and declaration actions for the given symbol.
        /// </summary>
        /// <returns>
        /// <see cref="EventProcessedState"/> indicating the current state of processing of the given compilation event.
        /// </returns>
        [PerformanceSensitive(
            "https://developercommunity.visualstudio.com/content/problem/805524/ctrl-suggestions-are-very-slow-and-produce-gatheri.html",
            OftenCompletesSynchronously = true)]
        private async ValueTask<EventProcessedState> TryProcessSymbolDeclaredAsync(SymbolDeclaredCompilationEvent symbolEvent, AnalysisScope analysisScope, AnalysisState? analysisState, CancellationToken cancellationToken)
        {
            // Attempt to execute all analyzer actions.
            var processedState = EventProcessedState.Processed;
            var symbol = symbolEvent.Symbol;
            var isGeneratedCodeSymbol = IsGeneratedCodeSymbol(symbol);

            var skipSymbolAnalysis = AnalysisScope.ShouldSkipSymbolAnalysis(symbolEvent);
            var skipDeclarationAnalysis = AnalysisScope.ShouldSkipDeclarationAnalysis(symbol);
            var hasPerSymbolActions = AnalyzerActions.SymbolStartActionsCount > 0 && (!skipSymbolAnalysis || !skipDeclarationAnalysis);

            var perSymbolActions = hasPerSymbolActions ?
                await GetPerSymbolAnalyzerActionsAsync(symbol, analysisScope, analysisState, cancellationToken).ConfigureAwait(false) :
                EmptyGroupedActions;

            if (!skipSymbolAnalysis &&
                !TryExecuteSymbolActions(symbolEvent, analysisScope, analysisState, isGeneratedCodeSymbol, cancellationToken))
            {
                processedState = EventProcessedState.NotProcessed;
            }

            if (!skipDeclarationAnalysis &&
                !TryExecuteDeclaringReferenceActions(symbolEvent, analysisScope, analysisState, isGeneratedCodeSymbol, perSymbolActions, cancellationToken))
            {
                processedState = EventProcessedState.NotProcessed;
            }

            if (processedState.Kind == EventProcessedStateKind.Processed &&
                hasPerSymbolActions &&
                !TryExecuteSymbolEndActions(perSymbolActions.AnalyzerActions, symbolEvent, analysisScope, analysisState, out var subsetProcessedAnalyzers))
            {
                Debug.Assert(!subsetProcessedAnalyzers.IsDefault);
                processedState = subsetProcessedAnalyzers.IsEmpty ? EventProcessedState.NotProcessed : EventProcessedState.CreatePartiallyProcessed(subsetProcessedAnalyzers);
            }

            return processedState;
        }

        /// <summary>
        /// Tries to execute symbol actions.
        /// </summary>
        /// <returns>
        /// True, if successfully executed the actions for the given analysis scope OR no actions were required to be executed for the given analysis scope.
        /// False, otherwise.
        /// </returns>
        private bool TryExecuteSymbolActions(SymbolDeclaredCompilationEvent symbolEvent, AnalysisScope analysisScope, AnalysisState? analysisState, bool isGeneratedCodeSymbol, CancellationToken cancellationToken)
        {
            var symbol = symbolEvent.Symbol;
            if (!analysisScope.ShouldAnalyze(symbol))
            {
                return true;
            }

            var processedAnalyzers = analysisState != null ? PooledHashSet<DiagnosticAnalyzer>.GetInstance() : null;
            try
            {
                var success = true;
                foreach (var (analyzer, actionsByKind) in _lazySymbolActionsByKind)
                {
                    if (!analysisScope.Contains(analyzer))
                    {
                        continue;
                    }

                    // Invoke symbol analyzers only for source symbols.
                    if ((int)symbol.Kind < actionsByKind.Length)
                    {
                        if (!AnalyzerExecutor.TryExecuteSymbolActions(actionsByKind[(int)symbol.Kind], analyzer, symbolEvent, s_getTopmostNodeForAnalysis, analysisScope, analysisState, isGeneratedCodeSymbol))
                        {
                            success = false;
                        }

                        processedAnalyzers?.Add(analyzer);
                    }
                }

                analysisState?.MarkSymbolCompleteForUnprocessedAnalyzers(symbol, analysisScope, processedAnalyzers!);
                return success;
            }
            finally
            {
                processedAnalyzers?.Free();
            }
        }

        private bool TryExecuteSymbolEndActions(
            in AnalyzerActions perSymbolActions,
            SymbolDeclaredCompilationEvent symbolEvent,
            AnalysisScope analysisScope,
            AnalysisState? analysisState,
            out ImmutableArray<DiagnosticAnalyzer> subsetProcessedAnalyzers)
        {
            Debug.Assert(AnalyzerActions.SymbolStartActionsCount > 0);

            var symbol = symbolEvent.Symbol;
            var symbolEndActions = perSymbolActions.SymbolEndActions;
            if (!analysisScope.ShouldAnalyze(symbol) || symbolEndActions.IsEmpty)
            {
                analysisState?.MarkSymbolEndAnalysisComplete(symbol, analysisScope.Analyzers);
                subsetProcessedAnalyzers = ImmutableArray<DiagnosticAnalyzer>.Empty;
                return true;
            }

            var success = true;
            var completedAnalyzers = ArrayBuilder<DiagnosticAnalyzer>.GetInstance();
            var processedAnalyzers = PooledHashSet<DiagnosticAnalyzer>.GetInstance();
            try
            {
                foreach (var groupedActions in symbolEndActions.GroupBy(a => a.Analyzer))
                {
                    var analyzer = groupedActions.Key;
                    if (!analysisScope.Contains(analyzer))
                    {
                        continue;
                    }

                    processedAnalyzers.Add(analyzer);

                    var symbolEndActionsForAnalyzer = groupedActions.ToImmutableArrayOrEmpty();
                    if (!symbolEndActionsForAnalyzer.IsEmpty &&
                        !AnalyzerExecutor.TryExecuteSymbolEndActions(symbolEndActionsForAnalyzer, analyzer, symbolEvent, s_getTopmostNodeForAnalysis, analysisState))
                    {
                        success = false;
                        continue;
                    }

                    AnalyzerExecutor.MarkSymbolEndAnalysisComplete(symbol, analyzer, analysisState);
                    completedAnalyzers.Add(analyzer);
                }

                if (processedAnalyzers.Count < analysisScope.Analyzers.Length)
                {
                    foreach (var analyzer in analysisScope.Analyzers)
                    {
                        if (!processedAnalyzers.Contains(analyzer))
                        {
                            AnalyzerExecutor.MarkSymbolEndAnalysisComplete(symbol, analyzer, analysisState);
                            completedAnalyzers.Add(analyzer);
                        }
                    }
                }

                if (!success)
                {
                    Debug.Assert(completedAnalyzers.Count < analysisScope.Analyzers.Length);
                    subsetProcessedAnalyzers = completedAnalyzers.ToImmutable();
                    return false;
                }
                else
                {
                    subsetProcessedAnalyzers = ImmutableArray<DiagnosticAnalyzer>.Empty;
                    return true;
                }
            }
            finally
            {
                processedAnalyzers.Free();
                completedAnalyzers.Free();
            }
        }

        private static SyntaxNode GetTopmostNodeForAnalysis(ISymbol symbol, SyntaxReference syntaxReference, Compilation compilation, CancellationToken cancellationToken)
        {
            var model = compilation.GetSemanticModel(syntaxReference.SyntaxTree);
            return model.GetTopmostNodeForDiagnosticAnalysis(symbol, syntaxReference.GetSyntax(cancellationToken));
        }

        protected abstract bool TryExecuteDeclaringReferenceActions(
            SymbolDeclaredCompilationEvent symbolEvent,
            AnalysisScope analysisScope,
            AnalysisState? analysisState,
            bool isGeneratedCodeSymbol,
            IGroupedAnalyzerActions additionalPerSymbolActions,
            CancellationToken cancellationToken);

        /// <summary>
        /// Tries to execute compilation unit actions.
        /// </summary>
        /// <returns>
        /// True, if successfully executed the actions for the given analysis scope OR no actions were required to be executed for the given analysis scope.
        /// False, otherwise.
        /// </returns>
        private bool TryProcessCompilationUnitCompleted(CompilationUnitCompletedEvent completedEvent, AnalysisScope analysisScope, AnalysisState? analysisState, CancellationToken cancellationToken)
        {
            // When the compiler is finished with a compilation unit, we can run user diagnostics which
            // might want to ask the compiler for all the diagnostics in the source file, for example
            // to get information about unnecessary usings.

            var semanticModel = SemanticModelProvider.GetSemanticModel(completedEvent.CompilationUnit, completedEvent.Compilation);
            if (!analysisScope.ShouldAnalyze(semanticModel.SyntaxTree))
            {
                return true;
            }

            var isGeneratedCode = IsGeneratedCode(semanticModel.SyntaxTree);
            if (isGeneratedCode && DoNotAnalyzeGeneratedCode)
            {
                analysisState?.MarkEventComplete(completedEvent, analysisScope.Analyzers);
                return true;
            }

            var processedAnalyzers = analysisState != null ? PooledHashSet<DiagnosticAnalyzer>.GetInstance() : null;
            try
            {
                var success = true;
                foreach (var (analyzer, semanticModelActions) in _lazySemanticModelActions)
                {
                    if (!analysisScope.Contains(analyzer))
                    {
                        continue;
                    }

                    // Only compiler analyzer supports span-based semantic model action callbacks.
                    if (completedEvent.FilterSpan.HasValue && !IsCompilerAnalyzer(analyzer))
                    {
                        continue;
                    }

                    // Execute actions for a given analyzer sequentially.
                    if (!AnalyzerExecutor.TryExecuteSemanticModelActions(semanticModelActions, analyzer, semanticModel, completedEvent, analysisScope, analysisState, isGeneratedCode))
                    {
                        success = false;
                    }

                    processedAnalyzers?.Add(analyzer);
                }

                analysisState?.MarkEventCompleteForUnprocessedAnalyzers(completedEvent, analysisScope, processedAnalyzers!);
                return success;
            }
            finally
            {
                processedAnalyzers?.Free();
            }
        }

        /// <summary>
        /// Tries to execute compilation started actions.
        /// </summary>
        /// <returns>
        /// True, if successfully executed the actions for the given analysis scope OR no actions were required to be executed for the given analysis scope.
        /// False, otherwise.
        /// </returns>
        private bool TryProcessCompilationStarted(CompilationStartedEvent startedEvent, AnalysisScope analysisScope, AnalysisState? analysisState)
        {
            return TryExecuteCompilationActions(_lazyCompilationActions, startedEvent, analysisScope, analysisState);
        }

        /// <summary>
        /// Tries to execute compilation completed actions.
        /// </summary>
        /// <returns>
        /// True, if successfully executed the actions for the given analysis scope OR no actions were required to be executed for the given analysis scope.
        /// False, otherwise.
        /// </returns>
        private bool TryProcessCompilationCompleted(CompilationCompletedEvent endEvent, AnalysisScope analysisScope, AnalysisState? analysisState)
        {
            return TryExecuteCompilationActions(_lazyCompilationEndActions, endEvent, analysisScope, analysisState);
        }

        /// <summary>
        /// Tries to execute compilation actions.
        /// </summary>
        /// <returns>
        /// True, if successfully executed the actions for the given analysis scope OR no actions were required to be executed for the given analysis scope.
        /// False, otherwise.
        /// </returns>
        private bool TryExecuteCompilationActions(
            ImmutableArray<(DiagnosticAnalyzer, ImmutableArray<CompilationAnalyzerAction>)> compilationActionsMap,
            CompilationEvent compilationEvent,
            AnalysisScope analysisScope,
            AnalysisState? analysisState)
        {
            Debug.Assert(compilationEvent is CompilationStartedEvent || compilationEvent is CompilationCompletedEvent);

            var processedAnalyzers = analysisState != null ? PooledHashSet<DiagnosticAnalyzer>.GetInstance() : null;
            try
            {
                var success = true;
                foreach (var (analyzer, compilationActions) in compilationActionsMap)
                {
                    if (!analysisScope.Contains(analyzer))
                    {
                        continue;
                    }

                    if (!AnalyzerExecutor.TryExecuteCompilationActions(compilationActions, analyzer, compilationEvent, analysisScope, analysisState))
                    {
                        success = false;
                    }

                    processedAnalyzers?.Add(analyzer);
                }

                analysisState?.MarkEventCompleteForUnprocessedAnalyzers(compilationEvent, analysisScope, processedAnalyzers!);
                return success;
            }
            finally
            {
                processedAnalyzers?.Free();
            }
        }

        internal static Action<Diagnostic> GetDiagnosticSink(Action<Diagnostic> addDiagnosticCore, Compilation compilation, AnalyzerOptions? analyzerOptions, SeverityFilter severityFilter, CancellationToken cancellationToken)
        {
            return diagnostic =>
            {
                var filteredDiagnostic = GetFilteredDiagnostic(diagnostic, compilation, analyzerOptions, severityFilter, cancellationToken);
                if (filteredDiagnostic != null)
                {
                    addDiagnosticCore(filteredDiagnostic);
                }
            };
        }

        internal static Action<Diagnostic, DiagnosticAnalyzer, bool> GetDiagnosticSink(Action<Diagnostic, DiagnosticAnalyzer, bool> addLocalDiagnosticCore, Compilation compilation, AnalyzerOptions? analyzerOptions, SeverityFilter severityFilter, CancellationToken cancellationToken)
        {
            return (diagnostic, analyzer, isSyntaxDiagnostic) =>
            {
                var filteredDiagnostic = GetFilteredDiagnostic(diagnostic, compilation, analyzerOptions, severityFilter, cancellationToken);
                if (filteredDiagnostic != null)
                {
                    addLocalDiagnosticCore(filteredDiagnostic, analyzer, isSyntaxDiagnostic);
                }
            };
        }

        internal static Action<Diagnostic, DiagnosticAnalyzer> GetDiagnosticSink(Action<Diagnostic, DiagnosticAnalyzer> addDiagnosticCore, Compilation compilation, AnalyzerOptions? analyzerOptions, SeverityFilter severityFilter, CancellationToken cancellationToken)
        {
            return (diagnostic, analyzer) =>
            {
                var filteredDiagnostic = GetFilteredDiagnostic(diagnostic, compilation, analyzerOptions, severityFilter, cancellationToken);
                if (filteredDiagnostic != null)
                {
                    addDiagnosticCore(filteredDiagnostic, analyzer);
                }
            };
        }

        private static Diagnostic? GetFilteredDiagnostic(Diagnostic diagnostic, Compilation compilation, AnalyzerOptions? analyzerOptions, SeverityFilter severityFilter, CancellationToken cancellationToken)
        {
            var filteredDiagnostic = compilation.Options.FilterDiagnostic(diagnostic, cancellationToken);
            return applyFurtherFiltering(filteredDiagnostic);

            Diagnostic? applyFurtherFiltering(Diagnostic? diagnostic)
            {
                // Apply bulk configuration from analyzer options for analyzer diagnostics, if applicable.
                if (diagnostic?.Location.SourceTree is { } tree &&
                    analyzerOptions.TryGetSeverityFromBulkConfiguration(tree, compilation, diagnostic.Descriptor, cancellationToken, out ReportDiagnostic severity))
                {
                    diagnostic = diagnostic.WithReportDiagnostic(severity);
                }

                if (diagnostic != null &&
                    severityFilter.Contains(DiagnosticDescriptor.MapSeverityToReport(diagnostic.Severity)))
                {
                    return null;
                }

                return diagnostic;
            }
        }

        private static async Task<(AnalyzerActions actions, ImmutableHashSet<DiagnosticAnalyzer> unsuppressedAnalyzers)> GetAnalyzerActionsAsync(
            ImmutableArray<DiagnosticAnalyzer> analyzers,
            AnalyzerManager analyzerManager,
            AnalyzerExecutor analyzerExecutor,
            SeverityFilter severityFilter)
        {
            var allAnalyzerActions = AnalyzerActions.Empty;
            var unsuppressedAnalyzersBuilder = PooledHashSet<DiagnosticAnalyzer>.GetInstance();
            foreach (var analyzer in analyzers)
            {
                if (!IsDiagnosticAnalyzerSuppressed(analyzer, analyzerExecutor.Compilation.Options, analyzerManager, analyzerExecutor, severityFilter))
                {
                    unsuppressedAnalyzersBuilder.Add(analyzer);

                    var analyzerActions = await analyzerManager.GetAnalyzerActionsAsync(analyzer, analyzerExecutor).ConfigureAwait(false);
                    allAnalyzerActions = allAnalyzerActions.Append(in analyzerActions);
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

        [PerformanceSensitive(
            "https://developercommunity.visualstudio.com/content/problem/805524/ctrl-suggestions-are-very-slow-and-produce-gatheri.html",
            OftenCompletesSynchronously = true)]
        private async ValueTask<IGroupedAnalyzerActions> GetPerSymbolAnalyzerActionsAsync(
            ISymbol symbol,
            AnalysisScope analysisScope,
            AnalysisState? analysisState,
            CancellationToken cancellationToken)
        {
            if (AnalyzerActions.SymbolStartActionsCount == 0 || symbol.IsImplicitlyDeclared)
            {
                return EmptyGroupedActions;
            }

            var allActions = EmptyGroupedActions;
            foreach (var analyzer in analysisScope.Analyzers)
            {
                if (!SymbolStartAnalyzers.Contains(analyzer))
                {
                    continue;
                }

                var analyzerActions = await GetPerSymbolAnalyzerActionsAsync(symbol, analyzer, analysisState, cancellationToken).ConfigureAwait(false);
                if (!analyzerActions.IsEmpty)
                {
                    allActions = allActions.Append(analyzerActions);
                }
            }

            return allActions;
        }

        [PerformanceSensitive(
            "https://developercommunity.visualstudio.com/content/problem/805524/ctrl-suggestions-are-very-slow-and-produce-gatheri.html",
            OftenCompletesSynchronously = true)]
        private async ValueTask<IGroupedAnalyzerActions> GetPerSymbolAnalyzerActionsAsync(
            ISymbol symbol,
            DiagnosticAnalyzer analyzer,
            AnalysisState? analysisState,
            CancellationToken cancellationToken)
        {
            Debug.Assert(AnalyzerActions.SymbolStartActionsCount > 0);
            Debug.Assert(SymbolStartAnalyzers.Contains(analyzer));

            if (symbol.IsImplicitlyDeclared)
            {
                return EmptyGroupedActions;
            }

            // PERF: For containing symbols, we want to cache the computed actions.
            // For member symbols, we do not want to cache as we will not reach this path again.
            if (!(symbol is INamespaceOrTypeSymbol namespaceOrType))
            {
                return await getAllActionsAsync(this, symbol, analyzer, analysisState, cancellationToken).ConfigureAwait(false);
            }

            if (PerSymbolAnalyzerActionsCache.TryGetValue((namespaceOrType, analyzer), out var actions))
            {
                return actions;
            }

            var allActions = await getAllActionsAsync(this, symbol, analyzer, analysisState, cancellationToken).ConfigureAwait(false);
            return PerSymbolAnalyzerActionsCache.GetOrAdd((namespaceOrType, analyzer), allActions);

            async ValueTask<IGroupedAnalyzerActions> getAllActionsAsync(AnalyzerDriver driver, ISymbol symbol, DiagnosticAnalyzer analyzer, AnalysisState? analysisState, CancellationToken cancellationToken)
            {
                // Compute additional inherited actions for this symbol by running the containing symbol's start actions.
                var inheritedActions = await getInheritedActionsAsync(driver, symbol, analyzer, analysisState, cancellationToken).ConfigureAwait(false);

                // Execute the symbol start actions for this symbol to compute additional actions for its members.
                AnalyzerActions myActions = await getSymbolActionsCoreAsync(driver, symbol, analyzer).ConfigureAwait(false);
                if (myActions.IsEmpty)
                {
                    return inheritedActions;
                }

                var allActions = inheritedActions.AnalyzerActions.Append(in myActions);
                return CreateGroupedActions(analyzer, allActions);
            }

            async ValueTask<IGroupedAnalyzerActions> getInheritedActionsAsync(AnalyzerDriver driver, ISymbol symbol, DiagnosticAnalyzer analyzer, AnalysisState? analysisState, CancellationToken cancellationToken)
            {
                if (symbol.ContainingSymbol != null)
                {
                    // Get container symbol's per-symbol actions, which also forces its start actions to execute.
                    var containerActions = await driver.GetPerSymbolAnalyzerActionsAsync(symbol.ContainingSymbol, analyzer, analysisState, cancellationToken).ConfigureAwait(false);
                    if (!containerActions.IsEmpty)
                    {
                        // Don't inherit actions for nested type and namespace from its containing type and namespace respectively.
                        // However, note that we bail out **after** computing container's per-symbol actions above.
                        // This is done to ensure that we have executed symbol started actions for the container before our start actions are executed.
                        if (symbol.ContainingSymbol.Kind != symbol.Kind)
                        {
                            // Don't inherit the symbol start and symbol end actions.
                            var containerAnalyzerActions = containerActions.AnalyzerActions;
                            var actions = AnalyzerActions.Empty.Append(in containerAnalyzerActions, appendSymbolStartAndSymbolEndActions: false);
                            return CreateGroupedActions(analyzer, actions);
                        }
                    }
                }

                return EmptyGroupedActions;
            }

            static async ValueTask<AnalyzerActions> getSymbolActionsCoreAsync(AnalyzerDriver driver, ISymbol symbol, DiagnosticAnalyzer analyzer)
            {
                if (!driver.UnsuppressedAnalyzers.Contains(analyzer) ||
                    driver.IsGeneratedCodeSymbol(symbol) && driver.ShouldSkipAnalysisOnGeneratedCode(analyzer))
                {
                    return AnalyzerActions.Empty;
                }
                else
                {
                    return await driver.AnalyzerManager.GetPerSymbolAnalyzerActionsAsync(symbol, analyzer, driver.AnalyzerExecutor).ConfigureAwait(false);
                }
            }
        }

        private static async Task<ImmutableSegmentedDictionary<DiagnosticAnalyzer, SemaphoreSlim>> CreateAnalyzerGateMapAsync(
            ImmutableHashSet<DiagnosticAnalyzer> analyzers,
            AnalyzerManager analyzerManager,
            AnalyzerExecutor analyzerExecutor,
            SeverityFilter severityFilter)
        {
            var builder = ImmutableSegmentedDictionary.CreateBuilder<DiagnosticAnalyzer, SemaphoreSlim>();
            foreach (var analyzer in analyzers)
            {
                Debug.Assert(!IsDiagnosticAnalyzerSuppressed(analyzer, analyzerExecutor.Compilation.Options, analyzerManager, analyzerExecutor, severityFilter));

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

        private static async Task<ImmutableSegmentedDictionary<DiagnosticAnalyzer, GeneratedCodeAnalysisFlags>> CreateGeneratedCodeAnalysisFlagsMapAsync(
            ImmutableHashSet<DiagnosticAnalyzer> analyzers,
            AnalyzerManager analyzerManager,
            AnalyzerExecutor analyzerExecutor,
            SeverityFilter severityFilter)
        {
            var builder = ImmutableSegmentedDictionary.CreateBuilder<DiagnosticAnalyzer, GeneratedCodeAnalysisFlags>();
            foreach (var analyzer in analyzers)
            {
                Debug.Assert(!IsDiagnosticAnalyzerSuppressed(analyzer, analyzerExecutor.Compilation.Options, analyzerManager, analyzerExecutor, severityFilter));

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
            if (TreatAllCodeAsNonGeneratedCode)
            {
                return false;
            }

            return IsGeneratedCodeSymbolMap.TryGetValue(symbol, out bool isGeneratedCodeSymbol) ?
                isGeneratedCodeSymbol :
                IsGeneratedCodeSymbolMap.GetOrAdd(symbol, computeIsGeneratedCodeSymbol());

            bool computeIsGeneratedCodeSymbol()
            {
                if (_lazyGeneratedCodeAttribute != null && GeneratedCodeUtilities.IsGeneratedSymbolWithGeneratedCodeAttribute(symbol, _lazyGeneratedCodeAttribute))
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
            if (TreatAllCodeAsNonGeneratedCode)
            {
                return false;
            }

            if (!GeneratedCodeFilesMap.TryGetValue(tree, out var isGenerated))
            {
                isGenerated = computeIsGeneratedCode();
                GeneratedCodeFilesMap.TryAdd(tree, isGenerated);
            }

            return isGenerated;

            bool computeIsGeneratedCode()
            {
                // Check for explicit user configuration for generated code from options.
                //     generated_code = true | false
                // If there is no explicit user configuration, fallback to our generated code heuristic.
                var options = AnalyzerExecutor.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(tree);
                return GeneratedCodeUtilities.GetIsGeneratedCodeFromOptions(options) ??
                    _isGeneratedCode(tree, AnalyzerExecutor.CancellationToken);
            }
        }

        protected bool DoNotAnalyzeGeneratedCode
        {
            get
            {
                Debug.Assert(_lazyDoNotAnalyzeGeneratedCode.HasValue);
                return _lazyDoNotAnalyzeGeneratedCode.Value;
            }
        }

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
            if (IsDiagnosticAnalyzerSuppressed(analyzer, compilationOptions, AnalyzerManager, executor, _severityFilter))
            {
                return AnalyzerActionCounts.Empty;
            }

            var analyzerActions = await AnalyzerManager.GetAnalyzerActionsAsync(analyzer, executor).ConfigureAwait(false);
            if (analyzerActions.IsEmpty)
            {
                return AnalyzerActionCounts.Empty;
            }

            return new AnalyzerActionCounts(in analyzerActions);
        }

        /// <summary>
        /// Returns true if all the diagnostics that can be produced by this analyzer are suppressed through options.
        /// </summary>
        internal static bool IsDiagnosticAnalyzerSuppressed(
            DiagnosticAnalyzer analyzer,
            CompilationOptions options,
            AnalyzerManager analyzerManager,
            AnalyzerExecutor analyzerExecutor,
            SeverityFilter severityFilter)
        {
            return analyzerManager.IsDiagnosticAnalyzerSuppressed(analyzer, options, s_IsCompilerAnalyzerFunc, analyzerExecutor, severityFilter);
        }

        private static bool IsCompilerAnalyzer(DiagnosticAnalyzer analyzer) => analyzer is CompilerDiagnosticAnalyzer;

        public void Dispose()
        {
            _lazyCompilationEventQueue?.TryComplete();
            _lazyDiagnosticQueue?.TryComplete();
            _lazyQueueRegistration?.Dispose();
        }
    }

    /// <summary>
    /// Driver to execute diagnostic analyzers for a given compilation.
    /// It uses a <see cref="AsyncQueue{TElement}"/> of <see cref="CompilationEvent"/>s to drive its analysis.
    /// </summary>
    internal partial class AnalyzerDriver<TLanguageKindEnum> : AnalyzerDriver where TLanguageKindEnum : struct
    {
        private readonly Func<SyntaxNode, TLanguageKindEnum> _getKind;
        private GroupedAnalyzerActions? _lazyCoreActions;

        /// <summary>
        /// Create an analyzer driver.
        /// </summary>
        /// <param name="analyzers">The set of analyzers to include in the analysis</param>
        /// <param name="getKind">A delegate that returns the language-specific kind for a given syntax node</param>
        /// <param name="analyzerManager">AnalyzerManager to manage analyzers for the lifetime of analyzer host.</param>
        /// <param name="severityFilter">Filtered diagnostic severities in the compilation, i.e. diagnostics with effective severity from this set should not be reported.</param>
        /// <param name="isComment">Delegate to identify if the given trivia is a comment.</param>
        internal AnalyzerDriver(ImmutableArray<DiagnosticAnalyzer> analyzers, Func<SyntaxNode, TLanguageKindEnum> getKind, AnalyzerManager analyzerManager, SeverityFilter severityFilter, Func<SyntaxTrivia, bool> isComment)
            : base(analyzers, analyzerManager, severityFilter, isComment)
        {
            _getKind = getKind;
        }

        private GroupedAnalyzerActions GetOrCreateCoreActions()
        {
            if (_lazyCoreActions == null)
            {
                Interlocked.CompareExchange(ref _lazyCoreActions, createCoreActions(), null);
            }

            return _lazyCoreActions;

            GroupedAnalyzerActions createCoreActions()
            {
                if (AnalyzerActions.IsEmpty)
                {
                    return GroupedAnalyzerActions.Empty;
                }

                // Keep analyzers in original order.
                var analyzers = Analyzers.WhereAsArray(UnsuppressedAnalyzers.Contains);
                return GroupedAnalyzerActions.Create(analyzers, AnalyzerActions);
            }
        }

        private static void ComputeShouldExecuteActions(
            in AnalyzerActions coreActions,
            in AnalyzerActions additionalActions,
            ISymbol symbol,
            out bool executeSyntaxNodeActions,
            out bool executeCodeBlockActions,
            out bool executeOperationActions,
            out bool executeOperationBlockActions)
        {
            executeSyntaxNodeActions = false;
            executeCodeBlockActions = false;
            executeOperationActions = false;
            executeOperationBlockActions = false;

            var canHaveExecutableCodeBlock = AnalyzerExecutor.CanHaveExecutableCodeBlock(symbol);
            computeShouldExecuteActions(coreActions, canHaveExecutableCodeBlock, ref executeSyntaxNodeActions, ref executeCodeBlockActions, ref executeOperationActions, ref executeOperationBlockActions);
            computeShouldExecuteActions(additionalActions, canHaveExecutableCodeBlock, ref executeSyntaxNodeActions, ref executeCodeBlockActions, ref executeOperationActions, ref executeOperationBlockActions);
            return;

            static void computeShouldExecuteActions(
                AnalyzerActions analyzerActions,
                bool canHaveExecutableCodeBlock,
                ref bool executeSyntaxNodeActions,
                ref bool executeCodeBlockActions,
                ref bool executeOperationActions,
                ref bool executeOperationBlockActions)
            {
                if (analyzerActions.IsEmpty)
                {
                    return;
                }

                executeSyntaxNodeActions |= analyzerActions.SyntaxNodeActionsCount > 0;
                executeOperationActions |= analyzerActions.OperationActionsCount > 0;

                if (canHaveExecutableCodeBlock)
                {
                    executeCodeBlockActions |= analyzerActions.CodeBlockStartActionsCount > 0 || analyzerActions.CodeBlockActionsCount > 0;
                    executeOperationBlockActions |= analyzerActions.OperationBlockStartActionsCount > 0 || analyzerActions.OperationBlockActionsCount > 0;
                }
            }
        }

        protected override IGroupedAnalyzerActions EmptyGroupedActions => GroupedAnalyzerActions.Empty;
        protected override IGroupedAnalyzerActions CreateGroupedActions(DiagnosticAnalyzer analyzer, in AnalyzerActions analyzerActions)
            => GroupedAnalyzerActions.Create(analyzer, analyzerActions);

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
            AnalysisState? analysisState,
            bool isGeneratedCodeSymbol,
            IGroupedAnalyzerActions additionalPerSymbolActions,
            CancellationToken cancellationToken)
        {
            var symbol = symbolEvent.Symbol;

            ComputeShouldExecuteActions(
                AnalyzerActions, additionalPerSymbolActions.AnalyzerActions, symbol,
                executeSyntaxNodeActions: out var executeSyntaxNodeActions,
                executeCodeBlockActions: out var executeCodeBlockActions,
                executeOperationActions: out var executeOperationActions,
                executeOperationBlockActions: out var executeOperationBlockActions);

            var success = true;
            if (executeSyntaxNodeActions || executeOperationActions || executeCodeBlockActions || executeOperationBlockActions)
            {
                var declaringReferences = symbolEvent.DeclaringSyntaxReferences;
                var coreActions = GetOrCreateCoreActions();
                for (var i = 0; i < declaringReferences.Length; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var decl = declaringReferences[i];
                    if (analysisScope.FilterFileOpt != null && analysisScope.FilterFileOpt?.SourceTree != decl.SyntaxTree)
                    {
                        continue;
                    }

                    var isInGeneratedCode = isGeneratedCodeSymbol || IsGeneratedOrHiddenCodeLocation(decl.SyntaxTree, decl.Span);
                    if (isInGeneratedCode && DoNotAnalyzeGeneratedCode)
                    {
                        analysisState?.MarkDeclarationComplete(symbol, i, analysisScope.Analyzers);
                        continue;
                    }

                    if (!TryExecuteDeclaringReferenceActions(decl, i, symbolEvent, analysisScope, analysisState, coreActions, (GroupedAnalyzerActions)additionalPerSymbolActions,
                        executeSyntaxNodeActions, executeOperationActions, executeCodeBlockActions, executeOperationBlockActions, isInGeneratedCode, cancellationToken))
                    {
                        success = false;
                    }
                }
            }
            else if (analysisState != null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                analysisState.MarkDeclarationsComplete(symbol, analysisScope.Analyzers);

                var declaringReferences = symbolEvent.DeclaringSyntaxReferences;
                for (var i = 0; i < declaringReferences.Length; i++)
                {
                    var decl = declaringReferences[i];
                    ClearCachedAnalysisDataIfAnalyzed(decl, symbol, i, analysisState);
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
            semanticModel.ComputeDeclarationsInNode(topmostNodeForAnalysis, declaredSymbol, getSymbol, builder, cancellationToken, levelsToCompute);
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
            AnalysisState? analysisState,
            GroupedAnalyzerActions coreActions,
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

            var semanticModel = symbolEvent.SemanticModelWithCachedBoundNodes ??
                SemanticModelProvider.GetSemanticModel(decl.SyntaxTree, symbolEvent.Compilation);

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
            if (success && analysisState != null && !declarationAnalysisData.IsPartialAnalysis)
            {
                // Ensure that we do not mark declaration complete/clear state if cancellation was requested.
                // Other thread(s) might still be executing analysis, and clearing state could lead to corrupt execution
                // or unknown exceptions.
                cancellationToken.ThrowIfCancellationRequested();

                foreach (var analyzer in analysisScope.Analyzers)
                {
                    analysisState.MarkDeclarationComplete(symbol, declarationIndex, analyzer);
                }

                if (cacheAnalysisData)
                {
                    ClearCachedAnalysisDataIfAnalyzed(decl, symbol, declarationIndex, analysisState);
                }
            }

            return success;

            void executeNodeActions()
            {
                if (shouldExecuteSyntaxNodeActions)
                {
                    var nodesToAnalyze = declarationAnalysisData.DescendantNodesToAnalyze;
                    executeNodeActionsByKind(analysisScope, nodesToAnalyze, coreActions);
                    executeNodeActionsByKind(analysisScope, nodesToAnalyze, additionalPerSymbolActions);
                }
            }

            void executeNodeActionsByKind(AnalysisScope analysisScope, ImmutableArray<SyntaxNode> nodesToAnalyze, GroupedAnalyzerActions groupedActions)
            {
                foreach (var (analyzer, groupedActionsForAnalyzer) in groupedActions.GroupedActionsByAnalyzer)
                {
                    var nodeActionsByKind = groupedActionsForAnalyzer.NodeActionsByAnalyzerAndKind;
                    if (nodeActionsByKind.IsEmpty || !analysisScope.Contains(analyzer))
                    {
                        continue;
                    }

                    if (!AnalyzerExecutor.TryExecuteSyntaxNodeActions(nodesToAnalyze, nodeActionsByKind,
                            analyzer, semanticModel, _getKind, declarationAnalysisData.TopmostNodeForAnalysis.FullSpan,
                            declarationIndex, symbol, analysisScope, analysisState, isInGeneratedCode))
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
                var executableCodeBlockActionsBuilder = ArrayBuilder<ExecutableCodeBlockAnalyzerActions>.GetInstance();
                try
                {
                    foreach (var declInNode in declarationAnalysisData.DeclarationsInNode)
                    {
                        if (declInNode.DeclaredNode == declarationAnalysisData.TopmostNodeForAnalysis || declInNode.DeclaredNode == declarationAnalysisData.DeclaringReferenceSyntax)
                        {
                            executableCodeBlocks = declInNode.ExecutableCodeBlocks;
                            if (!executableCodeBlocks.IsEmpty)
                            {
                                if (shouldExecuteCodeBlockActions || shouldExecuteOperationBlockActions)
                                {
                                    addExecutableCodeBlockAnalyzerActions(coreActions, analysisScope, executableCodeBlockActionsBuilder);
                                    addExecutableCodeBlockAnalyzerActions(additionalPerSymbolActions, analysisScope, executableCodeBlockActionsBuilder);
                                }

                                // Execute operation actions.
                                if (shouldExecuteOperationActions || shouldExecuteOperationBlockActions)
                                {
                                    var operationBlocksToAnalyze = GetOperationBlocksToAnalyze(executableCodeBlocks, semanticModel, cancellationToken);
                                    var operationsToAnalyze = getOperationsToAnalyzeWithStackGuard(operationBlocksToAnalyze);

                                    if (!operationsToAnalyze.IsEmpty)
                                    {
                                        try
                                        {
                                            executeOperationsActions(operationsToAnalyze);
                                            executeOperationsBlockActions(operationBlocksToAnalyze, operationsToAnalyze, executableCodeBlockActionsBuilder);
                                        }
                                        finally
                                        {
                                            AnalyzerExecutor.OnOperationBlockActionsExecuted(operationBlocksToAnalyze);
                                        }
                                    }
                                }

                                break;
                            }
                        }
                    }

                    executeCodeBlockActions(executableCodeBlocks, executableCodeBlockActionsBuilder);
                }
                finally
                {
                    executableCodeBlockActionsBuilder.Free();
                }
            }

            ImmutableArray<IOperation> getOperationsToAnalyzeWithStackGuard(ImmutableArray<IOperation> operationBlocksToAnalyze)
            {
                try
                {
                    return GetOperationsToAnalyze(operationBlocksToAnalyze);
                }
                catch (Exception ex) when (ex is InsufficientExecutionStackException)
                {
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
                    executeOperationsActionsByKind(analysisScope, operationsToAnalyze, coreActions);
                    executeOperationsActionsByKind(analysisScope, operationsToAnalyze, additionalPerSymbolActions);
                }
            }

            void executeOperationsActionsByKind(AnalysisScope analysisScope, ImmutableArray<IOperation> operationsToAnalyze, GroupedAnalyzerActions groupedActions)
            {
                foreach (var (analyzer, groupedActionsForAnalyzer) in groupedActions.GroupedActionsByAnalyzer)
                {
                    var operationActionsByKind = groupedActionsForAnalyzer.OperationActionsByAnalyzerAndKind;
                    if (operationActionsByKind.IsEmpty || !analysisScope.Contains(analyzer))
                    {
                        continue;
                    }

                    if (!AnalyzerExecutor.TryExecuteOperationActions(operationsToAnalyze, operationActionsByKind,
                            analyzer, semanticModel, declarationAnalysisData.TopmostNodeForAnalysis.FullSpan,
                            declarationIndex, symbol, analysisScope, analysisState, isInGeneratedCode))
                    {
                        success = false;
                    }
                }
            }

            void executeOperationsBlockActions(ImmutableArray<IOperation> operationBlocksToAnalyze, ImmutableArray<IOperation> operationsToAnalyze, IEnumerable<ExecutableCodeBlockAnalyzerActions> codeBlockActions)
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

                    if (!analysisScope.Contains(analyzerActions.Analyzer))
                    {
                        continue;
                    }

                    if (!AnalyzerExecutor.TryExecuteOperationBlockActions(
                        analyzerActions.OperationBlockStartActions, analyzerActions.OperationBlockActions,
                        analyzerActions.OperationBlockEndActions, analyzerActions.Analyzer, declarationAnalysisData.TopmostNodeForAnalysis, symbol,
                        operationBlocksToAnalyze, operationsToAnalyze, semanticModel, declarationIndex, analysisScope, analysisState, isInGeneratedCode))
                    {
                        success = false;
                    }
                }
            }

            void executeCodeBlockActions(ImmutableArray<SyntaxNode> executableCodeBlocks, IEnumerable<ExecutableCodeBlockAnalyzerActions> codeBlockActions)
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

                    if (!analysisScope.Contains(analyzerActions.Analyzer))
                    {
                        continue;
                    }

                    if (!AnalyzerExecutor.TryExecuteCodeBlockActions(
                        analyzerActions.CodeBlockStartActions, analyzerActions.CodeBlockActions,
                        analyzerActions.CodeBlockEndActions, analyzerActions.Analyzer, declarationAnalysisData.TopmostNodeForAnalysis, symbol,
                        executableCodeBlocks, semanticModel, _getKind, declarationIndex, analysisScope, analysisState, isInGeneratedCode))
                    {
                        success = false;
                    }
                }
            }

            static void addExecutableCodeBlockAnalyzerActions(
                GroupedAnalyzerActions groupedActions,
                AnalysisScope analysisScope,
                ArrayBuilder<ExecutableCodeBlockAnalyzerActions> builder)
            {
                foreach (var (analyzer, groupedActionsForAnalyzer) in groupedActions.GroupedActionsByAnalyzer)
                {
                    if (analysisScope.Contains(analyzer) &&
                        groupedActionsForAnalyzer.TryGetExecutableCodeBlockActions(out var executableCodeBlockActions))
                    {
                        builder.Add(executableCodeBlockActions);
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
            HashSet<SyntaxNode>? descendantDeclsToSkip = null;
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

                    descendantDeclsToSkip ??= new HashSet<SyntaxNode>();
                    descendantDeclsToSkip.Add(declarationNodeToSkip);
                }

                first = false;
            }

            Func<SyntaxNode, bool>? additionalFilter = semanticModel.GetSyntaxNodesToAnalyzeFilter(declaredNode, declaredSymbol);
            bool shouldAddNode(SyntaxNode node) => (descendantDeclsToSkip == null || !descendantDeclsToSkip.Contains(node)) && (additionalFilter is null || additionalFilter(node));
            var nodeBuilder = ArrayBuilder<SyntaxNode>.GetInstance();
            foreach (var node in declaredNode.DescendantNodesAndSelf(descendIntoChildren: shouldAddNode, descendIntoTrivia: true))
            {
                if (shouldAddNode(node) &&
                    !semanticModel.ShouldSkipSyntaxNodeAnalysis(node, declaredSymbol) &&
                    (!isPartialDeclAnalysis || analysisScope.ShouldAnalyze(node)))
                {
                    nodeBuilder.Add(node);
                }
            }

            return nodeBuilder.ToImmutableAndFree();
        }

        private static bool IsEquivalentSymbol(ISymbol declaredSymbol, ISymbol? otherSymbol)
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
                if (semanticModel.GetOperation(executableBlock, cancellationToken) is { } operation)
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
                                Debug.Assert(!operationBlock.Parent.IsImplicit);
                                operationsToAnalyze.Add(operationBlock.Parent);
                                break;

                            case OperationKind.ExpressionStatement:
                                // For constructor initializer, we generate an IInvocationOperation with an implicit IExpressionStatementOperation parent.
                                Debug.Assert(operationBlock.Kind == OperationKind.Invocation);
                                Debug.Assert(operationBlock.Parent.IsImplicit);
                                Debug.Assert(operationBlock.Parent.Parent is IConstructorBodyOperation ctorBody &&
                                    ctorBody.Initializer == operationBlock.Parent);
                                Debug.Assert(!operationBlock.Parent.Parent.IsImplicit);

                                operationsToAnalyze.Add(operationBlock.Parent.Parent);

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
