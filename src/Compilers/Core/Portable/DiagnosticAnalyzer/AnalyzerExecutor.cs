// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.FlowAnalysis;
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

        private readonly Compilation _compilation;
        private readonly AnalyzerOptions _analyzerOptions;
        private readonly Action<Diagnostic> _addNonCategorizedDiagnosticOpt;
        private readonly Action<Diagnostic, DiagnosticAnalyzer, bool> _addCategorizedLocalDiagnosticOpt;
        private readonly Action<Diagnostic, DiagnosticAnalyzer> _addCategorizedNonLocalDiagnosticOpt;
        private readonly Action<Suppression> _addSuppressionOpt;
        private readonly Action<Exception, DiagnosticAnalyzer, Diagnostic> _onAnalyzerException;
        private readonly Func<Exception, bool> _analyzerExceptionFilter;
        private readonly AnalyzerManager _analyzerManager;
        private readonly Func<DiagnosticAnalyzer, bool> _isCompilerAnalyzer;
        private readonly Func<DiagnosticAnalyzer, object> _getAnalyzerGateOpt;
        private readonly Func<SyntaxTree, SemanticModel> _getSemanticModelOpt;
        private readonly Func<DiagnosticAnalyzer, bool> _shouldSkipAnalysisOnGeneratedCode;
        private readonly Func<Diagnostic, DiagnosticAnalyzer, Compilation, CancellationToken, bool> _shouldSuppressGeneratedCodeDiagnostic;
        private readonly Func<SyntaxTree, TextSpan, bool> _isGeneratedCodeLocation;
        private readonly Func<DiagnosticAnalyzer, SyntaxTree, bool> _isAnalyzerSuppressedForTree;

        /// <summary>
        /// The values in this map convert to <see cref="TimeSpan"/> using <see cref="TimeSpan.FromTicks(long)"/>.
        /// </summary>
        private readonly ConcurrentDictionary<DiagnosticAnalyzer, StrongBox<long>> _analyzerExecutionTimeMapOpt;
        private readonly CompilationAnalysisValueProviderFactory _compilationAnalysisValueProviderFactory;
        private readonly CancellationToken _cancellationToken;

        private ConcurrentDictionary<IOperation, ControlFlowGraph> _lazyControlFlowGraphMap;

        /// <summary>
        /// Creates <see cref="AnalyzerExecutor"/> to execute analyzer actions with given arguments
        /// </summary>
        /// <param name="compilation">Compilation to be used in the analysis.</param>
        /// <param name="analyzerOptions">Analyzer options.</param>
        /// <param name="addNonCategorizedDiagnosticOpt">Optional delegate to add non-categorized analyzer diagnostics.</param>
        /// <param name="onAnalyzerException">
        /// Optional delegate which is invoked when an analyzer throws an exception.
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
        /// <param name="addCategorizedLocalDiagnosticOpt">Optional delegate to add categorized local analyzer diagnostics.</param>
        /// <param name="addCategorizedNonLocalDiagnosticOpt">Optional delegate to add categorized non-local analyzer diagnostics.</param>
        /// <param name="addSuppressionOpt">Optional thread-safe delegate to add diagnostic suppressions from suppressors.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static AnalyzerExecutor Create(
            Compilation compilation,
            AnalyzerOptions analyzerOptions,
            Action<Diagnostic> addNonCategorizedDiagnosticOpt,
            Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException,
            Func<Exception, bool> analyzerExceptionFilter,
            Func<DiagnosticAnalyzer, bool> isCompilerAnalyzer,
            AnalyzerManager analyzerManager,
            Func<DiagnosticAnalyzer, bool> shouldSkipAnalysisOnGeneratedCode,
            Func<Diagnostic, DiagnosticAnalyzer, Compilation, CancellationToken, bool> shouldSuppressGeneratedCodeDiagnostic,
            Func<SyntaxTree, TextSpan, bool> isGeneratedCodeLocation,
            Func<DiagnosticAnalyzer, SyntaxTree, bool> isAnalyzerSuppressedForTree,
            Func<DiagnosticAnalyzer, object> getAnalyzerGate,
            Func<SyntaxTree, SemanticModel> getSemanticModel,
            bool logExecutionTime = false,
            Action<Diagnostic, DiagnosticAnalyzer, bool> addCategorizedLocalDiagnosticOpt = null,
            Action<Diagnostic, DiagnosticAnalyzer> addCategorizedNonLocalDiagnosticOpt = null,
            Action<Suppression> addSuppressionOpt = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            // We can either report categorized (local/non-local) diagnostics or non-categorized diagnostics.
            Debug.Assert((addNonCategorizedDiagnosticOpt != null) ^ (addCategorizedLocalDiagnosticOpt != null));
            Debug.Assert((addCategorizedLocalDiagnosticOpt != null) == (addCategorizedNonLocalDiagnosticOpt != null));

            var analyzerExecutionTimeMapOpt = logExecutionTime ? new ConcurrentDictionary<DiagnosticAnalyzer, StrongBox<long>>() : null;

            return new AnalyzerExecutor(compilation, analyzerOptions, addNonCategorizedDiagnosticOpt, onAnalyzerException, analyzerExceptionFilter,
                isCompilerAnalyzer, analyzerManager, shouldSkipAnalysisOnGeneratedCode, shouldSuppressGeneratedCodeDiagnostic, isGeneratedCodeLocation,
                isAnalyzerSuppressedForTree, getAnalyzerGate, getSemanticModel, analyzerExecutionTimeMapOpt, addCategorizedLocalDiagnosticOpt, addCategorizedNonLocalDiagnosticOpt,
                addSuppressionOpt, cancellationToken);
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
            Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException,
            AnalyzerManager analyzerManager,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return new AnalyzerExecutor(
                compilation: null,
                analyzerOptions: null,
                addNonCategorizedDiagnosticOpt: null,
                isCompilerAnalyzer: null,
                shouldSkipAnalysisOnGeneratedCode: _ => false,
                shouldSuppressGeneratedCodeDiagnostic: (diagnostic, analyzer, compilation, ct) => false,
                isGeneratedCodeLocation: (_1, _2) => false,
                isAnalyzerSuppressedForTreeOpt: null,
                getAnalyzerGateOpt: null,
                getSemanticModelOpt: null,
                onAnalyzerException: onAnalyzerException,
                analyzerExceptionFilter: null,
                analyzerManager: analyzerManager,
                analyzerExecutionTimeMapOpt: null,
                addCategorizedLocalDiagnosticOpt: null,
                addCategorizedNonLocalDiagnosticOpt: null,
                addSuppressionOpt: null,
                cancellationToken: cancellationToken);
        }

        private AnalyzerExecutor(
            Compilation compilation,
            AnalyzerOptions analyzerOptions,
            Action<Diagnostic> addNonCategorizedDiagnosticOpt,
            Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException,
            Func<Exception, bool> analyzerExceptionFilter,
            Func<DiagnosticAnalyzer, bool> isCompilerAnalyzer,
            AnalyzerManager analyzerManager,
            Func<DiagnosticAnalyzer, bool> shouldSkipAnalysisOnGeneratedCode,
            Func<Diagnostic, DiagnosticAnalyzer, Compilation, CancellationToken, bool> shouldSuppressGeneratedCodeDiagnostic,
            Func<SyntaxTree, TextSpan, bool> isGeneratedCodeLocation,
            Func<DiagnosticAnalyzer, SyntaxTree, bool> isAnalyzerSuppressedForTreeOpt,
            Func<DiagnosticAnalyzer, object> getAnalyzerGateOpt,
            Func<SyntaxTree, SemanticModel> getSemanticModelOpt,
            ConcurrentDictionary<DiagnosticAnalyzer, StrongBox<long>> analyzerExecutionTimeMapOpt,
            Action<Diagnostic, DiagnosticAnalyzer, bool> addCategorizedLocalDiagnosticOpt,
            Action<Diagnostic, DiagnosticAnalyzer> addCategorizedNonLocalDiagnosticOpt,
            Action<Suppression> addSuppressionOpt,
            CancellationToken cancellationToken)
        {
            _compilation = compilation;
            _analyzerOptions = analyzerOptions;
            _addNonCategorizedDiagnosticOpt = addNonCategorizedDiagnosticOpt;
            _onAnalyzerException = onAnalyzerException;
            _analyzerExceptionFilter = analyzerExceptionFilter;
            _isCompilerAnalyzer = isCompilerAnalyzer;
            _analyzerManager = analyzerManager;
            _shouldSkipAnalysisOnGeneratedCode = shouldSkipAnalysisOnGeneratedCode;
            _shouldSuppressGeneratedCodeDiagnostic = shouldSuppressGeneratedCodeDiagnostic;
            _isGeneratedCodeLocation = isGeneratedCodeLocation;
            _isAnalyzerSuppressedForTree = isAnalyzerSuppressedForTreeOpt;
            _getAnalyzerGateOpt = getAnalyzerGateOpt;
            _getSemanticModelOpt = getSemanticModelOpt;
            _analyzerExecutionTimeMapOpt = analyzerExecutionTimeMapOpt;
            _addCategorizedLocalDiagnosticOpt = addCategorizedLocalDiagnosticOpt;
            _addCategorizedNonLocalDiagnosticOpt = addCategorizedNonLocalDiagnosticOpt;
            _addSuppressionOpt = addSuppressionOpt;
            _cancellationToken = cancellationToken;

            _compilationAnalysisValueProviderFactory = new CompilationAnalysisValueProviderFactory();
        }

        public AnalyzerExecutor WithCancellationToken(CancellationToken cancellationToken)
        {
            if (cancellationToken == _cancellationToken)
            {
                return this;
            }

            return new AnalyzerExecutor(_compilation, _analyzerOptions, _addNonCategorizedDiagnosticOpt, _onAnalyzerException, _analyzerExceptionFilter,
                _isCompilerAnalyzer, _analyzerManager, _shouldSkipAnalysisOnGeneratedCode, _shouldSuppressGeneratedCodeDiagnostic, _isGeneratedCodeLocation,
                _isAnalyzerSuppressedForTree, _getAnalyzerGateOpt, _getSemanticModelOpt, _analyzerExecutionTimeMapOpt, _addCategorizedLocalDiagnosticOpt, _addCategorizedNonLocalDiagnosticOpt,
                _addSuppressionOpt, cancellationToken);
        }

        internal Compilation Compilation => _compilation;
        internal AnalyzerOptions AnalyzerOptions => _analyzerOptions;
        internal CancellationToken CancellationToken => _cancellationToken;
        internal Action<Exception, DiagnosticAnalyzer, Diagnostic> OnAnalyzerException => _onAnalyzerException;
        internal ImmutableDictionary<DiagnosticAnalyzer, TimeSpan> AnalyzerExecutionTimes => _analyzerExecutionTimeMapOpt.ToImmutableDictionary(pair => pair.Key, pair => TimeSpan.FromTicks(pair.Value.Value));

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
                (analyzer: analyzer, context: context));
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
                    _compilation, _analyzerOptions, _compilationAnalysisValueProviderFactory, _cancellationToken);

                ExecuteAndCatchIfThrows(
                    startAction.Analyzer,
                    data => data.action(data.context),
                    (action: startAction.Action, context: context),
                    new AnalysisContextInfo(_compilation));
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
                    symbol, _compilation, _analyzerOptions, _cancellationToken);

                ExecuteAndCatchIfThrows(
                    startAction.Analyzer,
                    data => data.action(data.context),
                    (action: startAction.Action, context: context),
                    new AnalysisContextInfo(_compilation, symbol));
            }
        }

        /// <summary>
        /// Executes the given diagnostic suppressor.
        /// </summary>
        /// <param name="suppressor">Suppressor to be executed.</param>
        /// <param name="reportedDiagnostics">Reported analyzer/compiler diagnostics that can be suppressed.</param>
        public void ExecuteSuppressionAction(DiagnosticSuppressor suppressor, ImmutableArray<Diagnostic> reportedDiagnostics)
        {
            Debug.Assert(_addSuppressionOpt != null);

            if (reportedDiagnostics.IsEmpty)
            {
                return;
            }

            _cancellationToken.ThrowIfCancellationRequested();

            var supportedSuppressions = _analyzerManager.GetSupportedSuppressionDescriptors(suppressor, this);
            Func<SuppressionDescriptor, bool> isSupportedSuppression = supportedSuppressions.Contains;
            Action<SuppressionAnalysisContext> action = suppressor.ReportSuppressions;
            var context = new SuppressionAnalysisContext(_compilation, _analyzerOptions,
                reportedDiagnostics, _addSuppressionOpt, isSupportedSuppression, _getSemanticModelOpt, _cancellationToken);

            ExecuteAndCatchIfThrows(
                suppressor,
                data => data.action(data.context),
                (action, context),
                new AnalysisContextInfo(_compilation));
        }

        /// <summary>
        /// Tries to executes compilation actions or compilation end actions.
        /// </summary>
        /// <param name="compilationActions">Compilation actions to be executed.</param>
        /// <param name="analyzer">Analyzer whose actions are to be executed.</param>
        /// <param name="compilationEvent">Compilation event.</param>
        /// <param name="analysisScope">Scope for analyzer execution.</param>
        /// <param name="analysisStateOpt">An optional object to track analysis state.</param>
        /// <returns>
        /// True, if successfully executed the actions for the given analysis scope OR all the actions have already been executed for the given analysis scope.
        /// False, if there are some pending actions that are currently being executed on another thread.
        /// </returns>
        public bool TryExecuteCompilationActions(
            ImmutableArray<CompilationAnalyzerAction> compilationActions,
            DiagnosticAnalyzer analyzer,
            CompilationEvent compilationEvent,
            AnalysisScope analysisScope,
            AnalysisState analysisStateOpt)
        {
            Debug.Assert(compilationEvent is CompilationStartedEvent || compilationEvent is CompilationCompletedEvent);

            AnalyzerStateData analyzerStateOpt = null;

            try
            {
                if (TryStartProcessingEvent(compilationEvent, analyzer, analysisScope, analysisStateOpt, out analyzerStateOpt))
                {
                    ExecuteCompilationActionsCore(compilationActions, analyzer, analyzerStateOpt);
                    analysisStateOpt?.MarkEventComplete(compilationEvent, analyzer);
                    return true;
                }

                return IsEventComplete(compilationEvent, analyzer, analysisStateOpt);
            }
            finally
            {
                analyzerStateOpt?.ResetToReadyState();
            }
        }

        private void ExecuteCompilationActionsCore(ImmutableArray<CompilationAnalyzerAction> compilationActions, DiagnosticAnalyzer analyzer, AnalyzerStateData analyzerStateOpt)
        {
            var addDiagnostic = GetAddCompilationDiagnostic(analyzer);
            Func<Diagnostic, bool> isSupportedDiagnostic = d => IsSupportedDiagnostic(analyzer, d);

            foreach (var endAction in compilationActions)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                if (ShouldExecuteAction(analyzerStateOpt, endAction))
                {
                    var context = new CompilationAnalysisContext(
                        _compilation, _analyzerOptions, addDiagnostic,
                        isSupportedDiagnostic, _compilationAnalysisValueProviderFactory, _cancellationToken);

                    ExecuteAndCatchIfThrows(
                        endAction.Analyzer,
                        data => data.action(data.context),
                        (action: endAction.Action, context: context),
                        new AnalysisContextInfo(_compilation));

                    analyzerStateOpt?.ProcessedActions.Add(endAction);
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
        /// <param name="analysisStateOpt">An optional object to track analysis state.</param>
        /// <param name="isGeneratedCodeSymbol">Flag indicating if this is a generated code symbol.</param>
        /// <returns>
        /// True, if successfully executed the actions for the given analysis scope OR all the actions have already been executed for the given analysis scope.
        /// False, if there are some pending actions that are currently being executed on another thread.
        /// </returns>
        public bool TryExecuteSymbolActions(
            ImmutableArray<SymbolAnalyzerAction> symbolActions,
            DiagnosticAnalyzer analyzer,
            SymbolDeclaredCompilationEvent symbolDeclaredEvent,
            Func<ISymbol, SyntaxReference, Compilation, SyntaxNode> getTopMostNodeForAnalysis,
            AnalysisScope analysisScope,
            AnalysisState analysisStateOpt,
            bool isGeneratedCodeSymbol)
        {
            AnalyzerStateData analyzerStateOpt = null;

            try
            {
                var symbol = symbolDeclaredEvent.Symbol;
                if (TryStartAnalyzingSymbol(symbol, analyzer, analysisScope, analysisStateOpt, out analyzerStateOpt))
                {
                    ExecuteSymbolActionsCore(symbolActions, analyzer, symbolDeclaredEvent, getTopMostNodeForAnalysis, analyzerStateOpt, isGeneratedCodeSymbol);
                    analysisStateOpt?.MarkSymbolComplete(symbol, analyzer);
                    return true;
                }

                return IsSymbolComplete(symbol, analyzer, analysisStateOpt);
            }
            finally
            {
                analyzerStateOpt?.ResetToReadyState();
            }
        }

        private void ExecuteSymbolActionsCore(
            ImmutableArray<SymbolAnalyzerAction> symbolActions,
            DiagnosticAnalyzer analyzer,
            SymbolDeclaredCompilationEvent symbolDeclaredEvent,
            Func<ISymbol, SyntaxReference, Compilation, SyntaxNode> getTopMostNodeForAnalysis,
            AnalyzerStateData analyzerStateOpt,
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
            Func<Diagnostic, bool> isSupportedDiagnostic = d => IsSupportedDiagnostic(analyzer, d);

            foreach (var symbolAction in symbolActions)
            {
                var action = symbolAction.Action;
                var kinds = symbolAction.Kinds;

                if (kinds.Contains(symbol.Kind))
                {
                    if (ShouldExecuteAction(analyzerStateOpt, symbolAction))
                    {
                        _cancellationToken.ThrowIfCancellationRequested();

                        var context = new SymbolAnalysisContext(symbol, _compilation, _analyzerOptions, addDiagnostic,
                            isSupportedDiagnostic, _cancellationToken);

                        ExecuteAndCatchIfThrows(
                            symbolAction.Analyzer,
                            data => data.action(data.context),
                            (action: action, context: context),
                            new AnalysisContextInfo(_compilation, symbol));

                        analyzerStateOpt?.ProcessedActions.Add(symbolAction);
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
        /// <param name="analysisStateOpt">An optional object to track analysis state.</param>
        /// <returns>
        /// True, if successfully executed the actions for the given analysis scope OR all the actions have already been executed for the given analysis scope.
        /// False, if there are some pending actions.
        /// </returns>
        public bool TryExecuteSymbolEndActionsForContainer(
            INamespaceOrTypeSymbol containingSymbol,
            ISymbol processedMemberSymbol,
            DiagnosticAnalyzer analyzer,
            Func<ISymbol, SyntaxReference, Compilation, SyntaxNode> getTopMostNodeForAnalysis,
            AnalysisState analysisStateOpt,
            out SymbolDeclaredCompilationEvent containingSymbolDeclaredEvent)
        {
            Debug.Assert(containingSymbol != null);

            containingSymbolDeclaredEvent = null;
            if (!_analyzerManager.TryProcessCompletedMemberAndGetPendingSymbolEndActionsForContainer(containingSymbol, processedMemberSymbol, analyzer, out var containerEndActionsAndEvent))
            {
                return false;
            }

            ImmutableArray<SymbolEndAnalyzerAction> endActions = containerEndActionsAndEvent.symbolEndActions;
            containingSymbolDeclaredEvent = containerEndActionsAndEvent.symbolDeclaredEvent;
            return TryExecuteSymbolEndActionsCore(endActions, analyzer, containingSymbolDeclaredEvent, getTopMostNodeForAnalysis, analysisStateOpt);
        }

        /// <summary>
        /// Tries to execute the symbol end actions on the given symbol for the given analyzer.
        /// </summary>
        /// <param name="symbolEndActions">Symbol actions to be executed.</param>
        /// <param name="analyzer">Analyzer whose actions are to be executed.</param>
        /// <param name="symbolDeclaredEvent">Symbol event to be analyzed.</param>
        /// <param name="getTopMostNodeForAnalysis">Delegate to get topmost declaration node for a symbol declaration reference.</param>
        /// <param name="analysisStateOpt">An optional object to track analysis state.</param>
        /// <returns>
        /// True, if successfully executed the actions for the given analysis scope OR all the actions have already been executed for the given analysis scope.
        /// False, if there are some pending actions.
        /// </returns>
        public bool TryExecuteSymbolEndActions(
            ImmutableArray<SymbolEndAnalyzerAction> symbolEndActions,
            DiagnosticAnalyzer analyzer,
            SymbolDeclaredCompilationEvent symbolDeclaredEvent,
            Func<ISymbol, SyntaxReference, Compilation, SyntaxNode> getTopMostNodeForAnalysis,
            AnalysisState analysisStateOpt)
        {
            return _analyzerManager.TryStartExecuteSymbolEndActions(symbolEndActions, analyzer, symbolDeclaredEvent) &&
                TryExecuteSymbolEndActionsCore(symbolEndActions, analyzer, symbolDeclaredEvent, getTopMostNodeForAnalysis, analysisStateOpt);
        }

        private bool TryExecuteSymbolEndActionsCore(
            ImmutableArray<SymbolEndAnalyzerAction> symbolEndActions,
            DiagnosticAnalyzer analyzer,
            SymbolDeclaredCompilationEvent symbolDeclaredEvent,
            Func<ISymbol, SyntaxReference, Compilation, SyntaxNode> getTopMostNodeForAnalysis,
            AnalysisState analysisStateOpt)
        {
            var symbol = symbolDeclaredEvent.Symbol;

            AnalyzerStateData analyzerStateOpt = null;

            try
            {
                if (TryStartSymbolEndAnalysis(symbol, analyzer, analysisStateOpt, out analyzerStateOpt))
                {
                    ExecuteSymbolEndActionsCore(symbolEndActions, analyzer, symbolDeclaredEvent, getTopMostNodeForAnalysis, analyzerStateOpt);
                    MarkSymbolEndAnalysisComplete(symbol, analyzer, analysisStateOpt);
                    return true;
                }

                if (!IsSymbolEndAnalysisComplete(symbol, analyzer, analysisStateOpt))
                {
                    _analyzerManager.MarkSymbolEndAnalysisPending(symbol, analyzer, symbolEndActions, symbolDeclaredEvent);
                    return false;
                }

                return true;
            }
            finally
            {
                analyzerStateOpt?.ResetToReadyState();
            }
        }

        public void MarkSymbolEndAnalysisComplete(ISymbol symbol, DiagnosticAnalyzer analyzer, AnalysisState analysisStateOpt)
        {
            analysisStateOpt?.MarkSymbolEndAnalysisComplete(symbol, analyzer);
            _analyzerManager.MarkSymbolEndAnalysisComplete(symbol, analyzer);
        }

        private void ExecuteSymbolEndActionsCore(
            ImmutableArray<SymbolEndAnalyzerAction> symbolEndActions,
            DiagnosticAnalyzer analyzer,
            SymbolDeclaredCompilationEvent symbolDeclaredEvent,
            Func<ISymbol, SyntaxReference, Compilation, SyntaxNode> getTopMostNodeForAnalysis,
            AnalyzerStateData analyzerStateOpt)
        {
            Debug.Assert(getTopMostNodeForAnalysis != null);
            Debug.Assert(!IsAnalyzerSuppressedForSymbol(analyzer, symbolDeclaredEvent.Symbol));

            var symbol = symbolDeclaredEvent.Symbol;
            var addDiagnostic = GetAddDiagnostic(symbol, symbolDeclaredEvent.DeclaringSyntaxReferences, analyzer, getTopMostNodeForAnalysis);
            Func<Diagnostic, bool> isSupportedDiagnostic = d => IsSupportedDiagnostic(analyzer, d);

            foreach (var symbolAction in symbolEndActions)
            {
                var action = symbolAction.Action;

                if (ShouldExecuteAction(analyzerStateOpt, symbolAction))
                {
                    _cancellationToken.ThrowIfCancellationRequested();

                    var context = new SymbolAnalysisContext(symbol, _compilation, _analyzerOptions, addDiagnostic,
                        isSupportedDiagnostic, _cancellationToken);

                    ExecuteAndCatchIfThrows(
                        symbolAction.Analyzer,
                        data => data.action(data.context),
                        (action: action, context: context),
                        new AnalysisContextInfo(_compilation, symbol));

                    analyzerStateOpt?.ProcessedActions.Add(symbolAction);
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
        /// <param name="analysisStateOpt">An optional object to track analysis state.</param>
        /// <param name="isGeneratedCode">Flag indicating if the syntax tree being analyzed is generated code.</param>
        /// <returns>
        /// True, if successfully executed the actions for the given analysis scope OR all the actions have already been executed for the given analysis scope.
        /// False, if there are some pending actions that are currently being executed on another thread.
        /// </returns>
        public bool TryExecuteSemanticModelActions(
            ImmutableArray<SemanticModelAnalyzerAction> semanticModelActions,
            DiagnosticAnalyzer analyzer,
            SemanticModel semanticModel,
            CompilationEvent compilationUnitCompletedEvent,
            AnalysisScope analysisScope,
            AnalysisState analysisStateOpt,
            bool isGeneratedCode)
        {
            AnalyzerStateData analyzerStateOpt = null;

            try
            {
                if (TryStartProcessingEvent(compilationUnitCompletedEvent, analyzer, analysisScope, analysisStateOpt, out analyzerStateOpt))
                {
                    ExecuteSemanticModelActionsCore(semanticModelActions, analyzer, semanticModel, analyzerStateOpt, isGeneratedCode);
                    analysisStateOpt?.MarkEventComplete(compilationUnitCompletedEvent, analyzer);
                    return true;
                }

                return IsEventComplete(compilationUnitCompletedEvent, analyzer, analysisStateOpt);
            }
            finally
            {
                analyzerStateOpt?.ResetToReadyState();
            }
        }

        private void ExecuteSemanticModelActionsCore(
            ImmutableArray<SemanticModelAnalyzerAction> semanticModelActions,
            DiagnosticAnalyzer analyzer,
            SemanticModel semanticModel,
            AnalyzerStateData analyzerStateOpt,
            bool isGeneratedCode)
        {
            if (isGeneratedCode && _shouldSkipAnalysisOnGeneratedCode(analyzer) ||
                _isAnalyzerSuppressedForTree(analyzer, semanticModel.SyntaxTree))
            {
                return;
            }

            var diagReporter = GetAddDiagnostic(semanticModel.SyntaxTree, analyzer, isSyntaxDiagnostic: false);
            Func<Diagnostic, bool> isSupportedDiagnostic = d => IsSupportedDiagnostic(analyzer, d);

            foreach (var semanticModelAction in semanticModelActions)
            {
                if (ShouldExecuteAction(analyzerStateOpt, semanticModelAction))
                {
                    _cancellationToken.ThrowIfCancellationRequested();

                    var context = new SemanticModelAnalysisContext(semanticModel, _analyzerOptions, diagReporter.AddDiagnosticAction,
                        isSupportedDiagnostic, _cancellationToken);

                    // Catch Exception from action.
                    ExecuteAndCatchIfThrows(
                        semanticModelAction.Analyzer,
                        data => data.action(data.context),
                        (action: semanticModelAction.Action, context: context),
                        new AnalysisContextInfo(semanticModel));

                    analyzerStateOpt?.ProcessedActions.Add(semanticModelAction);
                }
            }

            diagReporter.Free();
        }

        /// <summary>
        /// Tries to execute the syntax tree actions on the given syntax tree.
        /// </summary>
        /// <param name="syntaxTreeActions">Syntax tree actions to be executed.</param>
        /// <param name="analyzer">Analyzer whose actions are to be executed.</param>
        /// <param name="tree">Syntax tree to analyze.</param>
        /// <param name="analysisScope">Scope for analyzer execution.</param>
        /// <param name="analysisStateOpt">An optional object to track analysis state.</param>
        /// <param name="isGeneratedCode">Flag indicating if the syntax tree being analyzed is generated code.</param>
        /// <returns>
        /// True, if successfully executed the actions for the given analysis scope OR all the actions have already been executed for the given analysis scope.
        /// False, if there are some pending actions that are currently being executed on another thread.
        /// </returns>
        public bool TryExecuteSyntaxTreeActions(
            ImmutableArray<SyntaxTreeAnalyzerAction> syntaxTreeActions,
            DiagnosticAnalyzer analyzer,
            SyntaxTree tree,
            AnalysisScope analysisScope,
            AnalysisState analysisStateOpt,
            bool isGeneratedCode)
        {
            AnalyzerStateData analyzerStateOpt = null;

            try
            {
                if (TryStartSyntaxAnalysis(tree, analyzer, analysisScope, analysisStateOpt, out analyzerStateOpt))
                {
                    ExecuteSyntaxTreeActionsCore(syntaxTreeActions, analyzer, tree, analyzerStateOpt, isGeneratedCode);
                    analysisStateOpt?.MarkSyntaxAnalysisComplete(tree, analyzer);
                    return true;
                }

                return analysisStateOpt == null || !analysisStateOpt.HasPendingSyntaxAnalysis(analysisScope);
            }
            finally
            {
                analyzerStateOpt?.ResetToReadyState();
            }
        }

        private void ExecuteSyntaxTreeActionsCore(
            ImmutableArray<SyntaxTreeAnalyzerAction> syntaxTreeActions,
            DiagnosticAnalyzer analyzer,
            SyntaxTree tree,
            AnalyzerStateData analyzerStateOpt,
            bool isGeneratedCode)
        {
            if (isGeneratedCode && _shouldSkipAnalysisOnGeneratedCode(analyzer) ||
                _isAnalyzerSuppressedForTree(analyzer, tree))
            {
                return;
            }

            var diagReporter = GetAddDiagnostic(tree, analyzer, isSyntaxDiagnostic: true);
            Func<Diagnostic, bool> isSupportedDiagnostic = d => IsSupportedDiagnostic(analyzer, d);

            foreach (var syntaxTreeAction in syntaxTreeActions)
            {
                if (ShouldExecuteAction(analyzerStateOpt, syntaxTreeAction))
                {
                    _cancellationToken.ThrowIfCancellationRequested();

                    var context = new SyntaxTreeAnalysisContext(tree, _analyzerOptions, diagReporter.AddDiagnosticAction, isSupportedDiagnostic, _compilation, _cancellationToken);

                    // Catch Exception from action.
                    ExecuteAndCatchIfThrows(
                        syntaxTreeAction.Analyzer,
                        data => data.action(data.context),
                        (action: syntaxTreeAction.Action, context: context),
                        new AnalysisContextInfo(_compilation, tree));

                    analyzerStateOpt?.ProcessedActions.Add(syntaxTreeAction);
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
            SyntaxNodeAnalyzerStateData analyzerStateOpt)
            where TLanguageKindEnum : struct
        {
            Debug.Assert(analyzerStateOpt == null || analyzerStateOpt.CurrentNode == node);
            Debug.Assert(!_isAnalyzerSuppressedForTree(syntaxNodeAction.Analyzer, node.SyntaxTree));

            if (ShouldExecuteAction(analyzerStateOpt, syntaxNodeAction))
            {
                var syntaxNodeContext = new SyntaxNodeAnalysisContext(node, containingSymbol, semanticModel, _analyzerOptions, addDiagnostic,
                    isSupportedDiagnostic, _cancellationToken);

                ExecuteAndCatchIfThrows(
                    syntaxNodeAction.Analyzer,
                    data => data.action(data.context),
                    (action: syntaxNodeAction.Action, context: syntaxNodeContext),
                    new AnalysisContextInfo(_compilation, node));

                analyzerStateOpt?.ProcessedActions.Add(syntaxNodeAction);
            }
        }

        private void ExecuteOperationAction(
            OperationAnalyzerAction operationAction,
            IOperation operation,
            ISymbol containingSymbol,
            SemanticModel semanticModel,
            Action<Diagnostic> addDiagnostic,
            Func<Diagnostic, bool> isSupportedDiagnostic,
            OperationAnalyzerStateData analyzerStateOpt)
        {
            Debug.Assert(analyzerStateOpt == null || analyzerStateOpt.CurrentOperation == operation);
            Debug.Assert(!_isAnalyzerSuppressedForTree(operationAction.Analyzer, semanticModel.SyntaxTree));

            if (ShouldExecuteAction(analyzerStateOpt, operationAction))
            {
                var operationContext = new OperationAnalysisContext(operation, containingSymbol, semanticModel.Compilation,
                    _analyzerOptions, addDiagnostic, isSupportedDiagnostic, GetControlFlowGraph, _cancellationToken);
                ExecuteAndCatchIfThrows(
                    operationAction.Analyzer,
                    data => data.action(data.context),
                    (action: operationAction.Action, context: operationContext),
                    new AnalysisContextInfo(_compilation, operation));

                analyzerStateOpt?.ProcessedActions.Add(operationAction);
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
            SyntaxReference declaration,
            int declarationIndex,
            AnalysisScope analysisScope,
            AnalysisState analysisStateOpt,
            bool isGeneratedCode)
            where TLanguageKindEnum : struct
        {
            DeclarationAnalyzerStateData analyzerStateOpt = null;

            try
            {
                if (TryStartAnalyzingDeclaration(declaredSymbol, declarationIndex, analyzer, analysisScope, analysisStateOpt, out analyzerStateOpt))
                {
                    ExecuteBlockActionsCore<CodeBlockStartAnalyzerAction<TLanguageKindEnum>, CodeBlockAnalyzerAction, SyntaxNodeAnalyzerAction<TLanguageKindEnum>, SyntaxNodeAnalyzerStateData, SyntaxNode, TLanguageKindEnum>(
                        codeBlockStartActions, codeBlockActions, codeBlockEndActions, analyzer,
                        declaredNode, declaredSymbol, executableCodeBlocks, (codeBlocks) => codeBlocks.SelectMany(cb => cb.DescendantNodesAndSelf()),
                        semanticModel, getKind, analyzerStateOpt?.CodeBlockAnalysisState, isGeneratedCode);
                    return true;
                }

                return IsDeclarationComplete(declaredSymbol, declarationIndex, analyzer, analysisStateOpt);
            }
            finally
            {
                analyzerStateOpt?.ResetToReadyState();
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
            SyntaxReference declaration,
            int declarationIndex,
            AnalysisScope analysisScope,
            AnalysisState analysisStateOpt,
            bool isGeneratedCode)
        {
            DeclarationAnalyzerStateData analyzerStateOpt = null;

            try
            {
                if (TryStartAnalyzingDeclaration(declaredSymbol, declarationIndex, analyzer, analysisScope, analysisStateOpt, out analyzerStateOpt))
                {
                    ExecuteBlockActionsCore<OperationBlockStartAnalyzerAction, OperationBlockAnalyzerAction, OperationAnalyzerAction, OperationAnalyzerStateData, IOperation, int>(
                        operationBlockStartActions, operationBlockActions, operationBlockEndActions, analyzer,
                        declaredNode, declaredSymbol, operationBlocks, (blocks) => operations, semanticModel,
                        null, analyzerStateOpt?.OperationBlockAnalysisState, isGeneratedCode);
                    return true;
                }

                return IsDeclarationComplete(declaredSymbol, declarationIndex, analyzer, analysisStateOpt);
            }
            finally
            {
                analyzerStateOpt?.ResetToReadyState();
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
           Func<SyntaxNode, TLanguageKindEnum> getKind,
           AnalysisState.BlockAnalyzerStateData<TBlockAction, TNodeStateData> analyzerStateOpt,
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
                _isAnalyzerSuppressedForTree(analyzer, declaredNode.SyntaxTree))
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
            if (analyzerStateOpt?.CurrentBlockEndActions != null)
            {
                // We have partially processed the code block actions.
                blockEndActions.AddAll(analyzerStateOpt.CurrentBlockEndActions.Cast<TBlockAction>());
                executableNodeActions.AddRange(analyzerStateOpt.CurrentBlockNodeActions.Cast<TNodeAction>());
            }
            else
            {
                // We have begun to process the code block actions.
                blockEndActions.AddAll(endActions);
            }

            var diagReporter = GetAddDiagnostic(semanticModel.SyntaxTree, declaredNode.FullSpan, analyzer, isSyntaxDiagnostic: false);
            Func<Diagnostic, bool> isSupportedDiagnostic = d => IsSupportedDiagnostic(analyzer, d);

            try
            {
                // Include the stateful actions.
                foreach (var startAction in startActions)
                {
                    if (ShouldExecuteAction(analyzerStateOpt, startAction))
                    {
                        var codeBlockStartAction = startAction as CodeBlockStartAnalyzerAction<TLanguageKindEnum>;
                        if (codeBlockStartAction != null)
                        {
                            var codeBlockEndActions = blockEndActions as PooledHashSet<CodeBlockAnalyzerAction>;
                            var codeBlockScope = new HostCodeBlockStartAnalysisScope<TLanguageKindEnum>();
                            var blockStartContext = new AnalyzerCodeBlockStartAnalysisContext<TLanguageKindEnum>(startAction.Analyzer,
                                codeBlockScope, declaredNode, declaredSymbol, semanticModel, _analyzerOptions, _cancellationToken);

                            // Catch Exception from the start action.
                            ExecuteAndCatchIfThrows(
                                startAction.Analyzer,
                                data =>
                                {
                                    data.action(data.context);
                                    data.blockEndActions.AddAll(data.scope.CodeBlockEndActions);
                                    data.syntaxNodeActions.AddRange(data.scope.SyntaxNodeActions);
                                },
                                (action: codeBlockStartAction.Action, context: blockStartContext, scope: codeBlockScope, blockEndActions: codeBlockEndActions, syntaxNodeActions: syntaxNodeActions),
                                new AnalysisContextInfo(_compilation, declaredSymbol, declaredNode));
                        }
                        else
                        {
                            var operationBlockStartAction = startAction as OperationBlockStartAnalyzerAction;
                            if (operationBlockStartAction != null)
                            {
                                var operationBlockEndActions = blockEndActions as PooledHashSet<OperationBlockAnalyzerAction>;
                                var operationBlockScope = new HostOperationBlockStartAnalysisScope();
                                var operationStartContext = new AnalyzerOperationBlockStartAnalysisContext(startAction.Analyzer,
                                    operationBlockScope, operationBlocks, declaredSymbol, semanticModel.Compilation, _analyzerOptions, GetControlFlowGraph, _cancellationToken);

                                // Catch Exception from the start action.
                                ExecuteAndCatchIfThrows(
                                    startAction.Analyzer,
                                    data =>
                                    {
                                        data.action(data.context);
                                        data.blockEndActions.AddAll(data.scope.OperationBlockEndActions);
                                        data.operationActions.AddRange(data.scope.OperationActions);
                                    },
                                    (action: operationBlockStartAction.Action, context: operationStartContext, scope: operationBlockScope, blockEndActions: operationBlockEndActions, operationActions: operationActions),
                                    new AnalysisContextInfo(_compilation, declaredSymbol));
                            }
                        }

                        analyzerStateOpt?.ProcessedActions.Add(startAction);
                    }
                }
            }
            finally
            {
                if (analyzerStateOpt != null)
                {
                    analyzerStateOpt.CurrentBlockEndActions = blockEndActions.ToImmutableHashSet<TBlockAction>();
                    analyzerStateOpt.CurrentBlockNodeActions = executableNodeActions.ToImmutableHashSet<AnalyzerAction>();
                }
            }

            // Execute stateful executable node analyzers, if any.
            if (executableNodeActions.Any())
            {
                if (syntaxNodeActions != null)
                {
                    var executableNodeActionsByKind = GetNodeActionsByKind(syntaxNodeActions);
                    var syntaxNodesToAnalyze = (IEnumerable<SyntaxNode>)getNodesToAnalyze(executableBlocks);
                    ExecuteSyntaxNodeActions(syntaxNodesToAnalyze, executableNodeActionsByKind, analyzer, declaredSymbol, semanticModel, getKind, diagReporter.AddDiagnosticAction, isSupportedDiagnostic, analyzerStateOpt?.ExecutableNodesAnalysisState as SyntaxNodeAnalyzerStateData);
                }
                else if (operationActions != null)
                {
                    var operationActionsByKind = GetOperationActionsByKind(operationActions);
                    var operationsToAnalyze = (IEnumerable<IOperation>)getNodesToAnalyze(executableBlocks);
                    ExecuteOperationActions(operationsToAnalyze, operationActionsByKind, analyzer, declaredSymbol, semanticModel, diagReporter.AddDiagnosticAction, isSupportedDiagnostic, analyzerStateOpt?.ExecutableNodesAnalysisState as OperationAnalyzerStateData);
                }
            }

            executableNodeActions.Free();

            ExecuteBlockActions(blockActions, declaredNode, declaredSymbol, analyzer, semanticModel, operationBlocks, diagReporter.AddDiagnosticAction, isSupportedDiagnostic, analyzerStateOpt);
            ExecuteBlockActions(blockEndActions, declaredNode, declaredSymbol, analyzer, semanticModel, operationBlocks, diagReporter.AddDiagnosticAction, isSupportedDiagnostic, analyzerStateOpt);

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
            AnalysisState.BlockAnalyzerStateData<TBlockAction, TNodeStateData> analyzerStateOpt)
            where TBlockAction : AnalyzerAction
            where TNodeStateData : AnalyzerStateData, new()
        {
            Debug.Assert(!_isAnalyzerSuppressedForTree(analyzer, declaredNode.SyntaxTree));

            foreach (var blockAction in blockActions)
            {
                if (ShouldExecuteAction(analyzerStateOpt, blockAction))
                {
                    var codeBlockAction = blockAction as CodeBlockAnalyzerAction;
                    if (codeBlockAction != null)
                    {
                        var context = new CodeBlockAnalysisContext(declaredNode, declaredSymbol, semanticModel, _analyzerOptions, addDiagnostic, isSupportedDiagnostic, _cancellationToken);

                        ExecuteAndCatchIfThrows(
                            codeBlockAction.Analyzer,
                            data => data.action(data.context),
                            (action: codeBlockAction.Action, context: context),
                            new AnalysisContextInfo(_compilation, declaredSymbol, declaredNode));
                    }
                    else
                    {
                        var operationBlockAction = blockAction as OperationBlockAnalyzerAction;
                        if (operationBlockAction != null)
                        {
                            var context = new OperationBlockAnalysisContext(operationBlocks, declaredSymbol, semanticModel.Compilation,
                                _analyzerOptions, addDiagnostic, isSupportedDiagnostic, GetControlFlowGraph, _cancellationToken);

                            ExecuteAndCatchIfThrows(
                                operationBlockAction.Analyzer,
                                data => data.action(data.context),
                                (action: operationBlockAction.Action, context: context),
                                new AnalysisContextInfo(_compilation, declaredSymbol));
                        }
                    }

                    analyzerStateOpt?.ProcessedActions.Add(blockAction);
                }
            }

            blockActions.Free();
        }

        internal static ImmutableDictionary<TLanguageKindEnum, ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>> GetNodeActionsByKind<TLanguageKindEnum>(
            IEnumerable<SyntaxNodeAnalyzerAction<TLanguageKindEnum>> nodeActions)
            where TLanguageKindEnum : struct
        {
            Debug.Assert(nodeActions != null && nodeActions.Any());

            var nodeActionsByKind = PooledDictionary<TLanguageKindEnum, ArrayBuilder<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>>.GetInstance();
            foreach (var nodeAction in nodeActions)
            {
                foreach (var kind in nodeAction.Kinds)
                {
                    ArrayBuilder<SyntaxNodeAnalyzerAction<TLanguageKindEnum>> actionsForKind;
                    if (!nodeActionsByKind.TryGetValue(kind, out actionsForKind))
                    {
                        nodeActionsByKind.Add(kind, actionsForKind = ArrayBuilder<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>.GetInstance());
                    }

                    actionsForKind.Add(nodeAction);
                }
            }

            var tuples = nodeActionsByKind.Select(kvp => KeyValuePairUtil.Create(kvp.Key, kvp.Value.ToImmutableAndFree()));
            var map = ImmutableDictionary.CreateRange(tuples);
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
           IDictionary<TLanguageKindEnum, ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>> nodeActionsByKind,
           DiagnosticAnalyzer analyzer,
           SemanticModel model,
           Func<SyntaxNode, TLanguageKindEnum> getKind,
           TextSpan filterSpan,
           SyntaxReference declaration,
           int declarationIndex,
           ISymbol declaredSymbol,
           AnalysisScope analysisScope,
           AnalysisState analysisStateOpt,
           bool isGeneratedCode)
           where TLanguageKindEnum : struct
        {
            DeclarationAnalyzerStateData analyzerStateOpt = null;

            try
            {
                if (TryStartAnalyzingDeclaration(declaredSymbol, declarationIndex, analyzer, analysisScope, analysisStateOpt, out analyzerStateOpt))
                {
                    ExecuteSyntaxNodeActionsCore(nodesToAnalyze, nodeActionsByKind, analyzer, declaredSymbol, model, getKind, filterSpan, analyzerStateOpt, isGeneratedCode);
                    return true;
                }

                return IsDeclarationComplete(declaredSymbol, declarationIndex, analyzer, analysisStateOpt);
            }
            finally
            {
                analyzerStateOpt?.ResetToReadyState();
            }
        }

        private void ExecuteSyntaxNodeActionsCore<TLanguageKindEnum>(
            IEnumerable<SyntaxNode> nodesToAnalyze,
            IDictionary<TLanguageKindEnum, ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>> nodeActionsByKind,
            DiagnosticAnalyzer analyzer,
            ISymbol containingSymbol,
            SemanticModel model,
            Func<SyntaxNode, TLanguageKindEnum> getKind,
            TextSpan filterSpan,
            SyntaxNodeAnalyzerStateData analyzerStateOpt,
            bool isGeneratedCode)
            where TLanguageKindEnum : struct
        {
            if (isGeneratedCode && _shouldSkipAnalysisOnGeneratedCode(analyzer) ||
                _isAnalyzerSuppressedForTree(analyzer, model.SyntaxTree))
            {
                return;
            }

            var diagReporter = GetAddDiagnostic(model.SyntaxTree, filterSpan, analyzer, isSyntaxDiagnostic: false);
            Func<Diagnostic, bool> isSupportedDiagnostic = d => IsSupportedDiagnostic(analyzer, d);
            ExecuteSyntaxNodeActions(nodesToAnalyze, nodeActionsByKind, analyzer, containingSymbol, model, getKind, diagReporter.AddDiagnosticAction, isSupportedDiagnostic, analyzerStateOpt);
            diagReporter.Free();
        }

        private void ExecuteSyntaxNodeActions<TLanguageKindEnum>(
            IEnumerable<SyntaxNode> nodesToAnalyze,
            IDictionary<TLanguageKindEnum, ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>> nodeActionsByKind,
            DiagnosticAnalyzer analyzer,
            ISymbol containingSymbol,
            SemanticModel model,
            Func<SyntaxNode, TLanguageKindEnum> getKind,
            Action<Diagnostic> addDiagnostic,
            Func<Diagnostic, bool> isSupportedDiagnostic,
            SyntaxNodeAnalyzerStateData analyzerStateOpt)
            where TLanguageKindEnum : struct
        {
            Debug.Assert(nodeActionsByKind != null);
            Debug.Assert(nodeActionsByKind.Any());
            Debug.Assert(!_isAnalyzerSuppressedForTree(analyzer, model.SyntaxTree));

            SyntaxNode partiallyProcessedNode = analyzerStateOpt?.CurrentNode;
            if (partiallyProcessedNode != null)
            {
                ExecuteSyntaxNodeActions(partiallyProcessedNode, nodeActionsByKind, containingSymbol, model, getKind, addDiagnostic, isSupportedDiagnostic, analyzerStateOpt);
            }

            foreach (var child in nodesToAnalyze)
            {
                if (ShouldExecuteNode(analyzerStateOpt, child, analyzer))
                {
                    SetCurrentNode(analyzerStateOpt, child);

                    ExecuteSyntaxNodeActions(child, nodeActionsByKind, containingSymbol, model, getKind, addDiagnostic, isSupportedDiagnostic, analyzerStateOpt);
                }
            }
        }

        private void ExecuteSyntaxNodeActions<TLanguageKindEnum>(
            SyntaxNode node,
            IDictionary<TLanguageKindEnum, ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>> nodeActionsByKind,
            ISymbol containingSymbol,
            SemanticModel model,
            Func<SyntaxNode, TLanguageKindEnum> getKind,
            Action<Diagnostic> addDiagnostic,
            Func<Diagnostic, bool> isSupportedDiagnostic,
            SyntaxNodeAnalyzerStateData analyzerStateOpt)
            where TLanguageKindEnum : struct
        {
            ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>> actionsForKind;
            if (nodeActionsByKind.TryGetValue(getKind(node), out actionsForKind))
            {
                foreach (var action in actionsForKind)
                {
                    ExecuteSyntaxNodeAction(action, node, containingSymbol, model, addDiagnostic, isSupportedDiagnostic, analyzerStateOpt);
                }
            }

            analyzerStateOpt?.ClearNodeAnalysisState();
        }

        internal static ImmutableDictionary<OperationKind, ImmutableArray<OperationAnalyzerAction>> GetOperationActionsByKind(IEnumerable<OperationAnalyzerAction> operationActions)
        {
            Debug.Assert(operationActions != null && operationActions.Any());

            var operationActionsByKind = PooledDictionary<OperationKind, ArrayBuilder<OperationAnalyzerAction>>.GetInstance();
            foreach (var operationAction in operationActions)
            {
                foreach (var kind in operationAction.Kinds)
                {
                    ArrayBuilder<OperationAnalyzerAction> actionsForKind;
                    if (!operationActionsByKind.TryGetValue(kind, out actionsForKind))
                    {
                        operationActionsByKind.Add(kind, actionsForKind = ArrayBuilder<OperationAnalyzerAction>.GetInstance());
                    }

                    actionsForKind.Add(operationAction);
                }
            }

            var tuples = operationActionsByKind.Select(kvp => KeyValuePairUtil.Create(kvp.Key, kvp.Value.ToImmutableAndFree()));
            var map = ImmutableDictionary.CreateRange(tuples);
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
            IDictionary<OperationKind, ImmutableArray<OperationAnalyzerAction>> operationActionsByKind,
            DiagnosticAnalyzer analyzer,
            SemanticModel model,
            TextSpan filterSpan,
            SyntaxReference declaration,
            int declarationIndex,
            ISymbol declaredSymbol,
            AnalysisScope analysisScope,
            AnalysisState analysisStateOpt,
            bool isGeneratedCode)
        {
            OperationAnalyzerStateData analyzerStateOpt = null;

            try
            {
                if (TryStartAnalyzingOperationReference(declaredSymbol, declarationIndex, analyzer, analysisScope, analysisStateOpt, out analyzerStateOpt))
                {
                    ExecuteOperationActionsCore(operationsToAnalyze, operationActionsByKind, analyzer, declaredSymbol, model, filterSpan, analyzerStateOpt, isGeneratedCode);
                    return true;
                }

                return IsDeclarationComplete(declaredSymbol, declarationIndex, analyzer, analysisStateOpt);
            }
            finally
            {
                analyzerStateOpt?.ResetToReadyState();
            }
        }

        private void ExecuteOperationActionsCore(
            IEnumerable<IOperation> operationsToAnalyze,
            IDictionary<OperationKind, ImmutableArray<OperationAnalyzerAction>> operationActionsByKind,
            DiagnosticAnalyzer analyzer,
            ISymbol containingSymbol,
            SemanticModel model,
            TextSpan filterSpan,
            OperationAnalyzerStateData analyzerStateOpt,
            bool isGeneratedCode)
        {
            if (isGeneratedCode && _shouldSkipAnalysisOnGeneratedCode(analyzer) ||
                _isAnalyzerSuppressedForTree(analyzer, model.SyntaxTree))
            {
                return;
            }

            var diagReporter = GetAddDiagnostic(model.SyntaxTree, filterSpan, analyzer, isSyntaxDiagnostic: false);
            Func<Diagnostic, bool> isSupportedDiagnostic = d => IsSupportedDiagnostic(analyzer, d);
            ExecuteOperationActions(operationsToAnalyze, operationActionsByKind, analyzer, containingSymbol, model, diagReporter.AddDiagnosticAction, isSupportedDiagnostic, analyzerStateOpt);
            diagReporter.Free();
        }

        private void ExecuteOperationActions(
            IEnumerable<IOperation> operationsToAnalyze,
            IDictionary<OperationKind, ImmutableArray<OperationAnalyzerAction>> operationActionsByKind,
            DiagnosticAnalyzer analyzer,
            ISymbol containingSymbol,
            SemanticModel model,
            Action<Diagnostic> addDiagnostic,
            Func<Diagnostic, bool> isSupportedDiagnostic,
            OperationAnalyzerStateData analyzerStateOpt)
        {
            Debug.Assert(operationActionsByKind != null);
            Debug.Assert(operationActionsByKind.Any());
            Debug.Assert(!_isAnalyzerSuppressedForTree(analyzer, model.SyntaxTree));

            IOperation partiallyProcessedNode = analyzerStateOpt?.CurrentOperation;
            if (partiallyProcessedNode != null)
            {
                ExecuteOperationActions(partiallyProcessedNode, operationActionsByKind, containingSymbol, model, addDiagnostic, isSupportedDiagnostic, analyzerStateOpt);
            }

            foreach (var child in operationsToAnalyze)
            {
                if (ShouldExecuteOperation(analyzerStateOpt, child, analyzer))
                {
                    SetCurrentOperation(analyzerStateOpt, child);

                    ExecuteOperationActions(child, operationActionsByKind, containingSymbol, model, addDiagnostic, isSupportedDiagnostic, analyzerStateOpt);
                }
            }
        }

        private void ExecuteOperationActions(
            IOperation operation,
            IDictionary<OperationKind, ImmutableArray<OperationAnalyzerAction>> operationActionsByKind,
            ISymbol containingSymbol,
            SemanticModel model,
            Action<Diagnostic> addDiagnostic,
            Func<Diagnostic, bool> isSupportedDiagnostic,
            OperationAnalyzerStateData analyzerStateOpt)
        {
            ImmutableArray<OperationAnalyzerAction> actionsForKind;
            if (operationActionsByKind.TryGetValue(operation.Kind, out actionsForKind))
            {
                foreach (var action in actionsForKind)
                {
                    ExecuteOperationAction(action, operation, containingSymbol, model, addDiagnostic, isSupportedDiagnostic, analyzerStateOpt);
                }
            }

            analyzerStateOpt?.ClearNodeAnalysisState();
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
            object gate = _getAnalyzerGateOpt?.Invoke(analyzer);
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
        }

        [PerformanceSensitive(
            "https://github.com/dotnet/roslyn/issues/23582",
            AllowCaptures = false)]
        private void ExecuteAndCatchIfThrows_NoLock<TArg>(DiagnosticAnalyzer analyzer, Action<TArg> analyze, TArg argument, AnalysisContextInfo? info)
        {
            try
            {
                _cancellationToken.ThrowIfCancellationRequested();

                PooledStopwatch timer = null;
                if (_analyzerExecutionTimeMapOpt != null)
                {
                    timer = PooledStopwatch.StartInstance();

                    // This call to Restart isn't required by the API, but is included to avoid measurement errors which
                    // can occur during periods of high allocation activity. In some cases, calls to Stopwatch
                    // operations can block at their return point on the completion of a background GC operation. When
                    // this occurs, the GC wait time ends up included in the measured time span. In the event the first
                    // call to Restart blocked on a GC operation, this call to Restart will most likely occur when the
                    // GC is no longer active. In practice, a substantial improvement to the consistency of analyzer
                    // timing data was observed.
                    //
                    // Note that the call to Stopwatch.Stop() is not affected, because the GC wait will occur after the
                    // timer has already recorded its stop time.
                    timer.Restart();
                }

                analyze(argument);

                if (timer != null)
                {
                    timer.Stop();
                    StrongBox<long> totalTicks = _analyzerExecutionTimeMapOpt.GetOrAdd(analyzer, _ => new StrongBox<long>(0));
                    Interlocked.Add(ref totalTicks.Value, timer.Elapsed.Ticks);
                    timer.Free();
                }
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
            var messageArguments = new[] { analyzerName, e.GetType().ToString(), e.Message };
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

        internal static Diagnostic CreateDriverExceptionDiagnostic(Exception e)
        {
            var title = CodeAnalysisResources.AnalyzerDriverFailure;
            var messageFormat = CodeAnalysisResources.AnalyzerDriverThrows;
            var messageArguments = new[] { e.GetType().ToString(), e.Message };
            var description = string.Format(CodeAnalysisResources.AnalyzerDriverThrowsDescription, e.CreateDiagnosticDescription());
            var descriptor = GetAnalyzerExceptionDiagnosticDescriptor(AnalyzerDriverExceptionDiagnosticId, title, description, messageFormat);
            return Diagnostic.Create(descriptor, Location.None, messageArguments);
        }

        internal static DiagnosticDescriptor GetAnalyzerExceptionDiagnosticDescriptor(string id = null, string title = null, string description = null, string messageFormat = null)
        {
            // TODO: It is not ideal to create a new descriptor per analyzer exception diagnostic instance.
            // However, until we add a LongMessage field to the Diagnostic, we are forced to park the instance specific description onto the Descriptor's Description field.
            // This requires us to create a new DiagnosticDescriptor instance per diagnostic instance.

            id = id ?? AnalyzerExceptionDiagnosticId;
            title = title ?? CodeAnalysisResources.CompilerAnalyzerFailure;
            messageFormat = messageFormat ?? CodeAnalysisResources.CompilerAnalyzerThrows;
            description = description ?? CodeAnalysisResources.CompilerAnalyzerFailure;

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
#pragma warning disable RS0013 // Its ok to realize the Descriptor for analyzer exception diagnostics, which are descriptor based and also rare.
                foreach (var tag in diagnostic.Descriptor.CustomTags)
#pragma warning restore RS0013
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

        private Action<Diagnostic> GetAddDiagnostic(ISymbol contextSymbol, ImmutableArray<SyntaxReference> cachedDeclaringReferences, DiagnosticAnalyzer analyzer, Func<ISymbol, SyntaxReference, Compilation, SyntaxNode> getTopMostNodeForAnalysis)
        {
            return GetAddDiagnostic(contextSymbol, cachedDeclaringReferences, _compilation, analyzer, _addNonCategorizedDiagnosticOpt,
                 _addCategorizedLocalDiagnosticOpt, _addCategorizedNonLocalDiagnosticOpt, getTopMostNodeForAnalysis, _shouldSuppressGeneratedCodeDiagnostic, _cancellationToken);
        }

        private static Action<Diagnostic> GetAddDiagnostic(
            ISymbol contextSymbol,
            ImmutableArray<SyntaxReference> cachedDeclaringReferences,
            Compilation compilation,
            DiagnosticAnalyzer analyzer,
            Action<Diagnostic> addNonCategorizedDiagnosticOpt,
            Action<Diagnostic, DiagnosticAnalyzer, bool> addCategorizedLocalDiagnosticOpt,
            Action<Diagnostic, DiagnosticAnalyzer> addCategorizedNonLocalDiagnosticOpt,
            Func<ISymbol, SyntaxReference, Compilation, SyntaxNode> getTopMostNodeForAnalysis,
            Func<Diagnostic, DiagnosticAnalyzer, Compilation, CancellationToken, bool> shouldSuppressGeneratedCodeDiagnostic,
            CancellationToken cancellationToken)
        {
            return diagnostic =>
            {
                if (shouldSuppressGeneratedCodeDiagnostic(diagnostic, analyzer, compilation, cancellationToken))
                {
                    return;
                }

                if (addCategorizedLocalDiagnosticOpt == null)
                {
                    Debug.Assert(addNonCategorizedDiagnosticOpt != null);
                    addNonCategorizedDiagnosticOpt(diagnostic);
                    return;
                }

                Debug.Assert(addNonCategorizedDiagnosticOpt == null);
                Debug.Assert(addCategorizedNonLocalDiagnosticOpt != null);

                if (diagnostic.Location.IsInSource)
                {
                    foreach (var syntaxRef in cachedDeclaringReferences)
                    {
                        if (syntaxRef.SyntaxTree == diagnostic.Location.SourceTree)
                        {
                            var syntax = getTopMostNodeForAnalysis(contextSymbol, syntaxRef, compilation);
                            if (diagnostic.Location.SourceSpan.IntersectsWith(syntax.FullSpan))
                            {
                                addCategorizedLocalDiagnosticOpt(diagnostic, analyzer, false);
                                return;
                            }
                        }
                    }
                }

                addCategorizedNonLocalDiagnosticOpt(diagnostic, analyzer);
            };
        }

        private Action<Diagnostic> GetAddCompilationDiagnostic(DiagnosticAnalyzer analyzer)
        {
            return diagnostic =>
            {
                if (_shouldSuppressGeneratedCodeDiagnostic(diagnostic, analyzer, _compilation, _cancellationToken))
                {
                    return;
                }

                if (_addCategorizedNonLocalDiagnosticOpt == null)
                {
                    Debug.Assert(_addNonCategorizedDiagnosticOpt != null);
                    _addNonCategorizedDiagnosticOpt(diagnostic);
                    return;
                }

                _addCategorizedNonLocalDiagnosticOpt(diagnostic, analyzer);
            };
        }

        private AnalyzerDiagnosticReporter GetAddDiagnostic(SyntaxTree tree, DiagnosticAnalyzer analyzer, bool isSyntaxDiagnostic)
        {
            return AnalyzerDiagnosticReporter.GetInstance(tree, null, _compilation, analyzer, isSyntaxDiagnostic,
                _addNonCategorizedDiagnosticOpt, _addCategorizedLocalDiagnosticOpt, _addCategorizedNonLocalDiagnosticOpt,
                _shouldSuppressGeneratedCodeDiagnostic, _cancellationToken);
        }

        private AnalyzerDiagnosticReporter GetAddDiagnostic(SyntaxTree tree, TextSpan? span, DiagnosticAnalyzer analyzer, bool isSyntaxDiagnostic)
        {
            return AnalyzerDiagnosticReporter.GetInstance(tree, span, _compilation, analyzer, false,
                _addNonCategorizedDiagnosticOpt, _addCategorizedLocalDiagnosticOpt, _addCategorizedNonLocalDiagnosticOpt,
                _shouldSuppressGeneratedCodeDiagnostic, _cancellationToken);
        }

        private static Action<Diagnostic> GetAddDiagnostic(
            SyntaxTree contextTree,
            TextSpan? span,
            Compilation compilation,
            DiagnosticAnalyzer analyzer,
            bool isSyntaxDiagnostic,
            Action<Diagnostic> addNonCategorizedDiagnosticOpt,
            Action<Diagnostic, DiagnosticAnalyzer, bool> addCategorizedLocalDiagnosticOpt,
            Action<Diagnostic, DiagnosticAnalyzer> addCategorizedNonLocalDiagnosticOpt,
            Func<Diagnostic, DiagnosticAnalyzer, Compilation, CancellationToken, bool> shouldSuppressGeneratedCodeDiagnostic,
            CancellationToken cancellationToken)
        {
            return diagnostic =>
            {
                if (shouldSuppressGeneratedCodeDiagnostic(diagnostic, analyzer, compilation, cancellationToken))
                {
                    return;
                }

                if (addCategorizedLocalDiagnosticOpt == null)
                {
                    Debug.Assert(addNonCategorizedDiagnosticOpt != null);
                    addNonCategorizedDiagnosticOpt(diagnostic);
                    return;
                }

                Debug.Assert(addNonCategorizedDiagnosticOpt == null);
                Debug.Assert(addCategorizedNonLocalDiagnosticOpt != null);

                if (diagnostic.Location.IsInSource &&
                    contextTree == diagnostic.Location.SourceTree &&
                    (!span.HasValue || span.Value.IntersectsWith(diagnostic.Location.SourceSpan)))
                {
                    addCategorizedLocalDiagnosticOpt(diagnostic, analyzer, isSyntaxDiagnostic);
                }
                else
                {
                    addCategorizedNonLocalDiagnosticOpt(diagnostic, analyzer);
                }
            };
        }

        private static bool ShouldExecuteAction(AnalyzerStateData analyzerStateOpt, AnalyzerAction action)
        {
            return analyzerStateOpt == null || !analyzerStateOpt.ProcessedActions.Contains(action);
        }

        private bool ShouldExecuteNode(SyntaxNodeAnalyzerStateData analyzerStateOpt, SyntaxNode node, DiagnosticAnalyzer analyzer)
        {
            // Check if the node has already been processed.
            if (analyzerStateOpt != null && analyzerStateOpt.ProcessedNodes.Contains(node))
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

        private bool ShouldExecuteOperation(OperationAnalyzerStateData analyzerStateOpt, IOperation operation, DiagnosticAnalyzer analyzer)
        {
            // Check if the operation has already been processed.
            if (analyzerStateOpt != null && analyzerStateOpt.ProcessedOperations.Contains(operation))
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

        private static void SetCurrentNode(SyntaxNodeAnalyzerStateData analyzerStateOpt, SyntaxNode node)
        {
            if (analyzerStateOpt != null)
            {
                Debug.Assert(node != null);
                analyzerStateOpt.CurrentNode = node;
            }
        }

        private static void SetCurrentOperation(OperationAnalyzerStateData analyzerStateOpt, IOperation operation)
        {
            if (analyzerStateOpt != null)
            {
                Debug.Assert(operation != null);
                analyzerStateOpt.CurrentOperation = operation;
            }
        }

        private static bool TryStartProcessingEvent(CompilationEvent nonSymbolCompilationEvent, DiagnosticAnalyzer analyzer, AnalysisScope analysisScope, AnalysisState analysisStateOpt, out AnalyzerStateData analyzerStateOpt)
        {
            Debug.Assert(!(nonSymbolCompilationEvent is SymbolDeclaredCompilationEvent));
            Debug.Assert(analysisScope.Analyzers.Contains(analyzer));

            analyzerStateOpt = null;
            return analysisStateOpt == null || analysisStateOpt.TryStartProcessingEvent(nonSymbolCompilationEvent, analyzer, out analyzerStateOpt);
        }

        private static bool TryStartSyntaxAnalysis(SyntaxTree tree, DiagnosticAnalyzer analyzer, AnalysisScope analysisScope, AnalysisState analysisStateOpt, out AnalyzerStateData analyzerStateOpt)
        {
            Debug.Assert(analysisScope.Analyzers.Contains(analyzer));

            analyzerStateOpt = null;
            return analysisStateOpt == null || analysisStateOpt.TryStartSyntaxAnalysis(tree, analyzer, out analyzerStateOpt);
        }

        private static bool TryStartAnalyzingSymbol(ISymbol symbol, DiagnosticAnalyzer analyzer, AnalysisScope analysisScope, AnalysisState analysisStateOpt, out AnalyzerStateData analyzerStateOpt)
        {
            Debug.Assert(analysisScope.Analyzers.Contains(analyzer));

            analyzerStateOpt = null;
            return analysisStateOpt == null || analysisStateOpt.TryStartAnalyzingSymbol(symbol, analyzer, out analyzerStateOpt);
        }

        private static bool TryStartSymbolEndAnalysis(ISymbol symbol, DiagnosticAnalyzer analyzer, AnalysisState analysisStateOpt, out AnalyzerStateData analyzerStateOpt)
        {
            analyzerStateOpt = null;
            return analysisStateOpt == null || analysisStateOpt.TryStartSymbolEndAnalysis(symbol, analyzer, out analyzerStateOpt);
        }

        private static bool TryStartAnalyzingDeclaration(ISymbol symbol, int declarationIndex, DiagnosticAnalyzer analyzer, AnalysisScope analysisScope, AnalysisState analysisStateOpt, out DeclarationAnalyzerStateData analyzerStateOpt)
        {
            Debug.Assert(analysisScope.Analyzers.Contains(analyzer));

            analyzerStateOpt = null;
            return analysisStateOpt == null || analysisStateOpt.TryStartAnalyzingDeclaration(symbol, declarationIndex, analyzer, out analyzerStateOpt);
        }

        private static bool TryStartAnalyzingOperationReference(ISymbol symbol, int declarationIndex, DiagnosticAnalyzer analyzer, AnalysisScope analysisScope, AnalysisState analysisStateOpt, out OperationAnalyzerStateData analyzerStateOpt)
        {
            Debug.Assert(analysisScope.Analyzers.Contains(analyzer));

            analyzerStateOpt = null;
            DeclarationAnalyzerStateData declarationAnalyzerStateOpt;
            if (analysisStateOpt == null)
            {
                return true;
            }

            if (analysisStateOpt.TryStartAnalyzingDeclaration(symbol, declarationIndex, analyzer, out declarationAnalyzerStateOpt))
            {
                analyzerStateOpt = declarationAnalyzerStateOpt.OperationBlockAnalysisState.ExecutableNodesAnalysisState;
                return true;
            }

            analyzerStateOpt = null;
            return false;
        }

        private static bool IsEventComplete(CompilationEvent compilationEvent, DiagnosticAnalyzer analyzer, AnalysisState analysisStateOpt)
        {
            return analysisStateOpt == null || analysisStateOpt.IsEventComplete(compilationEvent, analyzer);
        }

        private static bool IsSymbolComplete(ISymbol symbol, DiagnosticAnalyzer analyzer, AnalysisState analysisStateOpt)
        {
            return analysisStateOpt == null || analysisStateOpt.IsSymbolComplete(symbol, analyzer);
        }

        private static bool IsSymbolEndAnalysisComplete(ISymbol symbol, DiagnosticAnalyzer analyzer, AnalysisState analysisStateOpt)
        {
            return analysisStateOpt == null || analysisStateOpt.IsSymbolEndAnalysisComplete(symbol, analyzer);
        }

        private static bool IsDeclarationComplete(ISymbol symbol, int declarationIndex, DiagnosticAnalyzer analyzer, AnalysisState analysisStateOpt)
        {
            return analysisStateOpt == null || analysisStateOpt.IsDeclarationComplete(symbol, declarationIndex, analyzer);
        }

        internal TimeSpan ResetAnalyzerExecutionTime(DiagnosticAnalyzer analyzer)
        {
            Debug.Assert(_analyzerExecutionTimeMapOpt != null);
            StrongBox<long> executionTime;
            if (!_analyzerExecutionTimeMapOpt.TryRemove(analyzer, out executionTime))
            {
                return TimeSpan.Zero;
            }

            return TimeSpan.FromTicks(executionTime.Value);
        }

        private ControlFlowGraph GetControlFlowGraph(IOperation operation)
        {
            Debug.Assert(operation != null);
            Debug.Assert(operation.Parent == null);

            if (_lazyControlFlowGraphMap == null)
            {
                Interlocked.CompareExchange(ref _lazyControlFlowGraphMap, new ConcurrentDictionary<IOperation, ControlFlowGraph>(), null);
            }

            return _lazyControlFlowGraphMap.GetOrAdd(operation, op => ControlFlowGraphBuilder.Create(op));
        }

        private bool IsAnalyzerSuppressedForSymbol(DiagnosticAnalyzer analyzer, ISymbol symbol)
        {
            Debug.Assert(_isAnalyzerSuppressedForTree != null);

            foreach (var location in symbol.Locations)
            {
                if (location.SourceTree != null &&
                    !_isAnalyzerSuppressedForTree(analyzer, location.SourceTree))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
