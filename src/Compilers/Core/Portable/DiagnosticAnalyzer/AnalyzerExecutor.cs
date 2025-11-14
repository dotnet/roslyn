// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Contains the core execution logic for callbacks into analyzers.
    /// </summary>
    internal partial class AnalyzerExecutor
    {
        private const string DiagnosticCategory = "Compiler";

        // internal for testing purposes only.
        internal const string AnalyzerExceptionDiagnosticId = "AD0001";
        internal const string AnalyzerDriverExceptionDiagnosticId = "AD0002";

        private readonly Action<Diagnostic, AnalyzerOptions, CancellationToken>? _addNonCategorizedDiagnostic;
        private readonly Action<Diagnostic, DiagnosticAnalyzer, AnalyzerOptions, bool, CancellationToken>? _addCategorizedLocalDiagnostic;
        private readonly Action<Diagnostic, DiagnosticAnalyzer, AnalyzerOptions, CancellationToken>? _addCategorizedNonLocalDiagnostic;
        private readonly Action<Suppression>? _addSuppression;
        private readonly Func<Exception, bool>? _analyzerExceptionFilter;
        private readonly AnalyzerManager _analyzerManager;
        private readonly Func<DiagnosticAnalyzer, bool> _isCompilerAnalyzer;
        private readonly Func<DiagnosticAnalyzer, object?> _getAnalyzerGate;
        private readonly Func<SyntaxTree, SemanticModel> _getSemanticModel;
        private readonly Func<DiagnosticAnalyzer, bool> _shouldSkipAnalysisOnGeneratedCode;
        private readonly Func<Diagnostic, DiagnosticAnalyzer, Compilation, CancellationToken, bool> _shouldSuppressGeneratedCodeDiagnostic;
        private readonly Func<SyntaxTree, TextSpan, CancellationToken, bool> _isGeneratedCodeLocation;
        private readonly Func<DiagnosticAnalyzer, SyntaxTree, SyntaxTreeOptionsProvider?, CancellationToken, bool> _isAnalyzerSuppressedForTree;

        /// <summary>
        /// The values in this map convert to <see cref="TimeSpan"/> using <see cref="TimeSpan.FromTicks(long)"/>.
        /// </summary>
        private readonly ConcurrentDictionary<DiagnosticAnalyzer, StrongBox<long>>? _analyzerExecutionTimeMap;
        private readonly CompilationAnalysisValueProviderFactory _compilationAnalysisValueProviderFactory;

        /// <summary>
        /// Cache of analyzer to analyzer specific options.  If <see langword="null"/> there are no specific
        /// options, and the shared options should be used for all analyzers.  This is the common case, which
        /// means we don't pay for the extra indirection of a dictionary lookup normally.
        /// <para/>
        /// Note: this map is generated 
        /// at construction time, and is unchanging after that point.  So it can be safely read from multiple
        /// threads without need for locks.
        /// </summary>
        private readonly Dictionary<DiagnosticAnalyzer, AnalyzerOptions>? _analyzerToCachedOptions;

        private Func<IOperation, ControlFlowGraph>? _lazyGetControlFlowGraph;

        private ConcurrentDictionary<IOperation, ControlFlowGraph>? _lazyControlFlowGraphMap;

        private Func<IOperation, ControlFlowGraph> GetControlFlowGraph
            => _lazyGetControlFlowGraph ??= GetControlFlowGraphImpl;

        private bool IsAnalyzerSuppressedForTree(DiagnosticAnalyzer analyzer, SyntaxTree tree, CancellationToken cancellationToken)
        {
            return _isAnalyzerSuppressedForTree(analyzer, tree, Compilation.Options.SyntaxTreeOptionsProvider, cancellationToken);
        }

        /// <summary>
        /// Creates <see cref="AnalyzerExecutor"/> to execute analyzer actions with given arguments
        /// </summary>
        /// <param name="compilation">Compilation to be used in the analysis.</param>
        /// <param name="analyzerOptions">Analyzer options.</param>
        /// <param name="addNonCategorizedDiagnostic">Optional delegate to add non-categorized analyzer diagnostics.</param>
        /// <param name="onAnalyzerException">
        /// Delegate which is invoked when an analyzer throws an exception.
        /// Delegate can do custom tasks such as report the given analyzer exception diagnostic, report a non-fatal watson for the exception, etc.
        /// </param>
        /// <param name="analyzerExceptionFilter">
        /// Optional delegate which is invoked when an analyzer throws an exception as an exception filter.
        /// Delegate can do custom tasks such as crash hosting process to create a dump.
        /// </param>
        /// <param name="isCompilerAnalyzer">Delegate to determine if the given analyzer is compiler analyzer. 
        /// We need to special case the compiler analyzer at few places for performance reasons.</param>
        /// <param name="diagnosticAnalyzers">Analyzers to query for custom options if <paramref
        /// name="getAnalyzerConfigOptionsProvider"/> is provided.</param>
        /// <param name="getAnalyzerConfigOptionsProvider">Optional callback to allow individual configuration options
        /// on a per analyzer basis.</param>
        /// <param name="analyzerManager">Analyzer manager to fetch supported diagnostics.</param>
        /// <param name="getAnalyzerGate">
        /// Delegate to fetch the gate object to guard all callbacks into the analyzer.
        /// It should return a unique gate object for the given analyzer instance for non-concurrent analyzers, and null otherwise.
        /// All analyzer callbacks for non-concurrent analyzers will be guarded with a lock on the gate.
        /// </param>
        /// <param name="getSemanticModel">Delegate to get a semantic model for the given syntax tree which can be shared across analyzers.</param>
        /// <param name="severityFilter"><see cref="SeverityFilter"/> for analysis.</param>
        /// <param name="shouldSkipAnalysisOnGeneratedCode">Delegate to identify if analysis should be skipped on generated code.</param>
        /// <param name="shouldSuppressGeneratedCodeDiagnostic">Delegate to identify if diagnostic reported while analyzing generated code should be suppressed.</param>
        /// <param name="isGeneratedCodeLocation">Delegate to identify if the given location is in generated code.</param>
        /// <param name="isAnalyzerSuppressedForTree">Delegate to identify if the given analyzer is suppressed for the given tree.</param>
        /// <param name="logExecutionTime">Flag indicating whether we need to log analyzer execution time.</param>
        /// <param name="addCategorizedLocalDiagnostic">Optional delegate to add categorized local analyzer diagnostics.</param>
        /// <param name="addCategorizedNonLocalDiagnostic">Optional delegate to add categorized non-local analyzer diagnostics.</param>
        /// <param name="addSuppression">Optional thread-safe delegate to add diagnostic suppressions from suppressors.</param>
        public static AnalyzerExecutor Create(
            Compilation compilation,
            AnalyzerOptions analyzerOptions,
            Action<Diagnostic, AnalyzerOptions, CancellationToken>? addNonCategorizedDiagnostic,
            Action<Exception, DiagnosticAnalyzer, Diagnostic, CancellationToken> onAnalyzerException,
            Func<Exception, bool>? analyzerExceptionFilter,
            Func<DiagnosticAnalyzer, bool> isCompilerAnalyzer,
            ImmutableArray<DiagnosticAnalyzer> diagnosticAnalyzers,
            Func<DiagnosticAnalyzer, AnalyzerConfigOptionsProvider>? getAnalyzerConfigOptionsProvider,
            AnalyzerManager analyzerManager,
            Func<DiagnosticAnalyzer, bool> shouldSkipAnalysisOnGeneratedCode,
            Func<Diagnostic, DiagnosticAnalyzer, Compilation, CancellationToken, bool> shouldSuppressGeneratedCodeDiagnostic,
            Func<SyntaxTree, TextSpan, CancellationToken, bool> isGeneratedCodeLocation,
            Func<DiagnosticAnalyzer, SyntaxTree, SyntaxTreeOptionsProvider?, CancellationToken, bool> isAnalyzerSuppressedForTree,
            Func<DiagnosticAnalyzer, object?> getAnalyzerGate,
            Func<SyntaxTree, SemanticModel> getSemanticModel,
            SeverityFilter severityFilter,
            bool logExecutionTime = false,
            Action<Diagnostic, DiagnosticAnalyzer, AnalyzerOptions, bool, CancellationToken>? addCategorizedLocalDiagnostic = null,
            Action<Diagnostic, DiagnosticAnalyzer, AnalyzerOptions, CancellationToken>? addCategorizedNonLocalDiagnostic = null,
            Action<Suppression>? addSuppression = null)
        {
            // We can either report categorized (local/non-local) diagnostics or non-categorized diagnostics.
            Debug.Assert((addNonCategorizedDiagnostic != null) ^ (addCategorizedLocalDiagnostic != null));
            Debug.Assert((addCategorizedLocalDiagnostic != null) == (addCategorizedNonLocalDiagnostic != null));

            var analyzerExecutionTimeMap = logExecutionTime ? new ConcurrentDictionary<DiagnosticAnalyzer, StrongBox<long>>() : null;

            return new AnalyzerExecutor(compilation, analyzerOptions, addNonCategorizedDiagnostic, onAnalyzerException, analyzerExceptionFilter,
                isCompilerAnalyzer, diagnosticAnalyzers, getAnalyzerConfigOptionsProvider, analyzerManager, shouldSkipAnalysisOnGeneratedCode, shouldSuppressGeneratedCodeDiagnostic, isGeneratedCodeLocation,
                isAnalyzerSuppressedForTree, getAnalyzerGate, getSemanticModel, severityFilter, analyzerExecutionTimeMap, addCategorizedLocalDiagnostic, addCategorizedNonLocalDiagnostic,
                addSuppression);
        }

        private AnalyzerExecutor(
            Compilation compilation,
            AnalyzerOptions analyzerOptions,
            Action<Diagnostic, AnalyzerOptions, CancellationToken>? addNonCategorizedDiagnosticOpt,
            Action<Exception, DiagnosticAnalyzer, Diagnostic, CancellationToken> onAnalyzerException,
            Func<Exception, bool>? analyzerExceptionFilter,
            Func<DiagnosticAnalyzer, bool> isCompilerAnalyzer,
            ImmutableArray<DiagnosticAnalyzer> diagnosticAnalyzers,
            Func<DiagnosticAnalyzer, AnalyzerConfigOptionsProvider>? getAnalyzerConfigOptionsProvider,
            AnalyzerManager analyzerManager,
            Func<DiagnosticAnalyzer, bool> shouldSkipAnalysisOnGeneratedCode,
            Func<Diagnostic, DiagnosticAnalyzer, Compilation, CancellationToken, bool> shouldSuppressGeneratedCodeDiagnostic,
            Func<SyntaxTree, TextSpan, CancellationToken, bool> isGeneratedCodeLocation,
            Func<DiagnosticAnalyzer, SyntaxTree, SyntaxTreeOptionsProvider?, CancellationToken, bool> isAnalyzerSuppressedForTree,
            Func<DiagnosticAnalyzer, object?> getAnalyzerGate,
            Func<SyntaxTree, SemanticModel> getSemanticModel,
            SeverityFilter severityFilter,
            ConcurrentDictionary<DiagnosticAnalyzer, StrongBox<long>>? analyzerExecutionTimeMap,
            Action<Diagnostic, DiagnosticAnalyzer, AnalyzerOptions, bool, CancellationToken>? addCategorizedLocalDiagnostic,
            Action<Diagnostic, DiagnosticAnalyzer, AnalyzerOptions, CancellationToken>? addCategorizedNonLocalDiagnostic,
            Action<Suppression>? addSuppression)
        {
            Compilation = compilation;
            AnalyzerOptions = analyzerOptions;
            _addNonCategorizedDiagnostic = addNonCategorizedDiagnosticOpt;
            OnAnalyzerException = onAnalyzerException;
            _analyzerExceptionFilter = analyzerExceptionFilter;
            _isCompilerAnalyzer = isCompilerAnalyzer;
            _analyzerManager = analyzerManager;
            _shouldSkipAnalysisOnGeneratedCode = shouldSkipAnalysisOnGeneratedCode;
            _shouldSuppressGeneratedCodeDiagnostic = shouldSuppressGeneratedCodeDiagnostic;
            _isGeneratedCodeLocation = isGeneratedCodeLocation;
            _isAnalyzerSuppressedForTree = isAnalyzerSuppressedForTree;
            _getAnalyzerGate = getAnalyzerGate;
            _getSemanticModel = getSemanticModel;
            SeverityFilter = severityFilter;
            _analyzerExecutionTimeMap = analyzerExecutionTimeMap;
            _addCategorizedLocalDiagnostic = addCategorizedLocalDiagnostic;
            _addCategorizedNonLocalDiagnostic = addCategorizedNonLocalDiagnostic;
            _addSuppression = addSuppression;

            _compilationAnalysisValueProviderFactory = new CompilationAnalysisValueProviderFactory();

            if (getAnalyzerConfigOptionsProvider != null)
            {
                var hasDifferentOptions = false;

                var map = new Dictionary<DiagnosticAnalyzer, AnalyzerOptions>(
                    capacity: diagnosticAnalyzers.Length, ReferenceEqualityComparer.Instance);

                // Deduping map for the distinct AnalyzerConfigOptionsProvider we get back from getAnalyzerConfigOptionsProvider.
                // The common case in VS host is that there is generally only 1-2 of these providers.  For example, a provider
                // that looks in editorconfig+vsoptions, and a provider that only looks in editorconfig.  We only want to make
                // a corresponding number of AnalyzerOptions instances for each unique provider we see.
                var optionsProviderToOptions = new Dictionary<AnalyzerConfigOptionsProvider, AnalyzerOptions>(ReferenceEqualityComparer.Instance);

                foreach (var analyzer in diagnosticAnalyzers)
                {
                    var specificOptionsProvider = getAnalyzerConfigOptionsProvider(analyzer);
                    var specificOptions = optionsProviderToOptions.GetOrAdd(
                        specificOptionsProvider, () => analyzerOptions.WithAnalyzerConfigOptionsProvider(specificOptionsProvider));
                    map[analyzer] = specificOptions;

                    if (specificOptions != analyzerOptions)
                        hasDifferentOptions = true;
                }

                // Only if there is at least one analyzer with specific options, we need to maintain the map.
                // Otherwise, we can just toss it and use the shared options for all analyzers.
                if (hasDifferentOptions)
                    _analyzerToCachedOptions = map;
            }
        }

        internal Compilation Compilation { get; }

        internal AnalyzerOptions AnalyzerOptions { get; }

        internal SeverityFilter SeverityFilter { get; }

        internal Action<Exception, DiagnosticAnalyzer, Diagnostic, CancellationToken> OnAnalyzerException { get; }

        internal ImmutableDictionary<DiagnosticAnalyzer, TimeSpan> AnalyzerExecutionTimes
        {
            get
            {
                Debug.Assert(_analyzerExecutionTimeMap != null);
                return _analyzerExecutionTimeMap.ToImmutableDictionary(pair => pair.Key, pair => TimeSpan.FromTicks(pair.Value.Value));
            }
        }

        /// <summary>
        /// Executes the <see cref="DiagnosticAnalyzer.Initialize(AnalysisContext)"/> for the given analyzer.
        /// </summary>
        /// <param name="sessionScope">Session scope to store register session wide analyzer actions.</param>
        /// <param name="severityFilter">Severity filter for analysis.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <remarks>
        /// Note that this API doesn't execute any <see cref="CompilationStartAnalyzerAction"/> registered by the Initialize invocation.
        /// Use <see cref="ExecuteCompilationStartActions(ImmutableArray{CompilationStartAnalyzerAction}, HostCompilationStartAnalysisScope, CancellationToken)"/> API
        /// to get execute these actions to get the per-compilation analyzer actions.
        /// </remarks>
        public void ExecuteInitializeMethod(HostSessionStartAnalysisScope sessionScope, SeverityFilter severityFilter, CancellationToken cancellationToken)
        {
            var context = new AnalyzerAnalysisContext(sessionScope, severityFilter);

            ExecuteAndCatchIfThrows(
                sessionScope.Analyzer,
                static data => data.sessionScope.Analyzer.Initialize(data.context),
                (sessionScope, context),
                contextInfo: null,
                cancellationToken);
        }

        /// <summary>
        /// Executes the compilation start actions.
        /// </summary>
        /// <param name="actions"><see cref="AnalyzerActions"/> whose compilation start actions are to be executed.</param>
        /// <param name="compilationScope">Compilation scope to store the analyzer actions.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public void ExecuteCompilationStartActions(ImmutableArray<CompilationStartAnalyzerAction> actions, HostCompilationStartAnalysisScope compilationScope, CancellationToken cancellationToken)
        {
            // This context doesn't build up any state as we pass it to the Action method of the analyzer. As such, we
            // can use the same instance across all actions as long as the same options are picked for each analyzer.
            var context = new AnalyzerCompilationStartAnalysisContext(compilationScope,
                Compilation, AnalyzerOptions, _compilationAnalysisValueProviderFactory, cancellationToken);
            var contextInfo = new AnalysisContextInfo(Compilation);

            foreach (var startAction in actions)
            {
                // See if we need to use an analyzer specific options instance.
                context = WithAnalyzerSpecificOptions(
                    context, startAction.Analyzer, static (context, options) => context.WithOptions(options));

                ExecuteAndCatchIfThrows(
                    startAction.Analyzer,
                    static data => data.startAction.Action(data.context),
                    (startAction, context),
                    contextInfo,
                    cancellationToken);
            }
        }

        private TAnalysisContext WithAnalyzerSpecificOptions<TAnalysisContext>(
            TAnalysisContext context,
            DiagnosticAnalyzer analyzer,
            Func<TAnalysisContext, AnalyzerOptions, TAnalysisContext> withOptions)
        {
            // No specific options factory.  Can use the shared context.
            if (_analyzerToCachedOptions is null)
                return context;

            return withOptions(context, GetAnalyzerSpecificOptions(analyzer));
        }

        /// <summary>
        /// Given an analyzer, returns any specific options for it, or the shared options if none.
        /// </summary>
        private AnalyzerOptions GetAnalyzerSpecificOptions(DiagnosticAnalyzer analyzer)
            => _analyzerToCachedOptions?[analyzer] ?? AnalyzerOptions;

        /// <summary>
        /// Executes the symbol start actions.
        /// </summary>
        /// <param name="symbol">Symbol whose symbol start actions are to be executed.</param>
        /// <param name="actions"><see cref="AnalyzerActions"/> whose symbol start actions are to be executed.</param>
        /// <param name="symbolScope">Symbol scope to store the analyzer actions.</param>
        /// <param name="isGeneratedCodeSymbol">Flag indicating if the symbol being analyzed is generated code.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public void ExecuteSymbolStartActions(
            ISymbol symbol,
            ImmutableArray<SymbolStartAnalyzerAction> actions,
            HostSymbolStartAnalysisScope symbolScope,
            bool isGeneratedCodeSymbol,
            SyntaxTree? filterTree,
            TextSpan? filterSpan,
            CancellationToken cancellationToken)
        {
            if (isGeneratedCodeSymbol && _shouldSkipAnalysisOnGeneratedCode(symbolScope.Analyzer) ||
                IsAnalyzerSuppressedForSymbol(symbolScope.Analyzer, symbol, cancellationToken))
            {
                return;
            }

            // This context doesn't build up any state as we pass it to the Action method of the analyzer. As such, we
            // can use the same instance across all actions (as long as the options stay the same per analyzer).
            var context = new AnalyzerSymbolStartAnalysisContext(symbolScope,
                symbol, Compilation, AnalyzerOptions, isGeneratedCodeSymbol, filterTree, filterSpan, cancellationToken);
            var contextInfo = new AnalysisContextInfo(Compilation, symbol);

            foreach (var startAction in actions)
            {
                Debug.Assert(startAction.Analyzer == symbolScope.Analyzer);

                // See if we need to use an analyzer specific options instance.
                context = WithAnalyzerSpecificOptions(
                    context, startAction.Analyzer, static (context, options) => context.WithOptions(options));

                ExecuteAndCatchIfThrows(
                    startAction.Analyzer,
                    static data => data.startAction.Action(data.context),
                    (startAction, context),
                    contextInfo,
                    cancellationToken);
            }
        }

        /// <summary>
        /// Executes the given diagnostic suppressor.
        /// </summary>
        /// <param name="suppressor">Suppressor to be executed.</param>
        /// <param name="reportedDiagnostics">Reported analyzer/compiler diagnostics that can be suppressed.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public void ExecuteSuppressionAction(DiagnosticSuppressor suppressor, ImmutableArray<Diagnostic> reportedDiagnostics, CancellationToken cancellationToken)
        {
            Debug.Assert(_addSuppression != null);

            if (reportedDiagnostics.IsEmpty)
            {
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var supportedSuppressions = _analyzerManager.GetSupportedSuppressionDescriptors(suppressor, this, cancellationToken);

            using var _ = PooledDelegates.GetPooledFunction(
                static (d, supportedSuppressions) => supportedSuppressions.Contains(d),
                supportedSuppressions,
                out Func<SuppressionDescriptor, bool> isSupportedSuppression);

            var options = GetAnalyzerSpecificOptions(suppressor);
            var context = new SuppressionAnalysisContext(Compilation, options,
                reportedDiagnostics, _addSuppression, isSupportedSuppression, _getSemanticModel, cancellationToken);

            ExecuteAndCatchIfThrows(
                suppressor,
                static data => data.suppressor.ReportSuppressions(data.context),
                (suppressor, context),
                new AnalysisContextInfo(Compilation),
                cancellationToken);
        }

        /// <summary>
        /// Executes compilation actions or compilation end actions.
        /// </summary>
        /// <param name="compilationActions">Compilation actions to be executed.</param>
        /// <param name="analyzer">Analyzer whose actions are to be executed.</param>
        /// <param name="compilationEvent">Compilation event.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public void ExecuteCompilationActions(
            ImmutableArray<CompilationAnalyzerAction> compilationActions,
            DiagnosticAnalyzer analyzer,
            CompilationEvent compilationEvent,
            CancellationToken cancellationToken)
        {
            Debug.Assert(compilationEvent is CompilationStartedEvent || compilationEvent is CompilationCompletedEvent);

            var analyzerOptions = this.GetAnalyzerSpecificOptions(analyzer);
            var addDiagnostic = GetAddCompilationDiagnostic(analyzer, analyzerOptions, cancellationToken);

            using var _ = PooledDelegates.GetPooledFunction(
                static (d, ct, arg) => arg.self.IsSupportedDiagnostic(arg.analyzer, d, ct),
                (self: this, analyzer),
                out Func<Diagnostic, CancellationToken, bool> isSupportedDiagnostic);

            // This context doesn't build up any state as we pass it to the Action method of the analyzer. As such, we
            // can use the same instance across all actions.
            var context = new CompilationAnalysisContext(
                Compilation, analyzerOptions, addDiagnostic,
                isSupportedDiagnostic, _compilationAnalysisValueProviderFactory, cancellationToken);
            var contextInfo = new AnalysisContextInfo(Compilation);

            foreach (var endAction in compilationActions)
            {
                ExecuteAndCatchIfThrows(
                    endAction.Analyzer,
                    static data => data.endAction.Action(data.context),
                    (endAction, context),
                    contextInfo,
                    cancellationToken);
            }
        }

        /// <summary>
        /// Execute the symbol actions on the given symbol.
        /// </summary>
        /// <param name="symbolActions">Symbol actions to be executed.</param>
        /// <param name="analyzer">Analyzer whose actions are to be executed.</param>
        /// <param name="symbolDeclaredEvent">Symbol event to be analyzed.</param>
        /// <param name="getTopMostNodeForAnalysis">Delegate to get topmost declaration node for a symbol declaration reference.</param>
        /// <param name="isGeneratedCodeSymbol">Flag indicating if this is a generated code symbol.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public void ExecuteSymbolActions(
            ImmutableArray<SymbolAnalyzerAction> symbolActions,
            DiagnosticAnalyzer analyzer,
            SymbolDeclaredCompilationEvent symbolDeclaredEvent,
            Func<ISymbol, SyntaxReference, Compilation, CancellationToken, SyntaxNode> getTopMostNodeForAnalysis,
            bool isGeneratedCodeSymbol,
            SyntaxTree? filterTree,
            TextSpan? filterSpan,
            CancellationToken cancellationToken)
        {
            Debug.Assert(getTopMostNodeForAnalysis != null);
            Debug.Assert(!filterSpan.HasValue || filterTree != null);

            if (isGeneratedCodeSymbol && _shouldSkipAnalysisOnGeneratedCode(analyzer) ||
                IsAnalyzerSuppressedForSymbol(analyzer, symbolDeclaredEvent.Symbol, cancellationToken))
            {
                return;
            }

            var symbol = symbolDeclaredEvent.Symbol;
            var analyzerOptions = this.GetAnalyzerSpecificOptions(analyzer);

            using var _1 = PooledDelegates.GetPooledAction(
                static (diagnostic, tuple) =>
                {
                    var (self, analyzer, symbolDeclaredEvent, analyzerOptions, getTopMostNodeForAnalysis, cancellationToken) = tuple;
                    tuple.self.AddSymbolDiagnostic(symbolDeclaredEvent, diagnostic, analyzer, analyzerOptions, getTopMostNodeForAnalysis, cancellationToken);
                },
                (self: this, analyzer, symbolDeclaredEvent, analyzerOptions, getTopMostNodeForAnalysis, cancellationToken),
                out Action<Diagnostic> addDiagnostic);

            using var _2 = PooledDelegates.GetPooledFunction(
                static (d, ct, arg) => arg.self.IsSupportedDiagnostic(arg.analyzer, d, ct),
                (self: this, analyzer),
                out Func<Diagnostic, CancellationToken, bool> isSupportedDiagnostic);

            // This context doesn't build up any state as we pass it to the Action method of the analyzer. As such, we
            // can use the same instance across all actions.
            var context = new SymbolAnalysisContext(
                symbol, Compilation, analyzerOptions, addDiagnostic,
                isSupportedDiagnostic, isGeneratedCodeSymbol, filterTree,
                filterSpan, cancellationToken);
            var contextInfo = new AnalysisContextInfo(Compilation, symbol);

            foreach (var symbolAction in symbolActions)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (symbolAction.Kinds.Contains(symbol.Kind))
                {
                    ExecuteAndCatchIfThrows(
                        symbolAction.Analyzer,
                        static data => data.symbolAction.Action(data.context),
                        (symbolAction, context),
                        contextInfo,
                        cancellationToken);
                }
            }
        }

        /// <summary>
        /// Execute the symbol end actions on the given namespace or type containing symbol for the process member symbol for the given analyzer.
        /// </summary>
        /// <param name="containingSymbol">Symbol whose actions are to be executed.</param>
        /// <param name="processedMemberSymbol">Completed member symbol.</param>
        /// <param name="analyzer">Analyzer whose actions are to be executed.</param>
        /// <param name="getTopMostNodeForAnalysis">Delegate to get topmost declaration node for a symbol declaration reference.</param>
        /// <param name="filterSpan">Optional filter span for analysis.</param>
        /// <param name="isGeneratedCode">Flag indicating if the containing symbol being analyzed is generated code.</param>
        public bool TryExecuteSymbolEndActionsForContainer(
            INamespaceOrTypeSymbol containingSymbol,
            ISymbol processedMemberSymbol,
            DiagnosticAnalyzer analyzer,
            Func<ISymbol, SyntaxReference, Compilation, CancellationToken, SyntaxNode> getTopMostNodeForAnalysis,
            bool isGeneratedCode,
            SyntaxTree? filterTree,
            TextSpan? filterSpan,
            CancellationToken cancellationToken,
            [NotNullWhen(returnValue: true)] out SymbolDeclaredCompilationEvent? containingSymbolDeclaredEvent)
        {
            containingSymbolDeclaredEvent = null;
            if (!_analyzerManager.TryProcessCompletedMemberAndGetPendingSymbolEndActionsForContainer(containingSymbol, processedMemberSymbol, analyzer, out var containerEndActionsAndEvent))
            {
                return false;
            }

            ImmutableArray<SymbolEndAnalyzerAction> endActions = containerEndActionsAndEvent.symbolEndActions;
            containingSymbolDeclaredEvent = containerEndActionsAndEvent.symbolDeclaredEvent;
            ExecuteSymbolEndActionsCore(endActions, analyzer, containingSymbolDeclaredEvent, getTopMostNodeForAnalysis, isGeneratedCode, filterTree, filterSpan, cancellationToken);
            return true;
        }

        /// <summary>
        /// Tries to execute the symbol end actions on the given symbol for the given analyzer.
        /// </summary>
        /// <param name="symbolEndActions">Symbol actions to be executed.</param>
        /// <param name="analyzer">Analyzer whose actions are to be executed.</param>
        /// <param name="symbolDeclaredEvent">Symbol event to be analyzed.</param>
        /// <param name="getTopMostNodeForAnalysis">Delegate to get topmost declaration node for a symbol declaration reference.</param>
        /// <param name="filterSpan">Optional filter span for analysis.</param>
        /// <param name="isGeneratedCode">Flag indicating if the symbol being analyzed is generated code.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>
        /// True, if successfully executed the actions for the given analysis scope OR all the actions have already been executed for the given analysis scope.
        /// False, if there are some pending actions.
        /// </returns>
        public bool TryExecuteSymbolEndActions(
            ImmutableArray<SymbolEndAnalyzerAction> symbolEndActions,
            DiagnosticAnalyzer analyzer,
            SymbolDeclaredCompilationEvent symbolDeclaredEvent,
            Func<ISymbol, SyntaxReference, Compilation, CancellationToken, SyntaxNode> getTopMostNodeForAnalysis,
            bool isGeneratedCode,
            SyntaxTree? filterTree,
            TextSpan? filterSpan,
            CancellationToken cancellationToken)
        {
            if (!_analyzerManager.TryStartExecuteSymbolEndActions(symbolEndActions, analyzer, symbolDeclaredEvent))
                return false;

            ExecuteSymbolEndActionsCore(symbolEndActions, analyzer, symbolDeclaredEvent, getTopMostNodeForAnalysis, isGeneratedCode, filterTree, filterSpan, cancellationToken);
            return true;
        }

        private void ExecuteSymbolEndActionsCore(
            ImmutableArray<SymbolEndAnalyzerAction> symbolEndActions,
            DiagnosticAnalyzer analyzer,
            SymbolDeclaredCompilationEvent symbolDeclaredEvent,
            Func<ISymbol, SyntaxReference, Compilation, CancellationToken, SyntaxNode> getTopMostNodeForAnalysis,
            bool isGeneratedCode,
            SyntaxTree? filterTree,
            TextSpan? filterSpan,
            CancellationToken cancellationToken)
        {
            Debug.Assert(getTopMostNodeForAnalysis != null);
            Debug.Assert(!isGeneratedCode || !_shouldSkipAnalysisOnGeneratedCode(analyzer));
            Debug.Assert(!IsAnalyzerSuppressedForSymbol(analyzer, symbolDeclaredEvent.Symbol, cancellationToken));
            Debug.Assert(!filterSpan.HasValue || filterTree != null);

            var symbol = symbolDeclaredEvent.Symbol;
            var analyzerOptions = this.GetAnalyzerSpecificOptions(analyzer);

            using var _1 = PooledDelegates.GetPooledAction(
                static (diagnostic, tuple) =>
                {
                    var (self, analyzer, symbolDeclaredEvent, analyzerOptions, getTopMostNodeForAnalysis, cancellationToken) = tuple;
                    tuple.self.AddSymbolDiagnostic(symbolDeclaredEvent, diagnostic, analyzer, analyzerOptions, getTopMostNodeForAnalysis, cancellationToken);
                },
                (self: this, analyzer, symbolDeclaredEvent, analyzerOptions, getTopMostNodeForAnalysis, cancellationToken),
                out Action<Diagnostic> addDiagnostic);

            using var _2 = PooledDelegates.GetPooledFunction(
                static (d, ct, arg) => arg.self.IsSupportedDiagnostic(arg.analyzer, d, ct),
                (self: this, analyzer),
                out Func<Diagnostic, CancellationToken, bool> isSupportedDiagnostic);

            // This context doesn't build up any state as we pass it to the Action method of the analyzer. As such, we
            // can use the same instance across all actions.
            var context = new SymbolAnalysisContext(
                symbol, Compilation, analyzerOptions, addDiagnostic,
                isSupportedDiagnostic, isGeneratedCode, filterTree, filterSpan, cancellationToken);
            var contextInfo = new AnalysisContextInfo(Compilation, symbol);

            foreach (var symbolAction in symbolEndActions)
            {
                ExecuteAndCatchIfThrows(
                    symbolAction.Analyzer,
                    static data => data.symbolAction.Action(data.context),
                    (symbolAction, context),
                    contextInfo,
                    cancellationToken);
            }

            _analyzerManager.MarkSymbolEndAnalysisComplete(symbol, analyzer);
        }

        /// <summary>
        /// Execute the semantic model actions on the given semantic model.
        /// </summary>
        /// <param name="semanticModelActions">Semantic model actions to be executed.</param>
        /// <param name="analyzer">Analyzer whose actions are to be executed.</param>
        /// <param name="semanticModel">Semantic model to analyze.</param>
        /// <param name="filterSpan">Optional filter span for analysis.</param>
        /// <param name="isGeneratedCode">Flag indicating if the syntax tree being analyzed is generated code.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public void ExecuteSemanticModelActions(
            ImmutableArray<SemanticModelAnalyzerAction> semanticModelActions,
            DiagnosticAnalyzer analyzer,
            SemanticModel semanticModel,
            TextSpan? filterSpan,
            bool isGeneratedCode,
            CancellationToken cancellationToken)
        {
            if (isGeneratedCode && _shouldSkipAnalysisOnGeneratedCode(analyzer) ||
                IsAnalyzerSuppressedForTree(analyzer, semanticModel.SyntaxTree, cancellationToken))
            {
                return;
            }

            var analyzerOptions = this.GetAnalyzerSpecificOptions(analyzer);
            var diagReporter = GetAddSemanticDiagnostic(semanticModel.SyntaxTree, analyzer, analyzerOptions, cancellationToken);

            using var _ = PooledDelegates.GetPooledFunction(
                static (d, ct, arg) => arg.self.IsSupportedDiagnostic(arg.analyzer, d, ct),
                (self: this, analyzer),
                out Func<Diagnostic, CancellationToken, bool> isSupportedDiagnostic);

            // This context doesn't build up any state as we pass it to the Action method of the analyzer. As such, we
            // can use the same instance across all actions.
            var context = new SemanticModelAnalysisContext(
                semanticModel, analyzerOptions, diagReporter.AddDiagnosticAction,
                isSupportedDiagnostic, filterSpan, isGeneratedCode, cancellationToken);
            var contextInfo = new AnalysisContextInfo(semanticModel);

            foreach (var semanticModelAction in semanticModelActions)
            {
                ExecuteAndCatchIfThrows(
                    semanticModelAction.Analyzer,
                    static data => data.semanticModelAction.Action(data.context),
                    (semanticModelAction, context),
                    contextInfo,
                    cancellationToken);
            }

            diagReporter.Free();
        }

        /// <summary>
        /// Execute the syntax tree actions on the given syntax tree.
        /// </summary>
        /// <param name="syntaxTreeActions">Syntax tree actions to be executed.</param>
        /// <param name="analyzer">Analyzer whose actions are to be executed.</param>
        /// <param name="file">Syntax tree to analyze.</param>
        /// <param name="filterSpan">Optional filter span within the <paramref name="file"/> for analysis.</param>
        /// <param name="isGeneratedCode">Flag indicating if the syntax tree being analyzed is generated code.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public void ExecuteSyntaxTreeActions(
            ImmutableArray<SyntaxTreeAnalyzerAction> syntaxTreeActions,
            DiagnosticAnalyzer analyzer,
            SourceOrAdditionalFile file,
            TextSpan? filterSpan,
            bool isGeneratedCode,
            CancellationToken cancellationToken)
        {
            Debug.Assert(file.SourceTree != null);

            var tree = file.SourceTree;
            if (isGeneratedCode && _shouldSkipAnalysisOnGeneratedCode(analyzer) ||
                IsAnalyzerSuppressedForTree(analyzer, tree, cancellationToken))
            {
                return;
            }

            var analyzerOptions = this.GetAnalyzerSpecificOptions(analyzer);
            var diagReporter = GetAddSyntaxDiagnostic(file, analyzer, analyzerOptions, cancellationToken);

            using var _ = PooledDelegates.GetPooledFunction(
                static (d, ct, arg) => arg.self.IsSupportedDiagnostic(arg.analyzer, d, ct),
                (self: this, analyzer),
                out Func<Diagnostic, CancellationToken, bool> isSupportedDiagnostic);

            // This context doesn't build up any state as we pass it to the Action method of the analyzer. As such, we
            // can use the same instance across all actions.
            var context = new SyntaxTreeAnalysisContext(
                tree, analyzerOptions, diagReporter.AddDiagnosticAction, isSupportedDiagnostic,
                Compilation, filterSpan, isGeneratedCode, cancellationToken);
            var contextInfo = new AnalysisContextInfo(Compilation, file);

            foreach (var syntaxTreeAction in syntaxTreeActions)
            {
                ExecuteAndCatchIfThrows(
                    syntaxTreeAction.Analyzer,
                    static data => data.syntaxTreeAction.Action(data.context),
                    (syntaxTreeAction, context),
                    contextInfo,
                    cancellationToken);
            }

            diagReporter.Free();
        }

        /// <summary>
        /// Execute the additional file actions.
        /// </summary>
        /// <param name="additionalFileActions">Actions to be executed.</param>
        /// <param name="analyzer">Analyzer whose actions are to be executed.</param>
        /// <param name="file">Additional file to analyze.</param>
        /// <param name="filterSpan">Optional filter span within the <paramref name="file"/> for analysis.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public void ExecuteAdditionalFileActions(
            ImmutableArray<AdditionalFileAnalyzerAction> additionalFileActions,
            DiagnosticAnalyzer analyzer,
            SourceOrAdditionalFile file,
            TextSpan? filterSpan,
            CancellationToken cancellationToken)
        {
            Debug.Assert(file.AdditionalFile != null);
            var additionalFile = file.AdditionalFile;

            var analyzerOptions = this.GetAnalyzerSpecificOptions(analyzer);
            var diagReporter = GetAddSyntaxDiagnostic(file, analyzer, analyzerOptions, cancellationToken);

            using var _ = PooledDelegates.GetPooledFunction(
                static (d, ct, arg) => arg.self.IsSupportedDiagnostic(arg.analyzer, d, ct),
                (self: this, analyzer),
                out Func<Diagnostic, CancellationToken, bool> isSupportedDiagnostic);

            // This context doesn't build up any state as we pass it to the Action method of the analyzer. As such, we
            // can use the same instance across all actions.
            var context = new AdditionalFileAnalysisContext(
                additionalFile, analyzerOptions, diagReporter.AddDiagnosticAction, isSupportedDiagnostic,
                Compilation, filterSpan, cancellationToken);
            var contextInfo = new AnalysisContextInfo(Compilation, file);

            foreach (var additionalFileAction in additionalFileActions)
            {
                ExecuteAndCatchIfThrows(
                    additionalFileAction.Analyzer,
                    static data => data.additionalFileAction.Action(data.context),
                    (additionalFileAction, context),
                    contextInfo,
                    cancellationToken);
            }

            diagReporter.Free();
        }

        private void ExecuteSyntaxNodeAction<TLanguageKindEnum>(
            SyntaxNodeAnalyzerAction<TLanguageKindEnum> syntaxNodeAction,
            SyntaxNode node,
            ExecutionData executionData,
            Action<Diagnostic> addDiagnostic,
            Func<Diagnostic, CancellationToken, bool> isSupportedDiagnostic,
            CancellationToken cancellationToken)
            where TLanguageKindEnum : struct
        {
            Debug.Assert(!executionData.IsGeneratedCode || !_shouldSkipAnalysisOnGeneratedCode(syntaxNodeAction.Analyzer));
            Debug.Assert(!IsAnalyzerSuppressedForTree(syntaxNodeAction.Analyzer, node.SyntaxTree, cancellationToken));

            var syntaxNodeContext = new SyntaxNodeAnalysisContext(
                node, executionData.DeclaredSymbol, executionData.SemanticModel,
                GetAnalyzerSpecificOptions(syntaxNodeAction.Analyzer), addDiagnostic,
                isSupportedDiagnostic, executionData.FilterSpan, executionData.IsGeneratedCode, cancellationToken);

            ExecuteAndCatchIfThrows(
                syntaxNodeAction.Analyzer,
                static data => data.syntaxNodeAction.Action(data.syntaxNodeContext),
                (syntaxNodeAction, syntaxNodeContext),
                new AnalysisContextInfo(Compilation, node),
                cancellationToken);
        }

        private void ExecuteOperationAction(
            OperationAnalyzerAction operationAction,
            IOperation operation,
            ExecutionData executionData,
            Action<Diagnostic> addDiagnostic,
            Func<Diagnostic, CancellationToken, bool> isSupportedDiagnostic,
            CancellationToken cancellationToken)
        {
            Debug.Assert(!executionData.IsGeneratedCode || !_shouldSkipAnalysisOnGeneratedCode(operationAction.Analyzer));
            Debug.Assert(!IsAnalyzerSuppressedForTree(operationAction.Analyzer, executionData.SemanticModel.SyntaxTree, cancellationToken));

            var operationContext = new OperationAnalysisContext(
                operation, executionData.DeclaredSymbol, executionData.SemanticModel.Compilation,
                GetAnalyzerSpecificOptions(operationAction.Analyzer), addDiagnostic, isSupportedDiagnostic, GetControlFlowGraph,
                executionData.FilterSpan, executionData.IsGeneratedCode, cancellationToken);

            ExecuteAndCatchIfThrows(
                operationAction.Analyzer,
                static data => data.operationAction.Action(data.operationContext),
                (operationAction, operationContext),
                new AnalysisContextInfo(Compilation, operation),
                cancellationToken);
        }

        private readonly struct ExecutionData(
            DiagnosticAnalyzer analyzer,
            AnalyzerOptions analyzerOptions,
            ISymbol declaredSymbol,
            SemanticModel semanticModel,
            TextSpan? filterSpan,
            bool isGeneratedCode)
        {
            public readonly DiagnosticAnalyzer Analyzer = analyzer;
            public readonly AnalyzerOptions AnalyzerOptions = analyzerOptions;
            public readonly ISymbol DeclaredSymbol = declaredSymbol;
            public readonly SemanticModel SemanticModel = semanticModel;
            public readonly TextSpan? FilterSpan = filterSpan;
            public readonly bool IsGeneratedCode = isGeneratedCode;
        }

        /// <summary>
        /// Execute code block actions for the given analyzer for the given declaration.
        /// </summary>
        public void ExecuteCodeBlockActions<TLanguageKindEnum>(
            ImmutableArray<CodeBlockStartAnalyzerAction<TLanguageKindEnum>> startActions,
            ImmutableArray<CodeBlockAnalyzerAction> actions,
            ImmutableArray<CodeBlockAnalyzerAction> endActions,
            DiagnosticAnalyzer analyzer,
            SyntaxNode declaredNode,
            ISymbol declaredSymbol,
            ImmutableArray<SyntaxNode> executableCodeBlocks,
            SemanticModel semanticModel,
            Func<SyntaxNode, TLanguageKindEnum> getKind,
            TextSpan? filterSpan,
            bool isGeneratedCode,
            CancellationToken cancellationToken)
            where TLanguageKindEnum : struct
        {
            Debug.Assert(!executableCodeBlocks.IsEmpty);

            // The actions we discover in 'addActions' and then execute in 'executeActions'.
            var ephemeralActions = ArrayBuilder<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>.GetInstance();
            var analyzerOptions = this.GetAnalyzerSpecificOptions(analyzer);
            ExecuteBlockActionsCore(
                startActions,
                actions,
                endActions,
                declaredNode,
                new ExecutionData(analyzer, analyzerOptions, declaredSymbol, semanticModel, filterSpan, isGeneratedCode),
                addActions: static (startAction, endActions, executionData, args, cancellationToken) =>
                {
                    var (@this, startActions, executableCodeBlocks, declaredNode, getKind, ephemeralActions) = args;

                    var scope = new HostCodeBlockStartAnalysisScope<TLanguageKindEnum>(startAction.Analyzer);
                    var startContext = new AnalyzerCodeBlockStartAnalysisContext<TLanguageKindEnum>(
                        scope, declaredNode, executionData.DeclaredSymbol, executionData.SemanticModel,
                        executionData.AnalyzerOptions, executionData.FilterSpan, executionData.IsGeneratedCode, cancellationToken);

                    // Catch Exception from the start action.
                    @this.ExecuteAndCatchIfThrows(
                        startAction.Analyzer,
                        static args => args.startAction.Action(args.startContext),
                        argument: (startAction, startContext),
                        new AnalysisContextInfo(@this.Compilation, executionData.DeclaredSymbol, declaredNode),
                        cancellationToken);

                    endActions.AddAll(scope.CodeBlockEndActions);
                    ephemeralActions.AddRange(scope.SyntaxNodeActions);
                },
                executeActions: static (diagReporter, isSupportedDiagnostic, executionData, args, cancellationToken) =>
                {
                    var (@this, startActions, executableCodeBlocks, declaredNode, getKind, ephemeralActions) = args;
                    if (ephemeralActions.Any())
                    {
                        var executableNodeActionsByKind = GetNodeActionsByKind(ephemeralActions);
                        var syntaxNodesToAnalyze = ArrayBuilder<SyntaxNode>.GetInstance();

                        foreach (var block in executableCodeBlocks)
                        {
                            var filter = executionData.SemanticModel.GetSyntaxNodesToAnalyzeFilter(block, executionData.DeclaredSymbol);
                            if (filter is not null)
                            {
                                foreach (var descendantNode in block.DescendantNodesAndSelf(descendIntoChildren: filter))
                                {
                                    if (filter(descendantNode))
                                        syntaxNodesToAnalyze.Add(descendantNode);
                                }
                            }
                            else
                            {
                                syntaxNodesToAnalyze.AddRange(block.DescendantNodesAndSelf());
                            }
                        }

                        @this.ExecuteSyntaxNodeActions(
                            syntaxNodesToAnalyze, executableNodeActionsByKind, executionData,
                            getKind, diagReporter, isSupportedDiagnostic,
                            hasCodeBlockStartOrSymbolStartActions: startActions.Any(),
                            cancellationToken);
                        syntaxNodesToAnalyze.Free();
                    }
                },
                executeBlockActions: static (blockActions, diagReporter, isSupportedDiagnostic, executionData, args, cancellationToken) =>
                {
                    var (@this, startActions, executableCodeBlocks, declaredNode, getKind, ephemeralActions) = args;

                    var context = new CodeBlockAnalysisContext(declaredNode, executionData.DeclaredSymbol, executionData.SemanticModel,
                        executionData.AnalyzerOptions, diagReporter.AddDiagnosticAction, isSupportedDiagnostic, executionData.FilterSpan, executionData.IsGeneratedCode, cancellationToken);

                    foreach (var blockAction in blockActions)
                    {
                        @this.ExecuteAndCatchIfThrows(
                            blockAction.Analyzer,
                            static data => data.blockAction.Action(data.context),
                            (blockAction, context),
                            new AnalysisContextInfo(@this.Compilation, executionData.DeclaredSymbol, declaredNode),
                            cancellationToken);
                    }
                },
                argument: (@this: this, startActions, executableCodeBlocks, declaredNode, getKind, ephemeralActions),
                cancellationToken);
            ephemeralActions.Free();
        }

        /// <summary>
        /// Execute operation block actions for the given analyzer for the given declaration.
        /// </summary>
        public void ExecuteOperationBlockActions(
            ImmutableArray<OperationBlockStartAnalyzerAction> startActions,
            ImmutableArray<OperationBlockAnalyzerAction> actions,
            ImmutableArray<OperationBlockAnalyzerAction> endActions,
            DiagnosticAnalyzer analyzer,
            SyntaxNode declaredNode,
            ISymbol declaredSymbol,
            ImmutableArray<IOperation> operationBlocks,
            ImmutableArray<IOperation> operations,
            SemanticModel semanticModel,
            TextSpan? filterSpan,
            bool isGeneratedCode,
            CancellationToken cancellationToken)
        {
            Debug.Assert(!operationBlocks.IsEmpty);

            // The actions we discover in 'addActions' and then execute in 'executeActions'.
            var ephemeralActions = ArrayBuilder<OperationAnalyzerAction>.GetInstance();

            var analyzerOptions = this.GetAnalyzerSpecificOptions(analyzer);
            ExecuteBlockActionsCore(
                startActions,
                actions,
                endActions,
                declaredNode,
                new ExecutionData(analyzer, analyzerOptions, declaredSymbol, semanticModel, filterSpan, isGeneratedCode),
                addActions: static (startAction, endActions, executionData, args, cancellationToken) =>
                {
                    var (@this, startActions, declaredNode, operationBlocks, operations, ephemeralActions) = args;
                    var scope = new HostOperationBlockStartAnalysisScope(startAction.Analyzer);
                    var startContext = new AnalyzerOperationBlockStartAnalysisContext(
                        scope, operationBlocks, executionData.DeclaredSymbol, executionData.SemanticModel.Compilation, executionData.AnalyzerOptions,
                        @this.GetControlFlowGraph, declaredNode.SyntaxTree, executionData.FilterSpan, executionData.IsGeneratedCode, cancellationToken);

                    // Catch Exception from the start action.
                    @this.ExecuteAndCatchIfThrows(
                        startAction.Analyzer,
                        static args => args.startAction.Action(args.startContext),
                        argument: (startAction, startContext),
                        new AnalysisContextInfo(@this.Compilation, executionData.DeclaredSymbol),
                        cancellationToken);

                    endActions.AddAll(scope.OperationBlockEndActions);
                    ephemeralActions.AddRange(scope.OperationActions);
                },
                executeActions: static (diagReporter, isSupportedDiagnostic, executionData, args, cancellationToken) =>
                {
                    var (@this, startActions, declaredNode, operationBlocks, operations, ephemeralActions) = args;
                    if (ephemeralActions.Any())
                    {
                        @this.ExecuteOperationActions(
                            operations, GetOperationActionsByKind(ephemeralActions),
                            executionData, diagReporter, isSupportedDiagnostic,
                            hasOperationBlockStartOrSymbolStartActions: startActions.Any(),
                            cancellationToken);
                    }
                },
                executeBlockActions: static (blockActions, diagReporter, isSupportedDiagnostic, executionData, args, cancellationToken) =>
                {
                    var (@this, startActions, declaredNode, operationBlocks, operations, ephemeralActions) = args;

                    var context = new OperationBlockAnalysisContext(operationBlocks, executionData.DeclaredSymbol, @this.Compilation,
                        executionData.AnalyzerOptions, diagReporter.AddDiagnosticAction, isSupportedDiagnostic, @this.GetControlFlowGraph, declaredNode.SyntaxTree,
                        executionData.FilterSpan, executionData.IsGeneratedCode, cancellationToken);

                    foreach (var blockAction in blockActions)
                    {
                        @this.ExecuteAndCatchIfThrows(
                            blockAction.Analyzer,
                            static data => data.blockAction.Action(data.context),
                            (blockAction, context),
                            new AnalysisContextInfo(@this.Compilation, executionData.DeclaredSymbol),
                            cancellationToken);
                    }
                },
                argument: (@this: this, startActions, declaredNode, operationBlocks, operations, ephemeralActions),
                cancellationToken);
            ephemeralActions.Free();
        }

        private void ExecuteBlockActionsCore<TBlockStartAction, TBlockAction, TArgs>(
            ImmutableArray<TBlockStartAction> startActions,
            ImmutableArray<TBlockAction> actions,
            ImmutableArray<TBlockAction> endActions,
            SyntaxNode declaredNode,
            ExecutionData executionData,
            Action<TBlockStartAction, HashSet<TBlockAction>, ExecutionData, TArgs, CancellationToken> addActions,
            Action<AnalyzerDiagnosticReporter, Func<Diagnostic, CancellationToken, bool>, ExecutionData, TArgs, CancellationToken> executeActions,
            Action<HashSet<TBlockAction>, AnalyzerDiagnosticReporter, Func<Diagnostic, CancellationToken, bool>, ExecutionData, TArgs, CancellationToken> executeBlockActions,
            TArgs argument,
            CancellationToken cancellationToken)
            where TBlockStartAction : AnalyzerAction
            where TBlockAction : AnalyzerAction
            where TArgs : struct
        {
            Debug.Assert(declaredNode != null);
            Debug.Assert(executionData.DeclaredSymbol != null);
            Debug.Assert(CanHaveExecutableCodeBlock(executionData.DeclaredSymbol));
            Debug.Assert(startActions.Any() || endActions.Any() || actions.Any());

            if (executionData.IsGeneratedCode && _shouldSkipAnalysisOnGeneratedCode(executionData.Analyzer) ||
                IsAnalyzerSuppressedForTree(executionData.Analyzer, declaredNode.SyntaxTree, cancellationToken))
            {
                return;
            }

            // Compute the sets of code block end, code block, and stateful node actions.

            var blockEndActions = PooledHashSet<TBlockAction>.GetInstance();
            var blockActions = PooledHashSet<TBlockAction>.GetInstance();

            // Include the code block actions.
            blockActions.AddAll(actions);

            // Include the initial code block end actions.
            blockEndActions.AddAll(endActions);

            var diagReporter = GetAddSemanticDiagnostic(
                executionData.SemanticModel.SyntaxTree, declaredNode.FullSpan,
                executionData.Analyzer, executionData.AnalyzerOptions, cancellationToken);

            // Include the stateful actions.
            foreach (var startAction in startActions)
                addActions(startAction, blockEndActions, executionData, argument, cancellationToken);

            using var _ = PooledDelegates.GetPooledFunction(
                static (d, ct, arg) => arg.self.IsSupportedDiagnostic(arg.Analyzer, d, ct),
                (self: this, executionData.Analyzer),
                out Func<Diagnostic, CancellationToken, bool> isSupportedDiagnostic);

            // Execute stateful executable node analyzers, if any.
            executeActions(diagReporter, isSupportedDiagnostic, executionData, argument, cancellationToken);

            executeBlockActions(blockActions, diagReporter, isSupportedDiagnostic, executionData, argument, cancellationToken);
            executeBlockActions(blockEndActions, diagReporter, isSupportedDiagnostic, executionData, argument, cancellationToken);

            diagReporter.Free();
            blockActions.Free();
            blockEndActions.Free();
        }

        internal static ImmutableSegmentedDictionary<TLanguageKindEnum, ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>> GetNodeActionsByKind<TLanguageKindEnum>(
            ArrayBuilder<SyntaxNodeAnalyzerAction<TLanguageKindEnum>> nodeActions)
            where TLanguageKindEnum : struct
        {
            if (nodeActions.IsEmpty)
                return ImmutableSegmentedDictionary<TLanguageKindEnum, ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>>.Empty;

            var nodeActionsByKind = PooledDictionary<TLanguageKindEnum, ArrayBuilder<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>>.GetInstance();
            foreach (var nodeAction in nodeActions)
            {
                foreach (var kind in nodeAction.Kinds)
                {
                    nodeActionsByKind.AddPooled(kind, nodeAction);
                }
            }

            return nodeActionsByKind.ToImmutableSegmentedDictionaryAndFree();
        }

        /// <summary>
        /// Execute syntax node actions for the given analyzer for the given declaration.
        /// </summary>
        public void ExecuteSyntaxNodeActions<TLanguageKindEnum>(
           ArrayBuilder<SyntaxNode> nodesToAnalyze,
           ImmutableSegmentedDictionary<TLanguageKindEnum, ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>> nodeActionsByKind,
           DiagnosticAnalyzer analyzer,
           SemanticModel model,
           Func<SyntaxNode, TLanguageKindEnum> getKind,
           TextSpan spanForContainingTopmostNodeForAnalysis,
           ISymbol declaredSymbol,
           TextSpan? filterSpan,
           bool isGeneratedCode,
           bool hasCodeBlockStartOrSymbolStartActions,
           CancellationToken cancellationToken)
           where TLanguageKindEnum : struct
        {
            if (isGeneratedCode && _shouldSkipAnalysisOnGeneratedCode(analyzer) ||
                IsAnalyzerSuppressedForTree(analyzer, model.SyntaxTree, cancellationToken))
            {
                return;
            }

            var analyzerOptions = this.GetAnalyzerSpecificOptions(analyzer);
            var diagReporter = GetAddSemanticDiagnostic(
                model.SyntaxTree, spanForContainingTopmostNodeForAnalysis, analyzer, analyzerOptions, cancellationToken);

            using var _ = PooledDelegates.GetPooledFunction(
                static (d, ct, arg) => arg.self.IsSupportedDiagnostic(arg.analyzer, d, ct),
                (self: this, analyzer),
                out Func<Diagnostic, CancellationToken, bool> isSupportedDiagnostic);

            ExecuteSyntaxNodeActions(
                nodesToAnalyze, nodeActionsByKind,
                new ExecutionData(analyzer, analyzerOptions, declaredSymbol, model, filterSpan, isGeneratedCode),
                getKind, diagReporter, isSupportedDiagnostic, hasCodeBlockStartOrSymbolStartActions, cancellationToken);

            diagReporter.Free();
        }

        private void ExecuteSyntaxNodeActions<TLanguageKindEnum>(
            ArrayBuilder<SyntaxNode> nodesToAnalyze,
            ImmutableSegmentedDictionary<TLanguageKindEnum, ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>> nodeActionsByKind,
            ExecutionData executionData,
            Func<SyntaxNode, TLanguageKindEnum> getKind,
            AnalyzerDiagnosticReporter diagReporter,
            Func<Diagnostic, CancellationToken, bool> isSupportedDiagnostic,
            bool hasCodeBlockStartOrSymbolStartActions,
            CancellationToken cancellationToken)
            where TLanguageKindEnum : struct
        {
            Debug.Assert(nodeActionsByKind.Any());
            Debug.Assert(!executionData.IsGeneratedCode || !_shouldSkipAnalysisOnGeneratedCode(executionData.Analyzer));
            Debug.Assert(!IsAnalyzerSuppressedForTree(executionData.Analyzer, executionData.SemanticModel.SyntaxTree, cancellationToken));

            foreach (var node in nodesToAnalyze)
            {
                // Most nodes have no registered actions. Check for actions before checking if the analyzer should be
                // executed on the node since the generated code check in ShouldExecuteNode can be expensive in
                // aggregate.
                if (nodeActionsByKind.TryGetValue(getKind(node), out var actionsForKind))
                {
                    RoslynDebug.Assert(!actionsForKind.IsEmpty, $"Unexpected empty action collection in {nameof(nodeActionsByKind)}");
                    if (ShouldExecuteNode(node, executionData.Analyzer, cancellationToken))
                    {
                        // If analyzer hasn't registered any CodeBlockStart or SymbolStart actions, then update the filter span
                        // for local diagnostics to be the callback node's full span.
                        // For this case, any diagnostic reported in node's callback outside it's full span will be considered
                        // a non-local diagnostic.
                        if (!hasCodeBlockStartOrSymbolStartActions)
                            diagReporter.FilterSpanForLocalDiagnostics = node.FullSpan;

                        foreach (var action in actionsForKind)
                        {
                            ExecuteSyntaxNodeAction(action, node, executionData, diagReporter.AddDiagnosticAction, isSupportedDiagnostic, cancellationToken);
                        }
                    }
                }
            }
        }

        internal static ImmutableSegmentedDictionary<OperationKind, ImmutableArray<OperationAnalyzerAction>> GetOperationActionsByKind(
            ArrayBuilder<OperationAnalyzerAction> operationActions)
        {
            if (operationActions.IsEmpty)
                return ImmutableSegmentedDictionary<OperationKind, ImmutableArray<OperationAnalyzerAction>>.Empty;

            var operationActionsByKind = PooledDictionary<OperationKind, ArrayBuilder<OperationAnalyzerAction>>.GetInstance();
            foreach (var operationAction in operationActions)
            {
                foreach (var kind in operationAction.Kinds)
                {
                    operationActionsByKind.AddPooled(kind, operationAction);
                }
            }

            return operationActionsByKind.ToImmutableSegmentedDictionaryAndFree();
        }

        /// <summary>
        /// Execute operation actions for the given analyzer for the given declaration.
        /// </summary>
        /// <returns>
        /// True, if successfully executed the actions for the given analysis scope OR all the actions have already been executed for the given analysis scope.
        /// False, if there are some pending actions that are currently being executed on another thread.
        /// </returns>
        public void ExecuteOperationActions(
            ImmutableArray<IOperation> operationsToAnalyze,
            ImmutableSegmentedDictionary<OperationKind, ImmutableArray<OperationAnalyzerAction>> operationActionsByKind,
            DiagnosticAnalyzer analyzer,
            SemanticModel model,
            TextSpan spanForContainingOperationBlock,
            ISymbol declaredSymbol,
            TextSpan? filterSpan,
            bool isGeneratedCode,
            bool hasOperationBlockStartOrSymbolStartActions,
            CancellationToken cancellationToken)
        {
            if (isGeneratedCode && _shouldSkipAnalysisOnGeneratedCode(analyzer) ||
                IsAnalyzerSuppressedForTree(analyzer, model.SyntaxTree, cancellationToken))
            {
                return;
            }

            var analyzerOptions = this.GetAnalyzerSpecificOptions(analyzer);
            var diagReporter = GetAddSemanticDiagnostic(
                model.SyntaxTree, spanForContainingOperationBlock, analyzer, analyzerOptions, cancellationToken);

            using var _ = PooledDelegates.GetPooledFunction(
                static (d, ct, arg) => arg.self.IsSupportedDiagnostic(arg.analyzer, d, ct),
                (self: this, analyzer),
                out Func<Diagnostic, CancellationToken, bool> isSupportedDiagnostic);

            ExecuteOperationActions(
                operationsToAnalyze, operationActionsByKind,
                new ExecutionData(analyzer, analyzerOptions, declaredSymbol, model, filterSpan, isGeneratedCode),
                diagReporter, isSupportedDiagnostic, hasOperationBlockStartOrSymbolStartActions, cancellationToken);

            diagReporter.Free();
        }

        private void ExecuteOperationActions(
            ImmutableArray<IOperation> operationsToAnalyze,
            ImmutableSegmentedDictionary<OperationKind, ImmutableArray<OperationAnalyzerAction>> operationActionsByKind,
            ExecutionData executionData,
            AnalyzerDiagnosticReporter diagReporter,
            Func<Diagnostic, CancellationToken, bool> isSupportedDiagnostic,
            bool hasOperationBlockStartOrSymbolStartActions,
            CancellationToken cancellationToken)
        {
            Debug.Assert(operationActionsByKind != null);
            Debug.Assert(operationActionsByKind.Any());
            Debug.Assert(!executionData.IsGeneratedCode || !_shouldSkipAnalysisOnGeneratedCode(executionData.Analyzer));
            Debug.Assert(!IsAnalyzerSuppressedForTree(executionData.Analyzer, executionData.SemanticModel.SyntaxTree, cancellationToken));

            foreach (var operation in operationsToAnalyze)
            {
                // Most operations have no registered actions. Check for actions before checking if the analyzer should
                // be executed on the operation since the generated code check in ShouldExecuteOperation can be
                // expensive in aggregate.
                if (operationActionsByKind.TryGetValue(operation.Kind, out var actionsForKind))
                {
                    RoslynDebug.Assert(!actionsForKind.IsEmpty, $"Unexpected empty action collection in {nameof(operationActionsByKind)}");
                    if (ShouldExecuteOperation(operation, executionData.Analyzer, cancellationToken))
                    {
                        // If analyzer hasn't registered any OperationBlockStart or SymbolStart actions, then update
                        // the filter span for local diagnostics to be the callback operation's full span.
                        // For this case, any diagnostic reported in operation's callback outside it's full span
                        // will be considered a non-local diagnostic.
                        if (!hasOperationBlockStartOrSymbolStartActions)
                            diagReporter.FilterSpanForLocalDiagnostics = operation.Syntax.FullSpan;

                        foreach (var action in actionsForKind)
                        {
                            ExecuteOperationAction(action, operation, executionData, diagReporter.AddDiagnosticAction, isSupportedDiagnostic, cancellationToken);
                        }
                    }
                }
            }
        }

        internal static bool CanHaveExecutableCodeBlock(ISymbol symbol)
        {
            switch (symbol.Kind)
            {
                case SymbolKind.Method:
                case SymbolKind.Event:
                case SymbolKind.Property:
                case SymbolKind.NamedType:
                case SymbolKind.Namespace: // We are exposing assembly/module attributes on global namespace symbol.
                    return true;

                case SymbolKind.Field:
                    Debug.Assert(((IFieldSymbol)symbol).AssociatedSymbol == null);
                    return true;

                default:
                    return false;
            }
        }

        internal void ExecuteAndCatchIfThrows<TArg>(DiagnosticAnalyzer analyzer, Action<TArg> analyze, TArg argument, AnalysisContextInfo? contextInfo, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            SharedStopwatch timer = default;
            if (_analyzerExecutionTimeMap != null)
            {
                timer = SharedStopwatch.StartNew();
            }

            var gate = _getAnalyzerGate(analyzer);
            if (gate != null)
            {
                lock (gate)
                {
                    ExecuteAndCatchIfThrows_NoLock(analyzer, analyze, argument, contextInfo, cancellationToken);
                }
            }
            else
            {
                ExecuteAndCatchIfThrows_NoLock(analyzer, analyze, argument, contextInfo, cancellationToken);
            }

            if (_analyzerExecutionTimeMap != null)
            {
                var elapsed = timer.Elapsed.Ticks;
                StrongBox<long> totalTicks = _analyzerExecutionTimeMap.GetOrAdd(analyzer, _ => new StrongBox<long>(0));
                Interlocked.Add(ref totalTicks.Value, elapsed);
            }
        }

        [PerformanceSensitive(
            "https://github.com/dotnet/roslyn/issues/23582",
            AllowCaptures = false)]
        private void ExecuteAndCatchIfThrows_NoLock<TArg>(DiagnosticAnalyzer analyzer, Action<TArg> analyze, TArg argument, AnalysisContextInfo? info, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                analyze(argument);
            }
            catch (Exception ex) when (HandleAnalyzerException(analyzer, ex, info) &&
                HandleAnalyzerException(ex, analyzer, info, OnAnalyzerException, _analyzerExceptionFilter, cancellationToken))
            {
            }
        }

        private bool HandleAnalyzerException(DiagnosticAnalyzer analyzer, Exception ex, in AnalysisContextInfo? info)
        {
            if (!this.Compilation.CatchAnalyzerExceptions)
            {
                Debug.Assert(false);
                Environment.FailFast(CreateAnalyzerExceptionDiagnostic(analyzer, ex, info).ToString());
                return false;
            }

            return true;
        }

        internal static bool HandleAnalyzerException(
            Exception exception,
            DiagnosticAnalyzer analyzer,
            AnalysisContextInfo? info,
            Action<Exception, DiagnosticAnalyzer, Diagnostic, CancellationToken> onAnalyzerException,
            Func<Exception, bool>? analyzerExceptionFilter,
            CancellationToken cancellationToken)
        {
            if (!exceptionFilter(exception, analyzerExceptionFilter, cancellationToken))
            {
                return false;
            }

            // Diagnostic for analyzer exception.
            var diagnostic = CreateAnalyzerExceptionDiagnostic(analyzer, exception, info);
            try
            {
                onAnalyzerException(exception, analyzer, diagnostic, cancellationToken);
            }
            catch (Exception)
            {
                // Ignore exceptions from exception handlers.
            }

            return true;

            static bool exceptionFilter(Exception ex, Func<Exception, bool>? analyzerExceptionFilter, CancellationToken cancellationToken)
            {
                if ((ex as OperationCanceledException)?.CancellationToken == cancellationToken)
                {
                    return false;
                }

                if (analyzerExceptionFilter != null)
                {
                    return analyzerExceptionFilter(ex);
                }

                return true;
            }
        }

        internal static Diagnostic CreateAnalyzerExceptionDiagnostic(DiagnosticAnalyzer analyzer, Exception e, AnalysisContextInfo? info = null)
        {
            var analyzerName = analyzer.ToString();
            var title = CodeAnalysisResources.CompilerAnalyzerFailure;
            var messageFormat = CodeAnalysisResources.CompilerAnalyzerThrows;
            var contextInformation = string.Join(Environment.NewLine, CreateDiagnosticDescription(info, e), CreateDisablingMessage(analyzer, analyzerName)).Trim();
            var messageArguments = new[] { analyzerName, e.GetType().ToString(), e.Message, contextInformation };
            var descriptor = GetAnalyzerExceptionDiagnosticDescriptor(AnalyzerExceptionDiagnosticId, title, messageFormat);
            return Diagnostic.Create(descriptor, Location.None, messageArguments);
        }

        private static string CreateDiagnosticDescription(AnalysisContextInfo? info, Exception e)
        {
            if (info == null)
            {
                return e.CreateDiagnosticDescription();
            }

            return string.Join(Environment.NewLine,
                string.Format(CodeAnalysisResources.ExceptionContext, info?.GetContext()), e.CreateDiagnosticDescription());
        }

        private static string CreateDisablingMessage(DiagnosticAnalyzer analyzer, string analyzerName)
        {
            var diagnosticIds = ImmutableSortedSet<string>.Empty.WithComparer(StringComparer.OrdinalIgnoreCase);
            try
            {
                foreach (var diagnostic in analyzer.SupportedDiagnostics)
                {
                    // If a null diagnostic is returned, we would have already reported that to the user earlier; we can just skip this.
                    if (diagnostic != null)
                    {
                        diagnosticIds = diagnosticIds.Add(diagnostic.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                return string.Format(CodeAnalysisResources.CompilerAnalyzerThrows, analyzerName, ex.GetType().ToString(), ex.Message, ex.CreateDiagnosticDescription());
            }

            if (diagnosticIds.IsEmpty)
            {
                return "";
            }

            return string.Format(CodeAnalysisResources.DisableAnalyzerDiagnosticsMessage, string.Join(", ", diagnosticIds));
        }

        internal static Diagnostic CreateDriverExceptionDiagnostic(Exception e)
        {
            var title = CodeAnalysisResources.AnalyzerDriverFailure;
            var messageFormat = CodeAnalysisResources.AnalyzerDriverThrows;
            var messageArguments = new[] { e.GetType().ToString(), e.Message, e.CreateDiagnosticDescription() };
            var descriptor = GetAnalyzerExceptionDiagnosticDescriptor(AnalyzerDriverExceptionDiagnosticId, title, messageFormat);
            return Diagnostic.Create(descriptor, Location.None, messageArguments);
        }

        internal static DiagnosticDescriptor GetAnalyzerExceptionDiagnosticDescriptor(string? id = null, string? title = null, string? messageFormat = null)
        {
            // TODO: It is not ideal to create a new descriptor per analyzer exception diagnostic instance.
            // However, until we add a LongMessage field to the Diagnostic, we are forced to park the instance specific description onto the Descriptor's Description field.
            // This requires us to create a new DiagnosticDescriptor instance per diagnostic instance.

            id ??= AnalyzerExceptionDiagnosticId;
            title ??= CodeAnalysisResources.CompilerAnalyzerFailure;
            messageFormat ??= CodeAnalysisResources.CompilerAnalyzerThrows;

            return new DiagnosticDescriptor(
                id,
                title,
                messageFormat,
                category: DiagnosticCategory,
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true,
                customTags: WellKnownDiagnosticTags.AnalyzerException);
        }

        internal static bool IsAnalyzerExceptionDiagnostic(Diagnostic diagnostic)
        {
            if (diagnostic.Id == AnalyzerExceptionDiagnosticId || diagnostic.Id == AnalyzerDriverExceptionDiagnosticId)
            {
                foreach (var tag in diagnostic.Descriptor.ImmutableCustomTags)
                {
                    if (tag == WellKnownDiagnosticTags.AnalyzerException)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal static bool AreEquivalentAnalyzerExceptionDiagnostics(Diagnostic exceptionDiagnostic, Diagnostic other)
        {
            // We need to have custom de-duplication logic for diagnostics generated for analyzer exceptions.
            // We create a new descriptor instance per each analyzer exception diagnostic instance (see comments in method "GetAnalyzerExceptionDiagnostic" above).
            // This is primarily to allow us to embed exception stack trace in the diagnostic description.
            // However, this might mean that two exception diagnostics which are equivalent in terms of ID and Message, might not have equal description strings.
            // We want to classify such diagnostics as equal for de-duplication purpose to reduce the noise in output.

            Debug.Assert(IsAnalyzerExceptionDiagnostic(exceptionDiagnostic));

            if (!IsAnalyzerExceptionDiagnostic(other))
            {
                return false;
            }

            return exceptionDiagnostic.Id == other.Id &&
                exceptionDiagnostic.Severity == other.Severity &&
                exceptionDiagnostic.GetMessage() == other.GetMessage();
        }

        private bool IsSupportedDiagnostic(DiagnosticAnalyzer analyzer, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            if (diagnostic is DiagnosticWithInfo)
            {
                // Compiler diagnostic
                return true;
            }

            return _analyzerManager.IsSupportedDiagnostic(analyzer, diagnostic, _isCompilerAnalyzer, this, cancellationToken);
        }

        private void AddSymbolDiagnostic(
            SymbolDeclaredCompilationEvent symbolDeclaredEvent,
            Diagnostic diagnostic,
            DiagnosticAnalyzer analyzer,
            AnalyzerOptions options,
            Func<ISymbol, SyntaxReference, Compilation, CancellationToken, SyntaxNode> getTopMostNodeForAnalysis,
            CancellationToken cancellationToken)
        {
            if (_shouldSuppressGeneratedCodeDiagnostic(diagnostic, analyzer, this.Compilation, cancellationToken))
            {
                return;
            }

            if (_addCategorizedLocalDiagnostic == null)
            {
                Debug.Assert(_addNonCategorizedDiagnostic != null);
                _addNonCategorizedDiagnostic(diagnostic, options, cancellationToken);
                return;
            }

            Debug.Assert(_addNonCategorizedDiagnostic == null);
            Debug.Assert(_addCategorizedNonLocalDiagnostic != null);

            if (diagnostic.Location.IsInSource)
            {
                var symbol = symbolDeclaredEvent.Symbol;
                foreach (var syntaxRef in symbolDeclaredEvent.DeclaringSyntaxReferences)
                {
                    if (syntaxRef.SyntaxTree == diagnostic.Location.SourceTree)
                    {
                        var syntax = getTopMostNodeForAnalysis(symbol, syntaxRef, this.Compilation, cancellationToken);
                        if (diagnostic.Location.SourceSpan.IntersectsWith(syntax.FullSpan))
                        {
                            _addCategorizedLocalDiagnostic(diagnostic, analyzer, options, false, cancellationToken);
                            return;
                        }
                    }
                }
            }

            _addCategorizedNonLocalDiagnostic(diagnostic, analyzer, options, cancellationToken);
        }

        private Action<Diagnostic> GetAddCompilationDiagnostic(
            DiagnosticAnalyzer analyzer,
            AnalyzerOptions analyzerOptions,
            CancellationToken cancellationToken)
        {
            return diagnostic =>
            {
                if (_shouldSuppressGeneratedCodeDiagnostic(diagnostic, analyzer, Compilation, cancellationToken))
                {
                    return;
                }

                if (_addCategorizedNonLocalDiagnostic == null)
                {
                    Debug.Assert(_addNonCategorizedDiagnostic != null);
                    _addNonCategorizedDiagnostic(diagnostic, analyzerOptions, cancellationToken);
                    return;
                }

                _addCategorizedNonLocalDiagnostic(diagnostic, analyzer, analyzerOptions, cancellationToken);
            };
        }

        private AnalyzerDiagnosticReporter GetAddSemanticDiagnostic(
            SyntaxTree tree,
            DiagnosticAnalyzer analyzer,
            AnalyzerOptions analyzerOptions,
            CancellationToken cancellationToken)
        {
            return AnalyzerDiagnosticReporter.GetInstance(
                new SourceOrAdditionalFile(tree), span: null, Compilation, analyzer, analyzerOptions, isSyntaxDiagnostic: false,
                _addNonCategorizedDiagnostic, _addCategorizedLocalDiagnostic, _addCategorizedNonLocalDiagnostic,
                _shouldSuppressGeneratedCodeDiagnostic, cancellationToken);
        }

        private AnalyzerDiagnosticReporter GetAddSemanticDiagnostic(
            SyntaxTree tree,
            TextSpan? span,
            DiagnosticAnalyzer analyzer,
            AnalyzerOptions analyzerOptions,
            CancellationToken cancellationToken)
        {
            return AnalyzerDiagnosticReporter.GetInstance(
                new SourceOrAdditionalFile(tree), span, Compilation, analyzer, analyzerOptions, isSyntaxDiagnostic: false,
                _addNonCategorizedDiagnostic, _addCategorizedLocalDiagnostic, _addCategorizedNonLocalDiagnostic,
                _shouldSuppressGeneratedCodeDiagnostic, cancellationToken);
        }

        private AnalyzerDiagnosticReporter GetAddSyntaxDiagnostic(
            SourceOrAdditionalFile file,
            DiagnosticAnalyzer analyzer,
            AnalyzerOptions analyzerOptions,
            CancellationToken cancellationToken)
        {
            return AnalyzerDiagnosticReporter.GetInstance(
                file, span: null, Compilation, analyzer, analyzerOptions, isSyntaxDiagnostic: true,
                _addNonCategorizedDiagnostic, _addCategorizedLocalDiagnostic, _addCategorizedNonLocalDiagnostic,
                _shouldSuppressGeneratedCodeDiagnostic, cancellationToken);
        }

        private bool ShouldExecuteNode(SyntaxNode node, DiagnosticAnalyzer analyzer, CancellationToken cancellationToken)
        {
            // Check if the node is generated code that must be skipped.
            if (_shouldSkipAnalysisOnGeneratedCode(analyzer) &&
                _isGeneratedCodeLocation(node.SyntaxTree, node.Span, cancellationToken))
            {
                return false;
            }

            return true;
        }

        private bool ShouldExecuteOperation(IOperation operation, DiagnosticAnalyzer analyzer, CancellationToken cancellationToken)
        {
            // Check if the operation syntax is generated code that must be skipped.
            if (operation.Syntax != null && _shouldSkipAnalysisOnGeneratedCode(analyzer) &&
                _isGeneratedCodeLocation(operation.Syntax.SyntaxTree, operation.Syntax.Span, cancellationToken))
            {
                return false;
            }

            return true;
        }

        internal TimeSpan ResetAnalyzerExecutionTime(DiagnosticAnalyzer analyzer)
        {
            Debug.Assert(_analyzerExecutionTimeMap != null);
            if (!_analyzerExecutionTimeMap.TryRemove(analyzer, out var executionTime))
            {
                return TimeSpan.Zero;
            }

            return TimeSpan.FromTicks(executionTime.Value);
        }

        private ControlFlowGraph GetControlFlowGraphImpl(IOperation operation)
        {
            Debug.Assert(operation.Parent == null);

            if (_lazyControlFlowGraphMap == null)
            {
                Interlocked.CompareExchange(ref _lazyControlFlowGraphMap, new ConcurrentDictionary<IOperation, ControlFlowGraph>(), null);
            }

            return _lazyControlFlowGraphMap.GetOrAdd(operation, op => ControlFlowGraphBuilder.Create(op));
        }

        private bool IsAnalyzerSuppressedForSymbol(DiagnosticAnalyzer analyzer, ISymbol symbol, CancellationToken cancellationToken)
        {
            foreach (var location in symbol.Locations)
            {
                if (location.SourceTree != null &&
                    !IsAnalyzerSuppressedForTree(analyzer, location.SourceTree, cancellationToken))
                {
                    return false;
                }
            }

            return true;
        }

        public void OnOperationBlockActionsExecuted(ImmutableArray<IOperation> operationBlocks)
        {
            // Clear _lazyControlFlowGraphMap entries for each operation block after we have executed
            // all analysis callbacks for the given operation blocks. This avoids holding onto them
            // for the entire compilation lifetime.
            // These control flow graphs are created on demand and shared between flow analysis based analyzers.

            if (_lazyControlFlowGraphMap?.Count > 0)
            {
                foreach (var operationBlock in operationBlocks)
                {
                    var root = operationBlock.GetRootOperation();
                    _lazyControlFlowGraphMap.TryRemove(root, out _);
                }
            }
        }
    }
}
