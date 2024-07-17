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

        private readonly Action<Diagnostic, CancellationToken>? _addNonCategorizedDiagnostic;
        private readonly Action<Diagnostic, DiagnosticAnalyzer, bool, CancellationToken>? _addCategorizedLocalDiagnostic;
        private readonly Action<Diagnostic, DiagnosticAnalyzer, CancellationToken>? _addCategorizedNonLocalDiagnostic;
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
            Action<Diagnostic, CancellationToken>? addNonCategorizedDiagnostic,
            Action<Exception, DiagnosticAnalyzer, Diagnostic, CancellationToken> onAnalyzerException,
            Func<Exception, bool>? analyzerExceptionFilter,
            Func<DiagnosticAnalyzer, bool> isCompilerAnalyzer,
            AnalyzerManager analyzerManager,
            Func<DiagnosticAnalyzer, bool> shouldSkipAnalysisOnGeneratedCode,
            Func<Diagnostic, DiagnosticAnalyzer, Compilation, CancellationToken, bool> shouldSuppressGeneratedCodeDiagnostic,
            Func<SyntaxTree, TextSpan, CancellationToken, bool> isGeneratedCodeLocation,
            Func<DiagnosticAnalyzer, SyntaxTree, SyntaxTreeOptionsProvider?, CancellationToken, bool> isAnalyzerSuppressedForTree,
            Func<DiagnosticAnalyzer, object?> getAnalyzerGate,
            Func<SyntaxTree, SemanticModel> getSemanticModel,
            SeverityFilter severityFilter,
            bool logExecutionTime = false,
            Action<Diagnostic, DiagnosticAnalyzer, bool, CancellationToken>? addCategorizedLocalDiagnostic = null,
            Action<Diagnostic, DiagnosticAnalyzer, CancellationToken>? addCategorizedNonLocalDiagnostic = null,
            Action<Suppression>? addSuppression = null)
        {
            // We can either report categorized (local/non-local) diagnostics or non-categorized diagnostics.
            Debug.Assert((addNonCategorizedDiagnostic != null) ^ (addCategorizedLocalDiagnostic != null));
            Debug.Assert((addCategorizedLocalDiagnostic != null) == (addCategorizedNonLocalDiagnostic != null));

            var analyzerExecutionTimeMap = logExecutionTime ? new ConcurrentDictionary<DiagnosticAnalyzer, StrongBox<long>>() : null;

            return new AnalyzerExecutor(compilation, analyzerOptions, addNonCategorizedDiagnostic, onAnalyzerException, analyzerExceptionFilter,
                isCompilerAnalyzer, analyzerManager, shouldSkipAnalysisOnGeneratedCode, shouldSuppressGeneratedCodeDiagnostic, isGeneratedCodeLocation,
                isAnalyzerSuppressedForTree, getAnalyzerGate, getSemanticModel, severityFilter, analyzerExecutionTimeMap, addCategorizedLocalDiagnostic, addCategorizedNonLocalDiagnostic,
                addSuppression);
        }

        private AnalyzerExecutor(
            Compilation compilation,
            AnalyzerOptions analyzerOptions,
            Action<Diagnostic, CancellationToken>? addNonCategorizedDiagnosticOpt,
            Action<Exception, DiagnosticAnalyzer, Diagnostic, CancellationToken> onAnalyzerException,
            Func<Exception, bool>? analyzerExceptionFilter,
            Func<DiagnosticAnalyzer, bool> isCompilerAnalyzer,
            AnalyzerManager analyzerManager,
            Func<DiagnosticAnalyzer, bool> shouldSkipAnalysisOnGeneratedCode,
            Func<Diagnostic, DiagnosticAnalyzer, Compilation, CancellationToken, bool> shouldSuppressGeneratedCodeDiagnostic,
            Func<SyntaxTree, TextSpan, CancellationToken, bool> isGeneratedCodeLocation,
            Func<DiagnosticAnalyzer, SyntaxTree, SyntaxTreeOptionsProvider?, CancellationToken, bool> isAnalyzerSuppressedForTree,
            Func<DiagnosticAnalyzer, object?> getAnalyzerGate,
            Func<SyntaxTree, SemanticModel> getSemanticModel,
            SeverityFilter severityFilter,
            ConcurrentDictionary<DiagnosticAnalyzer, StrongBox<long>>? analyzerExecutionTimeMap,
            Action<Diagnostic, DiagnosticAnalyzer, bool, CancellationToken>? addCategorizedLocalDiagnostic,
            Action<Diagnostic, DiagnosticAnalyzer, CancellationToken>? addCategorizedNonLocalDiagnostic,
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
        /// <param name="analyzer">Analyzer to get session wide analyzer actions.</param>
        /// <param name="sessionScope">Session scope to store register session wide analyzer actions.</param>
        /// <param name="severityFilter">Severity filter for analysis.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <remarks>
        /// Note that this API doesn't execute any <see cref="CompilationStartAnalyzerAction"/> registered by the Initialize invocation.
        /// Use <see cref="ExecuteCompilationStartActions(ImmutableArray{CompilationStartAnalyzerAction}, HostCompilationStartAnalysisScope, CancellationToken)"/> API
        /// to get execute these actions to get the per-compilation analyzer actions.
        /// </remarks>
        public void ExecuteInitializeMethod(DiagnosticAnalyzer analyzer, HostSessionStartAnalysisScope sessionScope, SeverityFilter severityFilter, CancellationToken cancellationToken)
        {
            var context = new AnalyzerAnalysisContext(analyzer, sessionScope, severityFilter);

            // The Initialize method should be run asynchronously in case it is not well behaved, e.g. does not terminate.
            ExecuteAndCatchIfThrows(
                analyzer,
                analyze: data => data.analyzer.Initialize(data.context),
                argument: (analyzer, context),
                contextInfo: null,
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Executes the compilation start actions.
        /// </summary>
        /// <param name="actions"><see cref="AnalyzerActions"/> whose compilation start actions are to be executed.</param>
        /// <param name="compilationScope">Compilation scope to store the analyzer actions.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public void ExecuteCompilationStartActions(ImmutableArray<CompilationStartAnalyzerAction> actions, HostCompilationStartAnalysisScope compilationScope, CancellationToken cancellationToken)
        {
            foreach (var startAction in actions)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var context = new AnalyzerCompilationStartAnalysisContext(startAction.Analyzer, compilationScope,
                    Compilation, AnalyzerOptions, _compilationAnalysisValueProviderFactory, cancellationToken);

                ExecuteAndCatchIfThrows(
                    startAction.Analyzer,
                    analyze: data => data.action(data.context),
                    argument: (action: startAction.Action, context),
                    contextInfo: new AnalysisContextInfo(Compilation),
                    cancellationToken: cancellationToken);
            }
        }

        /// <summary>
        /// Executes the symbol start actions.
        /// </summary>
        /// <param name="symbol">Symbol whose symbol start actions are to be executed.</param>
        /// <param name="analyzer">Analyzer whose symbol start actions are to be executed.</param>
        /// <param name="actions"><see cref="AnalyzerActions"/> whose symbol start actions are to be executed.</param>
        /// <param name="symbolScope">Symbol scope to store the analyzer actions.</param>
        /// <param name="isGeneratedCodeSymbol">Flag indicating if the symbol being analyzed is generated code.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public void ExecuteSymbolStartActions(
            ISymbol symbol,
            DiagnosticAnalyzer analyzer,
            ImmutableArray<SymbolStartAnalyzerAction> actions,
            HostSymbolStartAnalysisScope symbolScope,
            bool isGeneratedCodeSymbol,
            SyntaxTree? filterTree,
            TextSpan? filterSpan,
            CancellationToken cancellationToken)
        {
            if (isGeneratedCodeSymbol && _shouldSkipAnalysisOnGeneratedCode(analyzer) ||
                IsAnalyzerSuppressedForSymbol(analyzer, symbol, cancellationToken))
            {
                return;
            }

            foreach (var startAction in actions)
            {
                Debug.Assert(startAction.Analyzer == analyzer);
                cancellationToken.ThrowIfCancellationRequested();

                var context = new AnalyzerSymbolStartAnalysisContext(startAction.Analyzer, symbolScope,
                    symbol, Compilation, AnalyzerOptions, isGeneratedCodeSymbol, filterTree, filterSpan, cancellationToken);

                ExecuteAndCatchIfThrows(
                    startAction.Analyzer,
                    analyze: data => data.action(data.context),
                    argument: (action: startAction.Action, context),
                    contextInfo: new AnalysisContextInfo(Compilation, symbol),
                    cancellationToken: cancellationToken);
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
            Func<SuppressionDescriptor, bool> isSupportedSuppression = supportedSuppressions.Contains;
            Action<SuppressionAnalysisContext> action = suppressor.ReportSuppressions;
            var context = new SuppressionAnalysisContext(Compilation, AnalyzerOptions,
                reportedDiagnostics, _addSuppression, isSupportedSuppression, _getSemanticModel, cancellationToken);

            ExecuteAndCatchIfThrows(
                suppressor,
                analyze: data => data.action(data.context),
                argument: (action, context),
                contextInfo: new AnalysisContextInfo(Compilation),
                cancellationToken: cancellationToken);
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

            var addDiagnostic = GetAddCompilationDiagnostic(analyzer, cancellationToken);

            using var _ = PooledDelegates.GetPooledFunction(unboundFunction: (d, ct, arg) => arg.self.IsSupportedDiagnostic(arg.analyzer, d, ct), argument: (self: this, analyzer), boundFunction: out Func<Diagnostic, CancellationToken, bool> isSupportedDiagnostic);

            foreach (var endAction in compilationActions)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var context = new CompilationAnalysisContext(
                        Compilation, AnalyzerOptions, addDiagnostic,
                        isSupportedDiagnostic, _compilationAnalysisValueProviderFactory, cancellationToken);

                ExecuteAndCatchIfThrows(
                    endAction.Analyzer,
                    analyze: data => data.action(data.context),
                    argument: (action: endAction.Action, context),
                    contextInfo: new AnalysisContextInfo(Compilation),
                    cancellationToken: cancellationToken);
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
            var addDiagnostic = GetAddDiagnostic(symbol, symbolDeclaredEvent.DeclaringSyntaxReferences, analyzer, getTopMostNodeForAnalysis, cancellationToken);

            using var _ = PooledDelegates.GetPooledFunction(unboundFunction: (d, ct, arg) => arg.self.IsSupportedDiagnostic(arg.analyzer, d, ct), argument: (self: this, analyzer), boundFunction: out Func<Diagnostic, CancellationToken, bool> isSupportedDiagnostic);

            foreach (var symbolAction in symbolActions)
            {
                var action = symbolAction.Action;
                var kinds = symbolAction.Kinds;

                if (kinds.Contains(symbol.Kind))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var context = new SymbolAnalysisContext(symbol, Compilation, AnalyzerOptions, addDiagnostic,
                        isSupportedDiagnostic, isGeneratedCodeSymbol, filterTree, filterSpan, cancellationToken);

                    ExecuteAndCatchIfThrows(
                        symbolAction.Analyzer,
                        analyze: data => data.action(data.context),
                        argument: (action, context),
                        contextInfo: new AnalysisContextInfo(Compilation, symbol),
                        cancellationToken: cancellationToken);
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
            var addDiagnostic = GetAddDiagnostic(symbol, symbolDeclaredEvent.DeclaringSyntaxReferences, analyzer, getTopMostNodeForAnalysis, cancellationToken);

            using var _ = PooledDelegates.GetPooledFunction(unboundFunction: (d, ct, arg) => arg.self.IsSupportedDiagnostic(arg.analyzer, d, ct), argument: (self: this, analyzer), boundFunction: out Func<Diagnostic, CancellationToken, bool> isSupportedDiagnostic);

            foreach (var symbolAction in symbolEndActions)
            {
                var action = symbolAction.Action;

                cancellationToken.ThrowIfCancellationRequested();

                var context = new SymbolAnalysisContext(symbol, Compilation, AnalyzerOptions, addDiagnostic,
                    isSupportedDiagnostic, isGeneratedCode, filterTree, filterSpan, cancellationToken);

                ExecuteAndCatchIfThrows(
                    symbolAction.Analyzer,
                    analyze: data => data.action(data.context),
                    argument: (action, context),
                    contextInfo: new AnalysisContextInfo(Compilation, symbol),
                    cancellationToken: cancellationToken);
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

            var diagReporter = GetAddSemanticDiagnostic(semanticModel.SyntaxTree, analyzer, cancellationToken);

            using var _ = PooledDelegates.GetPooledFunction(unboundFunction: (d, ct, arg) => arg.self.IsSupportedDiagnostic(arg.analyzer, d, ct), argument: (self: this, analyzer), boundFunction: out Func<Diagnostic, CancellationToken, bool> isSupportedDiagnostic);

            foreach (var semanticModelAction in semanticModelActions)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var context = new SemanticModelAnalysisContext(semanticModel, AnalyzerOptions, diagReporter.AddDiagnosticAction,
                    isSupportedDiagnostic, filterSpan, isGeneratedCode, cancellationToken);

                // Catch Exception from action.
                ExecuteAndCatchIfThrows(
                    semanticModelAction.Analyzer,
                    analyze: data => data.action(data.context),
                    argument: (action: semanticModelAction.Action, context),
                    contextInfo: new AnalysisContextInfo(semanticModel),
                    cancellationToken: cancellationToken);
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

            var diagReporter = GetAddSyntaxDiagnostic(file, analyzer, cancellationToken);

            using var _ = PooledDelegates.GetPooledFunction(unboundFunction: (d, ct, arg) => arg.self.IsSupportedDiagnostic(arg.analyzer, d, ct), argument: (self: this, analyzer), boundFunction: out Func<Diagnostic, CancellationToken, bool> isSupportedDiagnostic);

            foreach (var syntaxTreeAction in syntaxTreeActions)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var context = new SyntaxTreeAnalysisContext(tree, AnalyzerOptions, diagReporter.AddDiagnosticAction, isSupportedDiagnostic, Compilation, filterSpan, isGeneratedCode, cancellationToken);

                // Catch Exception from action.
                ExecuteAndCatchIfThrows(
                    syntaxTreeAction.Analyzer,
                    analyze: data => data.action(data.context),
                    argument: (action: syntaxTreeAction.Action, context),
                    contextInfo: new AnalysisContextInfo(Compilation, file),
                    cancellationToken: cancellationToken);
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

            var diagReporter = GetAddSyntaxDiagnostic(file, analyzer, cancellationToken);

            using var _ = PooledDelegates.GetPooledFunction(unboundFunction: (d, ct, arg) => arg.self.IsSupportedDiagnostic(arg.analyzer, d, ct), argument: (self: this, analyzer), boundFunction: out Func<Diagnostic, CancellationToken, bool> isSupportedDiagnostic);
            foreach (var additionalFileAction in additionalFileActions)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var context = new AdditionalFileAnalysisContext(additionalFile, AnalyzerOptions, diagReporter.AddDiagnosticAction, isSupportedDiagnostic, Compilation, filterSpan, cancellationToken);

                // Catch Exception from action.
                ExecuteAndCatchIfThrows(
                    additionalFileAction.Analyzer,
                    analyze: data => data.action(data.context),
                    argument: (action: additionalFileAction.Action, context),
                    contextInfo: new AnalysisContextInfo(Compilation, file),
                    cancellationToken: cancellationToken);
            }

            diagReporter.Free();
        }

        private void ExecuteSyntaxNodeAction<TLanguageKindEnum>(
            SyntaxNodeAnalyzerAction<TLanguageKindEnum> syntaxNodeAction,
            SyntaxNode node,
            ISymbol containingSymbol,
            SemanticModel semanticModel,
            Action<Diagnostic> addDiagnostic,
            Func<Diagnostic, CancellationToken, bool> isSupportedDiagnostic,
            TextSpan? filterSpan,
            bool isGeneratedCode,
            CancellationToken cancellationToken)
            where TLanguageKindEnum : struct
        {
            Debug.Assert(!isGeneratedCode || !_shouldSkipAnalysisOnGeneratedCode(syntaxNodeAction.Analyzer));
            Debug.Assert(!IsAnalyzerSuppressedForTree(syntaxNodeAction.Analyzer, node.SyntaxTree, cancellationToken));

            var syntaxNodeContext = new SyntaxNodeAnalysisContext(node, containingSymbol, semanticModel, AnalyzerOptions, addDiagnostic,
                    isSupportedDiagnostic, filterSpan, isGeneratedCode, cancellationToken);

            ExecuteAndCatchIfThrows(
                syntaxNodeAction.Analyzer,
                analyze: data => data.action(data.context),
                argument: (action: syntaxNodeAction.Action, context: syntaxNodeContext),
                contextInfo: new AnalysisContextInfo(Compilation, node),
                cancellationToken: cancellationToken);
        }

        private void ExecuteOperationAction(
            OperationAnalyzerAction operationAction,
            IOperation operation,
            ISymbol containingSymbol,
            SemanticModel semanticModel,
            Action<Diagnostic> addDiagnostic,
            Func<Diagnostic, CancellationToken, bool> isSupportedDiagnostic,
            TextSpan? filterSpan,
            bool isGeneratedCode,
            CancellationToken cancellationToken)
        {
            Debug.Assert(!isGeneratedCode || !_shouldSkipAnalysisOnGeneratedCode(operationAction.Analyzer));
            Debug.Assert(!IsAnalyzerSuppressedForTree(operationAction.Analyzer, semanticModel.SyntaxTree, cancellationToken));

            var operationContext = new OperationAnalysisContext(operation, containingSymbol, semanticModel.Compilation,
                    AnalyzerOptions, addDiagnostic, isSupportedDiagnostic, GetControlFlowGraph, filterSpan, isGeneratedCode, cancellationToken);
            ExecuteAndCatchIfThrows(
                operationAction.Analyzer,
                analyze: data => data.action(data.context),
                argument: (action: operationAction.Action, context: operationContext),
                contextInfo: new AnalysisContextInfo(Compilation, operation),
                cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Execute code block actions for the given analyzer for the given declaration.
        /// </summary>
        public void ExecuteCodeBlockActions<TLanguageKindEnum>(
            IEnumerable<CodeBlockStartAnalyzerAction<TLanguageKindEnum>> codeBlockStartActions,
            IEnumerable<CodeBlockAnalyzerAction> codeBlockActions,
            IEnumerable<CodeBlockAnalyzerAction> codeBlockEndActions,
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
            ExecuteBlockActionsCore<CodeBlockStartAnalyzerAction<TLanguageKindEnum>, CodeBlockAnalyzerAction, SyntaxNodeAnalyzerAction<TLanguageKindEnum>, SyntaxNode, TLanguageKindEnum>(
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
                semanticModel, getKind, filterSpan, isGeneratedCode, cancellationToken);
        }

        /// <summary>
        /// Execute operation block actions for the given analyzer for the given declaration.
        /// </summary>
        public void ExecuteOperationBlockActions(
            IEnumerable<OperationBlockStartAnalyzerAction> operationBlockStartActions,
            IEnumerable<OperationBlockAnalyzerAction> operationBlockActions,
            IEnumerable<OperationBlockAnalyzerAction> operationBlockEndActions,
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
            ExecuteBlockActionsCore<OperationBlockStartAnalyzerAction, OperationBlockAnalyzerAction, OperationAnalyzerAction, IOperation, int>(
                operationBlockStartActions, operationBlockActions, operationBlockEndActions, analyzer,
                declaredNode, declaredSymbol, operationBlocks, (blocks) => operations, semanticModel,
                getKind: null, filterSpan, isGeneratedCode, cancellationToken);
        }

        private void ExecuteBlockActionsCore<TBlockStartAction, TBlockAction, TNodeAction, TNode, TLanguageKindEnum>(
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
           TextSpan? filterSpan,
           bool isGeneratedCode,
           CancellationToken cancellationToken)
           where TLanguageKindEnum : struct
           where TBlockStartAction : AnalyzerAction
           where TBlockAction : AnalyzerAction
           where TNodeAction : AnalyzerAction
        {
            Debug.Assert(declaredNode != null);
            Debug.Assert(declaredSymbol != null);
            Debug.Assert(CanHaveExecutableCodeBlock(declaredSymbol));
            Debug.Assert(startActions.Any() || endActions.Any() || actions.Any());
            Debug.Assert(!executableBlocks.IsEmpty);

            if (isGeneratedCode && _shouldSkipAnalysisOnGeneratedCode(analyzer) ||
                IsAnalyzerSuppressedForTree(analyzer, declaredNode.SyntaxTree, cancellationToken))
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
            blockEndActions.AddAll(endActions);

            var diagReporter = GetAddSemanticDiagnostic(semanticModel.SyntaxTree, declaredNode.FullSpan, analyzer, cancellationToken);

            // Include the stateful actions.
            foreach (var startAction in startActions)
            {
                if (startAction is CodeBlockStartAnalyzerAction<TLanguageKindEnum> codeBlockStartAction)
                {
                    var codeBlockEndActions = blockEndActions as PooledHashSet<CodeBlockAnalyzerAction>;
                    var codeBlockScope = new HostCodeBlockStartAnalysisScope<TLanguageKindEnum>();
                    var blockStartContext = new AnalyzerCodeBlockStartAnalysisContext<TLanguageKindEnum>(startAction.Analyzer,
                        codeBlockScope, declaredNode, declaredSymbol, semanticModel, AnalyzerOptions, filterSpan, isGeneratedCode, cancellationToken);

                    // Catch Exception from the start action.
                    ExecuteAndCatchIfThrows(
                        startAction.Analyzer,
                        analyze: data =>
                        {
                            data.action(data.context);
                            data.blockEndActions?.AddAll(data.scope.CodeBlockEndActions);
                            data.syntaxNodeActions?.AddRange(data.scope.SyntaxNodeActions);
                        },
                        argument: (action: codeBlockStartAction.Action, context: blockStartContext, scope: codeBlockScope, blockEndActions: codeBlockEndActions, syntaxNodeActions),
                        contextInfo: new AnalysisContextInfo(Compilation, declaredSymbol, declaredNode),
                        cancellationToken: cancellationToken);
                }
                else
                {
                    if (startAction is OperationBlockStartAnalyzerAction operationBlockStartAction)
                    {
                        var operationBlockEndActions = blockEndActions as PooledHashSet<OperationBlockAnalyzerAction>;
                        var operationBlockScope = new HostOperationBlockStartAnalysisScope();
                        var operationStartContext = new AnalyzerOperationBlockStartAnalysisContext(startAction.Analyzer,
                            operationBlockScope, operationBlocks, declaredSymbol, semanticModel.Compilation, AnalyzerOptions,
                            GetControlFlowGraph, declaredNode.SyntaxTree, filterSpan, isGeneratedCode, cancellationToken);

                        // Catch Exception from the start action.
                        ExecuteAndCatchIfThrows(
                            startAction.Analyzer,
                            analyze: data =>
                            {
                                data.action(data.context);
                                data.blockEndActions?.AddAll(data.scope.OperationBlockEndActions);
                                data.operationActions?.AddRange(data.scope.OperationActions);
                            },
                            argument: (action: operationBlockStartAction.Action, context: operationStartContext, scope: operationBlockScope, blockEndActions: operationBlockEndActions, operationActions: operationActions),
                            contextInfo: new AnalysisContextInfo(Compilation, declaredSymbol),
                            cancellationToken: cancellationToken);
                    }
                }
            }

            using var _ = PooledDelegates.GetPooledFunction(unboundFunction: (d, ct, arg) => arg.self.IsSupportedDiagnostic(arg.analyzer, d, ct), argument: (self: this, analyzer), boundFunction: out Func<Diagnostic, CancellationToken, bool> isSupportedDiagnostic);

            // Execute stateful executable node analyzers, if any.
            if (executableNodeActions.Any())
            {
                if (syntaxNodeActions != null)
                {
                    Debug.Assert(getKind != null);

                    var executableNodeActionsByKind = GetNodeActionsByKind(syntaxNodeActions);
                    var syntaxNodesToAnalyze = (IEnumerable<SyntaxNode>)getNodesToAnalyze(executableBlocks);
                    ExecuteSyntaxNodeActions(syntaxNodesToAnalyze, executableNodeActionsByKind, analyzer, declaredSymbol, semanticModel, getKind, diagReporter, isSupportedDiagnostic, filterSpan, isGeneratedCode, hasCodeBlockStartOrSymbolStartActions: startActions.Any(), cancellationToken);
                }
                else if (operationActions != null)
                {
                    var operationActionsByKind = GetOperationActionsByKind(operationActions);
                    var operationsToAnalyze = (IEnumerable<IOperation>)getNodesToAnalyze(executableBlocks);
                    ExecuteOperationActions(operationsToAnalyze, operationActionsByKind, analyzer, declaredSymbol, semanticModel, diagReporter, isSupportedDiagnostic, filterSpan, isGeneratedCode, hasOperationBlockStartOrSymbolStartActions: startActions.Any(), cancellationToken);
                }
            }

            executableNodeActions.Free();

            ExecuteBlockActions(blockActions, declaredNode, declaredSymbol, analyzer, semanticModel, operationBlocks, diagReporter.AddDiagnosticAction, isSupportedDiagnostic, filterSpan, isGeneratedCode, cancellationToken);
            ExecuteBlockActions(blockEndActions, declaredNode, declaredSymbol, analyzer, semanticModel, operationBlocks, diagReporter.AddDiagnosticAction, isSupportedDiagnostic, filterSpan, isGeneratedCode, cancellationToken);

            diagReporter.Free();
        }

        private void ExecuteBlockActions<TBlockAction>(
            PooledHashSet<TBlockAction> blockActions,
            SyntaxNode declaredNode,
            ISymbol declaredSymbol,
            DiagnosticAnalyzer analyzer,
            SemanticModel semanticModel,
            ImmutableArray<IOperation> operationBlocks,
            Action<Diagnostic> addDiagnostic,
            Func<Diagnostic, CancellationToken, bool> isSupportedDiagnostic,
            TextSpan? filterSpan,
            bool isGeneratedCode,
            CancellationToken cancellationToken)
            where TBlockAction : AnalyzerAction
        {
            Debug.Assert(!isGeneratedCode || !_shouldSkipAnalysisOnGeneratedCode(analyzer));
            Debug.Assert(!IsAnalyzerSuppressedForTree(analyzer, declaredNode.SyntaxTree, cancellationToken));

            foreach (var blockAction in blockActions)
            {
                var codeBlockAction = blockAction as CodeBlockAnalyzerAction;
                if (codeBlockAction != null)
                {
                    var context = new CodeBlockAnalysisContext(declaredNode, declaredSymbol, semanticModel,
                        AnalyzerOptions, addDiagnostic, isSupportedDiagnostic, filterSpan, isGeneratedCode, cancellationToken);

                    ExecuteAndCatchIfThrows(
                        codeBlockAction.Analyzer,
                        analyze: data => data.action(data.context),
                        argument: (action: codeBlockAction.Action, context: context),
                        contextInfo: new AnalysisContextInfo(Compilation, declaredSymbol, declaredNode),
                        cancellationToken: cancellationToken);
                }
                else
                {
                    var operationBlockAction = blockAction as OperationBlockAnalyzerAction;
                    if (operationBlockAction != null)
                    {
                        var context = new OperationBlockAnalysisContext(operationBlocks, declaredSymbol, semanticModel.Compilation,
                            AnalyzerOptions, addDiagnostic, isSupportedDiagnostic, GetControlFlowGraph, declaredNode.SyntaxTree, filterSpan, isGeneratedCode, cancellationToken);

                        ExecuteAndCatchIfThrows(
                            operationBlockAction.Analyzer,
                            analyze: data => data.action(data.context),
                            argument: (action: operationBlockAction.Action, context),
                            contextInfo: new AnalysisContextInfo(Compilation, declaredSymbol),
                            cancellationToken: cancellationToken);
                    }
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
        /// Execute syntax node actions for the given analyzer for the given declaration.
        /// </summary>
        public void ExecuteSyntaxNodeActions<TLanguageKindEnum>(
           IEnumerable<SyntaxNode> nodesToAnalyze,
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

            var diagReporter = GetAddSemanticDiagnostic(model.SyntaxTree, spanForContainingTopmostNodeForAnalysis, analyzer, cancellationToken);

            using var _ = PooledDelegates.GetPooledFunction(unboundFunction: (d, ct, arg) => arg.self.IsSupportedDiagnostic(arg.analyzer, d, ct), argument: (self: this, analyzer), boundFunction: out Func<Diagnostic, CancellationToken, bool> isSupportedDiagnostic);
            ExecuteSyntaxNodeActions(nodesToAnalyze, nodeActionsByKind, analyzer, declaredSymbol, model, getKind, diagReporter, isSupportedDiagnostic, filterSpan, isGeneratedCode, hasCodeBlockStartOrSymbolStartActions, cancellationToken);
            diagReporter.Free();
        }

        private void ExecuteSyntaxNodeActions<TLanguageKindEnum>(
            IEnumerable<SyntaxNode> nodesToAnalyze,
            ImmutableSegmentedDictionary<TLanguageKindEnum, ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>> nodeActionsByKind,
            DiagnosticAnalyzer analyzer,
            ISymbol containingSymbol,
            SemanticModel model,
            Func<SyntaxNode, TLanguageKindEnum> getKind,
            AnalyzerDiagnosticReporter diagReporter,
            Func<Diagnostic, CancellationToken, bool> isSupportedDiagnostic,
            TextSpan? filterSpan,
            bool isGeneratedCode,
            bool hasCodeBlockStartOrSymbolStartActions,
            CancellationToken cancellationToken)
            where TLanguageKindEnum : struct
        {
            Debug.Assert(nodeActionsByKind.Any());
            Debug.Assert(!isGeneratedCode || !_shouldSkipAnalysisOnGeneratedCode(analyzer));
            Debug.Assert(!IsAnalyzerSuppressedForTree(analyzer, model.SyntaxTree, cancellationToken));

            foreach (var node in nodesToAnalyze)
            {
                // Most nodes have no registered actions. Check for actions before checking if the analyzer should be
                // executed on the node since the generated code check in ShouldExecuteNode can be expensive in
                // aggregate.
                if (nodeActionsByKind.TryGetValue(getKind(node), out var actionsForKind))
                {
                    Debug.Assert(!actionsForKind.IsEmpty, $"Unexpected empty action collection in {nameof(nodeActionsByKind)}");
                    if (ShouldExecuteNode(node, analyzer, cancellationToken))
                    {
                        // If analyzer hasn't registered any CodeBlockStart or SymbolStart actions, then update the filter span
                        // for local diagnostics to be the callback node's full span.
                        // For this case, any diagnostic reported in node's callback outside it's full span will be considered
                        // a non-local diagnostic.
                        if (!hasCodeBlockStartOrSymbolStartActions)
                            diagReporter.FilterSpanForLocalDiagnostics = node.FullSpan;

                        foreach (var action in actionsForKind)
                        {
                            ExecuteSyntaxNodeAction(action, node, containingSymbol, model, diagReporter.AddDiagnosticAction, isSupportedDiagnostic, filterSpan, isGeneratedCode, cancellationToken);
                        }
                    }
                }
            }
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
        /// Execute operation actions for the given analyzer for the given declaration.
        /// </summary>
        /// <returns>
        /// True, if successfully executed the actions for the given analysis scope OR all the actions have already been executed for the given analysis scope.
        /// False, if there are some pending actions that are currently being executed on another thread.
        /// </returns>
        public void ExecuteOperationActions(
            IEnumerable<IOperation> operationsToAnalyze,
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

            var diagReporter = GetAddSemanticDiagnostic(model.SyntaxTree, spanForContainingOperationBlock, analyzer, cancellationToken);

            using var _ = PooledDelegates.GetPooledFunction(unboundFunction: (d, ct, arg) => arg.self.IsSupportedDiagnostic(arg.analyzer, d, ct), argument: (self: this, analyzer), boundFunction: out Func<Diagnostic, CancellationToken, bool> isSupportedDiagnostic);
            ExecuteOperationActions(operationsToAnalyze, operationActionsByKind, analyzer, declaredSymbol, model, diagReporter, isSupportedDiagnostic, filterSpan, isGeneratedCode, hasOperationBlockStartOrSymbolStartActions, cancellationToken);
            diagReporter.Free();
        }

        private void ExecuteOperationActions(
            IEnumerable<IOperation> operationsToAnalyze,
            ImmutableSegmentedDictionary<OperationKind, ImmutableArray<OperationAnalyzerAction>> operationActionsByKind,
            DiagnosticAnalyzer analyzer,
            ISymbol containingSymbol,
            SemanticModel model,
            AnalyzerDiagnosticReporter diagReporter,
            Func<Diagnostic, CancellationToken, bool> isSupportedDiagnostic,
            TextSpan? filterSpan,
            bool isGeneratedCode,
            bool hasOperationBlockStartOrSymbolStartActions,
            CancellationToken cancellationToken)
        {
            Debug.Assert(operationActionsByKind != null);
            Debug.Assert(operationActionsByKind.Any());
            Debug.Assert(!isGeneratedCode || !_shouldSkipAnalysisOnGeneratedCode(analyzer));
            Debug.Assert(!IsAnalyzerSuppressedForTree(analyzer, model.SyntaxTree, cancellationToken));

            foreach (var operation in operationsToAnalyze)
            {
                // Most operations have no registered actions. Check for actions before checking if the analyzer should
                // be executed on the operation since the generated code check in ShouldExecuteOperation can be
                // expensive in aggregate.
                if (operationActionsByKind.TryGetValue(operation.Kind, out var actionsForKind))
                {
                    Debug.Assert(!actionsForKind.IsEmpty, $"Unexpected empty action collection in {nameof(operationActionsByKind)}");
                    if (ShouldExecuteOperation(operation, analyzer, cancellationToken))
                    {
                        // If analyzer hasn't registered any OperationBlockStart or SymbolStart actions, then update
                        // the filter span for local diagnostics to be the callback operation's full span.
                        // For this case, any diagnostic reported in operation's callback outside it's full span
                        // will be considered a non-local diagnostic.
                        if (!hasOperationBlockStartOrSymbolStartActions)
                            diagReporter.FilterSpanForLocalDiagnostics = operation.Syntax.FullSpan;

                        foreach (var action in actionsForKind)
                        {
                            ExecuteOperationAction(action, operation, containingSymbol, model, diagReporter.AddDiagnosticAction, isSupportedDiagnostic, filterSpan, isGeneratedCode, cancellationToken);
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

        internal void ExecuteAndCatchIfThrows<TArg>(DiagnosticAnalyzer analyzer, TArg argument, Action<TArg> analyze, AnalysisContextInfo? contextInfo, CancellationToken cancellationToken)
        {
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
                    ExecuteAndCatchIfThrows_NoLock(analyzer, analyze: analyze, argument: argument, info: contextInfo, cancellationToken: cancellationToken);
                }
            }
            else
            {
                ExecuteAndCatchIfThrows_NoLock(analyzer, analyze: analyze, argument: argument, info: contextInfo, cancellationToken: cancellationToken);
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
        private void ExecuteAndCatchIfThrows_NoLock<TArg>(DiagnosticAnalyzer analyzer, TArg argument, Action<TArg> analyze, AnalysisContextInfo? info, CancellationToken cancellationToken)
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
            if (!ExceptionFilter(exception, analyzerExceptionFilter, cancellationToken))
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

            static bool ExceptionFilter(Exception ex, Func<Exception, bool>? analyzerExceptionFilter, CancellationToken cancellationToken)
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

        private Action<Diagnostic> GetAddDiagnostic(ISymbol contextSymbol, ImmutableArray<SyntaxReference> cachedDeclaringReferences, DiagnosticAnalyzer analyzer, Func<ISymbol, SyntaxReference, Compilation, CancellationToken, SyntaxNode> getTopMostNodeForAnalysis, CancellationToken cancellationToken)
        {
            return GetAddDiagnostic(contextSymbol, cachedDeclaringReferences, Compilation, analyzer, _addNonCategorizedDiagnostic,
                 _addCategorizedLocalDiagnostic, _addCategorizedNonLocalDiagnostic, getTopMostNodeForAnalysis, _shouldSuppressGeneratedCodeDiagnostic, cancellationToken);
        }

        private static Action<Diagnostic> GetAddDiagnostic(
            ISymbol contextSymbol,
            ImmutableArray<SyntaxReference> cachedDeclaringReferences,
            Compilation compilation,
            DiagnosticAnalyzer analyzer,
            Action<Diagnostic, CancellationToken>? addNonCategorizedDiagnostic,
            Action<Diagnostic, DiagnosticAnalyzer, bool, CancellationToken>? addCategorizedLocalDiagnostic,
            Action<Diagnostic, DiagnosticAnalyzer, CancellationToken>? addCategorizedNonLocalDiagnostic,
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
                    addNonCategorizedDiagnostic(diagnostic, cancellationToken);
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
                                addCategorizedLocalDiagnostic(diagnostic, analyzer, false, cancellationToken);
                                return;
                            }
                        }
                    }
                }

                addCategorizedNonLocalDiagnostic(diagnostic, analyzer, cancellationToken);
            };
        }

        private Action<Diagnostic> GetAddCompilationDiagnostic(DiagnosticAnalyzer analyzer, CancellationToken cancellationToken)
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
                    _addNonCategorizedDiagnostic(diagnostic, cancellationToken);
                    return;
                }

                _addCategorizedNonLocalDiagnostic(diagnostic, analyzer, cancellationToken);
            };
        }

        private AnalyzerDiagnosticReporter GetAddSemanticDiagnostic(SyntaxTree tree, DiagnosticAnalyzer analyzer, CancellationToken cancellationToken)
        {
            return AnalyzerDiagnosticReporter.GetInstance(new SourceOrAdditionalFile(tree), span: null, Compilation, analyzer, isSyntaxDiagnostic: false,
                _addNonCategorizedDiagnostic, _addCategorizedLocalDiagnostic, _addCategorizedNonLocalDiagnostic,
                _shouldSuppressGeneratedCodeDiagnostic, cancellationToken);
        }

        private AnalyzerDiagnosticReporter GetAddSemanticDiagnostic(SyntaxTree tree, TextSpan? span, DiagnosticAnalyzer analyzer, CancellationToken cancellationToken)
        {
            return AnalyzerDiagnosticReporter.GetInstance(new SourceOrAdditionalFile(tree), span, Compilation, analyzer, isSyntaxDiagnostic: false,
                _addNonCategorizedDiagnostic, _addCategorizedLocalDiagnostic, _addCategorizedNonLocalDiagnostic,
                _shouldSuppressGeneratedCodeDiagnostic, cancellationToken);
        }

        private AnalyzerDiagnosticReporter GetAddSyntaxDiagnostic(SourceOrAdditionalFile file, DiagnosticAnalyzer analyzer, CancellationToken cancellationToken)
        {
            return AnalyzerDiagnosticReporter.GetInstance(file, span: null, Compilation, analyzer, isSyntaxDiagnostic: true,
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
