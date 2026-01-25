// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.CodeDom.Compiler;
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

        /// <summary>
        /// Separate pool for diagnostic analyzers as these collections commonly exceed ArrayBuilder's size threshold
        /// </summary>
        private static readonly ObjectPool<ArrayBuilder<DiagnosticAnalyzer>> s_diagnosticAnalyzerPool = new ObjectPool<ArrayBuilder<DiagnosticAnalyzer>>(() => new ArrayBuilder<DiagnosticAnalyzer>());

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
        internal readonly bool HasDiagnosticSuppressors;

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

        private ImmutableHashSet<DiagnosticAnalyzer>? _lazyNonConfigurableAndCustomConfigurableAnalyzers;

        /// <summary>
        /// Set of unsuppressed analyzers that report non-configurable or custom configurable diagnostics that cannot be suppressed with end user configuration.
        /// </summary>
        private ImmutableHashSet<DiagnosticAnalyzer> NonConfigurableAndCustomConfigurableAnalyzers
        {
            get
            {
                Debug.Assert(_lazyNonConfigurableAndCustomConfigurableAnalyzers != null);
                return _lazyNonConfigurableAndCustomConfigurableAnalyzers;
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

        private ConcurrentSet<string>? _lazySuppressedDiagnosticIdsForUnsuppressedAnalyzers;

        /// <summary>
        /// Lazily populated set of diagnostic IDs which are suppressed for some part of the compilation (tree/folder/entire compilation),
        /// but the analyzer reporting the diagnostic is itself not suppressed for the entire compilation, i.e. the analyzer
        /// belongs to <see cref="UnsuppressedAnalyzers"/>.
        /// </summary>
        private ConcurrentSet<string> SuppressedDiagnosticIdsForUnsuppressedAnalyzers
        {
            get
            {
                Debug.Assert(_lazySuppressedDiagnosticIdsForUnsuppressedAnalyzers != null);
                return _lazySuppressedDiagnosticIdsForUnsuppressedAnalyzers;
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
            HasDiagnosticSuppressors = this.Analyzers.Any(static a => a is DiagnosticSuppressor);
            _programmaticSuppressions = HasDiagnosticSuppressors ? new ConcurrentSet<Suppression>() : null;
            _diagnosticsProcessedForProgrammaticSuppressions = HasDiagnosticSuppressors ? new ConcurrentSet<Diagnostic>(ReferenceEqualityComparer.Instance) : null;
            _lazyAnalyzerGateMap = ImmutableSegmentedDictionary<DiagnosticAnalyzer, SemaphoreSlim>.Empty;
        }

        /// <summary>
        /// Initializes the <see cref="AnalyzerActions"/> and related actions maps for the analyzer driver.
        /// It kicks off the <see cref="WhenInitializedTask"/> task for initialization.
        /// Note: This method must be invoked exactly once on the driver.
        /// </summary>
        private void Initialize(
            AnalyzerExecutor analyzerExecutor,
            DiagnosticQueue diagnosticQueue,
            CompilationData compilationData,
            AnalysisScope analysisScope,
            ConcurrentSet<string>? suppressedDiagnosticIds,
            CancellationToken cancellationToken)
        {
            try
            {
                Debug.Assert(_lazyInitializeTask == null);

                _lazyAnalyzerExecutor = analyzerExecutor;
                _lazyCurrentCompilationData = compilationData;
                _lazyDiagnosticQueue = diagnosticQueue;
                _lazySuppressedDiagnosticIdsForUnsuppressedAnalyzers = suppressedDiagnosticIds;

                // Compute the set of effective actions based on suppression, and running the initial analyzers
                _lazyInitializeTask = Task.Run(async () =>
                {
                    (_lazyAnalyzerActions, _lazyUnsuppressedAnalyzers) = await GetAnalyzerActionsAsync(Analyzers, AnalyzerManager, analyzerExecutor, analysisScope, _severityFilter, cancellationToken).ConfigureAwait(false);
                    _lazyAnalyzerGateMap = await CreateAnalyzerGateMapAsync(UnsuppressedAnalyzers, AnalyzerManager, analyzerExecutor, analysisScope, _severityFilter, cancellationToken).ConfigureAwait(false);
                    _lazyNonConfigurableAndCustomConfigurableAnalyzers = ComputeNonConfigurableAndCustomConfigurableAnalyzers(UnsuppressedAnalyzers, cancellationToken);
                    _lazySymbolStartAnalyzers = ComputeSymbolStartAnalyzers(UnsuppressedAnalyzers);
                    _lazyGeneratedCodeAnalysisFlagsMap = await CreateGeneratedCodeAnalysisFlagsMapAsync(UnsuppressedAnalyzers, AnalyzerManager, analyzerExecutor, analysisScope, _severityFilter, cancellationToken).ConfigureAwait(false);
                    _lazyTreatAllCodeAsNonGeneratedCode = ComputeShouldTreatAllCodeAsNonGeneratedCode(UnsuppressedAnalyzers, GeneratedCodeAnalysisFlagsMap);
                    _lazyDoNotAnalyzeGeneratedCode = ComputeShouldSkipAnalysisOnGeneratedCode(UnsuppressedAnalyzers, GeneratedCodeAnalysisFlagsMap, TreatAllCodeAsNonGeneratedCode);
                    _lazyGeneratedCodeFilesMap = new ConcurrentDictionary<SyntaxTree, bool>();
                    _lazyGeneratedCodeSymbolsForTreeMap = new Dictionary<SyntaxTree, ImmutableHashSet<ISymbol>>();
                    _lazyIsGeneratedCodeSymbolMap = new ConcurrentDictionary<ISymbol, bool>();
                    _lazyTreesWithHiddenRegionsMap = new ConcurrentDictionary<SyntaxTree, bool>();
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
           AnalysisScope analysisScope,
           bool categorizeDiagnostics,
           bool trackSuppressedDiagnosticIds,
           CancellationToken cancellationToken)
        {
            Debug.Assert(_lazyInitializeTask == null);
            Debug.Assert(compilation.SemanticModelProvider != null);

            var diagnosticQueue = DiagnosticQueue.Create(categorizeDiagnostics);
            var suppressedDiagnosticIds = trackSuppressedDiagnosticIds ? new ConcurrentSet<string>() : null;

            Action<Diagnostic, AnalyzerOptions, CancellationToken>? addNotCategorizedDiagnostic = null;
            Action<Diagnostic, DiagnosticAnalyzer, AnalyzerOptions, bool, CancellationToken>? addCategorizedLocalDiagnostic = null;
            Action<Diagnostic, DiagnosticAnalyzer, AnalyzerOptions, CancellationToken>? addCategorizedNonLocalDiagnostic = null;
            if (categorizeDiagnostics)
            {
                addCategorizedLocalDiagnostic = GetDiagnosticSink(diagnosticQueue.EnqueueLocal, compilation, _severityFilter, suppressedDiagnosticIds);
                addCategorizedNonLocalDiagnostic = GetDiagnosticSink(diagnosticQueue.EnqueueNonLocal, compilation, _severityFilter, suppressedDiagnosticIds);
            }
            else
            {
                addNotCategorizedDiagnostic = GetDiagnosticSink(diagnosticQueue.Enqueue, compilation, _severityFilter, suppressedDiagnosticIds);
            }

            // Wrap onAnalyzerException to pass in filtered diagnostic.
            var options = analysisOptions.Options ?? AnalyzerOptions.Empty;

            var newOnAnalyzerException = (Exception ex, DiagnosticAnalyzer analyzer, Diagnostic diagnostic, CancellationToken cancellationToken) =>
            {
                // Note: in this callback, it's fine/correct to use analysisOptions.Options instead of any diagnostic analyzer
                // specific options. That's because the options are only used for filtering/determining-severities.  But we 
                // do not allow analyzers to control the filtering/severity around the reporting of analyzer exceptions themselves.
                // These are always passed through and reported to the user.
                var filteredDiagnostic = GetFilteredDiagnostic(diagnostic, compilation, options, _severityFilter, suppressedDiagnosticIds, cancellationToken);
                if (filteredDiagnostic != null)
                {
                    if (analysisOptions.OnAnalyzerException != null)
                    {
                        analysisOptions.OnAnalyzerException(ex, analyzer, filteredDiagnostic);
                    }
                    else if (categorizeDiagnostics)
                    {
                        addCategorizedNonLocalDiagnostic!(filteredDiagnostic, analyzer, options, cancellationToken);
                    }
                    else
                    {
                        addNotCategorizedDiagnostic!(filteredDiagnostic, options, cancellationToken);
                    }
                }
            };

            var analyzerExecutor = AnalyzerExecutor.Create(
                compilation, options, addNotCategorizedDiagnostic, newOnAnalyzerException, analysisOptions.AnalyzerExceptionFilter,
                IsCompilerAnalyzer, this.Analyzers, analysisOptions.GetAnalyzerConfigOptionsProvider,
                AnalyzerManager, ShouldSkipAnalysisOnGeneratedCode, ShouldSuppressGeneratedCodeDiagnostic, IsGeneratedOrHiddenCodeLocation, IsAnalyzerSuppressedForTree, GetAnalyzerGate,
                getSemanticModel: GetOrCreateSemanticModel, _severityFilter,
                analysisOptions.LogAnalyzerExecutionTime, addCategorizedLocalDiagnostic, addCategorizedNonLocalDiagnostic, s => _programmaticSuppressions!.Add(s));

            Initialize(analyzerExecutor, diagnosticQueue, compilationData, analysisScope, suppressedDiagnosticIds, cancellationToken);
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

        private ImmutableHashSet<DiagnosticAnalyzer> ComputeNonConfigurableAndCustomConfigurableAnalyzers(ImmutableHashSet<DiagnosticAnalyzer> unsuppressedAnalyzers, CancellationToken cancellationToken)
        {
            var builder = ImmutableHashSet.CreateBuilder<DiagnosticAnalyzer>();
            foreach (var analyzer in unsuppressedAnalyzers)
            {
                var descriptors = AnalyzerManager.GetSupportedDiagnosticDescriptors(analyzer, AnalyzerExecutor, cancellationToken);
                foreach (var descriptor in descriptors)
                {
                    if (descriptor.IsNotConfigurable() || descriptor.IsCustomSeverityConfigurable())
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
        /// <param name="cancellationToken">Cancellation token to abort analysis.</param>
        /// <remarks>Driver must be initialized before invoking this method, i.e. <see cref="Initialize(AnalyzerExecutor, DiagnosticQueue, CompilationData, AnalysisScope, ConcurrentSet{string}, CancellationToken)"/> method must have been invoked and <see cref="WhenInitializedTask"/> must be non-null.</remarks>
        internal async Task AttachQueueAndProcessAllEventsAsync(AsyncQueue<CompilationEvent> eventQueue, AnalysisScope analysisScope, CancellationToken cancellationToken)
        {
            try
            {
                if (_initializeSucceeded)
                {
                    _lazyCompilationEventQueue = eventQueue;
                    _lazyQueueRegistration = default(CancellationTokenRegistration);

                    await ExecutePrimaryAnalysisTaskAsync(analysisScope, usingPrePopulatedEventQueue: true, cancellationToken).ConfigureAwait(false);

                    _lazyPrimaryTask = Task.FromResult(true);
                }
            }
            finally
            {
                // Set primaryTask to be a cancelled task.
                _lazyPrimaryTask ??= Task.FromCanceled(new CancellationToken(canceled: true));
            }
        }

        /// <summary>
        /// Attaches event queue to the driver and start processing all events pertaining to the given analysis scope.
        /// </summary>
        /// <param name="eventQueue">Compilation events to analyze.</param>
        /// <param name="analysisScope">Scope of analysis.</param>
        /// <param name="usingPrePopulatedEventQueue">Boolean flag indicating whether we should only process the already populated events or wait for <see cref="CompilationCompletedEvent"/>.</param>
        /// <param name="cancellationToken">Cancellation token to abort analysis.</param>
        /// <remarks>Driver must be initialized before invoking this method, i.e. <see cref="Initialize(AnalyzerExecutor, DiagnosticQueue, CompilationData, AnalysisScope, ConcurrentSet{string}, CancellationToken)"/> method must have been invoked and <see cref="WhenInitializedTask"/> must be non-null.</remarks>
        internal void AttachQueueAndStartProcessingEvents(AsyncQueue<CompilationEvent> eventQueue, AnalysisScope analysisScope, bool usingPrePopulatedEventQueue, CancellationToken cancellationToken)
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

                    _lazyPrimaryTask = ExecutePrimaryAnalysisTaskAsync(analysisScope, usingPrePopulatedEventQueue, cancellationToken)
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

        private async Task ExecutePrimaryAnalysisTaskAsync(AnalysisScope analysisScope, bool usingPrePopulatedEventQueue, CancellationToken cancellationToken)
        {
            Debug.Assert(analysisScope != null);

            await WhenInitializedTask.ConfigureAwait(false);

            if (WhenInitializedTask.IsFaulted)
            {
                OnDriverException(WhenInitializedTask, this.AnalyzerExecutor, analysisScope.Analyzers, cancellationToken);
            }
            else if (!WhenInitializedTask.IsCanceled)
            {
                await ProcessCompilationEventsAsync(analysisScope, usingPrePopulatedEventQueue, cancellationToken).ConfigureAwait(false);

                // If not using pre-populated event queue (batch mode), then verify all symbol end actions were processed.
                if (!usingPrePopulatedEventQueue)
                {
                    AnalyzerManager.VerifyAllSymbolEndActionsExecuted();
                }
            }
        }

        private static void OnDriverException(Task faultedTask, AnalyzerExecutor analyzerExecutor, ImmutableArray<DiagnosticAnalyzer> analyzers, CancellationToken cancellationToken)
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

            analyzerExecutor.OnAnalyzerException(innerException, analyzer, diagnostic, cancellationToken);
        }

        private void ExecuteSyntaxTreeActions(AnalysisScope analysisScope, CancellationToken cancellationToken)
        {
            if (analysisScope.IsSingleFileAnalysis && !analysisScope.IsSyntacticSingleFileAnalysis)
            {
                // For partial analysis, only execute syntax tree actions if performing syntax analysis.
                return;
            }

            foreach (var tree in analysisScope.SyntaxTrees)
            {
                var isGeneratedCode = IsGeneratedCode(tree, cancellationToken);
                var file = new SourceOrAdditionalFile(tree);
                if (isGeneratedCode && DoNotAnalyzeGeneratedCode)
                {
                    continue;
                }

                foreach (var (analyzer, syntaxTreeActions) in _lazySyntaxTreeActions)
                {
                    if (!analysisScope.Contains(analyzer))
                    {
                        continue;
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    // Execute actions for a given analyzer sequentially.
                    AnalyzerExecutor.ExecuteSyntaxTreeActions(syntaxTreeActions, analyzer, file, analysisScope.FilterSpanOpt, isGeneratedCode, cancellationToken);
                }
            }
        }

        private void ExecuteAdditionalFileActions(AnalysisScope analysisScope, CancellationToken cancellationToken)
        {
            if (analysisScope.IsSingleFileAnalysis && !analysisScope.IsSyntacticSingleFileAnalysis)
            {
                // For partial analysis, only execute additional file actions if performing syntactic single file analysis.
                return;
            }

            foreach (var additionalFile in analysisScope.AdditionalFiles)
            {
                var file = new SourceOrAdditionalFile(additionalFile);

                foreach (var (analyzer, additionalFileActions) in _lazyAdditionalFileActions)
                {
                    if (!analysisScope.Contains(analyzer))
                    {
                        continue;
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    // Execute actions for a given analyzer sequentially.
                    AnalyzerExecutor.ExecuteAdditionalFileActions(additionalFileActions, analyzer, file, analysisScope.FilterSpanOpt, cancellationToken);
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
        /// <param name="trackSuppressedDiagnosticIds">Track diagnostic ids which are suppressed through options.</param>
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
            bool trackSuppressedDiagnosticIds,
            out Compilation newCompilation,
            CancellationToken cancellationToken)
        {
            Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException =
                (ex, analyzer, diagnostic) => addExceptionDiagnostic?.Invoke(diagnostic);

            Func<Exception, bool>? nullFilter = null;
            return CreateAndAttachToCompilation(compilation, analyzers, options, analyzerManager, onAnalyzerException, nullFilter, reportAnalyzer, severityFilter, trackSuppressedDiagnosticIds, out newCompilation, cancellationToken: cancellationToken);
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
            bool trackSuppressedDiagnosticIds,
            out Compilation newCompilation,
            CancellationToken cancellationToken)
        {
            AnalyzerDriver analyzerDriver = compilation.CreateAnalyzerDriver(analyzers, analyzerManager, severityFilter);
            newCompilation = compilation
                .WithSemanticModelProvider(CachingSemanticModelProvider.Instance)
                .WithEventQueue(new AsyncQueue<CompilationEvent>());

            var categorizeDiagnostics = false;
            var analysisOptions = new CompilationWithAnalyzersOptions(options, onAnalyzerException, analyzerExceptionFilter: analyzerExceptionFilter, concurrentAnalysis: true, logAnalyzerExecutionTime: reportAnalyzer, reportSuppressedDiagnostics: false);
            var analysisScope = AnalysisScope.CreateForBatchCompile(newCompilation, options.GetAdditionalFiles(), analyzers);
            analyzerDriver.Initialize(newCompilation, analysisOptions, new CompilationData(newCompilation), analysisScope, categorizeDiagnostics, trackSuppressedDiagnosticIds, cancellationToken);

            analyzerDriver.AttachQueueAndStartProcessingEvents(newCompilation.EventQueue!, analysisScope, usingPrePopulatedEventQueue: false, cancellationToken);
            return analyzerDriver;
        }

        /// <summary>
        /// Returns all diagnostics computed by the analyzers since the last time this was invoked.
        /// If <see cref="CompilationEventQueue"/> has been completed with all compilation events, then it waits for
        /// <see cref="WhenCompletedTask"/> task for the driver to finish processing all events and generate remaining analyzer diagnostics.
        /// </summary>
        public async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(Compilation compilation, CancellationToken cancellationToken)
        {
            var allDiagnostics = DiagnosticBag.GetInstance();
            if (CompilationEventQueue.IsCompleted)
            {
                await this.WhenCompletedTask.ConfigureAwait(false);

                if (this.WhenCompletedTask.IsFaulted)
                {
                    OnDriverException(this.WhenCompletedTask, this.AnalyzerExecutor, this.Analyzers, cancellationToken);
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

        /// <summary>
        /// Returns an array of  <see cref="DiagnosticDescriptor"/>s for all <see cref="Analyzers"/>
        /// along with <see cref="DiagnosticDescriptorErrorLoggerInfo"/> to be logged by the <see cref="ErrorLogger"/>.
        /// </summary>
        public ImmutableArray<(DiagnosticDescriptor Descriptor, DiagnosticDescriptorErrorLoggerInfo Info)> GetAllDiagnosticDescriptorsWithInfo(CancellationToken cancellationToken, out double totalAnalyzerExecutionTime)
        {
            var uniqueDiagnosticIds = PooledHashSet<string>.GetInstance();
            var analyzersSuppressedForSomeTree = SuppressedAnalyzersForTreeMap.SelectMany(kvp => kvp.Value).ToImmutableHashSet();
            totalAnalyzerExecutionTime = AnalyzerExecutionTimes.Sum(kvp => kvp.Value.TotalSeconds);

            var builder = ArrayBuilder<(DiagnosticDescriptor Descriptor, DiagnosticDescriptorErrorLoggerInfo Info)>.GetInstance();
            foreach (var analyzer in Analyzers)
            {
                var descriptors = AnalyzerManager.GetSupportedDiagnosticDescriptors(analyzer, AnalyzerExecutor, cancellationToken);

                // Check if this analyzer is suppressed for the entire compilation via global project level options
                // or for one or more syntax trees via editorconfig.
                bool isAnalyzerEverSuppressed = !UnsuppressedAnalyzers.Contains(analyzer) ||
                    analyzersSuppressedForSomeTree.Contains(analyzer);

                double analyzerExecutionTime = 0;
                if (AnalyzerExecutionTimes.TryGetValue(analyzer, out var analyzerExecutionTimeSpan))
                {
                    analyzerExecutionTime = analyzerExecutionTimeSpan.TotalSeconds;
                }

                var executionPercentage = (int)(analyzerExecutionTime * 100 / totalAnalyzerExecutionTime);

                foreach (var descriptor in descriptors)
                {
                    if (!uniqueDiagnosticIds.Add(descriptor.Id))
                        continue;

                    // Analyzers which were executed for the entire compilation might report
                    // multiple diagnostic IDs, such that a strict subset of those ids are suppressed
                    // for the entire compilation or for one or more syntax trees.
                    // We want to report these diagnostic IDs as suppressed.
                    var isDiagnosticIdEverSuppressed = isAnalyzerEverSuppressed ||
                        SuppressedDiagnosticIdsForUnsuppressedAnalyzers.Contains(descriptor.Id);

                    var effectiveSeverities = GetEffectiveSeverities(descriptor, AnalyzerExecutor.Compilation, AnalyzerExecutor.AnalyzerOptions, cancellationToken);
                    var info = new DiagnosticDescriptorErrorLoggerInfo(analyzerExecutionTime, executionPercentage, effectiveSeverities, isDiagnosticIdEverSuppressed);
                    builder.Add((descriptor, info));
                }
            }

            uniqueDiagnosticIds.Free();
            return builder.ToImmutableAndFree();

            static ImmutableHashSet<ReportDiagnostic> GetEffectiveSeverities(
                DiagnosticDescriptor descriptor,
                Compilation compilation,
                AnalyzerOptions analyzerOptions,
                CancellationToken cancellationToken)
            {
                var defaultSeverity = descriptor.IsEnabledByDefault ?
                    DiagnosticDescriptor.MapSeverityToReport(descriptor.DefaultSeverity) :
                    ReportDiagnostic.Suppress;

                if (descriptor.IsNotConfigurable())
                    return ImmutableHashSet.Create(defaultSeverity);

                if (compilation.Options.SpecificDiagnosticOptions.TryGetValue(descriptor.Id, out var severity) ||
                    compilation.Options.SyntaxTreeOptionsProvider?.TryGetGlobalDiagnosticValue(descriptor.Id, cancellationToken, out severity) == true)
                {
                    if (severity != ReportDiagnostic.Default)
                    {
                        defaultSeverity = severity;
                    }
                }

                // Handle /warnaserror
                if (defaultSeverity == ReportDiagnostic.Warn &&
                    compilation.Options.GeneralDiagnosticOption == ReportDiagnostic.Error)
                {
                    defaultSeverity = ReportDiagnostic.Error;
                }

                if (compilation.Options.SyntaxTreeOptionsProvider is not { } syntaxTreeProvider ||
                    compilation.SyntaxTrees.IsEmpty())
                {
                    return ImmutableHashSet.Create(defaultSeverity);
                }

                var builder = ImmutableHashSet.CreateBuilder<ReportDiagnostic>();
                foreach (var tree in compilation.SyntaxTrees)
                {
                    var severityForTree = defaultSeverity;

                    if (syntaxTreeProvider.TryGetDiagnosticValue(tree, descriptor.Id, cancellationToken, out severity) ||
                        analyzerOptions.TryGetSeverityFromBulkConfiguration(tree, compilation, descriptor, cancellationToken, out severity))
                    {
                        Debug.Assert(severity != ReportDiagnostic.Default);

                        // Handle /warnaserror
                        if (severity == ReportDiagnostic.Warn &&
                            compilation.Options.GeneralDiagnosticOption == ReportDiagnostic.Error)
                        {
                            severity = ReportDiagnostic.Error;
                        }

                        severityForTree = severity;
                    }

                    builder.Add(severityForTree);
                }

                return builder.ToImmutable();
            }
        }

        private SemanticModel GetOrCreateSemanticModel(SyntaxTree tree)
            => GetOrCreateSemanticModel(tree, AnalyzerExecutor.Compilation);

        protected SemanticModel GetOrCreateSemanticModel(SyntaxTree tree, Compilation compilation)
        {
            Debug.Assert(compilation.ContainsSyntaxTree(tree));

            return SemanticModelProvider.GetSemanticModel(tree, compilation);
        }

        public void ApplyProgrammaticSuppressions(DiagnosticBag reportedDiagnostics, Compilation compilation, CancellationToken cancellationToken)
        {
            Debug.Assert(!reportedDiagnostics.IsEmptyWithoutResolution);
            if (!HasDiagnosticSuppressors)
            {
                return;
            }

            var newDiagnostics = ApplyProgrammaticSuppressionsCore(reportedDiagnostics.ToReadOnly(), compilation, cancellationToken);
            reportedDiagnostics.Clear();
            reportedDiagnostics.AddRange(newDiagnostics);
        }

        public ImmutableArray<Diagnostic> ApplyProgrammaticSuppressions(ImmutableArray<Diagnostic> reportedDiagnostics, Compilation compilation, CancellationToken cancellationToken)
        {
            if (reportedDiagnostics.IsEmpty ||
                !HasDiagnosticSuppressors)
            {
                return reportedDiagnostics;
            }

            return ApplyProgrammaticSuppressionsCore(reportedDiagnostics, compilation, cancellationToken);
        }

        private ImmutableArray<Diagnostic> ApplyProgrammaticSuppressionsCore(ImmutableArray<Diagnostic> reportedDiagnostics, Compilation compilation, CancellationToken cancellationToken)
        {
            Debug.Assert(HasDiagnosticSuppressors);
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
                            var suppressableDiagnostics = getSuppressableDiagnostics(suppressor);
                            if (!suppressableDiagnostics.IsEmpty)
                            {
                                var task = Task.Run(
                                    () => AnalyzerExecutor.ExecuteSuppressionAction(suppressor, suppressableDiagnostics, cancellationToken),
                                    cancellationToken);
                                tasks.Add(task);
                            }
                        }

                        Task.WaitAll(tasks.ToArray(), cancellationToken);
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
                        AnalyzerExecutor.ExecuteSuppressionAction(suppressor, getSuppressableDiagnostics(suppressor), cancellationToken);
                    }
                }

                return;

                ImmutableArray<Diagnostic> getSuppressableDiagnostics(DiagnosticSuppressor suppressor)
                {
                    var supportedSuppressions = AnalyzerManager.GetSupportedSuppressionDescriptors(suppressor, AnalyzerExecutor, cancellationToken);
                    if (supportedSuppressions.IsEmpty)
                    {
                        return ImmutableArray<Diagnostic>.Empty;
                    }

                    using var builder = TemporaryArray<Diagnostic>.Empty;
                    foreach (var diagnostic in reportedDiagnostics)
                    {
                        if (supportedSuppressions.Contains(s => s.SuppressedDiagnosticId == diagnostic.Id))
                        {
                            builder.Add(diagnostic);
                        }
                    }

                    return builder.ToImmutableAndClear();
                }
            }

            static ImmutableDictionary<Diagnostic, ProgrammaticSuppressionInfo> createProgrammaticSuppressionsByDiagnosticMap(ConcurrentSet<Suppression> programmaticSuppressions)
            {
                var programmaticSuppressionsBuilder = PooledDictionary<Diagnostic, ArrayBuilder<Suppression>>.GetInstance();

                foreach (var programmaticSuppression in programmaticSuppressions)
                {
                    if (!programmaticSuppressionsBuilder.TryGetValue(programmaticSuppression.SuppressedDiagnostic, out var array))
                    {
                        array = ArrayBuilder<Suppression>.GetInstance();
                        programmaticSuppressionsBuilder.Add(programmaticSuppression.SuppressedDiagnostic, array);
                    }

                    if (!array.Contains(programmaticSuppression))
                        array.Add(programmaticSuppression);
                }

                var mapBuilder = ImmutableDictionary.CreateBuilder<Diagnostic, ProgrammaticSuppressionInfo>();
                foreach (var (diagnostic, set) in programmaticSuppressionsBuilder)
                {
                    mapBuilder.Add(diagnostic, new ProgrammaticSuppressionInfo(set.ToImmutableAndFree()));
                }

                programmaticSuppressionsBuilder.Free();
                return mapBuilder.ToImmutable();
            }
        }

        public ImmutableArray<Diagnostic> DequeueLocalDiagnosticsAndApplySuppressions(DiagnosticAnalyzer analyzer, bool syntax, Compilation compilation, CancellationToken cancellationToken)
        {
            var diagnostics = syntax ? DiagnosticQueue.DequeueLocalSyntaxDiagnostics(analyzer) : DiagnosticQueue.DequeueLocalSemanticDiagnostics(analyzer);
            return FilterDiagnosticsSuppressedInSourceOrByAnalyzers(diagnostics, compilation, cancellationToken);
        }

        public ImmutableArray<Diagnostic> DequeueNonLocalDiagnosticsAndApplySuppressions(DiagnosticAnalyzer analyzer, Compilation compilation, CancellationToken cancellationToken)
        {
            var diagnostics = DiagnosticQueue.DequeueNonLocalDiagnostics(analyzer);
            return FilterDiagnosticsSuppressedInSourceOrByAnalyzers(diagnostics, compilation, cancellationToken);
        }

        private ImmutableArray<Diagnostic> FilterDiagnosticsSuppressedInSourceOrByAnalyzers(ImmutableArray<Diagnostic> diagnostics, Compilation compilation, CancellationToken cancellationToken)
        {
            diagnostics = FilterDiagnosticsSuppressedInSource(diagnostics, compilation, CurrentCompilationData.SuppressMessageAttributeState);
            return ApplyProgrammaticSuppressionsAndFilterDiagnostics(diagnostics, compilation, cancellationToken);
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

        internal ImmutableArray<Diagnostic> ApplyProgrammaticSuppressionsAndFilterDiagnostics(ImmutableArray<Diagnostic> reportedDiagnostics, Compilation compilation, CancellationToken cancellationToken)
        {
            if (reportedDiagnostics.IsEmpty)
            {
                return reportedDiagnostics;
            }

            var diagnostics = ApplyProgrammaticSuppressions(reportedDiagnostics, compilation, cancellationToken);
            if (compilation.Options.ReportSuppressedDiagnostics || diagnostics.All(d => !d.IsSuppressed))
            {
                return diagnostics;
            }

            return diagnostics.WhereAsArray(d => !d.IsSuppressed);
        }

        private bool IsInGeneratedCode(Location location, Compilation compilation, CancellationToken cancellationToken)
        {
            if (!location.IsInSource)
            {
                return false;
            }

            Debug.Assert(location.SourceTree != null);

            // Check if this is a generated code location.
            if (IsGeneratedOrHiddenCodeLocation(location.SourceTree, location.SourceSpan, cancellationToken))
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
                    var root = tree.GetRoot(cancellationToken);

                    // PERF: Bail out early if file doesn't have "GeneratedCode" text.
                    if (!containsGeneratedCodeToken(root))
                        return ImmutableHashSet<ISymbol>.Empty;

                    var model = compilation.GetSemanticModel(tree);
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

                static bool containsGeneratedCodeToken(SyntaxNode root)
                {
                    return root.DescendantTokens().Any(static token => string.Equals(token.ValueText, "GeneratedCode", StringComparison.Ordinal) ||
                                                                       string.Equals(token.ValueText, nameof(GeneratedCodeAttribute), StringComparison.Ordinal));
                }
            }
        }

        private bool IsAnalyzerSuppressedForTree(DiagnosticAnalyzer analyzer, SyntaxTree tree, SyntaxTreeOptionsProvider? options, CancellationToken cancellationToken)
        {
            if (!SuppressedAnalyzersForTreeMap.TryGetValue(tree, out var suppressedAnalyzers))
            {
                suppressedAnalyzers = SuppressedAnalyzersForTreeMap.GetOrAdd(tree, ComputeSuppressedAnalyzersForTree(tree, options, cancellationToken));
            }

            return suppressedAnalyzers.Contains(analyzer);
        }

        private ImmutableHashSet<DiagnosticAnalyzer> ComputeSuppressedAnalyzersForTree(SyntaxTree tree, SyntaxTreeOptionsProvider? options, CancellationToken cancellationToken)
        {
            if (options is null)
            {
                return ImmutableHashSet<DiagnosticAnalyzer>.Empty;
            }

            ImmutableHashSet<DiagnosticAnalyzer>.Builder? suppressedAnalyzersBuilder = null;
            foreach (var analyzer in UnsuppressedAnalyzers)
            {
                if (NonConfigurableAndCustomConfigurableAnalyzers.Contains(analyzer))
                {
                    // Analyzers reporting non-configurable or custom configurable diagnostics cannot be suppressed as user configuration is ignored for these analyzers.
                    continue;
                }

                if ((SymbolStartAnalyzers.Contains(analyzer) || CompilationEndAnalyzers.Contains(analyzer)) &&
                    !ShouldSkipAnalysisOnGeneratedCode(analyzer))
                {
                    // SymbolStart/End analyzers and CompilationStart/End analyzers that analyze generated code
                    // cannot have any of their callbacks suppressed as they need to analyze the entire compilation for correctness.
                    continue;
                }

                var descriptors = AnalyzerManager.GetSupportedDiagnosticDescriptors(analyzer, AnalyzerExecutor, cancellationToken);
                var hasUnsuppressedDiagnostic = false;
                foreach (var descriptor in descriptors)
                {
                    var configuredSeverity = descriptor.GetEffectiveSeverity(AnalyzerExecutor.Compilation.Options);
                    if (options.TryGetDiagnosticValue(tree, descriptor.Id, cancellationToken, out var severityFromOptions) ||
                        options.TryGetGlobalDiagnosticValue(descriptor.Id, cancellationToken, out severityFromOptions))
                    {
                        configuredSeverity = severityFromOptions;
                    }

                    // Disabled by default descriptor with default configured severity is equivalent to suppressed.
                    if (!descriptor.IsEnabledByDefault && configuredSeverity == ReportDiagnostic.Default)
                    {
                        configuredSeverity = ReportDiagnostic.Suppress;
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

        private async Task ProcessCompilationEventsAsync(AnalysisScope analysisScope, bool prePopulatedEventQueue, CancellationToken cancellationToken)
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
                        workerTasks[i] = Task.Run(async () => await ProcessCompilationEventsCoreAsync(analysisScope, prePopulatedEventQueue, cancellationToken).ConfigureAwait(false));
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    // Kick off tasks to execute syntax tree actions.
                    var syntaxTreeActionsTask = analysisScope.SyntaxTrees.Any()
                        ? Task.Run(() => ExecuteSyntaxTreeActions(analysisScope, cancellationToken), cancellationToken)
                        : Task.CompletedTask;

                    // Kick off tasks to execute additional file actions.
                    var additionalFileActionsTask = analysisScope.AdditionalFiles.Any()
                        ? Task.Run(() => ExecuteAdditionalFileActions(analysisScope, cancellationToken), cancellationToken)
                        : Task.CompletedTask;

                    // If necessary, wait for all worker threads to complete processing events.
                    if (workerTasks.Length > 0 || syntaxTreeActionsTask.Status != TaskStatus.RanToCompletion || additionalFileActionsTask.Status != TaskStatus.RanToCompletion)
                    {
                        await Task.WhenAll(workerTasks.Concat(syntaxTreeActionsTask).Concat(additionalFileActionsTask)).ConfigureAwait(false);
                    }

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
                    completedEvent = await ProcessCompilationEventsCoreAsync(analysisScope, prePopulatedEventQueue, cancellationToken).ConfigureAwait(false);

                    ExecuteSyntaxTreeActions(analysisScope, cancellationToken);
                    ExecuteAdditionalFileActions(analysisScope, cancellationToken);
                }

                // Finally process the compilation completed event, if any.
                if (completedEvent != null)
                {
                    await ProcessEventAsync(completedEvent, analysisScope, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        private async Task<CompilationCompletedEvent?> ProcessCompilationEventsCoreAsync(AnalysisScope analysisScope, bool prePopulatedEventQueue, CancellationToken cancellationToken)
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

                    await ProcessEventAsync(compilationEvent, analysisScope, cancellationToken).ConfigureAwait(false);
                }

                return completedEvent;
            }
            catch (Exception e) when (FatalError.ReportAndPropagateUnlessCanceled(e, cancellationToken))
            {
                throw ExceptionUtilities.Unreachable();
            }
        }

        private async Task ProcessEventAsync(CompilationEvent e, AnalysisScope analysisScope, CancellationToken cancellationToken)
        {
            EventProcessedState eventProcessedState = await TryProcessEventCoreAsync(e, analysisScope, cancellationToken).ConfigureAwait(false);

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

            await OnEventProcessedCoreAsync(e, processedAnalyzers, analysisScope, cancellationToken).ConfigureAwait(false);
        }

        private async Task OnEventProcessedCoreAsync(CompilationEvent compilationEvent, ImmutableArray<DiagnosticAnalyzer> processedAnalyzers, AnalysisScope analysisScope, CancellationToken cancellationToken)
        {
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

                for (var type = symbol.ContainingType; type != null; type = type.ContainingType)
                    await processContainerOnMemberCompletedAsync(type, symbol, analyzer).ConfigureAwait(false);
            }

            async Task processContainerOnMemberCompletedAsync(INamespaceOrTypeSymbol containerSymbol, ISymbol processedMemberSymbol, DiagnosticAnalyzer analyzer)
            {
                if (containerSymbol != null &&
                    AnalyzerExecutor.TryExecuteSymbolEndActionsForContainer(containerSymbol, processedMemberSymbol,
                        analyzer, s_getTopmostNodeForAnalysis, IsGeneratedCodeSymbol(containerSymbol, cancellationToken),
                        analysisScope.OriginalFilterFile?.SourceTree, analysisScope.OriginalFilterSpan, cancellationToken, out var processedContainerEvent))
                {
                    await OnEventProcessedCoreAsync(processedContainerEvent, ImmutableArray.Create(analyzer), analysisScope, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        [PerformanceSensitive(
            "https://developercommunity.visualstudio.com/content/problem/805524/ctrl-suggestions-are-very-slow-and-produce-gatheri.html",
            OftenCompletesSynchronously = true)]
        private async ValueTask<EventProcessedState> TryProcessEventCoreAsync(CompilationEvent compilationEvent, AnalysisScope analysisScope, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            switch (compilationEvent)
            {
                case SymbolDeclaredCompilationEvent symbolEvent:
                    return await TryProcessSymbolDeclaredAsync(symbolEvent, analysisScope, cancellationToken).ConfigureAwait(false);

                case CompilationUnitCompletedEvent completedEvent:
                    ProcessCompilationUnitCompleted(completedEvent, analysisScope, cancellationToken);
                    return EventProcessedState.Processed;

                case CompilationCompletedEvent endEvent:
                    ProcessCompilationCompleted(endEvent, analysisScope, cancellationToken);
                    return EventProcessedState.Processed;

                case CompilationStartedEvent startedEvent:
                    ProcessCompilationStarted(startedEvent, analysisScope, cancellationToken);
                    return EventProcessedState.Processed;

                default:
                    throw new InvalidOperationException("Unexpected compilation event of type " + compilationEvent.GetType().Name);
            }
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
        private async ValueTask<EventProcessedState> TryProcessSymbolDeclaredAsync(SymbolDeclaredCompilationEvent symbolEvent, AnalysisScope analysisScope, CancellationToken cancellationToken)
        {
            var symbol = symbolEvent.Symbol;
            var isGeneratedCodeSymbol = IsGeneratedCodeSymbol(symbol, cancellationToken);

            var skipSymbolAnalysis = AnalysisScope.ShouldSkipSymbolAnalysis(symbolEvent);
            var skipDeclarationAnalysis = AnalysisScope.ShouldSkipDeclarationAnalysis(symbol);
            var hasPerSymbolActions = AnalyzerActions.SymbolStartActionsCount > 0 && (!skipSymbolAnalysis || !skipDeclarationAnalysis);

            var perSymbolActions = hasPerSymbolActions ?
                await GetPerSymbolAnalyzerActionsAsync(symbol, analysisScope, cancellationToken).ConfigureAwait(false) :
                EmptyGroupedActions;

            if (!skipSymbolAnalysis)
            {
                ExecuteSymbolActions(symbolEvent, analysisScope, isGeneratedCodeSymbol, cancellationToken);
            }

            if (!skipDeclarationAnalysis)
            {
                ExecuteDeclaringReferenceActions(symbolEvent, analysisScope, isGeneratedCodeSymbol, perSymbolActions, cancellationToken);
            }

            if (hasPerSymbolActions &&
                !TryExecuteSymbolEndActions(perSymbolActions.AnalyzerActions, symbolEvent, analysisScope, isGeneratedCodeSymbol, cancellationToken, out var subsetProcessedAnalyzers))
            {
                Debug.Assert(!subsetProcessedAnalyzers.IsDefault);
                return subsetProcessedAnalyzers.IsEmpty ? EventProcessedState.NotProcessed : EventProcessedState.CreatePartiallyProcessed(subsetProcessedAnalyzers);
            }

            return EventProcessedState.Processed;
        }

        private void ExecuteSymbolActions(SymbolDeclaredCompilationEvent symbolEvent, AnalysisScope analysisScope, bool isGeneratedCodeSymbol, CancellationToken cancellationToken)
        {
            var symbol = symbolEvent.Symbol;
            if (!analysisScope.ShouldAnalyze(symbolEvent, s_getTopmostNodeForAnalysis, cancellationToken))
            {
                return;
            }

            foreach (var (analyzer, actionsByKind) in _lazySymbolActionsByKind)
            {
                if (!analysisScope.Contains(analyzer))
                {
                    continue;
                }

                // Invoke symbol analyzers only for source symbols.
                if ((int)symbol.Kind < actionsByKind.Length)
                {
                    AnalyzerExecutor.ExecuteSymbolActions(actionsByKind[(int)symbol.Kind], analyzer, symbolEvent, s_getTopmostNodeForAnalysis, isGeneratedCodeSymbol, analysisScope.FilterFileOpt?.SourceTree, analysisScope.FilterSpanOpt, cancellationToken);
                }
            }
        }

        private bool TryExecuteSymbolEndActions(
            in AnalyzerActions perSymbolActions,
            SymbolDeclaredCompilationEvent symbolEvent,
            AnalysisScope analysisScope,
            bool isGeneratedCodeSymbol,
            CancellationToken cancellationToken,
            out ImmutableArray<DiagnosticAnalyzer> subsetProcessedAnalyzers)
        {
            Debug.Assert(AnalyzerActions.SymbolStartActionsCount > 0);

            var symbol = symbolEvent.Symbol;
            var symbolEndActions = perSymbolActions.SymbolEndActions;
            if (symbolEndActions.IsEmpty || !analysisScope.ShouldAnalyze(symbolEvent, s_getTopmostNodeForAnalysis, cancellationToken))
            {
                subsetProcessedAnalyzers = ImmutableArray<DiagnosticAnalyzer>.Empty;
                return true;
            }

            var success = true;
            ArrayBuilder<DiagnosticAnalyzer> completedAnalyzers = s_diagnosticAnalyzerPool.Allocate();
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
                        !AnalyzerExecutor.TryExecuteSymbolEndActions(symbolEndActionsForAnalyzer, analyzer, symbolEvent, s_getTopmostNodeForAnalysis, isGeneratedCodeSymbol, analysisScope.OriginalFilterFile?.SourceTree, analysisScope.OriginalFilterSpan, cancellationToken))
                    {
                        success = false;
                        continue;
                    }

                    AnalyzerManager.MarkSymbolEndAnalysisComplete(symbol, analyzer);
                    completedAnalyzers.Add(analyzer);
                }

                if (processedAnalyzers.Count < analysisScope.Analyzers.Length)
                {
                    foreach (var analyzer in analysisScope.Analyzers)
                    {
                        if (!processedAnalyzers.Contains(analyzer))
                        {
                            AnalyzerManager.MarkSymbolEndAnalysisComplete(symbol, analyzer);
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

                // Do not call completedAnalyzers.Free, as the ArrayBuilder isn't associated with our pool and even if it were, we don't
                // want the default freeing behavior of limiting pooled array size to ArrayBuilder.PooledArrayLengthLimitExclusive.
                // Instead, we need to explicitly add this item back to our pool.
                completedAnalyzers.Clear();
                s_diagnosticAnalyzerPool.Free(completedAnalyzers);
            }
        }

        private static SyntaxNode GetTopmostNodeForAnalysis(ISymbol symbol, SyntaxReference syntaxReference, Compilation compilation, CancellationToken cancellationToken)
        {
            var model = compilation.GetSemanticModel(syntaxReference.SyntaxTree);
            return model.GetTopmostNodeForDiagnosticAnalysis(symbol, syntaxReference.GetSyntax(cancellationToken));
        }

        protected abstract void ExecuteDeclaringReferenceActions(
            SymbolDeclaredCompilationEvent symbolEvent,
            AnalysisScope analysisScope,
            bool isGeneratedCodeSymbol,
            IGroupedAnalyzerActions additionalPerSymbolActions,
            CancellationToken cancellationToken);

        private void ProcessCompilationUnitCompleted(CompilationUnitCompletedEvent completedEvent, AnalysisScope analysisScope, CancellationToken cancellationToken)
        {
            // When the compiler is finished with a compilation unit, we can run user diagnostics which
            // might want to ask the compiler for all the diagnostics in the source file, for example
            // to get information about unnecessary usings.

            var semanticModel = GetOrCreateSemanticModel(completedEvent.CompilationUnit, completedEvent.Compilation);
            if (!analysisScope.ShouldAnalyze(semanticModel.SyntaxTree))
            {
                return;
            }

            var isGeneratedCode = IsGeneratedCode(semanticModel.SyntaxTree, cancellationToken);
            if (isGeneratedCode && DoNotAnalyzeGeneratedCode)
            {
                return;
            }

            foreach (var (analyzer, semanticModelActions) in _lazySemanticModelActions)
            {
                if (!analysisScope.Contains(analyzer))
                {
                    continue;
                }

                // Execute actions for a given analyzer sequentially.
                AnalyzerExecutor.ExecuteSemanticModelActions(semanticModelActions, analyzer, semanticModel, analysisScope.FilterSpanOpt, isGeneratedCode, cancellationToken);
            }
        }

        private void ProcessCompilationStarted(CompilationStartedEvent startedEvent, AnalysisScope analysisScope, CancellationToken cancellationToken)
        {
            ExecuteCompilationActions(_lazyCompilationActions, startedEvent, analysisScope, cancellationToken);
        }

        private void ProcessCompilationCompleted(CompilationCompletedEvent endEvent, AnalysisScope analysisScope, CancellationToken cancellationToken)
        {
            ExecuteCompilationActions(_lazyCompilationEndActions, endEvent, analysisScope, cancellationToken);
        }

        private void ExecuteCompilationActions(
            ImmutableArray<(DiagnosticAnalyzer, ImmutableArray<CompilationAnalyzerAction>)> compilationActionsMap,
            CompilationEvent compilationEvent,
            AnalysisScope analysisScope,
            CancellationToken cancellationToken)
        {
            Debug.Assert(compilationEvent is CompilationStartedEvent || compilationEvent is CompilationCompletedEvent);

            foreach (var (analyzer, compilationActions) in compilationActionsMap)
            {
                if (!analysisScope.Contains(analyzer))
                {
                    continue;
                }

                AnalyzerExecutor.ExecuteCompilationActions(compilationActions, analyzer, compilationEvent, cancellationToken);
            }
        }

        internal static Action<Diagnostic, AnalyzerOptions, CancellationToken> GetDiagnosticSink(Action<Diagnostic> addDiagnosticCore, Compilation compilation, SeverityFilter severityFilter, ConcurrentSet<string>? suppressedDiagnosticIds)
        {
            return (diagnostic, analyzerOptions, cancellationToken) =>
            {
                var filteredDiagnostic = GetFilteredDiagnostic(diagnostic, compilation, analyzerOptions, severityFilter, suppressedDiagnosticIds, cancellationToken);
                if (filteredDiagnostic != null)
                {
                    addDiagnosticCore(filteredDiagnostic);
                }
            };
        }

        internal static Action<Diagnostic, DiagnosticAnalyzer, AnalyzerOptions, bool, CancellationToken> GetDiagnosticSink(Action<Diagnostic, DiagnosticAnalyzer, bool> addLocalDiagnosticCore, Compilation compilation, SeverityFilter severityFilter, ConcurrentSet<string>? suppressedDiagnosticIds)
        {
            return (diagnostic, analyzer, analyzerOptions, isSyntaxDiagnostic, cancellationToken) =>
            {
                var filteredDiagnostic = GetFilteredDiagnostic(diagnostic, compilation, analyzerOptions, severityFilter, suppressedDiagnosticIds, cancellationToken);
                if (filteredDiagnostic != null)
                {
                    addLocalDiagnosticCore(filteredDiagnostic, analyzer, isSyntaxDiagnostic);
                }
            };
        }

        internal static Action<Diagnostic, DiagnosticAnalyzer, AnalyzerOptions?, CancellationToken> GetDiagnosticSink(Action<Diagnostic, DiagnosticAnalyzer> addDiagnosticCore, Compilation compilation, SeverityFilter severityFilter, ConcurrentSet<string>? suppressedDiagnosticIds)
        {
            return (diagnostic, analyzer, analyzerOptions, cancellationToken) =>
            {
                var filteredDiagnostic = GetFilteredDiagnostic(diagnostic, compilation, analyzerOptions, severityFilter, suppressedDiagnosticIds, cancellationToken);
                if (filteredDiagnostic != null)
                {
                    addDiagnosticCore(filteredDiagnostic, analyzer);
                }
            };
        }

        private static Diagnostic? GetFilteredDiagnostic(Diagnostic diagnostic, Compilation compilation, AnalyzerOptions? analyzerOptions, SeverityFilter severityFilter, ConcurrentSet<string>? suppressedDiagnosticIds, CancellationToken cancellationToken)
        {
            var filteredDiagnostic = compilation.Options.FilterDiagnostic(diagnostic, cancellationToken);
            filteredDiagnostic = applyFurtherFiltering(filteredDiagnostic);

            // Track diagnostics suppressed through compilation options or syntax tree options.
            if (filteredDiagnostic == null)
                suppressedDiagnosticIds?.Add(diagnostic.Id);

            return filteredDiagnostic;

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
            AnalysisScope analysisScope,
            SeverityFilter severityFilter,
            CancellationToken cancellationToken)
        {
            var unsuppressedAnalyzersBuilder = PooledHashSet<DiagnosticAnalyzer>.GetInstance();
            var actions = ArrayBuilder<AnalyzerActions>.GetInstance();

            foreach (var analyzer in analyzers)
            {
                if (!IsDiagnosticAnalyzerSuppressed(analyzer, analyzerExecutor.Compilation.Options, analyzerManager, analyzerExecutor, analysisScope, severityFilter, cancellationToken))
                {
                    unsuppressedAnalyzersBuilder.Add(analyzer);

                    var analyzerActions = await analyzerManager.GetAnalyzerActionsAsync(analyzer, analyzerExecutor, cancellationToken).ConfigureAwait(false);
                    actions.Add(analyzerActions);
                }
            }

            var allAnalyzerActions = AnalyzerActions.Merge(actions);
            actions.Free();

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

                var analyzerActions = await GetPerSymbolAnalyzerActionsAsync(symbol, analyzer, analysisScope.OriginalFilterFile?.SourceTree, analysisScope.OriginalFilterSpan, cancellationToken).ConfigureAwait(false);
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
            SyntaxTree? filterTree,
            TextSpan? filterSpan,
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
            if (symbol is not INamespaceOrTypeSymbol namespaceOrType)
            {
                return await getAllActionsAsync(this, symbol, analyzer, filterTree, filterSpan, cancellationToken).ConfigureAwait(false);
            }

            if (PerSymbolAnalyzerActionsCache.TryGetValue((namespaceOrType, analyzer), out var actions))
            {
                return actions;
            }

            var allActions = await getAllActionsAsync(this, symbol, analyzer, filterTree, filterSpan, cancellationToken).ConfigureAwait(false);
            return PerSymbolAnalyzerActionsCache.GetOrAdd((namespaceOrType, analyzer), allActions);

            async ValueTask<IGroupedAnalyzerActions> getAllActionsAsync(AnalyzerDriver driver, ISymbol symbol, DiagnosticAnalyzer analyzer, SyntaxTree? filterTree, TextSpan? filterSpan, CancellationToken cancellationToken)
            {
                // Compute additional inherited actions for this symbol by running the containing symbol's start actions.
                var inheritedActions = await getInheritedActionsAsync(driver, symbol, analyzer, filterTree, filterSpan, cancellationToken).ConfigureAwait(false);

                // Execute the symbol start actions for this symbol to compute additional actions for its members.
                AnalyzerActions myActions = await getSymbolActionsCoreAsync(driver, symbol, analyzer, filterTree, filterSpan, cancellationToken).ConfigureAwait(false);
                if (myActions.IsEmpty)
                {
                    return inheritedActions;
                }

                var allActions = inheritedActions.AnalyzerActions.Append(in myActions);
                return CreateGroupedActions(analyzer, allActions);
            }

            async ValueTask<IGroupedAnalyzerActions> getInheritedActionsAsync(AnalyzerDriver driver, ISymbol symbol, DiagnosticAnalyzer analyzer, SyntaxTree? filterTree, TextSpan? filterSpan, CancellationToken cancellationToken)
            {
                if (symbol.ContainingSymbol != null)
                {
                    // Get container symbol's per-symbol actions, which also forces its start actions to execute.
                    var containerActions = await driver.GetPerSymbolAnalyzerActionsAsync(symbol.ContainingSymbol, analyzer, filterTree, filterSpan, cancellationToken).ConfigureAwait(false);
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

            static async ValueTask<AnalyzerActions> getSymbolActionsCoreAsync(AnalyzerDriver driver, ISymbol symbol, DiagnosticAnalyzer analyzer, SyntaxTree? filterTree, TextSpan? filterSpan, CancellationToken cancellationToken)
            {
                if (!driver.UnsuppressedAnalyzers.Contains(analyzer))
                {
                    return AnalyzerActions.Empty;
                }

                var isGeneratedCodeSymbol = driver.IsGeneratedCodeSymbol(symbol, cancellationToken);
                if (isGeneratedCodeSymbol && driver.ShouldSkipAnalysisOnGeneratedCode(analyzer))
                {
                    return AnalyzerActions.Empty;
                }

                return await driver.AnalyzerManager.GetPerSymbolAnalyzerActionsAsync(symbol, isGeneratedCodeSymbol, filterTree, filterSpan, analyzer, driver.AnalyzerExecutor, cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task<ImmutableSegmentedDictionary<DiagnosticAnalyzer, SemaphoreSlim>> CreateAnalyzerGateMapAsync(
            ImmutableHashSet<DiagnosticAnalyzer> analyzers,
            AnalyzerManager analyzerManager,
            AnalyzerExecutor analyzerExecutor,
            AnalysisScope analysisScope,
            SeverityFilter severityFilter,
            CancellationToken cancellationToken)
        {
            var builder = ImmutableSegmentedDictionary.CreateBuilder<DiagnosticAnalyzer, SemaphoreSlim>();
            foreach (var analyzer in analyzers)
            {
                Debug.Assert(!IsDiagnosticAnalyzerSuppressed(analyzer, analyzerExecutor.Compilation.Options, analyzerManager, analyzerExecutor, analysisScope, severityFilter, cancellationToken));

                var isConcurrent = await analyzerManager.IsConcurrentAnalyzerAsync(analyzer, analyzerExecutor, cancellationToken).ConfigureAwait(false);
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
            AnalysisScope analysisScope,
            SeverityFilter severityFilter,
            CancellationToken cancellationToken)
        {
            var builder = ImmutableSegmentedDictionary.CreateBuilder<DiagnosticAnalyzer, GeneratedCodeAnalysisFlags>();
            foreach (var analyzer in analyzers)
            {
                Debug.Assert(!IsDiagnosticAnalyzerSuppressed(analyzer, analyzerExecutor.Compilation.Options, analyzerManager, analyzerExecutor, analysisScope, severityFilter, cancellationToken));

                var generatedCodeAnalysisFlags = await analyzerManager.GetGeneratedCodeAnalysisFlagsAsync(analyzer, analyzerExecutor, cancellationToken).ConfigureAwait(false);
                builder.Add(analyzer, generatedCodeAnalysisFlags);
            }

            return builder.ToImmutable();
        }

        [PerformanceSensitive(
            "https://github.com/dotnet/roslyn/pull/23637",
            AllowLocks = false)]
        private bool IsGeneratedCodeSymbol(ISymbol symbol, CancellationToken cancellationToken)
        {
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
                    if (!IsGeneratedOrHiddenCodeLocation(declaringRef.SyntaxTree, declaringRef.Span, cancellationToken))
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
        protected bool IsGeneratedCode(SyntaxTree tree, CancellationToken cancellationToken)
        {
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
                return GeneratedCodeUtilities.GetGeneratedCodeKindFromOptions(options).ToNullable() ??
                    _isGeneratedCode(tree, cancellationToken);
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
        protected bool IsGeneratedOrHiddenCodeLocation(SyntaxTree syntaxTree, TextSpan span, CancellationToken cancellationToken)
            => IsGeneratedCode(syntaxTree, cancellationToken) || IsHiddenSourceLocation(syntaxTree, span);

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

        internal async Task<AnalyzerActionCounts> GetAnalyzerActionCountsAsync(DiagnosticAnalyzer analyzer, CompilationOptions compilationOptions, AnalysisScope analysisScope, CancellationToken cancellationToken)
        {
            if (IsDiagnosticAnalyzerSuppressed(analyzer, compilationOptions, AnalyzerManager, AnalyzerExecutor, analysisScope, _severityFilter, cancellationToken))
            {
                return AnalyzerActionCounts.Empty;
            }

            var analyzerActions = await AnalyzerManager.GetAnalyzerActionsAsync(analyzer, AnalyzerExecutor, cancellationToken).ConfigureAwait(false);
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
            AnalysisScope analysisScope,
            SeverityFilter severityFilter,
            CancellationToken cancellationToken)
        {
            return analyzerManager.IsDiagnosticAnalyzerSuppressed(analyzer, options, s_IsCompilerAnalyzerFunc, analyzerExecutor, analysisScope, severityFilter, cancellationToken);
        }

        internal static bool IsCompilerAnalyzer(DiagnosticAnalyzer analyzer) => analyzer is CompilerDiagnosticAnalyzer;

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
        /// Execute syntax node, code block and operation actions for all declarations for the given symbol.
        /// </summary>
        protected override void ExecuteDeclaringReferenceActions(
            SymbolDeclaredCompilationEvent symbolEvent,
            AnalysisScope analysisScope,
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

            if (executeSyntaxNodeActions || executeOperationActions || executeCodeBlockActions || executeOperationBlockActions)
            {
                var declaringReferences = symbolEvent.DeclaringSyntaxReferences;
                var coreActions = GetOrCreateCoreActions();
                foreach (var decl in declaringReferences)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (analysisScope.FilterFileOpt != null && analysisScope.FilterFileOpt?.SourceTree != decl.SyntaxTree)
                    {
                        continue;
                    }

                    var isInGeneratedCode = isGeneratedCodeSymbol || IsGeneratedOrHiddenCodeLocation(decl.SyntaxTree, decl.Span, cancellationToken);
                    if (isInGeneratedCode && DoNotAnalyzeGeneratedCode)
                    {
                        continue;
                    }

                    ExecuteDeclaringReferenceActions(decl, symbolEvent, analysisScope, coreActions, (GroupedAnalyzerActions)additionalPerSymbolActions,
                        executeSyntaxNodeActions, executeOperationActions, executeCodeBlockActions, executeOperationBlockActions, isInGeneratedCode, cancellationToken);
                }
            }
        }

        private static DeclarationAnalysisData ComputeDeclarationAnalysisData(
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
            var data = new DeclarationAnalysisData(declaringReferenceSyntax, topmostNodeForAnalysis, declarationInfos, isPartialDeclAnalysis);
            AddSyntaxNodesToAnalyze(topmostNodeForAnalysis, symbol, declarationInfos, semanticModel, data.DescendantNodesToAnalyze, cancellationToken);
            return data;
        }

        private static void ComputeDeclarationsInNode(SemanticModel semanticModel, ISymbol declaredSymbol, SyntaxNode declaringReferenceSyntax, SyntaxNode topmostNodeForAnalysis, ArrayBuilder<DeclarationInfo> builder, CancellationToken cancellationToken)
        {
            // We only care about the top level symbol declaration and its immediate member declarations.
            int? levelsToCompute = 2;
            var getSymbol = topmostNodeForAnalysis != declaringReferenceSyntax || declaredSymbol.Kind == SymbolKind.Namespace;
            semanticModel.ComputeDeclarationsInNode(topmostNodeForAnalysis, declaredSymbol, getSymbol, builder, cancellationToken, levelsToCompute);
        }

        /// <summary>
        /// Execute syntax node, code block and operation actions for the given declaration.
        /// </summary>
        private void ExecuteDeclaringReferenceActions(
            SyntaxReference decl,
            SymbolDeclaredCompilationEvent symbolEvent,
            AnalysisScope analysisScope,
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
                GetOrCreateSemanticModel(decl.SyntaxTree, symbolEvent.Compilation);

            var declarationAnalysisData = ComputeDeclarationAnalysisData(symbol, decl, semanticModel, analysisScope, cancellationToken);
            if (analysisScope.ShouldAnalyze(declarationAnalysisData.TopmostNodeForAnalysis))
            {
                // Execute stateless syntax node actions.
                executeNodeActions();

                // Execute actions in executable code: code block actions, operation actions and operation block actions.
                executeExecutableCodeActions();
            }

            declarationAnalysisData.Free();

            return;

            void executeNodeActions()
            {
                if (shouldExecuteSyntaxNodeActions)
                {
                    var nodesToAnalyze = declarationAnalysisData.DescendantNodesToAnalyze;
                    executeNodeActionsByKind(nodesToAnalyze, coreActions, arePerSymbolActions: false);
                    executeNodeActionsByKind(nodesToAnalyze, additionalPerSymbolActions, arePerSymbolActions: true);
                }
            }

            void executeNodeActionsByKind(ArrayBuilder<SyntaxNode> nodesToAnalyze, GroupedAnalyzerActions groupedActions, bool arePerSymbolActions)
            {
                if (groupedActions.GroupedActionsByAnalyzer.Length == 0)
                {
                    return;
                }

                var analyzersForNodes = PooledHashSet<DiagnosticAnalyzer>.GetInstance();
                foreach (var node in nodesToAnalyze)
                {
                    if (groupedActions.AnalyzersByKind.TryGetValue(_getKind(node), out var analyzersForKind))
                    {
                        foreach (var analyzer in analyzersForKind)
                        {
                            analyzersForNodes.Add(analyzer);
                        }
                    }
                }

                foreach (var (analyzer, groupedActionsForAnalyzer) in groupedActions.GroupedActionsByAnalyzer)
                {
                    if (!analyzersForNodes.Contains(analyzer) || !analysisScope.Contains(analyzer))
                    {
                        continue;
                    }

                    // We further filter out the nodes to analyze based on analysis scope if we are performing
                    // partial analysis of the declaration, i.e. analyzing a sub-span within the declaration span,
                    // and additionally the analyzer has not registered any code block start actions. In case
                    // the analyzer has registered code block start actions, we need to make callbacks for all nodes
                    // in the code block to ensure the analyzer can correctly report code block end diagnostics.
                    if (declarationAnalysisData.IsPartialAnalysis && !groupedActionsForAnalyzer.HasCodeBlockStartActions)
                    {
                        var filteredNodesToAnalyze = ArrayBuilder<SyntaxNode>.GetInstance(nodesToAnalyze.Count);
                        foreach (var node in nodesToAnalyze)
                        {
                            if (analysisScope.ShouldAnalyze(node))
                                filteredNodesToAnalyze.Add(node);
                        }

                        executeSyntaxNodeActions(analyzer, groupedActionsForAnalyzer, filteredNodesToAnalyze);
                        filteredNodesToAnalyze.Free();
                    }
                    else
                    {
                        executeSyntaxNodeActions(analyzer, groupedActionsForAnalyzer, nodesToAnalyze);
                    }
                }

                analyzersForNodes.Free();

                void executeSyntaxNodeActions(
                    DiagnosticAnalyzer analyzer,
                    GroupedAnalyzerActionsForAnalyzer groupedActionsForAnalyzer,
                    ArrayBuilder<SyntaxNode> filteredNodesToAnalyze)
                {
                    AnalyzerExecutor.ExecuteSyntaxNodeActions(
                        filteredNodesToAnalyze, groupedActionsForAnalyzer.NodeActionsByAnalyzerAndKind,
                        analyzer, semanticModel, _getKind, declarationAnalysisData.TopmostNodeForAnalysis.FullSpan,
                        symbol, analysisScope.FilterSpanOpt, isInGeneratedCode, hasCodeBlockStartOrSymbolStartActions: groupedActionsForAnalyzer.HasCodeBlockStartActions || arePerSymbolActions,
                        cancellationToken);
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

                    AnalyzerExecutor.OnAnalyzerException(ex, analyzer, diagnostic, cancellationToken);
                    return ImmutableArray<IOperation>.Empty;
                }
            }

            void executeOperationsActions(ImmutableArray<IOperation> operationsToAnalyze)
            {
                if (shouldExecuteOperationActions)
                {
                    executeOperationsActionsByKind(operationsToAnalyze, coreActions, arePerSymbolActions: false);
                    executeOperationsActionsByKind(operationsToAnalyze, additionalPerSymbolActions, arePerSymbolActions: true);
                }
            }

            void executeOperationsActionsByKind(ImmutableArray<IOperation> operationsToAnalyze, GroupedAnalyzerActions groupedActions, bool arePerSymbolActions)
            {
                foreach (var (analyzer, groupedActionsForAnalyzer) in groupedActions.GroupedActionsByAnalyzer)
                {
                    var operationActionsByKind = groupedActionsForAnalyzer.OperationActionsByAnalyzerAndKind;
                    if (operationActionsByKind.IsEmpty || !analysisScope.Contains(analyzer))
                    {
                        continue;
                    }

                    // We further filter out the operation to analyze based on analysis scope if we are performing
                    // partial analysis of the declaration, i.e. analyzing a sub-span within the declaration span,
                    // and additionally the analyzer has not registered any operation block start actions. In case the
                    // analyzer has registered operation block start actions, we need to make callbacks for all operations
                    // in the operation block to ensure the analyzer can correctly report operation block end diagnostics.
                    var filteredOperationsToAnalyze = declarationAnalysisData.IsPartialAnalysis && !groupedActionsForAnalyzer.HasOperationBlockStartActions
                        ? operationsToAnalyze.WhereAsArray(operation => analysisScope.ShouldAnalyze(operation.Syntax))
                        : operationsToAnalyze;

                    AnalyzerExecutor.ExecuteOperationActions(filteredOperationsToAnalyze, operationActionsByKind,
                        analyzer, semanticModel, declarationAnalysisData.TopmostNodeForAnalysis.FullSpan,
                        symbol, analysisScope.FilterSpanOpt, isInGeneratedCode, hasOperationBlockStartOrSymbolStartActions: groupedActionsForAnalyzer.HasOperationBlockStartActions || arePerSymbolActions,
                        cancellationToken);
                }
            }

            void executeOperationsBlockActions(ImmutableArray<IOperation> operationBlocksToAnalyze, ImmutableArray<IOperation> operationsToAnalyze, ArrayBuilder<ExecutableCodeBlockAnalyzerActions> codeBlockActions)
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

                    AnalyzerExecutor.ExecuteOperationBlockActions(
                        analyzerActions.OperationBlockStartActions, analyzerActions.OperationBlockActions,
                        analyzerActions.OperationBlockEndActions, analyzerActions.Analyzer, declarationAnalysisData.TopmostNodeForAnalysis, symbol,
                        operationBlocksToAnalyze, operationsToAnalyze, semanticModel, analysisScope.FilterSpanOpt, isInGeneratedCode, cancellationToken);
                }
            }

            void executeCodeBlockActions(ImmutableArray<SyntaxNode> executableCodeBlocks, ArrayBuilder<ExecutableCodeBlockAnalyzerActions> codeBlockActions)
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

                    AnalyzerExecutor.ExecuteCodeBlockActions(
                        analyzerActions.CodeBlockStartActions, analyzerActions.CodeBlockActions,
                        analyzerActions.CodeBlockEndActions, analyzerActions.Analyzer, declarationAnalysisData.TopmostNodeForAnalysis, symbol,
                        executableCodeBlocks, semanticModel, _getKind, analysisScope.FilterSpanOpt, isInGeneratedCode, cancellationToken);
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

        private static void AddSyntaxNodesToAnalyze(
            SyntaxNode declaredNode,
            ISymbol declaredSymbol,
            ImmutableArray<DeclarationInfo> declarationsInNode,
            SemanticModel semanticModel,
            ArrayBuilder<SyntaxNode> nodesToAnalyze,
            CancellationToken cancellationToken)
        {
            // Eliminate descendant member declarations within declarations.
            // There will be separate symbols declared for the members.
            HashSet<SyntaxNode>? descendantDeclsToSkip = null;
            bool first = true;
            foreach (var declInNode in declarationsInNode)
            {
                cancellationToken.ThrowIfCancellationRequested();

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

                        return;
                    }

                    // Compute the topmost node representing the syntax declaration for the member that needs to be skipped.
                    var declarationNodeToSkip = declInNode.DeclaredNode;
                    var declaredSymbolOfDeclInNode = declInNode.DeclaredSymbol ?? semanticModel.GetDeclaredSymbol(declInNode.DeclaredNode, cancellationToken);
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
            foreach (var node in declaredNode.DescendantNodesAndSelf(descendIntoChildren: shouldAddNode, descendIntoTrivia: true))
            {
                if (shouldAddNode(node) &&
                    !semanticModel.ShouldSkipSyntaxNodeAnalysis(node, declaredSymbol))
                {
                    nodesToAnalyze.Add(node);
                }
            }
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
                                // For constructor initializer, we generate an IInvocationOperation (or invalid
                                // operation in the case of an error) with an implicit IExpressionStatementOperation parent.
                                Debug.Assert(operationBlock.Kind is OperationKind.Invocation or OperationKind.Invalid);
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
