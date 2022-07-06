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
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

using AnalyzerStateData = Microsoft.CodeAnalysis.Diagnostics.AnalysisState.AnalyzerStateData;
using DeclarationAnalyzerStateData = Microsoft.CodeAnalysis.Diagnostics.AnalysisState.DeclarationAnalyzerStateData;
using OperationAnalyzerStateData = Microsoft.CodeAnalysis.Diagnostics.AnalysisState.OperationAnalyzerStateData;
using SyntaxNodeAnalyzerStateData = Microsoft.CodeAnalysis.Diagnostics.AnalysisState.SyntaxNodeAnalyzerStateData;

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

        private readonly Compilation? _compilation;
        private readonly AnalyzerOptions? _analyzerOptions;
        private readonly Action<Diagnostic>? _addNonCategorizedDiagnostic;
        private readonly Action<Diagnostic, DiagnosticAnalyzer, bool>? _addCategorizedLocalDiagnostic;
        private readonly Action<Diagnostic, DiagnosticAnalyzer>? _addCategorizedNonLocalDiagnostic;
        private readonly Action<Suppression>? _addSuppression;
        private readonly Action<Exception, DiagnosticAnalyzer, Diagnostic> _onAnalyzerException;
        private readonly Func<Exception, bool>? _analyzerExceptionFilter;
        private readonly AnalyzerManager _analyzerManager;
        private readonly Func<DiagnosticAnalyzer, bool>? _isCompilerAnalyzer;
        private readonly Func<DiagnosticAnalyzer, object?>? _getAnalyzerGate;
        private readonly Func<SyntaxTree, SemanticModel>? _getSemanticModel;
        private readonly Func<DiagnosticAnalyzer, bool> _shouldSkipAnalysisOnGeneratedCode;
        private readonly Func<Diagnostic, DiagnosticAnalyzer, Compilation, CancellationToken, bool> _shouldSuppressGeneratedCodeDiagnostic;
        private readonly Func<SyntaxTree, TextSpan, bool> _isGeneratedCodeLocation;
        private readonly Func<DiagnosticAnalyzer, SyntaxTree, SyntaxTreeOptionsProvider?, bool>? _isAnalyzerSuppressedForTree;

        /// <summary>
        /// The values in this map convert to <see cref="TimeSpan"/> using <see cref="TimeSpan.FromTicks(long)"/>.
        /// </summary>
        private readonly ConcurrentDictionary<DiagnosticAnalyzer, StrongBox<long>>? _analyzerExecutionTimeMap;
        private readonly CompilationAnalysisValueProviderFactory _compilationAnalysisValueProviderFactory;
        private readonly CancellationToken _cancellationToken;

        private Func<IOperation, ControlFlowGraph>? _lazyGetControlFlowGraph;

        private ConcurrentDictionary<IOperation, ControlFlowGraph>? _lazyControlFlowGraphMap;

        private Func<IOperation, ControlFlowGraph> GetControlFlowGraph
            => _lazyGetControlFlowGraph ??= GetControlFlowGraphImpl;

        private bool IsAnalyzerSuppressedForTree(DiagnosticAnalyzer analyzer, SyntaxTree tree)
        {
            Debug.Assert(_isAnalyzerSuppressedForTree != null);
            return _isAnalyzerSuppressedForTree(analyzer, tree, Compilation.Options.SyntaxTreeOptionsProvider);
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
        /// <param name="analyzerManager">Analyzer manager to fetch supported diagnostics.</param>
        /// <param name="getAnalyzerGate">
        /// Delegate to fetch the gate object to guard all callbacks into the analyzer.
        /// It should return a unique gate object for the given analyzer instance for non-concurrent analyzers, and null otherwise.
        /// All analyzer callbacks for non-concurrent analyzers will be guarded with a lock on the gate.
        /// </param>
        /// <param name="getSemanticModel">Delegate to get a semantic model for the given syntax tree which can be shared across analyzers.</param>
        /// <param name="shouldSkipAnalysisOnGeneratedCode">Delegate to identify if analysis should be skipped on generated code.</param>
        /// <param name="shouldSuppressGeneratedCodeDiagnostic">Delegate to identify if diagnostic reported while analyzing generated code should be suppressed.</param>
        /// <param name="isGeneratedCodeLocation">Delegate to identify if the given location is in generated code.</param>
        /// <param name="isAnalyzerSuppressedForTree">Delegate to identify if the given analyzer is suppressed for the given tree.</param>
        /// <param name="logExecutionTime">Flag indicating whether we need to log analyzer execution time.</param>
        /// <param name="addCategorizedLocalDiagnostic">Optional delegate to add categorized local analyzer diagnostics.</param>
        /// <param name="addCategorizedNonLocalDiagnostic">Optional delegate to add categorized non-local analyzer diagnostics.</param>
        /// <param name="addSuppression">Optional thread-safe delegate to add diagnostic suppressions from suppressors.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static AnalyzerExecutor Create(
            Compilation compilation,
            AnalyzerOptions analyzerOptions,
            Action<Diagnostic>? addNonCategorizedDiagnostic,
            Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException,
            Func<Exception, bool>? analyzerExceptionFilter,
            Func<DiagnosticAnalyzer, bool> isCompilerAnalyzer,
            AnalyzerManager analyzerManager,
            Func<DiagnosticAnalyzer, bool> shouldSkipAnalysisOnGeneratedCode,
            Func<Diagnostic, DiagnosticAnalyzer, Compilation, CancellationToken, bool> shouldSuppressGeneratedCodeDiagnostic,
            Func<SyntaxTree, TextSpan, bool> isGeneratedCodeLocation,
            Func<DiagnosticAnalyzer, SyntaxTree, SyntaxTreeOptionsProvider?, bool> isAnalyzerSuppressedForTree,
            Func<DiagnosticAnalyzer, object?> getAnalyzerGate,
            Func<SyntaxTree, SemanticModel> getSemanticModel,
            bool logExecutionTime = false,
            Action<Diagnostic, DiagnosticAnalyzer, bool>? addCategorizedLocalDiagnostic = null,
            Action<Diagnostic, DiagnosticAnalyzer>? addCategorizedNonLocalDiagnostic = null,
            Action<Suppression>? addSuppression = null,
            CancellationToken cancellationToken = default)
        {
            // We can either report categorized (local/non-local) diagnostics or non-categorized diagnostics.
            Debug.Assert((addNonCategorizedDiagnostic != null) ^ (addCategorizedLocalDiagnostic != null));
            Debug.Assert((addCategorizedLocalDiagnostic != null) == (addCategorizedNonLocalDiagnostic != null));

            var analyzerExecutionTimeMap = logExecutionTime ? new ConcurrentDictionary<DiagnosticAnalyzer, StrongBox<long>>() : null;

            return new AnalyzerExecutor(compilation, analyzerOptions, addNonCategorizedDiagnostic, onAnalyzerException, analyzerExceptionFilter,
                isCompilerAnalyzer, analyzerManager, shouldSkipAnalysisOnGeneratedCode, shouldSuppressGeneratedCodeDiagnostic, isGeneratedCodeLocation,
                isAnalyzerSuppressedForTree, getAnalyzerGate, getSemanticModel, analyzerExecutionTimeMap, addCategorizedLocalDiagnostic, addCategorizedNonLocalDiagnostic,
                addSuppression, cancellationToken);
        }

        /// <summary>
        /// Creates <see cref="AnalyzerExecutor"/> to fetch <see cref="DiagnosticAnalyzer.SupportedDiagnostics"/>.
        /// </summary>
        /// <param name="onAnalyzerException">
        /// Optional delegate which is invoked when an analyzer throws an exception.
        /// Delegate can do custom tasks such as report the given analyzer exception diagnostic, report a non-fatal watson for the exception, etc.
        /// </param>
        /// <param name="analyzerManager">Analyzer manager to fetch supported diagnostics.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static AnalyzerExecutor CreateForSupportedDiagnostics(
            Action<Exception, DiagnosticAnalyzer, Diagnostic>? onAnalyzerException,
            AnalyzerManager analyzerManager,
            CancellationToken cancellationToken = default)
        {
            onAnalyzerException ??= (ex, analyzer, diagnostic) => { };
            return new AnalyzerExecutor(
                compilation: null,
                analyzerOptions: null,
                addNonCategorizedDiagnosticOpt: null,
                isCompilerAnalyzer: null,
                shouldSkipAnalysisOnGeneratedCode: _ => false,
                shouldSuppressGeneratedCodeDiagnostic: (diagnostic, analyzer, compilation, ct) => false,
                isGeneratedCodeLocation: (_1, _2) => false,
                isAnalyzerSuppressedForTree: null,
                getAnalyzerGate: null,
                getSemanticModel: null,
                onAnalyzerException: onAnalyzerException,
                analyzerExceptionFilter: null,
                analyzerManager: analyzerManager,
                analyzerExecutionTimeMap: null,
                addCategorizedLocalDiagnostic: null,
                addCategorizedNonLocalDiagnostic: null,
                addSuppression: null,
                cancellationToken: cancellationToken);
        }

        private AnalyzerExecutor(
            Compilation? compilation,
            AnalyzerOptions? analyzerOptions,
            Action<Diagnostic>? addNonCategorizedDiagnosticOpt,
            Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException,
            Func<Exception, bool>? analyzerExceptionFilter,
            Func<DiagnosticAnalyzer, bool>? isCompilerAnalyzer,
            AnalyzerManager analyzerManager,
            Func<DiagnosticAnalyzer, bool> shouldSkipAnalysisOnGeneratedCode,
            Func<Diagnostic, DiagnosticAnalyzer, Compilation, CancellationToken, bool> shouldSuppressGeneratedCodeDiagnostic,
            Func<SyntaxTree, TextSpan, bool> isGeneratedCodeLocation,
            Func<DiagnosticAnalyzer, SyntaxTree, SyntaxTreeOptionsProvider?, bool>? isAnalyzerSuppressedForTree,
            Func<DiagnosticAnalyzer, object?>? getAnalyzerGate,
            Func<SyntaxTree, SemanticModel>? getSemanticModel,
            ConcurrentDictionary<DiagnosticAnalyzer, StrongBox<long>>? analyzerExecutionTimeMap,
            Action<Diagnostic, DiagnosticAnalyzer, bool>? addCategorizedLocalDiagnostic,
            Action<Diagnostic, DiagnosticAnalyzer>? addCategorizedNonLocalDiagnostic,
            Action<Suppression>? addSuppression,
            CancellationToken cancellationToken)
        {
            _compilation = compilation;
            _analyzerOptions = analyzerOptions;
            _addNonCategorizedDiagnostic = addNonCategorizedDiagnosticOpt;
            _onAnalyzerException = onAnalyzerException;
            _analyzerExceptionFilter = analyzerExceptionFilter;
            _isCompilerAnalyzer = isCompilerAnalyzer;
            _analyzerManager = analyzerManager;
            _shouldSkipAnalysisOnGeneratedCode = shouldSkipAnalysisOnGeneratedCode;
            _shouldSuppressGeneratedCodeDiagnostic = shouldSuppressGeneratedCodeDiagnostic;
            _isGeneratedCodeLocation = isGeneratedCodeLocation;
            _isAnalyzerSuppressedForTree = isAnalyzerSuppressedForTree;
            _getAnalyzerGate = getAnalyzerGate;
            _getSemanticModel = getSemanticModel;
            _analyzerExecutionTimeMap = analyzerExecutionTimeMap;
            _addCategorizedLocalDiagnostic = addCategorizedLocalDiagnostic;
            _addCategorizedNonLocalDiagnostic = addCategorizedNonLocalDiagnostic;
            _addSuppression = addSuppression;
            _cancellationToken = cancellationToken;

            _compilationAnalysisValueProviderFactory = new CompilationAnalysisValueProviderFactory();
        }

        public AnalyzerExecutor WithCancellationToken(CancellationToken cancellationToken)
        {
            if (cancellationToken == _cancellationToken)
            {
                return this;
            }

            return new AnalyzerExecutor(_compilation, _analyzerOptions, _addNonCategorizedDiagnostic, _onAnalyzerException, _analyzerExceptionFilter,
                _isCompilerAnalyzer, _analyzerManager, _shouldSkipAnalysisOnGeneratedCode, _shouldSuppressGeneratedCodeDiagnostic, _isGeneratedCodeLocation,
                _isAnalyzerSuppressedForTree, _getAnalyzerGate, _getSemanticModel, _analyzerExecutionTimeMap, _addCategorizedLocalDiagnostic, _addCategorizedNonLocalDiagnostic,
                _addSuppression, cancellationToken);
        }

        internal bool TryGetCompilationAndAnalyzerOptions(
            [NotNullWhen(true)] out Compilation? compilation,
            [NotNullWhen(true)] out AnalyzerOptions? analyzerOptions)
        {
            (compilation, analyzerOptions) = (_compilation, _analyzerOptions);
            return compilation != null && analyzerOptions != null;
        }

        internal Compilation Compilation
        {
            get
            {
                Debug.Assert(_compilation != null);
                return _compilation;
            }
        }

        internal AnalyzerOptions AnalyzerOptions
        {
            get
            {
                Debug.Assert(_analyzerOptions != null);
                return _analyzerOptions;
            }
        }

        internal CancellationToken CancellationToken => _cancellationToken;
        internal Action<Exception, DiagnosticAnalyzer, Diagnostic> OnAnalyzerException => _onAnalyzerException;
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
        /// <param name="analyzer">Analyzer to get session wide analyzer actions.</param>
        /// <param name="sessionScope">Session scope to store register session wide analyzer actions.</param>
        /// <remarks>
        /// Note that this API doesn't execute any <see cref="CompilationStartAnalyzerAction"/> registered by the Initialize invocation.
        /// Use <see cref="ExecuteCompilationStartActions(ImmutableArray{CompilationStartAnalyzerAction}, HostCompilationStartAnalysisScope)"/> API
        /// to get execute these actions to get the per-compilation analyzer actions.
        /// </remarks>
        public void ExecuteInitializeMethod(DiagnosticAnalyzer analyzer, HostSessionStartAnalysisScope sessionScope)
        {
            var context = new AnalyzerAnalysisContext(analyzer, sessionScope);

            // The Initialize method should be run asynchronously in case it is not well behaved, e.g. does not terminate.
            ExecuteAndCatchIfThrows(
                analyzer,
                data => data.analyzer.Initialize(data.context),
                (analyzer, context));
        }

        /// <summary>
        /// Executes the compilation start actions.
        /// </summary>
        /// <param name="actions"><see cref="AnalyzerActions"/> whose compilation start actions are to be executed.</param>
        /// <param name="compilationScope">Compilation scope to store the analyzer actions.</param>
        public void ExecuteCompilationStartActions(ImmutableArray<CompilationStartAnalyzerAction> actions, HostCompilationStartAnalysisScope compilationScope)
        {
            foreach (var startAction in actions)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                var context = new AnalyzerCompilationStartAnalysisContext(startAction.Analyzer, compilationScope,
                    Compilation, AnalyzerOptions, _compilationAnalysisValueProviderFactory, _cancellationToken);

                ExecuteAndCatchIfThrows(
                    startAction.Analyzer,
                    data => data.action(data.context),
                    (action: startAction.Action, context),
                    new AnalysisContextInfo(Compilation));
            }
        }

        /// <summary>
        /// Executes the symbol start actions.
        /// </summary>
        /// <param name="actions"><see cref="AnalyzerActions"/> whose symbol start actions are to be executed.</param>
        /// <param name="symbolScope">Symbol scope to store the analyzer actions.</param>
        public void ExecuteSymbolStartActions(
            ISymbol symbol,
            DiagnosticAnalyzer analyzer,
            ImmutableArray<SymbolStartAnalyzerAction> actions,
            HostSymbolStartAnalysisScope symbolScope)
        {
            if (IsAnalyzerSuppressedForSymbol(analyzer, symbol))
            {
                return;
            }

            foreach (var startAction in actions)
            {
                Debug.Assert(startAction.Analyzer == analyzer);
                _cancellationToken.ThrowIfCancellationRequested();

                var context = new AnalyzerSymbolStartAnalysisContext(startAction.Analyzer, symbolScope,
                    symbol, Compilation, AnalyzerOptions, _cancellationToken);

                ExecuteAndCatchIfThrows(
                    startAction.Analyzer,
                    data => data.action(data.context),
                    (action: startAction.Action, context),
                    new AnalysisContextInfo(Compilation, symbol));
            }
        }

        /// <summary>
        /// Executes the given diagnostic suppressor.
        /// </summary>
        /// <param name="suppressor">Suppressor to be executed.</param>
        /// <param name="reportedDiagnostics">Reported analyzer/compiler diagnostics that can be suppressed.</param>
        public void ExecuteSuppressionAction(DiagnosticSuppressor suppressor, ImmutableArray<Diagnostic> reportedDiagnostics)
        {
            Debug.Assert(_addSuppression != null);
            Debug.Assert(_getSemanticModel != null);

            if (reportedDiagnostics.IsEmpty)
            {
                return;
            }

            _cancellationToken.ThrowIfCancellationRequested();

            var supportedSuppressions = _analyzerManager.GetSupportedSuppressionDescriptors(suppressor, this);
            Func<SuppressionDescriptor, bool> isSupportedSuppression = supportedSuppressions.Contains;
            Action<SuppressionAnalysisContext> action = suppressor.ReportSuppressions;
            var context = new SuppressionAnalysisContext(Compilation, AnalyzerOptions,
                reportedDiagnostics, _addSuppression, isSupportedSuppression, _getSemanticModel, _cancellationToken);

            ExecuteAndCatchIfThrows(
                suppressor,
                data => data.action(data.context),
                (action, context),
                new AnalysisContextInfo(Compilation));
        }

        /// <summary>
        /// Tries to executes compilation actions or compilation end actions.
        /// </summary>
        /// <param name="compilationActions">Compilation actions to be executed.</param>
        /// <param name="analyzer">Analyzer whose actions are to be executed.</param>
        /// <param name="compilationEvent">Compilation event.</param>
        /// <param name="analysisScope">Scope for analyzer execution.</param>
        /// <param name="analysisState">An optional object to track analysis state.</param>
        /// <returns>
        /// True, if successfully executed the actions for the given analysis scope OR all the actions have already been executed for the given analysis scope.
        /// False, if there are some pending actions that are currently being executed on another thread.
        /// </returns>
        public bool TryExecuteCompilationActions(
            ImmutableArray<CompilationAnalyzerAction> compilationActions,
            DiagnosticAnalyzer analyzer,
            CompilationEvent compilationEvent,
            AnalysisScope analysisScope,
            AnalysisState? analysisState)
        {
            Debug.Assert(compilationEvent is CompilationStartedEvent || compilationEvent is CompilationCompletedEvent);

            AnalyzerStateData? analyzerState = null;

            try
            {
                if (TryStartProcessingEvent(compilationEvent, analyzer, analysisScope, analysisState, out analyzerState))
                {
                    ExecuteCompilationActionsCore(compilationActions, analyzer, analyzerState);
                    analysisState?.MarkEventComplete(compilationEvent, analyzer);
                    return true;
                }

                return IsEventComplete(compilationEvent, analyzer, analysisState);
            }
            finally
            {
                analyzerState?.ResetToReadyState();
            }
        }

        private void ExecuteCompilationActionsCore(ImmutableArray<CompilationAnalyzerAction> compilationActions, DiagnosticAnalyzer analyzer, AnalyzerStateData? analyzerState)
        {
            var addDiagnostic = GetAddCompilationDiagnostic(analyzer);

            using var _ = PooledDelegates.GetPooledFunction((d, arg) => arg.self.IsSupportedDiagnostic(arg.analyzer, d), (self: this, analyzer), out Func<Diagnostic, bool> isSupportedDiagnostic);

            foreach (var endAction in compilationActions)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                if (ShouldExecuteAction(analyzerState, endAction))
                {
                    var context = new CompilationAnalysisContext(
                        Compilation, AnalyzerOptions, addDiagnostic,
                        isSupportedDiagnostic, _compilationAnalysisValueProviderFactory, _cancellationToken);

                    ExecuteAndCatchIfThrows(
                        endAction.Analyzer,
                        data => data.action(data.context),
                        (action: endAction.Action, context),
                        new AnalysisContextInfo(Compilation));

                    analyzerState?.ProcessedActions.Add(endAction);
                }
            }
        }

        /// <summary>
        /// Tries to execute the symbol actions on the given symbol.
        /// </summary>
        /// <param name="symbolActions">Symbol actions to be executed.</param>
        /// <param name="analyzer">Analyzer whose actions are to be executed.</param>
        /// <param name="symbolDeclaredEvent">Symbol event to be analyzed.</param>
        /// <param name="getTopMostNodeForAnalysis">Delegate to get topmost declaration node for a symbol declaration reference.</param>
        /// <param name="analysisScope">Scope for analyzer execution.</param>
        /// <param name="analysisState">An optional object to track analysis state.</param>
        /// <param name="isGeneratedCodeSymbol">Flag indicating if this is a generated code symbol.</param>
        /// <returns>
        /// True, if successfully executed the actions for the given analysis scope OR all the actions have already been executed for the given analysis scope.
        /// False, if there are some pending actions that are currently being executed on another thread.
        /// </returns>
        public bool TryExecuteSymbolActions(
            ImmutableArray<SymbolAnalyzerAction> symbolActions,
            DiagnosticAnalyzer analyzer,
            SymbolDeclaredCompilationEvent symbolDeclaredEvent,
            Func<ISymbol, SyntaxReference, Compilation, CancellationToken, SyntaxNode> getTopMostNodeForAnalysis,
            AnalysisScope analysisScope,
            AnalysisState? analysisState,
            bool isGeneratedCodeSymbol)
        {
            AnalyzerStateData? analyzerState = null;

            try
            {
                var symbol = symbolDeclaredEvent.Symbol;
                if (TryStartAnalyzingSymbol(symbol, analyzer, analysisScope, analysisState, out analyzerState))
                {
                    ExecuteSymbolActionsCore(symbolActions, analyzer, symbolDeclaredEvent, getTopMostNodeForAnalysis, analyzerState, isGeneratedCodeSymbol);
                    analysisState?.MarkSymbolComplete(symbol, analyzer);
                    return true;
                }

                return IsSymbolComplete(symbol, analyzer, analysisState);
            }
            finally
            {
                analyzerState?.ResetToReadyState();
            }
        }

        private void ExecuteSymbolActionsCore(
            ImmutableArray<SymbolAnalyzerAction> symbolActions,
            DiagnosticAnalyzer analyzer,
            SymbolDeclaredCompilationEvent symbolDeclaredEvent,
            Func<ISymbol, SyntaxReference, Compilation, CancellationToken, SyntaxNode> getTopMostNodeForAnalysis,
            AnalyzerStateData? analyzerState,
            bool isGeneratedCodeSymbol)
        {
            Debug.Assert(getTopMostNodeForAnalysis != null);

            if (isGeneratedCodeSymbol && _shouldSkipAnalysisOnGeneratedCode(analyzer) ||
                IsAnalyzerSuppressedForSymbol(analyzer, symbolDeclaredEvent.Symbol))
            {
                return;
            }

            var symbol = symbolDeclaredEvent.Symbol;
            var addDiagnostic = GetAddDiagnostic(symbol, symbolDeclaredEvent.DeclaringSyntaxReferences, analyzer, getTopMostNodeForAnalysis);

            using var _ = PooledDelegates.GetPooledFunction((d, arg) => arg.self.IsSupportedDiagnostic(arg.analyzer, d), (self: this, analyzer), out Func<Diagnostic, bool> isSupportedDiagnostic);

            foreach (var symbolAction in symbolActions)
            {
                var action = symbolAction.Action;
                var kinds = symbolAction.Kinds;

                if (kinds.Contains(symbol.Kind))
                {
                    if (ShouldExecuteAction(analyzerState, symbolAction))
                    {
                        _cancellationToken.ThrowIfCancellationRequested();

                        var context = new SymbolAnalysisContext(symbol, Compilation, AnalyzerOptions, addDiagnostic,
                            isSupportedDiagnostic, _cancellationToken);

                        ExecuteAndCatchIfThrows(
                            symbolAction.Analyzer,
                            data => data.action(data.context),
                            (action, context),
                            new AnalysisContextInfo(Compilation, symbol));

                        analyzerState?.ProcessedActions.Add(symbolAction);
                    }
                }
            }
        }

        /// <summary>
        /// Tries to execute the symbol end actions on the given namespace or type containing symbol for the process member symbol for the given analyzer.
        /// </summary>
        /// <param name="containingSymbol">Symbol whose actions are to be executed.</param>
        /// <param name="processedMemberSymbol">Completed member symbol.</param>
        /// <param name="analyzer">Analyzer whose actions are to be executed.</param>
        /// <param name="getTopMostNodeForAnalysis">Delegate to get topmost declaration node for a symbol declaration reference.</param>
        /// <param name="analysisState">An optional object to track analysis state.</param>
        /// <returns>
        /// True, if successfully executed the actions for the given analysis scope OR all the actions have already been executed for the given analysis scope.
        /// False, if there are some pending actions.
        /// </returns>
        public bool TryExecuteSymbolEndActionsForContainer(
            INamespaceOrTypeSymbol containingSymbol,
            ISymbol processedMemberSymbol,
            DiagnosticAnalyzer analyzer,
            Func<ISymbol, SyntaxReference, Compilation, CancellationToken, SyntaxNode> getTopMostNodeForAnalysis,
            AnalysisState? analysisState,
            [NotNullWhen(returnValue: true)] out SymbolDeclaredCompilationEvent? containingSymbolDeclaredEvent)
        {
            containingSymbolDeclaredEvent = null;
            if (!_analyzerManager.TryProcessCompletedMemberAndGetPendingSymbolEndActionsForContainer(containingSymbol, processedMemberSymbol, analyzer, out var containerEndActionsAndEvent))
            {
                return false;
            }

            ImmutableArray<SymbolEndAnalyzerAction> endActions = containerEndActionsAndEvent.symbolEndActions;
            containingSymbolDeclaredEvent = containerEndActionsAndEvent.symbolDeclaredEvent;
            return TryExecuteSymbolEndActionsCore(endActions, analyzer, containingSymbolDeclaredEvent, getTopMostNodeForAnalysis, analysisState);
        }

        /// <summary>
        /// Tries to execute the symbol end actions on the given symbol for the given analyzer.
        /// </summary>
        /// <param name="symbolEndActions">Symbol actions to be executed.</param>
        /// <param name="analyzer">Analyzer whose actions are to be executed.</param>
        /// <param name="symbolDeclaredEvent">Symbol event to be analyzed.</param>
        /// <param name="getTopMostNodeForAnalysis">Delegate to get topmost declaration node for a symbol declaration reference.</param>
        /// <param name="analysisState">An optional object to track analysis state.</param>
        /// <returns>
        /// True, if successfully executed the actions for the given analysis scope OR all the actions have already been executed for the given analysis scope.
        /// False, if there are some pending actions.
        /// </returns>
        public bool TryExecuteSymbolEndActions(
            ImmutableArray<SymbolEndAnalyzerAction> symbolEndActions,
            DiagnosticAnalyzer analyzer,
            SymbolDeclaredCompilationEvent symbolDeclaredEvent,
            Func<ISymbol, SyntaxReference, Compilation, CancellationToken, SyntaxNode> getTopMostNodeForAnalysis,
            AnalysisState? analysisState)
        {
            return _analyzerManager.TryStartExecuteSymbolEndActions(symbolEndActions, analyzer, symbolDeclaredEvent) &&
                TryExecuteSymbolEndActionsCore(symbolEndActions, analyzer, symbolDeclaredEvent, getTopMostNodeForAnalysis, analysisState);
        }

        private bool TryExecuteSymbolEndActionsCore(
            ImmutableArray<SymbolEndAnalyzerAction> symbolEndActions,
            DiagnosticAnalyzer analyzer,
            SymbolDeclaredCompilationEvent symbolDeclaredEvent,
            Func<ISymbol, SyntaxReference, Compilation, CancellationToken, SyntaxNode> getTopMostNodeForAnalysis,
            AnalysisState? analysisState)
        {
            var symbol = symbolDeclaredEvent.Symbol;

            AnalyzerStateData? analyzerState = null;

            try
            {
                if (TryStartSymbolEndAnalysis(symbol, analyzer, analysisState, out analyzerState))
                {
                    ExecuteSymbolEndActionsCore(symbolEndActions, analyzer, symbolDeclaredEvent, getTopMostNodeForAnalysis, analyzerState);
                    MarkSymbolEndAnalysisComplete(symbol, analyzer, analysisState);
                    return true;
                }

                if (!IsSymbolEndAnalysisComplete(symbol, analyzer, analysisState))
                {
                    _analyzerManager.MarkSymbolEndAnalysisPending(symbol, analyzer, symbolEndActions, symbolDeclaredEvent);
                    return false;
                }

                return true;
            }
            finally
            {
                analyzerState?.ResetToReadyState();
            }
        }

        public void MarkSymbolEndAnalysisComplete(ISymbol symbol, DiagnosticAnalyzer analyzer, AnalysisState? analysisState)
        {
            analysisState?.MarkSymbolEndAnalysisComplete(symbol, analyzer);
            _analyzerManager.MarkSymbolEndAnalysisComplete(symbol, analyzer);
        }

        private void ExecuteSymbolEndActionsCore(
            ImmutableArray<SymbolEndAnalyzerAction> symbolEndActions,
            DiagnosticAnalyzer analyzer,
            SymbolDeclaredCompilationEvent symbolDeclaredEvent,
            Func<ISymbol, SyntaxReference, Compilation, CancellationToken, SyntaxNode> getTopMostNodeForAnalysis,
            AnalyzerStateData? analyzerState)
        {
            Debug.Assert(getTopMostNodeForAnalysis != null);
            Debug.Assert(!IsAnalyzerSuppressedForSymbol(analyzer, symbolDeclaredEvent.Symbol));

            var symbol = symbolDeclaredEvent.Symbol;
            var addDiagnostic = GetAddDiagnostic(symbol, symbolDeclaredEvent.DeclaringSyntaxReferences, analyzer, getTopMostNodeForAnalysis);

            using var _ = PooledDelegates.GetPooledFunction((d, arg) => arg.self.IsSupportedDiagnostic(arg.analyzer, d), (self: this, analyzer), out Func<Diagnostic, bool> isSupportedDiagnostic);

            foreach (var symbolAction in symbolEndActions)
            {
                var action = symbolAction.Action;

                if (ShouldExecuteAction(analyzerState, symbolAction))
                {
                    _cancellationToken.ThrowIfCancellationRequested();

                    var context = new SymbolAnalysisContext(symbol, Compilation, AnalyzerOptions, addDiagnostic,
                        isSupportedDiagnostic, _cancellationToken);

                    ExecuteAndCatchIfThrows(
                        symbolAction.Analyzer,
                        data => data.action(data.context),
                        (action, context),
                        new AnalysisContextInfo(Compilation, symbol));

                    analyzerState?.ProcessedActions.Add(symbolAction);
                }
            }
        }

        /// <summary>
        /// Tries to execute the semantic model actions on the given semantic model.
        /// </summary>
        /// <param name="semanticModelActions">Semantic model actions to be executed.</param>
        /// <param name="analyzer">Analyzer whose actions are to be executed.</param>
        /// <param name="semanticModel">Semantic model to analyze.</param>
        /// <param name="compilationUnitCompletedEvent">Compilation event for semantic model analysis.</param>
        /// <param name="analysisScope">Scope for analyzer execution.</param>
        /// <param name="analysisState">An optional object to track analysis state.</param>
        /// <param name="isGeneratedCode">Flag indicating if the syntax tree being analyzed is generated code.</param>
        /// <returns>
        /// True, if successfully executed the actions for the given analysis scope OR all the actions have already been executed for the given analysis scope.
        /// False, if there are some pending actions that are currently being executed on another thread.
        /// </returns>
        public bool TryExecuteSemanticModelActions(
            ImmutableArray<SemanticModelAnalyzerAction> semanticModelActions,
            DiagnosticAnalyzer analyzer,
            SemanticModel semanticModel,
            CompilationUnitCompletedEvent compilationUnitCompletedEvent,
            AnalysisScope analysisScope,
            AnalysisState? analysisState,
            bool isGeneratedCode)
        {
            Debug.Assert(!compilationUnitCompletedEvent.FilterSpan.HasValue || _isCompilerAnalyzer!(analyzer), "Only compiler analyzer supports span-based semantic model action callbacks");

            AnalyzerStateData? analyzerState = null;

            try
            {
                if (TryStartProcessingEvent(compilationUnitCompletedEvent, analyzer, analysisScope, analysisState, out analyzerState))
                {
                    ExecuteSemanticModelActionsCore(semanticModelActions, analyzer, semanticModel, analyzerState, analysisScope, isGeneratedCode);
                    analysisState?.MarkEventComplete(compilationUnitCompletedEvent, analyzer);
                    return true;
                }

                return IsEventComplete(compilationUnitCompletedEvent, analyzer, analysisState);
            }
            finally
            {
                analyzerState?.ResetToReadyState();
            }
        }

        private void ExecuteSemanticModelActionsCore(
            ImmutableArray<SemanticModelAnalyzerAction> semanticModelActions,
            DiagnosticAnalyzer analyzer,
            SemanticModel semanticModel,
            AnalyzerStateData? analyzerState,
            AnalysisScope analysisScope,
            bool isGeneratedCode)
        {
            if (isGeneratedCode && _shouldSkipAnalysisOnGeneratedCode(analyzer) ||
                IsAnalyzerSuppressedForTree(analyzer, semanticModel.SyntaxTree))
            {
                return;
            }

            var diagReporter = GetAddSemanticDiagnostic(semanticModel.SyntaxTree, analyzer);

            using var _ = PooledDelegates.GetPooledFunction((d, arg) => arg.self.IsSupportedDiagnostic(arg.analyzer, d), (self: this, analyzer), out Func<Diagnostic, bool> isSupportedDiagnostic);

            foreach (var semanticModelAction in semanticModelActions)
            {
                if (ShouldExecuteAction(analyzerState, semanticModelAction))
                {
                    _cancellationToken.ThrowIfCancellationRequested();

                    var context = new SemanticModelAnalysisContext(semanticModel, AnalyzerOptions, diagReporter.AddDiagnosticAction,
                        isSupportedDiagnostic, analysisScope.FilterSpanOpt, _cancellationToken);

                    // Catch Exception from action.
                    ExecuteAndCatchIfThrows(
                        semanticModelAction.Analyzer,
                        data => data.action(data.context),
                        (action: semanticModelAction.Action, context),
                        new AnalysisContextInfo(semanticModel));

                    analyzerState?.ProcessedActions.Add(semanticModelAction);
                }
            }

            diagReporter.Free();
        }

        /// <summary>
        /// Tries to execute the syntax tree actions on the given syntax tree.
        /// </summary>
        /// <param name="syntaxTreeActions">Syntax tree actions to be executed.</param>
        /// <param name="analyzer">Analyzer whose actions are to be executed.</param>
        /// <param name="file">Syntax tree to analyze.</param>
        /// <param name="analysisScope">Scope for analyzer execution.</param>
        /// <param name="analysisState">An optional object to track analysis state.</param>
        /// <param name="isGeneratedCode">Flag indicating if the syntax tree being analyzed is generated code.</param>
        /// <returns>
        /// True, if successfully executed the actions for the given analysis scope OR all the actions have already been executed for the given analysis scope.
        /// False, if there are some pending actions that are currently being executed on another thread.
        /// </returns>
        public bool TryExecuteSyntaxTreeActions(
            ImmutableArray<SyntaxTreeAnalyzerAction> syntaxTreeActions,
            DiagnosticAnalyzer analyzer,
            SourceOrAdditionalFile file,
            AnalysisScope analysisScope,
            AnalysisState? analysisState,
            bool isGeneratedCode)
        {
            Debug.Assert(file.SourceTree != null);
            AnalyzerStateData? analyzerState = null;

            try
            {
                if (TryStartSyntaxAnalysis(file, analyzer, analysisScope, analysisState, out analyzerState))
                {
                    ExecuteSyntaxTreeActionsCore(syntaxTreeActions, analyzer, file, analyzerState, isGeneratedCode);
                    analysisState?.MarkSyntaxAnalysisComplete(file, analyzer);
                    return true;
                }

                return analysisState == null || !analysisState.HasPendingSyntaxAnalysis(analysisScope);
            }
            finally
            {
                analyzerState?.ResetToReadyState();
            }
        }

        private void ExecuteSyntaxTreeActionsCore(
            ImmutableArray<SyntaxTreeAnalyzerAction> syntaxTreeActions,
            DiagnosticAnalyzer analyzer,
            SourceOrAdditionalFile file,
            AnalyzerStateData? analyzerState,
            bool isGeneratedCode)
        {
            Debug.Assert(file.SourceTree != null);

            var tree = file.SourceTree;
            if (isGeneratedCode && _shouldSkipAnalysisOnGeneratedCode(analyzer) ||
                IsAnalyzerSuppressedForTree(analyzer, tree))
            {
                return;
            }

            var diagReporter = GetAddSyntaxDiagnostic(file, analyzer);

            using var _ = PooledDelegates.GetPooledFunction((d, arg) => arg.self.IsSupportedDiagnostic(arg.analyzer, d), (self: this, analyzer), out Func<Diagnostic, bool> isSupportedDiagnostic);

            foreach (var syntaxTreeAction in syntaxTreeActions)
            {
                if (ShouldExecuteAction(analyzerState, syntaxTreeAction))
                {
                    _cancellationToken.ThrowIfCancellationRequested();

                    var context = new SyntaxTreeAnalysisContext(tree, AnalyzerOptions, diagReporter.AddDiagnosticAction, isSupportedDiagnostic, Compilation, _cancellationToken);

                    // Catch Exception from action.
                    ExecuteAndCatchIfThrows(
                        syntaxTreeAction.Analyzer,
                        data => data.action(data.context),
                        (action: syntaxTreeAction.Action, context),
                        new AnalysisContextInfo(Compilation, file));

                    analyzerState?.ProcessedActions.Add(syntaxTreeAction);
                }
            }

            diagReporter.Free();
        }

        /// <summary>
        /// Tries to execute the additional file actions.
        /// </summary>
        /// <param name="additionalFileActions">Actions to be executed.</param>
        /// <param name="analyzer">Analyzer whose actions are to be executed.</param>
        /// <param name="file">Additional file to analyze.</param>
        /// <param name="analysisScope">Scope for analyzer execution.</param>
        /// <param name="analysisState">An optional object to track analysis state.</param>
        /// <returns>
        /// True, if successfully executed the actions for the given analysis scope OR all the actions have already been executed for the given analysis scope.
        /// False, if there are some pending actions that are currently being executed on another thread.
        /// </returns>
        public bool TryExecuteAdditionalFileActions(
            ImmutableArray<AdditionalFileAnalyzerAction> additionalFileActions,
            DiagnosticAnalyzer analyzer,
            SourceOrAdditionalFile file,
            AnalysisScope analysisScope,
            AnalysisState? analysisState)
        {
            Debug.Assert(file.AdditionalFile != null);
            AnalyzerStateData? analyzerState = null;

            try
            {
                if (TryStartSyntaxAnalysis(file, analyzer, analysisScope, analysisState, out analyzerState))
                {
                    ExecuteAdditionalFileActionsCore(additionalFileActions, analyzer, file, analyzerState);
                    analysisState?.MarkSyntaxAnalysisComplete(file, analyzer);
                    return true;
                }

                return analysisState == null || !analysisState.HasPendingSyntaxAnalysis(analysisScope);
            }
            finally
            {
                analyzerState?.ResetToReadyState();
            }
        }

        private void ExecuteAdditionalFileActionsCore(
            ImmutableArray<AdditionalFileAnalyzerAction> additionalFileActions,
            DiagnosticAnalyzer analyzer,
            SourceOrAdditionalFile file,
            AnalyzerStateData? analyzerState)
        {
            Debug.Assert(file.AdditionalFile != null);
            var additionalFile = file.AdditionalFile;

            var diagReporter = GetAddSyntaxDiagnostic(file, analyzer);

            using var _ = PooledDelegates.GetPooledFunction((d, arg) => arg.self.IsSupportedDiagnostic(arg.analyzer, d), (self: this, analyzer), out Func<Diagnostic, bool> isSupportedDiagnostic);
            foreach (var additionalFileAction in additionalFileActions)
            {
                if (ShouldExecuteAction(analyzerState, additionalFileAction))
                {
                    _cancellationToken.ThrowIfCancellationRequested();

                    var context = new AdditionalFileAnalysisContext(additionalFile, AnalyzerOptions, diagReporter.AddDiagnosticAction, isSupportedDiagnostic, Compilation, _cancellationToken);

                    // Catch Exception from action.
                    ExecuteAndCatchIfThrows(
                        additionalFileAction.Analyzer,
                        data => data.action(data.context),
                        (action: additionalFileAction.Action, context),
                        new AnalysisContextInfo(Compilation, file));

                    analyzerState?.ProcessedActions.Add(additionalFileAction);
                }
            }

            diagReporter.Free();
        }

        private void ExecuteSyntaxNodeAction<TLanguageKindEnum>(
            SyntaxNodeAnalyzerAction<TLanguageKindEnum> syntaxNodeAction,
            SyntaxNode node,
            ISymbol containingSymbol,
            SemanticModel semanticModel,
            Action<Diagnostic> addDiagnostic,
            Func<Diagnostic, bool> isSupportedDiagnostic,
            SyntaxNodeAnalyzerStateData? analyzerState)
            where TLanguageKindEnum : struct
        {
            Debug.Assert(analyzerState == null || analyzerState.CurrentNode == node);
            Debug.Assert(!IsAnalyzerSuppressedForTree(syntaxNodeAction.Analyzer, node.SyntaxTree));

            if (ShouldExecuteAction(analyzerState, syntaxNodeAction))
            {
                var syntaxNodeContext = new SyntaxNodeAnalysisContext(node, containingSymbol, semanticModel, AnalyzerOptions, addDiagnostic,
                    isSupportedDiagnostic, _cancellationToken);

                ExecuteAndCatchIfThrows(
                    syntaxNodeAction.Analyzer,
                    data => data.action(data.context),
                    (action: syntaxNodeAction.Action, context: syntaxNodeContext),
                    new AnalysisContextInfo(Compilation, node));

                analyzerState?.ProcessedActions.Add(syntaxNodeAction);
            }
        }

        private void ExecuteOperationAction(
            OperationAnalyzerAction operationAction,
            IOperation operation,
            ISymbol containingSymbol,
            SemanticModel semanticModel,
            Action<Diagnostic> addDiagnostic,
            Func<Diagnostic, bool> isSupportedDiagnostic,
            OperationAnalyzerStateData? analyzerState)
        {
            Debug.Assert(analyzerState == null || analyzerState.CurrentOperation == operation);
            Debug.Assert(!IsAnalyzerSuppressedForTree(operationAction.Analyzer, semanticModel.SyntaxTree));

            if (ShouldExecuteAction(analyzerState, operationAction))
            {
                var operationContext = new OperationAnalysisContext(operation, containingSymbol, semanticModel.Compilation,
                    AnalyzerOptions, addDiagnostic, isSupportedDiagnostic, GetControlFlowGraph, _cancellationToken);
                ExecuteAndCatchIfThrows(
                    operationAction.Analyzer,
                    data => data.action(data.context),
                    (action: operationAction.Action, context: operationContext),
                    new AnalysisContextInfo(Compilation, operation));

                analyzerState?.ProcessedActions.Add(operationAction);
            }
        }

        /// <summary>
        /// Tries to execute code block actions for the given analyzer for the given declaration.
        /// </summary>
        /// <returns>
        /// True, if successfully executed the actions for the given analysis scope OR all the actions have already been executed for the given analysis scope.
        /// False, if there are some pending actions that are currently being executed on another thread.
        /// </returns>
        public bool TryExecuteCodeBlockActions<TLanguageKindEnum>(
            IEnumerable<CodeBlockStartAnalyzerAction<TLanguageKindEnum>> codeBlockStartActions,
            IEnumerable<CodeBlockAnalyzerAction> codeBlockActions,
            IEnumerable<CodeBlockAnalyzerAction> codeBlockEndActions,
            DiagnosticAnalyzer analyzer,
            SyntaxNode declaredNode,
            ISymbol declaredSymbol,
            ImmutableArray<SyntaxNode> executableCodeBlocks,
            SemanticModel semanticModel,
            Func<SyntaxNode, TLanguageKindEnum> getKind,
            int declarationIndex,
            AnalysisScope analysisScope,
            AnalysisState? analysisState,
            bool isGeneratedCode)
            where TLanguageKindEnum : struct
        {
            DeclarationAnalyzerStateData? analyzerState = null;

            try
            {
                if (TryStartAnalyzingDeclaration(declaredSymbol, declarationIndex, analyzer, analysisScope, analysisState, out analyzerState))
                {
                    ExecuteBlockActionsCore<CodeBlockStartAnalyzerAction<TLanguageKindEnum>, CodeBlockAnalyzerAction, SyntaxNodeAnalyzerAction<TLanguageKindEnum>, SyntaxNodeAnalyzerStateData, SyntaxNode, TLanguageKindEnum>(
                        codeBlockStartActions, codeBlockActions, codeBlockEndActions, analyzer,
                        declaredNode, declaredSymbol, executableCodeBlocks, (codeBlocks) => codeBlocks.SelectMany(
                            cb =>
                            {
                                var filter = semanticModel.GetSyntaxNodesToAnalyzeFilter(cb, declaredSymbol);

                                if (filter is object)
                                {
                                    return cb.DescendantNodesAndSelf(descendIntoChildren: filter).Where(filter);
                                }
                                else
                                {
                                    return cb.DescendantNodesAndSelf();
                                }
                            }),
                        semanticModel, getKind, analyzerState?.CodeBlockAnalysisState, isGeneratedCode);
                    return true;
                }

                return IsDeclarationComplete(declaredSymbol, declarationIndex, analyzer, analysisState);
            }
            finally
            {
                analyzerState?.ResetToReadyState();
            }
        }

        /// <summary>
        /// Tries to execute operation block actions for the given analyzer for the given declaration.
        /// </summary>
        /// <returns>
        /// True, if successfully executed the actions for the given analysis scope OR all the actions have already been executed for the given analysis scope.
        /// False, if there are some pending actions that are currently being executed on another thread.
        /// </returns>
        public bool TryExecuteOperationBlockActions(
            IEnumerable<OperationBlockStartAnalyzerAction> operationBlockStartActions,
            IEnumerable<OperationBlockAnalyzerAction> operationBlockActions,
            IEnumerable<OperationBlockAnalyzerAction> operationBlockEndActions,
            DiagnosticAnalyzer analyzer,
            SyntaxNode declaredNode,
            ISymbol declaredSymbol,
            ImmutableArray<IOperation> operationBlocks,
            ImmutableArray<IOperation> operations,
            SemanticModel semanticModel,
            int declarationIndex,
            AnalysisScope analysisScope,
            AnalysisState? analysisState,
            bool isGeneratedCode)
        {
            DeclarationAnalyzerStateData? analyzerState = null;

            try
            {
                if (TryStartAnalyzingDeclaration(declaredSymbol, declarationIndex, analyzer, analysisScope, analysisState, out analyzerState))
                {
                    ExecuteBlockActionsCore<OperationBlockStartAnalyzerAction, OperationBlockAnalyzerAction, OperationAnalyzerAction, OperationAnalyzerStateData, IOperation, int>(
                        operationBlockStartActions, operationBlockActions, operationBlockEndActions, analyzer,
                        declaredNode, declaredSymbol, operationBlocks, (blocks) => operations, semanticModel,
                        getKind: null, analyzerState?.OperationBlockAnalysisState, isGeneratedCode);
                    return true;
                }

                return IsDeclarationComplete(declaredSymbol, declarationIndex, analyzer, analysisState);
            }
            finally
            {
                analyzerState?.ResetToReadyState();
            }
        }

        private void ExecuteBlockActionsCore<TBlockStartAction, TBlockAction, TNodeAction, TNodeStateData, TNode, TLanguageKindEnum>(
           IEnumerable<TBlockStartAction> startActions,
           IEnumerable<TBlockAction> actions,
           IEnumerable<TBlockAction> endActions,
           DiagnosticAnalyzer analyzer,
           SyntaxNode declaredNode,
           ISymbol declaredSymbol,
           ImmutableArray<TNode> executableBlocks,
           Func<ImmutableArray<TNode>, IEnumerable<TNode>> getNodesToAnalyze,
           SemanticModel semanticModel,
           Func<SyntaxNode, TLanguageKindEnum>? getKind,
           AnalysisState.BlockAnalyzerStateData<TBlockAction, TNodeStateData>? analyzerState,
           bool isGeneratedCode)
           where TLanguageKindEnum : struct
           where TBlockStartAction : AnalyzerAction
           where TBlockAction : AnalyzerAction
           where TNodeAction : AnalyzerAction
           where TNodeStateData : AnalyzerStateData, new()
        {
            Debug.Assert(declaredNode != null);
            Debug.Assert(declaredSymbol != null);
            Debug.Assert(CanHaveExecutableCodeBlock(declaredSymbol));
            Debug.Assert(startActions.Any() || endActions.Any() || actions.Any());
            Debug.Assert(!executableBlocks.IsEmpty);

            if (isGeneratedCode && _shouldSkipAnalysisOnGeneratedCode(analyzer) ||
                IsAnalyzerSuppressedForTree(analyzer, declaredNode.SyntaxTree))
            {
                return;
            }

            // Compute the sets of code block end, code block, and stateful node actions.

            var blockEndActions = PooledHashSet<TBlockAction>.GetInstance();
            var blockActions = PooledHashSet<TBlockAction>.GetInstance();
            var executableNodeActions = ArrayBuilder<TNodeAction>.GetInstance();
            var syntaxNodeActions = executableNodeActions as ArrayBuilder<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>;
            var operationActions = executableNodeActions as ArrayBuilder<OperationAnalyzerAction>;
            ImmutableArray<IOperation> operationBlocks = executableBlocks[0] is IOperation ? (ImmutableArray<IOperation>)(object)executableBlocks : ImmutableArray<IOperation>.Empty;

            // Include the code block actions.
            blockActions.AddAll(actions);

            // Include the initial code block end actions.
            if (analyzerState?.CurrentBlockEndActions != null)
            {
                // We have partially processed the code block actions.
                blockEndActions.AddAll(analyzerState.CurrentBlockEndActions.Cast<TBlockAction>());
                executableNodeActions.AddRange(analyzerState.CurrentBlockNodeActions.Cast<TNodeAction>());
            }
            else
            {
                // We have begun to process the code block actions.
                blockEndActions.AddAll(endActions);
            }

            var diagReporter = GetAddSemanticDiagnostic(semanticModel.SyntaxTree, declaredNode.FullSpan, analyzer);

            try
            {
                // Include the stateful actions.
                foreach (var startAction in startActions)
                {
                    if (ShouldExecuteAction(analyzerState, startAction))
                    {
                        if (startAction is CodeBlockStartAnalyzerAction<TLanguageKindEnum> codeBlockStartAction)
                        {
                            var codeBlockEndActions = blockEndActions as PooledHashSet<CodeBlockAnalyzerAction>;
                            var codeBlockScope = new HostCodeBlockStartAnalysisScope<TLanguageKindEnum>();
                            var blockStartContext = new AnalyzerCodeBlockStartAnalysisContext<TLanguageKindEnum>(startAction.Analyzer,
                                codeBlockScope, declaredNode, declaredSymbol, semanticModel, AnalyzerOptions, _cancellationToken);

                            // Catch Exception from the start action.
                            ExecuteAndCatchIfThrows(
                                startAction.Analyzer,
                                data =>
                                {
                                    data.action(data.context);
                                    data.blockEndActions?.AddAll(data.scope.CodeBlockEndActions);
                                    data.syntaxNodeActions?.AddRange(data.scope.SyntaxNodeActions);
                                },
                                (action: codeBlockStartAction.Action, context: blockStartContext, scope: codeBlockScope, blockEndActions: codeBlockEndActions, syntaxNodeActions),
                                new AnalysisContextInfo(Compilation, declaredSymbol, declaredNode));
                        }
                        else
                        {
                            if (startAction is OperationBlockStartAnalyzerAction operationBlockStartAction)
                            {
                                var operationBlockEndActions = blockEndActions as PooledHashSet<OperationBlockAnalyzerAction>;
                                var operationBlockScope = new HostOperationBlockStartAnalysisScope();
                                var operationStartContext = new AnalyzerOperationBlockStartAnalysisContext(startAction.Analyzer,
                                    operationBlockScope, operationBlocks, declaredSymbol, semanticModel.Compilation, AnalyzerOptions, GetControlFlowGraph, _cancellationToken);

                                // Catch Exception from the start action.
                                ExecuteAndCatchIfThrows(
                                    startAction.Analyzer,
                                    data =>
                                    {
                                        data.action(data.context);
                                        data.blockEndActions?.AddAll(data.scope.OperationBlockEndActions);
                                        data.operationActions?.AddRange(data.scope.OperationActions);
                                    },
                                    (action: operationBlockStartAction.Action, context: operationStartContext, scope: operationBlockScope, blockEndActions: operationBlockEndActions, operationActions: operationActions),
                                    new AnalysisContextInfo(Compilation, declaredSymbol));
                            }
                        }

                        analyzerState?.ProcessedActions.Add(startAction);
                    }
                }
            }
            finally
            {
                if (analyzerState != null)
                {
                    analyzerState.CurrentBlockEndActions = blockEndActions.ToImmutableHashSet<TBlockAction>();
                    analyzerState.CurrentBlockNodeActions = executableNodeActions.ToImmutableHashSet<AnalyzerAction>();
                }
            }

            using var _ = PooledDelegates.GetPooledFunction((d, arg) => arg.self.IsSupportedDiagnostic(arg.analyzer, d), (self: this, analyzer), out Func<Diagnostic, bool> isSupportedDiagnostic);

            // Execute stateful executable node analyzers, if any.
            if (executableNodeActions.Any())
            {
                if (syntaxNodeActions != null)
                {
                    Debug.Assert(getKind != null);

                    var executableNodeActionsByKind = GetNodeActionsByKind(syntaxNodeActions);
                    var syntaxNodesToAnalyze = (IEnumerable<SyntaxNode>)getNodesToAnalyze(executableBlocks);
                    ExecuteSyntaxNodeActions(syntaxNodesToAnalyze, executableNodeActionsByKind, analyzer, declaredSymbol, semanticModel, getKind, diagReporter.AddDiagnosticAction, isSupportedDiagnostic, analyzerState?.ExecutableNodesAnalysisState as SyntaxNodeAnalyzerStateData);
                }
                else if (operationActions != null)
                {
                    var operationActionsByKind = GetOperationActionsByKind(operationActions);
                    var operationsToAnalyze = (IEnumerable<IOperation>)getNodesToAnalyze(executableBlocks);
                    ExecuteOperationActions(operationsToAnalyze, operationActionsByKind, analyzer, declaredSymbol, semanticModel, diagReporter.AddDiagnosticAction, isSupportedDiagnostic, analyzerState?.ExecutableNodesAnalysisState as OperationAnalyzerStateData);
                }
            }

            executableNodeActions.Free();

            ExecuteBlockActions(blockActions, declaredNode, declaredSymbol, analyzer, semanticModel, operationBlocks, diagReporter.AddDiagnosticAction, isSupportedDiagnostic, analyzerState);
            ExecuteBlockActions(blockEndActions, declaredNode, declaredSymbol, analyzer, semanticModel, operationBlocks, diagReporter.AddDiagnosticAction, isSupportedDiagnostic, analyzerState);

            diagReporter.Free();
        }

        private void ExecuteBlockActions<TBlockAction, TNodeStateData>(
            PooledHashSet<TBlockAction> blockActions,
            SyntaxNode declaredNode,
            ISymbol declaredSymbol,
            DiagnosticAnalyzer analyzer,
            SemanticModel semanticModel,
            ImmutableArray<IOperation> operationBlocks,
            Action<Diagnostic> addDiagnostic,
            Func<Diagnostic, bool> isSupportedDiagnostic,
            AnalysisState.BlockAnalyzerStateData<TBlockAction, TNodeStateData>? analyzerState)
            where TBlockAction : AnalyzerAction
            where TNodeStateData : AnalyzerStateData, new()
        {
            Debug.Assert(!IsAnalyzerSuppressedForTree(analyzer, declaredNode.SyntaxTree));

            foreach (var blockAction in blockActions)
            {
                if (ShouldExecuteAction(analyzerState, blockAction))
                {
                    var codeBlockAction = blockAction as CodeBlockAnalyzerAction;
                    if (codeBlockAction != null)
                    {
                        var context = new CodeBlockAnalysisContext(declaredNode, declaredSymbol, semanticModel, AnalyzerOptions, addDiagnostic, isSupportedDiagnostic, _cancellationToken);

                        ExecuteAndCatchIfThrows(
                            codeBlockAction.Analyzer,
                            data => data.action(data.context),
                            (action: codeBlockAction.Action, context: context),
                            new AnalysisContextInfo(Compilation, declaredSymbol, declaredNode));
                    }
                    else
                    {
                        var operationBlockAction = blockAction as OperationBlockAnalyzerAction;
                        if (operationBlockAction != null)
                        {
                            var context = new OperationBlockAnalysisContext(operationBlocks, declaredSymbol, semanticModel.Compilation,
                                AnalyzerOptions, addDiagnostic, isSupportedDiagnostic, GetControlFlowGraph, _cancellationToken);

                            ExecuteAndCatchIfThrows(
                                operationBlockAction.Analyzer,
                                data => data.action(data.context),
                                (action: operationBlockAction.Action, context),
                                new AnalysisContextInfo(Compilation, declaredSymbol));
                        }
                    }

                    analyzerState?.ProcessedActions.Add(blockAction);
                }
            }

            blockActions.Free();
        }

        internal static ImmutableSegmentedDictionary<TLanguageKindEnum, ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>> GetNodeActionsByKind<TLanguageKindEnum>(
            IEnumerable<SyntaxNodeAnalyzerAction<TLanguageKindEnum>> nodeActions)
            where TLanguageKindEnum : struct
        {
            Debug.Assert(nodeActions != null && nodeActions.Any());

            var nodeActionsByKind = PooledDictionary<TLanguageKindEnum, ArrayBuilder<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>>.GetInstance();
            foreach (var nodeAction in nodeActions)
            {
                foreach (var kind in nodeAction.Kinds)
                {
                    if (!nodeActionsByKind.TryGetValue(kind, out var actionsForKind))
                    {
                        nodeActionsByKind.Add(kind, actionsForKind = ArrayBuilder<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>.GetInstance());
                    }

                    actionsForKind.Add(nodeAction);
                }
            }

            var tuples = nodeActionsByKind.Select(kvp => KeyValuePairUtil.Create(kvp.Key, kvp.Value.ToImmutableAndFree()));
            var map = ImmutableSegmentedDictionary.CreateRange(tuples);
            nodeActionsByKind.Free();
            return map;
        }

        /// <summary>
        /// Tries to execute syntax node actions for the given analyzer for the given declaration.
        /// </summary>
        /// <returns>
        /// True, if successfully executed the actions for the given analysis scope OR all the actions have already been executed for the given analysis scope.
        /// False, if there are some pending actions that are currently being executed on another thread.
        /// </returns>
        public bool TryExecuteSyntaxNodeActions<TLanguageKindEnum>(
           IEnumerable<SyntaxNode> nodesToAnalyze,
           ImmutableSegmentedDictionary<TLanguageKindEnum, ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>> nodeActionsByKind,
           DiagnosticAnalyzer analyzer,
           SemanticModel model,
           Func<SyntaxNode, TLanguageKindEnum> getKind,
           TextSpan filterSpan,
           int declarationIndex,
           ISymbol declaredSymbol,
           AnalysisScope analysisScope,
           AnalysisState? analysisState,
           bool isGeneratedCode)
           where TLanguageKindEnum : struct
        {
            DeclarationAnalyzerStateData? analyzerState = null;

            try
            {
                if (TryStartAnalyzingDeclaration(declaredSymbol, declarationIndex, analyzer, analysisScope, analysisState, out analyzerState))
                {
                    ExecuteSyntaxNodeActionsCore(nodesToAnalyze, nodeActionsByKind, analyzer, declaredSymbol, model, getKind, filterSpan, analyzerState, isGeneratedCode);
                    return true;
                }

                return IsDeclarationComplete(declaredSymbol, declarationIndex, analyzer, analysisState);
            }
            finally
            {
                analyzerState?.ResetToReadyState();
            }
        }

        private void ExecuteSyntaxNodeActionsCore<TLanguageKindEnum>(
            IEnumerable<SyntaxNode> nodesToAnalyze,
            ImmutableSegmentedDictionary<TLanguageKindEnum, ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>> nodeActionsByKind,
            DiagnosticAnalyzer analyzer,
            ISymbol containingSymbol,
            SemanticModel model,
            Func<SyntaxNode, TLanguageKindEnum> getKind,
            TextSpan filterSpan,
            SyntaxNodeAnalyzerStateData? analyzerState,
            bool isGeneratedCode)
            where TLanguageKindEnum : struct
        {
            if (isGeneratedCode && _shouldSkipAnalysisOnGeneratedCode(analyzer) ||
                IsAnalyzerSuppressedForTree(analyzer, model.SyntaxTree))
            {
                return;
            }

            var diagReporter = GetAddSemanticDiagnostic(model.SyntaxTree, filterSpan, analyzer);

            using var _ = PooledDelegates.GetPooledFunction((d, arg) => arg.self.IsSupportedDiagnostic(arg.analyzer, d), (self: this, analyzer), out Func<Diagnostic, bool> isSupportedDiagnostic);
            ExecuteSyntaxNodeActions(nodesToAnalyze, nodeActionsByKind, analyzer, containingSymbol, model, getKind, diagReporter.AddDiagnosticAction, isSupportedDiagnostic, analyzerState);
            diagReporter.Free();
        }

        private void ExecuteSyntaxNodeActions<TLanguageKindEnum>(
            IEnumerable<SyntaxNode> nodesToAnalyze,
            ImmutableSegmentedDictionary<TLanguageKindEnum, ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>> nodeActionsByKind,
            DiagnosticAnalyzer analyzer,
            ISymbol containingSymbol,
            SemanticModel model,
            Func<SyntaxNode, TLanguageKindEnum> getKind,
            Action<Diagnostic> addDiagnostic,
            Func<Diagnostic, bool> isSupportedDiagnostic,
            SyntaxNodeAnalyzerStateData? analyzerState)
            where TLanguageKindEnum : struct
        {
            Debug.Assert(nodeActionsByKind.Any());
            Debug.Assert(!IsAnalyzerSuppressedForTree(analyzer, model.SyntaxTree));

            var partiallyProcessedNode = analyzerState?.CurrentNode;
            if (partiallyProcessedNode != null)
            {
                ExecuteSyntaxNodeActions(partiallyProcessedNode, nodeActionsByKind, containingSymbol, model, getKind, addDiagnostic, isSupportedDiagnostic, analyzerState);
            }

            foreach (var child in nodesToAnalyze)
            {
                if (ShouldExecuteNode(analyzerState, child, analyzer))
                {
                    SetCurrentNode(analyzerState, child);

                    ExecuteSyntaxNodeActions(child, nodeActionsByKind, containingSymbol, model, getKind, addDiagnostic, isSupportedDiagnostic, analyzerState);
                }
            }
        }

        private void ExecuteSyntaxNodeActions<TLanguageKindEnum>(
            SyntaxNode node,
            ImmutableSegmentedDictionary<TLanguageKindEnum, ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>> nodeActionsByKind,
            ISymbol containingSymbol,
            SemanticModel model,
            Func<SyntaxNode, TLanguageKindEnum> getKind,
            Action<Diagnostic> addDiagnostic,
            Func<Diagnostic, bool> isSupportedDiagnostic,
            SyntaxNodeAnalyzerStateData? analyzerState)
            where TLanguageKindEnum : struct
        {
            if (nodeActionsByKind.TryGetValue(getKind(node), out var actionsForKind))
            {
                foreach (var action in actionsForKind)
                {
                    ExecuteSyntaxNodeAction(action, node, containingSymbol, model, addDiagnostic, isSupportedDiagnostic, analyzerState);
                }
            }

            analyzerState?.ClearNodeAnalysisState();
        }

        internal static ImmutableSegmentedDictionary<OperationKind, ImmutableArray<OperationAnalyzerAction>> GetOperationActionsByKind(IEnumerable<OperationAnalyzerAction> operationActions)
        {
            Debug.Assert(operationActions.Any());

            var operationActionsByKind = PooledDictionary<OperationKind, ArrayBuilder<OperationAnalyzerAction>>.GetInstance();
            foreach (var operationAction in operationActions)
            {
                foreach (var kind in operationAction.Kinds)
                {
                    if (!operationActionsByKind.TryGetValue(kind, out var actionsForKind))
                    {
                        operationActionsByKind.Add(kind, actionsForKind = ArrayBuilder<OperationAnalyzerAction>.GetInstance());
                    }

                    actionsForKind.Add(operationAction);
                }
            }

            var tuples = operationActionsByKind.Select(kvp => KeyValuePairUtil.Create(kvp.Key, kvp.Value.ToImmutableAndFree()));
            var map = ImmutableSegmentedDictionary.CreateRange(tuples);
            operationActionsByKind.Free();
            return map;
        }

        /// <summary>
        /// Tries to execute operation actions for the given analyzer for the given declaration.
        /// </summary>
        /// <returns>
        /// True, if successfully executed the actions for the given analysis scope OR all the actions have already been executed for the given analysis scope.
        /// False, if there are some pending actions that are currently being executed on another thread.
        /// </returns>
        public bool TryExecuteOperationActions(
            IEnumerable<IOperation> operationsToAnalyze,
            ImmutableSegmentedDictionary<OperationKind, ImmutableArray<OperationAnalyzerAction>> operationActionsByKind,
            DiagnosticAnalyzer analyzer,
            SemanticModel model,
            TextSpan filterSpan,
            int declarationIndex,
            ISymbol declaredSymbol,
            AnalysisScope analysisScope,
            AnalysisState? analysisState,
            bool isGeneratedCode)
        {
            DeclarationAnalyzerStateData? analyzerState = null;

            try
            {
                if (TryStartAnalyzingDeclaration(declaredSymbol, declarationIndex, analyzer, analysisScope, analysisState, out analyzerState))
                {
                    ExecuteOperationActionsCore(operationsToAnalyze, operationActionsByKind, analyzer, declaredSymbol, model, filterSpan, analyzerState?.OperationBlockAnalysisState.ExecutableNodesAnalysisState, isGeneratedCode);
                    return true;
                }

                return IsDeclarationComplete(declaredSymbol, declarationIndex, analyzer, analysisState);
            }
            finally
            {
                analyzerState?.ResetToReadyState();
            }
        }

        private void ExecuteOperationActionsCore(
            IEnumerable<IOperation> operationsToAnalyze,
            ImmutableSegmentedDictionary<OperationKind, ImmutableArray<OperationAnalyzerAction>> operationActionsByKind,
            DiagnosticAnalyzer analyzer,
            ISymbol containingSymbol,
            SemanticModel model,
            TextSpan filterSpan,
            OperationAnalyzerStateData? analyzerState,
            bool isGeneratedCode)
        {
            if (isGeneratedCode && _shouldSkipAnalysisOnGeneratedCode(analyzer) ||
                IsAnalyzerSuppressedForTree(analyzer, model.SyntaxTree))
            {
                return;
            }

            var diagReporter = GetAddSemanticDiagnostic(model.SyntaxTree, filterSpan, analyzer);

            using var _ = PooledDelegates.GetPooledFunction((d, arg) => arg.self.IsSupportedDiagnostic(arg.analyzer, d), (self: this, analyzer), out Func<Diagnostic, bool> isSupportedDiagnostic);
            ExecuteOperationActions(operationsToAnalyze, operationActionsByKind, analyzer, containingSymbol, model, diagReporter.AddDiagnosticAction, isSupportedDiagnostic, analyzerState);
            diagReporter.Free();
        }

        private void ExecuteOperationActions(
            IEnumerable<IOperation> operationsToAnalyze,
            ImmutableSegmentedDictionary<OperationKind, ImmutableArray<OperationAnalyzerAction>> operationActionsByKind,
            DiagnosticAnalyzer analyzer,
            ISymbol containingSymbol,
            SemanticModel model,
            Action<Diagnostic> addDiagnostic,
            Func<Diagnostic, bool> isSupportedDiagnostic,
            OperationAnalyzerStateData? analyzerState)
        {
            Debug.Assert(operationActionsByKind != null);
            Debug.Assert(operationActionsByKind.Any());
            Debug.Assert(!IsAnalyzerSuppressedForTree(analyzer, model.SyntaxTree));

            var partiallyProcessedOperation = analyzerState?.CurrentOperation;
            if (partiallyProcessedOperation != null)
            {
                ExecuteOperationActions(partiallyProcessedOperation, operationActionsByKind, containingSymbol, model, addDiagnostic, isSupportedDiagnostic, analyzerState);
            }

            foreach (var child in operationsToAnalyze)
            {
                if (ShouldExecuteOperation(analyzerState, child, analyzer))
                {
                    SetCurrentOperation(analyzerState, child);

                    ExecuteOperationActions(child, operationActionsByKind, containingSymbol, model, addDiagnostic, isSupportedDiagnostic, analyzerState);
                }
            }
        }

        private void ExecuteOperationActions(
            IOperation operation,
            ImmutableSegmentedDictionary<OperationKind, ImmutableArray<OperationAnalyzerAction>> operationActionsByKind,
            ISymbol containingSymbol,
            SemanticModel model,
            Action<Diagnostic> addDiagnostic,
            Func<Diagnostic, bool> isSupportedDiagnostic,
            OperationAnalyzerStateData? analyzerState)
        {
            if (operationActionsByKind.TryGetValue(operation.Kind, out var actionsForKind))
            {
                foreach (var action in actionsForKind)
                {
                    ExecuteOperationAction(action, operation, containingSymbol, model, addDiagnostic, isSupportedDiagnostic, analyzerState);
                }
            }

            analyzerState?.ClearNodeAnalysisState();
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

        internal void ExecuteAndCatchIfThrows<TArg>(DiagnosticAnalyzer analyzer, Action<TArg> analyze, TArg argument, AnalysisContextInfo? info = null)
        {
            SharedStopwatch timer = default;
            if (_analyzerExecutionTimeMap != null)
            {
                timer = SharedStopwatch.StartNew();
            }

            var gate = _getAnalyzerGate?.Invoke(analyzer);
            if (gate != null)
            {
                lock (gate)
                {
                    ExecuteAndCatchIfThrows_NoLock(analyzer, analyze, argument, info);
                }
            }
            else
            {
                ExecuteAndCatchIfThrows_NoLock(analyzer, analyze, argument, info);
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
        private void ExecuteAndCatchIfThrows_NoLock<TArg>(DiagnosticAnalyzer analyzer, Action<TArg> analyze, TArg argument, AnalysisContextInfo? info)
        {
            try
            {
                _cancellationToken.ThrowIfCancellationRequested();
                analyze(argument);
            }
            catch (Exception e) when (ExceptionFilter(e))
            {
                // Diagnostic for analyzer exception.
                var diagnostic = CreateAnalyzerExceptionDiagnostic(analyzer, e, info);
                try
                {
                    _onAnalyzerException(e, analyzer, diagnostic);
                }
                catch (Exception)
                {
                    // Ignore exceptions from exception handlers.
                }
            }
        }

        internal bool ExceptionFilter(Exception ex)
        {
            if ((ex as OperationCanceledException)?.CancellationToken == _cancellationToken)
            {
                return false;
            }

            if (_analyzerExceptionFilter != null)
            {
                return _analyzerExceptionFilter(ex);
            }

            return true;
        }

        internal static Diagnostic CreateAnalyzerExceptionDiagnostic(DiagnosticAnalyzer analyzer, Exception e, AnalysisContextInfo? info = null)
        {
            var analyzerName = analyzer.ToString();
            var title = CodeAnalysisResources.CompilerAnalyzerFailure;
            var messageFormat = CodeAnalysisResources.CompilerAnalyzerThrows;
            var contextInformation = string.Join(Environment.NewLine, CreateDiagnosticDescription(info, e), CreateDisablingMessage(analyzer, analyzerName)).Trim();
            var messageArguments = new[] { analyzerName, e.GetType().ToString(), e.Message, contextInformation };
            var description = string.Format(CodeAnalysisResources.CompilerAnalyzerThrowsDescription, analyzerName, CreateDiagnosticDescription(info, e));
            var descriptor = GetAnalyzerExceptionDiagnosticDescriptor(AnalyzerExceptionDiagnosticId, title, description, messageFormat);
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
                return string.Format(CodeAnalysisResources.CompilerAnalyzerThrowsDescription, analyzerName, ex.CreateDiagnosticDescription());
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
            var messageArguments = new[] { e.GetType().ToString(), e.Message };
            var description = string.Format(CodeAnalysisResources.AnalyzerDriverThrowsDescription, e.CreateDiagnosticDescription());
            var descriptor = GetAnalyzerExceptionDiagnosticDescriptor(AnalyzerDriverExceptionDiagnosticId, title, description, messageFormat);
            return Diagnostic.Create(descriptor, Location.None, messageArguments);
        }

        internal static DiagnosticDescriptor GetAnalyzerExceptionDiagnosticDescriptor(string? id = null, string? title = null, string? description = null, string? messageFormat = null)
        {
            // TODO: It is not ideal to create a new descriptor per analyzer exception diagnostic instance.
            // However, until we add a LongMessage field to the Diagnostic, we are forced to park the instance specific description onto the Descriptor's Description field.
            // This requires us to create a new DiagnosticDescriptor instance per diagnostic instance.

            id ??= AnalyzerExceptionDiagnosticId;
            title ??= CodeAnalysisResources.CompilerAnalyzerFailure;
            messageFormat ??= CodeAnalysisResources.CompilerAnalyzerThrows;
            description ??= CodeAnalysisResources.CompilerAnalyzerFailure;

            return new DiagnosticDescriptor(
                id,
                title,
                messageFormat,
                description: description,
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

        private bool IsSupportedDiagnostic(DiagnosticAnalyzer analyzer, Diagnostic diagnostic)
        {
            Debug.Assert(_isCompilerAnalyzer != null);

            if (diagnostic is DiagnosticWithInfo)
            {
                // Compiler diagnostic
                return true;
            }

            return _analyzerManager.IsSupportedDiagnostic(analyzer, diagnostic, _isCompilerAnalyzer, this);
        }

        private Action<Diagnostic> GetAddDiagnostic(ISymbol contextSymbol, ImmutableArray<SyntaxReference> cachedDeclaringReferences, DiagnosticAnalyzer analyzer, Func<ISymbol, SyntaxReference, Compilation, CancellationToken, SyntaxNode> getTopMostNodeForAnalysis)
        {
            return GetAddDiagnostic(contextSymbol, cachedDeclaringReferences, Compilation, analyzer, _addNonCategorizedDiagnostic,
                 _addCategorizedLocalDiagnostic, _addCategorizedNonLocalDiagnostic, getTopMostNodeForAnalysis, _shouldSuppressGeneratedCodeDiagnostic, _cancellationToken);
        }

        private static Action<Diagnostic> GetAddDiagnostic(
            ISymbol contextSymbol,
            ImmutableArray<SyntaxReference> cachedDeclaringReferences,
            Compilation compilation,
            DiagnosticAnalyzer analyzer,
            Action<Diagnostic>? addNonCategorizedDiagnostic,
            Action<Diagnostic, DiagnosticAnalyzer, bool>? addCategorizedLocalDiagnostic,
            Action<Diagnostic, DiagnosticAnalyzer>? addCategorizedNonLocalDiagnostic,
            Func<ISymbol, SyntaxReference, Compilation, CancellationToken, SyntaxNode> getTopMostNodeForAnalysis,
            Func<Diagnostic, DiagnosticAnalyzer, Compilation, CancellationToken, bool> shouldSuppressGeneratedCodeDiagnostic,
            CancellationToken cancellationToken)
        {
            return diagnostic =>
            {
                if (shouldSuppressGeneratedCodeDiagnostic(diagnostic, analyzer, compilation, cancellationToken))
                {
                    return;
                }

                if (addCategorizedLocalDiagnostic == null)
                {
                    Debug.Assert(addNonCategorizedDiagnostic != null);
                    addNonCategorizedDiagnostic(diagnostic);
                    return;
                }

                Debug.Assert(addNonCategorizedDiagnostic == null);
                Debug.Assert(addCategorizedNonLocalDiagnostic != null);

                if (diagnostic.Location.IsInSource)
                {
                    foreach (var syntaxRef in cachedDeclaringReferences)
                    {
                        if (syntaxRef.SyntaxTree == diagnostic.Location.SourceTree)
                        {
                            var syntax = getTopMostNodeForAnalysis(contextSymbol, syntaxRef, compilation, cancellationToken);
                            if (diagnostic.Location.SourceSpan.IntersectsWith(syntax.FullSpan))
                            {
                                addCategorizedLocalDiagnostic(diagnostic, analyzer, false);
                                return;
                            }
                        }
                    }
                }

                addCategorizedNonLocalDiagnostic(diagnostic, analyzer);
            };
        }

        private Action<Diagnostic> GetAddCompilationDiagnostic(DiagnosticAnalyzer analyzer)
        {
            return diagnostic =>
            {
                if (_shouldSuppressGeneratedCodeDiagnostic(diagnostic, analyzer, Compilation, _cancellationToken))
                {
                    return;
                }

                if (_addCategorizedNonLocalDiagnostic == null)
                {
                    Debug.Assert(_addNonCategorizedDiagnostic != null);
                    _addNonCategorizedDiagnostic(diagnostic);
                    return;
                }

                _addCategorizedNonLocalDiagnostic(diagnostic, analyzer);
            };
        }

        private AnalyzerDiagnosticReporter GetAddSemanticDiagnostic(SyntaxTree tree, DiagnosticAnalyzer analyzer)
        {
            return AnalyzerDiagnosticReporter.GetInstance(new SourceOrAdditionalFile(tree), span: null, Compilation, analyzer, isSyntaxDiagnostic: false,
                _addNonCategorizedDiagnostic, _addCategorizedLocalDiagnostic, _addCategorizedNonLocalDiagnostic,
                _shouldSuppressGeneratedCodeDiagnostic, _cancellationToken);
        }

        private AnalyzerDiagnosticReporter GetAddSemanticDiagnostic(SyntaxTree tree, TextSpan? span, DiagnosticAnalyzer analyzer)
        {
            return AnalyzerDiagnosticReporter.GetInstance(new SourceOrAdditionalFile(tree), span, Compilation, analyzer, isSyntaxDiagnostic: false,
                _addNonCategorizedDiagnostic, _addCategorizedLocalDiagnostic, _addCategorizedNonLocalDiagnostic,
                _shouldSuppressGeneratedCodeDiagnostic, _cancellationToken);
        }

        private AnalyzerDiagnosticReporter GetAddSyntaxDiagnostic(SourceOrAdditionalFile file, DiagnosticAnalyzer analyzer)
        {
            return AnalyzerDiagnosticReporter.GetInstance(file, span: null, Compilation, analyzer, isSyntaxDiagnostic: true,
                _addNonCategorizedDiagnostic, _addCategorizedLocalDiagnostic, _addCategorizedNonLocalDiagnostic,
                _shouldSuppressGeneratedCodeDiagnostic, _cancellationToken);
        }

        private static bool ShouldExecuteAction(AnalyzerStateData? analyzerState, AnalyzerAction action)
        {
            return analyzerState == null || !analyzerState.ProcessedActions.Contains(action);
        }

        private bool ShouldExecuteNode(SyntaxNodeAnalyzerStateData? analyzerState, SyntaxNode node, DiagnosticAnalyzer analyzer)
        {
            // Check if the node has already been processed.
            if (analyzerState != null && analyzerState.ProcessedNodes.Contains(node))
            {
                return false;
            }

            // Check if the node is generated code that must be skipped.
            if (_shouldSkipAnalysisOnGeneratedCode(analyzer) &&
                _isGeneratedCodeLocation(node.SyntaxTree, node.Span))
            {
                return false;
            }

            return true;
        }

        private bool ShouldExecuteOperation(OperationAnalyzerStateData? analyzerState, IOperation operation, DiagnosticAnalyzer analyzer)
        {
            // Check if the operation has already been processed.
            if (analyzerState != null && analyzerState.ProcessedOperations.Contains(operation))
            {
                return false;
            }

            // Check if the operation syntax is generated code that must be skipped.
            if (operation.Syntax != null && _shouldSkipAnalysisOnGeneratedCode(analyzer) &&
                _isGeneratedCodeLocation(operation.Syntax.SyntaxTree, operation.Syntax.Span))
            {
                return false;
            }

            return true;
        }

        private static void SetCurrentNode(SyntaxNodeAnalyzerStateData? analyzerState, SyntaxNode node)
        {
            if (analyzerState != null)
            {
                Debug.Assert(node != null);
                analyzerState.CurrentNode = node;
            }
        }

        private static void SetCurrentOperation(OperationAnalyzerStateData? analyzerState, IOperation operation)
        {
            if (analyzerState != null)
            {
                Debug.Assert(operation != null);
                analyzerState.CurrentOperation = operation;
            }
        }

        private static bool TryStartProcessingEvent(
            CompilationEvent nonSymbolCompilationEvent,
            DiagnosticAnalyzer analyzer,
            AnalysisScope analysisScope,
            AnalysisState? analysisState,
            out AnalyzerStateData? analyzerState)
        {
            Debug.Assert(nonSymbolCompilationEvent is not SymbolDeclaredCompilationEvent);
            Debug.Assert(analysisScope.Contains(analyzer));

            analyzerState = null;
            return analysisState == null || analysisState.TryStartProcessingEvent(nonSymbolCompilationEvent, analyzer, out analyzerState);
        }

        private static bool TryStartSyntaxAnalysis(SourceOrAdditionalFile file,
            DiagnosticAnalyzer analyzer,
            AnalysisScope analysisScope,
            AnalysisState? analysisState,
            out AnalyzerStateData? analyzerState)
        {
            Debug.Assert(analysisScope.Contains(analyzer));

            analyzerState = null;
            return analysisState == null || analysisState.TryStartSyntaxAnalysis(file, analyzer, out analyzerState);
        }

        private static bool TryStartAnalyzingSymbol(
            ISymbol symbol,
            DiagnosticAnalyzer analyzer,
            AnalysisScope analysisScope,
            AnalysisState? analysisState,
            out AnalyzerStateData? analyzerState)
        {
            Debug.Assert(analysisScope.Contains(analyzer));

            analyzerState = null;
            return analysisState == null || analysisState.TryStartAnalyzingSymbol(symbol, analyzer, out analyzerState);
        }

        private static bool TryStartSymbolEndAnalysis(
            ISymbol symbol,
            DiagnosticAnalyzer analyzer,
            AnalysisState? analysisState,
            out AnalyzerStateData? analyzerState)
        {
            analyzerState = null;
            return analysisState == null || analysisState.TryStartSymbolEndAnalysis(symbol, analyzer, out analyzerState);
        }

        private static bool TryStartAnalyzingDeclaration(
            ISymbol symbol,
            int declarationIndex,
            DiagnosticAnalyzer analyzer,
            AnalysisScope analysisScope,
            AnalysisState? analysisState,
            out DeclarationAnalyzerStateData? analyzerState)
        {
            Debug.Assert(analysisScope.Contains(analyzer));

            analyzerState = null;
            return analysisState == null || analysisState.TryStartAnalyzingDeclaration(symbol, declarationIndex, analyzer, out analyzerState);
        }

        private static bool IsEventComplete(CompilationEvent compilationEvent, DiagnosticAnalyzer analyzer, AnalysisState? analysisState)
        {
            return analysisState == null || analysisState.IsEventComplete(compilationEvent, analyzer);
        }

        private static bool IsSymbolComplete(ISymbol symbol, DiagnosticAnalyzer analyzer, AnalysisState? analysisState)
        {
            return analysisState == null || analysisState.IsSymbolComplete(symbol, analyzer);
        }

        private static bool IsSymbolEndAnalysisComplete(ISymbol symbol, DiagnosticAnalyzer analyzer, AnalysisState? analysisState)
        {
            return analysisState == null || analysisState.IsSymbolEndAnalysisComplete(symbol, analyzer);
        }

        private static bool IsDeclarationComplete(ISymbol symbol, int declarationIndex, DiagnosticAnalyzer analyzer, AnalysisState? analysisState)
        {
            return analysisState == null || analysisState.IsDeclarationComplete(symbol, declarationIndex, analyzer);
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

        private bool IsAnalyzerSuppressedForSymbol(DiagnosticAnalyzer analyzer, ISymbol symbol)
        {
            foreach (var location in symbol.Locations)
            {
                if (location.SourceTree != null &&
                    !IsAnalyzerSuppressedForTree(analyzer, location.SourceTree))
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
