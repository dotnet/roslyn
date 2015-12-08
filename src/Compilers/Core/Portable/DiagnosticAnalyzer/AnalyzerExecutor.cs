// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Semantics;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

using AnalyzerStateData = Microsoft.CodeAnalysis.Diagnostics.AnalysisState.AnalyzerStateData;
using SyntaxNodeAnalyzerStateData = Microsoft.CodeAnalysis.Diagnostics.AnalysisState.SyntaxNodeAnalyzerStateData;
using OperationAnalyzerStateData = Microsoft.CodeAnalysis.Diagnostics.AnalysisState.OperationAnalyzerStateData;
using DeclarationAnalyzerStateData = Microsoft.CodeAnalysis.Diagnostics.AnalysisState.DeclarationAnalyzerStateData;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Contains the core execution logic for callbacks into analyzers.
    /// </summary>
    internal class AnalyzerExecutor
    {
        private const string DiagnosticCategory = "Compiler";

        // internal for testing purposes only.
        internal const string AnalyzerExceptionDiagnosticId = "AD0001";
        internal const string AnalyzerDriverExceptionDiagnosticId = "AD0002";

        private readonly Compilation _compilation;
        private readonly AnalyzerOptions _analyzerOptions;
        private readonly Action<Diagnostic> _addDiagnostic;
        private readonly Action<Diagnostic, DiagnosticAnalyzer, bool> _addLocalDiagnosticOpt;
        private readonly Action<Diagnostic, DiagnosticAnalyzer> _addNonLocalDiagnosticOpt;
        private readonly Action<Exception, DiagnosticAnalyzer, Diagnostic> _onAnalyzerException;
        private readonly AnalyzerManager _analyzerManager;
        private readonly Func<DiagnosticAnalyzer, bool> _isCompilerAnalyzer;
        private readonly Func<DiagnosticAnalyzer, SemaphoreSlim> _getAnalyzerGateOpt;
        private readonly ConcurrentDictionary<DiagnosticAnalyzer, TimeSpan> _analyzerExecutionTimeMapOpt;

        private readonly CancellationToken _cancellationToken;

        /// <summary>
        /// Creates <see cref="AnalyzerExecutor"/> to execute analyzer actions with given arguments
        /// </summary>
        /// <param name="compilation">Compilation to be used in the analysis.</param>
        /// <param name="analyzerOptions">Analyzer options.</param>
        /// <param name="addDiagnostic">Delegate to add analyzer diagnostics.</param>
        /// <param name="onAnalyzerException">
        /// Optional delegate which is invoked when an analyzer throws an exception.
        /// Delegate can do custom tasks such as report the given analyzer exception diagnostic, report a non-fatal watson for the exception, etc.
        /// </param>
        /// <param name="isCompilerAnalyzer">Delegate to determine if the given analyzer is compiler analyzer. 
        /// We need to special case the compiler analyzer at few places for performance reasons.</param>
        /// <param name="analyzerManager">Analyzer manager to fetch supported diagnostics.</param>
        /// <param name="getAnalyzerGate">
        /// Delegate to fetch the gate object to guard all callbacks into the analyzer.
        /// It should return a unique gate object for the given analyzer instance for non-concurrent analyzers, and null otherwise.
        /// All analyzer callbacks for non-concurrent analyzers will be guarded with a lock on the gate.
        /// </param>
        /// <param name="logExecutionTime">Flag indicating whether we need to log analyzer execution time.</param>
        /// <param name="addLocalDiagnosticOpt">Optional delegate to add local analyzer diagnostics.</param>
        /// <param name="addNonLocalDiagnosticOpt">Optional delegate to add non-local analyzer diagnostics.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static AnalyzerExecutor Create(
            Compilation compilation,
            AnalyzerOptions analyzerOptions,
            Action<Diagnostic> addDiagnostic,
            Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException,
            Func<DiagnosticAnalyzer, bool> isCompilerAnalyzer,
            AnalyzerManager analyzerManager,
            Func<DiagnosticAnalyzer, SemaphoreSlim> getAnalyzerGate,
            bool logExecutionTime = false,
            Action<Diagnostic, DiagnosticAnalyzer, bool> addLocalDiagnosticOpt = null,
            Action<Diagnostic, DiagnosticAnalyzer> addNonLocalDiagnosticOpt = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var analyzerExecutionTimeMapOpt = logExecutionTime ? new ConcurrentDictionary<DiagnosticAnalyzer, TimeSpan>() : null;

            return new AnalyzerExecutor(compilation, analyzerOptions, addDiagnostic, onAnalyzerException, isCompilerAnalyzer, 
                analyzerManager, getAnalyzerGate, analyzerExecutionTimeMapOpt, addLocalDiagnosticOpt, addNonLocalDiagnosticOpt, cancellationToken);
        }

        /// <summary>
        /// Creates <see cref="AnalyzerExecutor"/> to fetch <see cref="DiagnosticAnalyzer.SupportedDiagnostics"/>.
        /// </summary>
        /// <param name="onAnalyzerException">
        /// Optional delegate which is invoked when an analyzer throws an exception.
        /// Delegate can do custom tasks such as report the given analyzer exception diagnostic, report a non-fatal watson for the exception, etc.
        /// </param>
        /// <param name="analyzerManager">Analyzer manager to fetch supported diagnostics.</param>
        /// <param name="logExecutionTime">Flag indicating whether we need to log analyzer execution time.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static AnalyzerExecutor CreateForSupportedDiagnostics(
            Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException,
            AnalyzerManager analyzerManager,
            bool logExecutionTime = false,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return new AnalyzerExecutor(
                compilation: null,
                analyzerOptions: null,
                addDiagnostic: null,
                isCompilerAnalyzer: null,
                getAnalyzerGateOpt: null,
                onAnalyzerException: onAnalyzerException,
                analyzerManager: analyzerManager,
                analyzerExecutionTimeMapOpt: null,
                addLocalDiagnosticOpt: null,
                addNonLocalDiagnosticOpt: null,
                cancellationToken: cancellationToken);
        }

        private AnalyzerExecutor(
            Compilation compilation,
            AnalyzerOptions analyzerOptions,
            Action<Diagnostic> addDiagnostic,
            Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException,
            Func<DiagnosticAnalyzer, bool> isCompilerAnalyzer,
            AnalyzerManager analyzerManager,
            Func<DiagnosticAnalyzer, SemaphoreSlim> getAnalyzerGateOpt,
            ConcurrentDictionary<DiagnosticAnalyzer, TimeSpan> analyzerExecutionTimeMapOpt,
            Action<Diagnostic, DiagnosticAnalyzer, bool> addLocalDiagnosticOpt,
            Action<Diagnostic, DiagnosticAnalyzer> addNonLocalDiagnosticOpt,
            CancellationToken cancellationToken)
        {
            _compilation = compilation;
            _analyzerOptions = analyzerOptions;
            _addDiagnostic = addDiagnostic;
            _onAnalyzerException = onAnalyzerException;
            _isCompilerAnalyzer = isCompilerAnalyzer;
            _analyzerManager = analyzerManager;
            _getAnalyzerGateOpt = getAnalyzerGateOpt;
            _analyzerExecutionTimeMapOpt = analyzerExecutionTimeMapOpt;
            _addLocalDiagnosticOpt = addLocalDiagnosticOpt;
            _addNonLocalDiagnosticOpt = addNonLocalDiagnosticOpt;
            _cancellationToken = cancellationToken;
        }

        public AnalyzerExecutor WithCancellationToken(CancellationToken cancellationToken)
        {
            if (cancellationToken == _cancellationToken)
            {
                return this;
            }

            return new AnalyzerExecutor(_compilation, _analyzerOptions, _addDiagnostic, _onAnalyzerException, _isCompilerAnalyzer,
                _analyzerManager, _getAnalyzerGateOpt, _analyzerExecutionTimeMapOpt, _addLocalDiagnosticOpt, _addNonLocalDiagnosticOpt, cancellationToken);
        }

        internal Compilation Compilation => _compilation;
        internal AnalyzerOptions AnalyzerOptions => _analyzerOptions;
        internal CancellationToken CancellationToken => _cancellationToken;
        internal Action<Exception, DiagnosticAnalyzer, Diagnostic> OnAnalyzerException => _onAnalyzerException;
        internal ImmutableDictionary<DiagnosticAnalyzer, TimeSpan> AnalyzerExecutionTimes => _analyzerExecutionTimeMapOpt.ToImmutableDictionary();

        /// <summary>
        /// Executes the <see cref="DiagnosticAnalyzer.Initialize(AnalysisContext)"/> for the given analyzer.
        /// </summary>
        /// <param name="analyzer">Analyzer to get session wide analyzer actions.</param>
        /// <param name="sessionScope">Session scope to store register session wide analyzer actions.</param>
        /// <remarks>
        /// Note that this API doesn't execute any <see cref="CompilationStartAnalyzerAction"/> registered by the Initialize invocation.
        /// Use <see cref="ExecuteCompilationStartActionsAsync(ImmutableArray{CompilationStartAnalyzerAction}, HostCompilationStartAnalysisScope)"/> API
        /// to get execute these actions to get the per-compilation analyzer actions.
        /// </remarks>
        public async Task ExecuteInitializeMethodAsync(DiagnosticAnalyzer analyzer, HostSessionStartAnalysisScope sessionScope)
        {
            // The Initialize method should be run asynchronously in case it is not well behaved, e.g. does not terminate.
            await ExecuteAndCatchIfThrowsAsync(analyzer, () => analyzer.Initialize(new AnalyzerAnalysisContext(analyzer, sessionScope))).ConfigureAwait(false);
        }

        /// <summary>
        /// Executes the compilation start actions.
        /// </summary>
        /// <param name="actions"><see cref="AnalyzerActions"/> whose compilation start actions are to be executed.</param>
        /// <param name="compilationScope">Compilation scope to store the analyzer actions.</param>
        public async Task ExecuteCompilationStartActionsAsync(ImmutableArray<CompilationStartAnalyzerAction> actions, HostCompilationStartAnalysisScope compilationScope)
        {
            foreach (var startAction in actions)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                await ExecuteAndCatchIfThrowsAsync(startAction.Analyzer,
                    () => startAction.Action(new AnalyzerCompilationStartAnalysisContext(startAction.Analyzer, compilationScope, _compilation, _analyzerOptions, _cancellationToken))).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Executes compilation actions or compilation end actions.
        /// </summary>
        /// <param name="compilationActions">Compilation actions to be executed.</param>
        /// <param name="analyzer">Analyzer whose actions are to be executed.</param>
        /// <param name="compilationEvent">Compilation event.</param>
        /// <param name="analysisScope">Scope for analyzer execution.</param>
        /// <param name="analysisStateOpt">An optional object to track analysis state.</param>
        public async Task ExecuteCompilationActionsAsync(
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
                analyzerStateOpt = await TryStartProcessingEventAsync(compilationEvent, analyzer, analysisScope, analysisStateOpt, _cancellationToken).ConfigureAwait(false);
                if (analysisStateOpt == null || analyzerStateOpt != null)
                {
                    await ExecuteCompilationActionsCoreAsync(compilationActions, analyzer, analyzerStateOpt).ConfigureAwait(false);

                    if (analysisStateOpt != null)
                    {
                        await analysisStateOpt.MarkEventCompleteAsync(compilationEvent, analyzer, _cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                analyzerStateOpt?.ResetToReadyState();
            }
        }

        private async Task ExecuteCompilationActionsCoreAsync(ImmutableArray<CompilationAnalyzerAction> compilationActions, DiagnosticAnalyzer analyzer, AnalyzerStateData analyzerStateOpt)
        {
            var addDiagnostic = GetAddCompilationDiagnostic(analyzer);

            foreach (var endAction in compilationActions)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                if (ShouldExecuteAction(analyzerStateOpt, endAction))
                {
                    await ExecuteAndCatchIfThrowsAsync(endAction.Analyzer,
                        () => endAction.Action(new CompilationAnalysisContext(_compilation, _analyzerOptions, addDiagnostic,
                            d => IsSupportedDiagnostic(endAction.Analyzer, d), _cancellationToken))).ConfigureAwait(false);

                    analyzerStateOpt?.ProcessedActions.Add(endAction);
                }
            }
        }

        /// <summary>
        /// Executes the symbol actions on the given symbol.
        /// </summary>
        /// <param name="symbolActions">Symbol actions to be executed.</param>
        /// <param name="analyzer">Analyzer whose actions are to be executed.</param>
        /// <param name="symbol">Symbol to be analyzed.</param>
        /// <param name="getTopMostNodeForAnalysis">Delegate to get topmost declaration node for a symbol declaration reference.</param>
        /// <param name="analysisScope">Scope for analyzer execution.</param>
        /// <param name="analysisStateOpt">An optional object to track analysis state.</param>
        public async Task ExecuteSymbolActionsAsync(
            ImmutableArray<SymbolAnalyzerAction> symbolActions,
            DiagnosticAnalyzer analyzer,
            ISymbol symbol,
            Func<ISymbol, SyntaxReference, Compilation, SyntaxNode> getTopMostNodeForAnalysis,
            AnalysisScope analysisScope,
            AnalysisState analysisStateOpt)
        {
            AnalyzerStateData analyzerStateOpt = null;

            try
            {
                analyzerStateOpt = await TryStartAnalyzingSymbolAsync(symbol, analyzer, analysisScope, analysisStateOpt, _cancellationToken).ConfigureAwait(false);
                if (analysisStateOpt == null || analyzerStateOpt != null)
                {
                    await ExecuteSymbolActionsCoreAsync(symbolActions, analyzer, symbol, getTopMostNodeForAnalysis, analyzerStateOpt).ConfigureAwait(false);

                    if (analysisStateOpt != null)
                    {
                        await analysisStateOpt.MarkSymbolCompleteAsync(symbol, analyzer, _cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                analyzerStateOpt?.ResetToReadyState();
            }
        }

        private async Task ExecuteSymbolActionsCoreAsync(
            ImmutableArray<SymbolAnalyzerAction> symbolActions,
            DiagnosticAnalyzer analyzer,
            ISymbol symbol,
            Func<ISymbol, SyntaxReference, Compilation, SyntaxNode> getTopMostNodeForAnalysis,
            AnalyzerStateData analyzerStateOpt)
        {
            Debug.Assert(getTopMostNodeForAnalysis != null);

            var addDiagnostic = GetAddDiagnostic(symbol, _compilation, analyzer, _addDiagnostic, _addLocalDiagnosticOpt, _addNonLocalDiagnosticOpt, getTopMostNodeForAnalysis);

            foreach (var symbolAction in symbolActions)
            {
                var action = symbolAction.Action;
                var kinds = symbolAction.Kinds;

                if (kinds.Contains(symbol.Kind))
                {
                    if (ShouldExecuteAction(analyzerStateOpt, symbolAction))
                    {
                        _cancellationToken.ThrowIfCancellationRequested();

                        await ExecuteAndCatchIfThrowsAsync(symbolAction.Analyzer,
                        () => action(new SymbolAnalysisContext(symbol, _compilation, _analyzerOptions, addDiagnostic,
                            d => IsSupportedDiagnostic(symbolAction.Analyzer, d), _cancellationToken))).ConfigureAwait(false);

                        analyzerStateOpt?.ProcessedActions.Add(symbolAction);
                    }
                }
            }
        }

        /// <summary>
        /// Executes the semantic model actions on the given semantic model.
        /// </summary>
        /// <param name="semanticModelActions">Semantic model actions to be executed.</param>
        /// <param name="analyzer">Analyzer whose actions are to be executed.</param>
        /// <param name="semanticModel">Semantic model to analyze.</param>
        /// <param name="compilationUnitCompletedEvent">Compilation event for semantic model analysis.</param>
        /// <param name="analysisScope">Scope for analyzer execution.</param>
        /// <param name="analysisStateOpt">An optional object to track analysis state.</param>
        public async Task ExecuteSemanticModelActionsAsync(
            ImmutableArray<SemanticModelAnalyzerAction> semanticModelActions,
            DiagnosticAnalyzer analyzer,
            SemanticModel semanticModel,
            CompilationEvent compilationUnitCompletedEvent,
            AnalysisScope analysisScope,
            AnalysisState analysisStateOpt)
        {
            AnalyzerStateData analyzerStateOpt = null;

            try
            {
                analyzerStateOpt = await TryStartProcessingEventAsync(compilationUnitCompletedEvent, analyzer, analysisScope, analysisStateOpt, _cancellationToken).ConfigureAwait(false);
                if (analysisStateOpt == null || analyzerStateOpt != null)
                {
                    await ExecuteSemanticModelActionsCoreAsync(semanticModelActions, analyzer, semanticModel, analyzerStateOpt).ConfigureAwait(false);

                    if (analysisStateOpt != null)
                    {
                        await analysisStateOpt.MarkEventCompleteAsync(compilationUnitCompletedEvent, analyzer, _cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                analyzerStateOpt?.ResetToReadyState();
            }
        }

        private async Task ExecuteSemanticModelActionsCoreAsync(
            ImmutableArray<SemanticModelAnalyzerAction> semanticModelActions,
            DiagnosticAnalyzer analyzer,
            SemanticModel semanticModel,
            AnalyzerStateData analyzerStateOpt)
        {
            var addDiagnostic = GetAddDiagnostic(semanticModel.SyntaxTree, analyzer, isSyntaxDiagnostic: false);

            foreach (var semanticModelAction in semanticModelActions)
            {
                if (ShouldExecuteAction(analyzerStateOpt, semanticModelAction))
                {
                    _cancellationToken.ThrowIfCancellationRequested();

                    // Catch Exception from action.
                    await ExecuteAndCatchIfThrowsAsync(semanticModelAction.Analyzer,
                        () => semanticModelAction.Action(new SemanticModelAnalysisContext(semanticModel, _analyzerOptions, addDiagnostic,
                            d => IsSupportedDiagnostic(semanticModelAction.Analyzer, d), _cancellationToken))).ConfigureAwait(false);

                    analyzerStateOpt?.ProcessedActions.Add(semanticModelAction);
                }
            }
        }

        /// <summary>
        /// Executes the syntax tree actions on the given syntax tree.
        /// </summary>
        /// <param name="syntaxTreeActions">Syntax tree actions to be executed.</param>
        /// <param name="analyzer">Analyzer whose actions are to be executed.</param>
        /// <param name="tree">Syntax tree to analyze.</param>
        /// <param name="analysisScope">Scope for analyzer execution.</param>
        /// <param name="analysisStateOpt">An optional object to track analysis state.</param>
        public async Task ExecuteSyntaxTreeActionsAsync(
            ImmutableArray<SyntaxTreeAnalyzerAction> syntaxTreeActions,
            DiagnosticAnalyzer analyzer,
            SyntaxTree tree,
            AnalysisScope analysisScope,
            AnalysisState analysisStateOpt)
        {
            AnalyzerStateData analyzerStateOpt = null;

            try
            {
                analyzerStateOpt = await TryStartSyntaxAnalysis(tree, analyzer, analysisScope, analysisStateOpt, _cancellationToken).ConfigureAwait(false);
                if (analysisStateOpt == null || analyzerStateOpt != null)
                {
                    await ExecuteSyntaxTreeActionsCoreAsync(syntaxTreeActions, analyzer, tree, analyzerStateOpt).ConfigureAwait(false);

                    if (analysisStateOpt != null)
                    {
                        await analysisStateOpt.MarkSyntaxAnalysisCompleteAsync(tree, analyzer, _cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                analyzerStateOpt?.ResetToReadyState();
            }
        }

        private async Task ExecuteSyntaxTreeActionsCoreAsync(
            ImmutableArray<SyntaxTreeAnalyzerAction> syntaxTreeActions,
            DiagnosticAnalyzer analyzer,
            SyntaxTree tree,
            AnalyzerStateData analyzerStateOpt)
        {
            var addDiagnostic = GetAddDiagnostic(tree, analyzer, isSyntaxDiagnostic: true);

            foreach (var syntaxTreeAction in syntaxTreeActions)
            {
                if (ShouldExecuteAction(analyzerStateOpt, syntaxTreeAction))
                {
                    _cancellationToken.ThrowIfCancellationRequested();

                    // Catch Exception from action.
                    await ExecuteAndCatchIfThrowsAsync(syntaxTreeAction.Analyzer,
                        () => syntaxTreeAction.Action(new SyntaxTreeAnalysisContext(tree, _analyzerOptions, addDiagnostic,
                            d => IsSupportedDiagnostic(syntaxTreeAction.Analyzer, d), _cancellationToken))).ConfigureAwait(false);

                    analyzerStateOpt?.ProcessedActions.Add(syntaxTreeAction);
                }
            }
        }

        private async Task ExecuteSyntaxNodeActionAsync<TLanguageKindEnum>(
            SyntaxNodeAnalyzerAction<TLanguageKindEnum> syntaxNodeAction,
            SyntaxNode node,
            SemanticModel semanticModel,
            Action<Diagnostic> addDiagnostic,
            SyntaxNodeAnalyzerStateData analyzerStateOpt)
            where TLanguageKindEnum : struct
        {
            Debug.Assert(analyzerStateOpt == null || analyzerStateOpt.CurrentNode == node);

            if (ShouldExecuteAction(analyzerStateOpt, syntaxNodeAction))
            {
                var syntaxNodeContext = new SyntaxNodeAnalysisContext(node, semanticModel, _analyzerOptions, addDiagnostic,
                    d => IsSupportedDiagnostic(syntaxNodeAction.Analyzer, d), _cancellationToken);
                await ExecuteAndCatchIfThrowsAsync(syntaxNodeAction.Analyzer, () => syntaxNodeAction.Action(syntaxNodeContext)).ConfigureAwait(false);

                analyzerStateOpt?.ProcessedActions.Add(syntaxNodeAction);
            }
        }

        private async Task ExecuteOperationActionAsync(
            OperationAnalyzerAction operationAction,
            IOperation operation,
            Action<Diagnostic> addDiagnostic,
            OperationAnalyzerStateData analyzerStateOpt)
        {
            Debug.Assert(analyzerStateOpt == null || analyzerStateOpt.CurrentOperation == operation);

            if (ShouldExecuteAction(analyzerStateOpt, operationAction))
            {
                var operationContext = new OperationAnalysisContext(operation, _analyzerOptions, _addDiagnostic, d => IsSupportedDiagnostic(operationAction.Analyzer, d), _cancellationToken);
                await ExecuteAndCatchIfThrowsAsync(operationAction.Analyzer, () => operationAction.Action(operationContext)).ConfigureAwait(false);

                analyzerStateOpt?.ProcessedActions.Add(operationAction);
            }
        }

        public async Task ExecuteCodeBlockActionsAsync<TLanguageKindEnum>(
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
            AnalysisScope analysisScope,
            AnalysisState analysisStateOpt)
            where TLanguageKindEnum : struct
        {
            DeclarationAnalyzerStateData analyzerStateOpt = null;

            try
            {
                analyzerStateOpt = await TryStartAnalyzingSyntaxReferenceAsync(declaration, analyzer, analysisScope, analysisStateOpt, _cancellationToken).ConfigureAwait(false);
                if (analysisStateOpt == null || analyzerStateOpt != null)
                {
                    await ExecuteBlockActionsCoreAsync<CodeBlockStartAnalyzerAction<TLanguageKindEnum>, CodeBlockAnalyzerAction, SyntaxNodeAnalyzerAction<TLanguageKindEnum>, SyntaxNodeAnalyzerStateData, SyntaxNode, TLanguageKindEnum>(
                        codeBlockStartActions, codeBlockActions, codeBlockEndActions, analyzer,
                        declaredNode, declaredSymbol, executableCodeBlocks, (codeBlocks) => codeBlocks.SelectMany(cb => cb.DescendantNodesAndSelf()), semanticModel, getKind, analyzerStateOpt?.CodeBlockAnalysisState).ConfigureAwait(false);
                }
            }
            finally
            {
                analyzerStateOpt?.ResetToReadyState();
            }
        }

        public async Task ExecuteOperationBlockActionsAsync(
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
            AnalysisScope analysisScope,
            AnalysisState analysisStateOpt)
        {
            DeclarationAnalyzerStateData analyzerStateOpt = null;

            try
            {
                analyzerStateOpt = await TryStartAnalyzingSyntaxReferenceAsync(declaration, analyzer, analysisScope, analysisStateOpt, _cancellationToken).ConfigureAwait(false);
                if (analysisStateOpt == null || analysisStateOpt != null)
                {
                    await ExecuteBlockActionsCoreAsync<OperationBlockStartAnalyzerAction, OperationBlockAnalyzerAction, OperationAnalyzerAction, OperationAnalyzerStateData, IOperation, int>(
                        operationBlockStartActions, operationBlockActions, operationBlockEndActions, analyzer,
                        declaredNode, declaredSymbol, operationBlocks, (blocks) => operations, semanticModel, null, analyzerStateOpt?.OperationBlockAnalysisState).ConfigureAwait(false);
                }
            }
            finally
            {
                analyzerStateOpt?.ResetToReadyState();
            }
        }

        private async Task ExecuteBlockActionsCoreAsync<TBlockStartAction, TBlockAction, TNodeAction, TNodeStateData, TNode, TLanguageKindEnum>(
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
           AnalysisState.BlockAnalyzerStateData<TBlockAction, TNodeStateData> analyzerStateOpt)
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

            var addDiagnostic = GetAddDiagnostic(semanticModel.SyntaxTree, declaredNode.FullSpan, analyzer, isSyntaxDiagnostic: false);

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
                            // Catch Exception from the start action.
                            await ExecuteAndCatchIfThrowsAsync(startAction.Analyzer, () =>
                            {
                                var codeBlockScope = new HostCodeBlockStartAnalysisScope<TLanguageKindEnum>();
                                var blockStartContext = new AnalyzerCodeBlockStartAnalysisContext<TLanguageKindEnum>(startAction.Analyzer,
                                    codeBlockScope, declaredNode, declaredSymbol, semanticModel, _analyzerOptions, _cancellationToken);
                                codeBlockStartAction.Action(blockStartContext);
                                codeBlockEndActions.AddAll(codeBlockScope.CodeBlockEndActions);
                                syntaxNodeActions.AddRange(codeBlockScope.SyntaxNodeActions);
                            }).ConfigureAwait(false);
                        }
                        else
                        {
                            var operationBlockStartAction = startAction as OperationBlockStartAnalyzerAction;
                            if (operationBlockStartAction != null)
                            {
                                var operationBlockEndActions = blockEndActions as PooledHashSet<OperationBlockAnalyzerAction>;
                                // Catch Exception from the start action.
                                await ExecuteAndCatchIfThrowsAsync(startAction.Analyzer, () =>
                                {
                                    var operationBlockScope = new HostOperationBlockStartAnalysisScope();
                                    var operationStartContext = new AnalyzerOperationBlockStartAnalysisContext(startAction.Analyzer,
                                        operationBlockScope, operationBlocks, declaredSymbol, _analyzerOptions, _cancellationToken);
                                    operationBlockStartAction.Action(operationStartContext);
                                    operationBlockEndActions.AddAll(operationBlockScope.OperationBlockEndActions);
                                    operationActions.AddRange(operationBlockScope.OperationActions);
                                }).ConfigureAwait(false);
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
                    await ExecuteSyntaxNodeActionsAsync(syntaxNodesToAnalyze, executableNodeActionsByKind, semanticModel, getKind, addDiagnostic, analyzerStateOpt?.ExecutableNodesAnalysisState as SyntaxNodeAnalyzerStateData).ConfigureAwait(false);
                }
                else if (operationActions != null)
                {
                    var operationActionsByKind = GetOperationActionsByKind(operationActions);
                    var operationsToAnalyze = (IEnumerable<IOperation>)getNodesToAnalyze(executableBlocks);
                    await ExecuteOperationActionsAsync(operationsToAnalyze, operationActionsByKind, addDiagnostic, analyzerStateOpt?.ExecutableNodesAnalysisState as OperationAnalyzerStateData).ConfigureAwait(false);
                }
            }

            executableNodeActions.Free();

            await ExecuteBlockActionsAsync(blockActions, declaredNode, declaredSymbol, semanticModel, operationBlocks, addDiagnostic, analyzerStateOpt).ConfigureAwait(false);
            await ExecuteBlockActionsAsync(blockEndActions, declaredNode, declaredSymbol, semanticModel, operationBlocks, addDiagnostic, analyzerStateOpt).ConfigureAwait(false);
        }

        private async Task ExecuteBlockActionsAsync<TBlockAction, TNodeStateData>(
            PooledHashSet<TBlockAction> blockActions,
            SyntaxNode declaredNode,
            ISymbol declaredSymbol,
            SemanticModel semanticModel,
            ImmutableArray<IOperation> operationBlocks,
            Action<Diagnostic> addDiagnostic,
            AnalysisState.BlockAnalyzerStateData<TBlockAction, TNodeStateData> analyzerStateOpt)
            where TBlockAction : AnalyzerAction
            where TNodeStateData : AnalyzerStateData, new()
        {
            foreach (var blockAction in blockActions)
            {
                if (ShouldExecuteAction(analyzerStateOpt, blockAction))
                {
                    var codeBlockAction = blockAction as CodeBlockAnalyzerAction;
                    Func<Diagnostic, bool> isSupportedDiagnostic = d => IsSupportedDiagnostic(blockAction.Analyzer, d);
                    if (codeBlockAction != null)
                    {
                        await ExecuteAndCatchIfThrowsAsync(
                            codeBlockAction.Analyzer,
                            () => codeBlockAction.Action(new CodeBlockAnalysisContext(declaredNode, declaredSymbol, semanticModel, _analyzerOptions, addDiagnostic, isSupportedDiagnostic, _cancellationToken))).ConfigureAwait(false);
                    }
                    else
                    {
                        var operationBlockAction = blockAction as OperationBlockAnalyzerAction;
                        if (operationBlockAction != null)
                        {
                            await ExecuteAndCatchIfThrowsAsync(
                                operationBlockAction.Analyzer,
                                () => operationBlockAction.Action(new OperationBlockAnalysisContext(operationBlocks, declaredSymbol, _analyzerOptions, addDiagnostic, isSupportedDiagnostic, _cancellationToken))).ConfigureAwait(false);
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

            var tuples = nodeActionsByKind.Select(kvp => KeyValuePair.Create(kvp.Key, kvp.Value.ToImmutableAndFree()));
            var map = ImmutableDictionary.CreateRange(tuples);
            nodeActionsByKind.Free();
            return map;
        }

        public async Task ExecuteSyntaxNodeActionsAsync<TLanguageKindEnum>(
           IEnumerable<SyntaxNode> nodesToAnalyze,
           IDictionary<TLanguageKindEnum, ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>> nodeActionsByKind,
           DiagnosticAnalyzer analyzer,
           SemanticModel model,
           Func<SyntaxNode, TLanguageKindEnum> getKind,
           TextSpan filterSpan,
           SyntaxReference declaration,
           AnalysisScope analysisScope,
           AnalysisState analysisStateOpt)
           where TLanguageKindEnum : struct
        {
            DeclarationAnalyzerStateData analyzerStateOpt = null;

            try
            {
                analyzerStateOpt = await TryStartAnalyzingSyntaxReferenceAsync(declaration, analyzer, analysisScope, analysisStateOpt, _cancellationToken).ConfigureAwait(false);
                if (analysisStateOpt == null || analyzerStateOpt != null)
                {
                    await ExecuteSyntaxNodeActionsCoreAsync(nodesToAnalyze, nodeActionsByKind, analyzer, model, getKind, filterSpan, analyzerStateOpt).ConfigureAwait(false);
                }
            }
            finally
            {
                analyzerStateOpt?.ResetToReadyState();
            }
        }

        private async Task ExecuteSyntaxNodeActionsCoreAsync<TLanguageKindEnum>(
            IEnumerable<SyntaxNode> nodesToAnalyze,
            IDictionary<TLanguageKindEnum, ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>> nodeActionsByKind,
            DiagnosticAnalyzer analyzer,
            SemanticModel model,
            Func<SyntaxNode, TLanguageKindEnum> getKind,
            TextSpan filterSpan,
            SyntaxNodeAnalyzerStateData analyzerStateOpt)
            where TLanguageKindEnum : struct
        {
            var addDiagnostic = GetAddDiagnostic(model.SyntaxTree, filterSpan, analyzer, isSyntaxDiagnostic: false);
            await ExecuteSyntaxNodeActionsAsync(nodesToAnalyze, nodeActionsByKind, model, getKind, addDiagnostic, analyzerStateOpt).ConfigureAwait(false);
        }

        private async Task ExecuteSyntaxNodeActionsAsync<TLanguageKindEnum>(
            IEnumerable<SyntaxNode> nodesToAnalyze,
            IDictionary<TLanguageKindEnum, ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>> nodeActionsByKind,
            SemanticModel model,
            Func<SyntaxNode, TLanguageKindEnum> getKind,
            Action<Diagnostic> addDiagnostic,
            SyntaxNodeAnalyzerStateData analyzerStateOpt)
            where TLanguageKindEnum : struct
        {
            Debug.Assert(nodeActionsByKind != null);
            Debug.Assert(nodeActionsByKind.Any());

            SyntaxNode partiallyProcessedNode = analyzerStateOpt?.CurrentNode;
            if (partiallyProcessedNode != null)
            {
                await ExecuteSyntaxNodeActionsAsync(partiallyProcessedNode, nodeActionsByKind, model, getKind, addDiagnostic, analyzerStateOpt).ConfigureAwait(false);
            }

            foreach (var child in nodesToAnalyze)
            {
                if (ShouldExecuteNode(analyzerStateOpt, child))
                {
                    SetCurrentNode(analyzerStateOpt, child);

                    await ExecuteSyntaxNodeActionsAsync(child, nodeActionsByKind, model, getKind, addDiagnostic, analyzerStateOpt).ConfigureAwait(false);
                }
            }
        }

        private async Task ExecuteSyntaxNodeActionsAsync<TLanguageKindEnum>(
            SyntaxNode node,
            IDictionary<TLanguageKindEnum, ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>> nodeActionsByKind,
            SemanticModel model,
            Func<SyntaxNode, TLanguageKindEnum> getKind,
            Action<Diagnostic> addDiagnostic,
            SyntaxNodeAnalyzerStateData analyzerStateOpt)
            where TLanguageKindEnum : struct
        {
            ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>> actionsForKind;
            if (nodeActionsByKind.TryGetValue(getKind(node), out actionsForKind))
            {
                foreach (var action in actionsForKind)
                {
                    await ExecuteSyntaxNodeActionAsync(action, node, model, addDiagnostic, analyzerStateOpt).ConfigureAwait(false);
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

            var tuples = operationActionsByKind.Select(kvp => KeyValuePair.Create(kvp.Key, kvp.Value.ToImmutableAndFree()));
            var map = ImmutableDictionary.CreateRange(tuples);
            operationActionsByKind.Free();
            return map;
        }

        public async Task ExecuteOperationActionsAsync(
            IEnumerable<IOperation> operationsToAnalyze,
            IDictionary<OperationKind, ImmutableArray<OperationAnalyzerAction>> operationActionsByKind,
            DiagnosticAnalyzer analyzer,
            SemanticModel model,
            TextSpan filterSpan,
            SyntaxReference declaration,
            AnalysisScope analysisScope,
            AnalysisState analysisStateOpt)
        {
            OperationAnalyzerStateData analyzerStateOpt = null;

            try
            {
                analyzerStateOpt = await TryStartAnalyzingOperationReferenceAsync(declaration, analyzer, analysisScope, analysisStateOpt, _cancellationToken).ConfigureAwait(false);
                if (analysisStateOpt == null || analyzerStateOpt != null)
                {
                    await ExecuteOperationActionsCoreAsync(operationsToAnalyze, operationActionsByKind, analyzer, model, filterSpan, analyzerStateOpt).ConfigureAwait(false);
                }
            }
            finally
            {
                analyzerStateOpt?.ResetToReadyState();
            }
        }

        private async Task ExecuteOperationActionsCoreAsync(
            IEnumerable<IOperation> operationsToAnalyze,
            IDictionary<OperationKind, ImmutableArray<OperationAnalyzerAction>> operationActionsByKind,
            DiagnosticAnalyzer analyzer,
            SemanticModel model,
            TextSpan filterSpan,
            OperationAnalyzerStateData analyzerStateOpt)
        {
            var addDiagnostic = GetAddDiagnostic(model.SyntaxTree, filterSpan, analyzer, isSyntaxDiagnostic: false);
            await ExecuteOperationActionsAsync(operationsToAnalyze, operationActionsByKind, addDiagnostic, analyzerStateOpt).ConfigureAwait(false);
        }

        private async Task ExecuteOperationActionsAsync(
            IEnumerable<IOperation> operationsToAnalyze,
            IDictionary<OperationKind, ImmutableArray<OperationAnalyzerAction>> operationActionsByKind,
            Action<Diagnostic> addDiagnostic,
            OperationAnalyzerStateData analyzerStateOpt)
        {
            Debug.Assert(operationActionsByKind != null);
            Debug.Assert(operationActionsByKind.Any());

            IOperation partiallyProcessedNode = analyzerStateOpt?.CurrentOperation;
            if (partiallyProcessedNode != null)
            {
                await ExecuteOperationActionsAsync(partiallyProcessedNode, operationActionsByKind, addDiagnostic, analyzerStateOpt).ConfigureAwait(false);
            }

            foreach (var child in operationsToAnalyze)
            {
                if (ShouldExecuteOperation(analyzerStateOpt, child))
                {
                    SetCurrentOperation(analyzerStateOpt, child);

                    await ExecuteOperationActionsAsync(child, operationActionsByKind, addDiagnostic, analyzerStateOpt).ConfigureAwait(false);
                }
            }
        }

        private async Task ExecuteOperationActionsAsync(
            IOperation operation,
            IDictionary<OperationKind, ImmutableArray<OperationAnalyzerAction>> operationActionsByKind,
            Action<Diagnostic> addDiagnostic,
            OperationAnalyzerStateData analyzerStateOpt)
        {
            ImmutableArray<OperationAnalyzerAction> actionsForKind;
            if (operationActionsByKind.TryGetValue(operation.Kind, out actionsForKind))
            {
                foreach (var action in actionsForKind)
                {
                    await ExecuteOperationActionAsync(action, operation, addDiagnostic, analyzerStateOpt).ConfigureAwait(false);
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
                    return true;

                case SymbolKind.Field:
                    Debug.Assert(((IFieldSymbol)symbol).AssociatedSymbol == null);
                    return true;

                default:
                    return false;
            }
        }

        internal async Task ExecuteAndCatchIfThrowsAsync(DiagnosticAnalyzer analyzer, Action analyze)
        {
            SemaphoreSlim gate = _getAnalyzerGateOpt?.Invoke(analyzer);
            if (gate != null)
            {
                using (await gate.DisposableWaitAsync(_cancellationToken).ConfigureAwait(false))
                {
                    ExecuteAndCatchIfThrows_NoLock(analyzer, analyze);
                }
            }
            else
            {
                ExecuteAndCatchIfThrows_NoLock(analyzer, analyze);
            }
        }

        private void ExecuteAndCatchIfThrows_NoLock(DiagnosticAnalyzer analyzer, Action analyze)
        {
            try
            {
                Stopwatch timer = null;
                if (_analyzerExecutionTimeMapOpt != null)
                {
                    timer = Stopwatch.StartNew();
                }

                analyze();

                if (timer != null)
                {
                    timer.Stop();

                    _analyzerExecutionTimeMapOpt.AddOrUpdate(analyzer, timer.Elapsed, (a, accumulatedTime) => accumulatedTime + timer.Elapsed);
                }
            }
            catch (Exception e) when (!IsCanceled(e, _cancellationToken))
            {
                // Diagnostic for analyzer exception.
                var diagnostic = CreateAnalyzerExceptionDiagnostic(analyzer, e);
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

        internal static bool IsCanceled(Exception ex, CancellationToken cancellationToken)
        {
            return (ex as OperationCanceledException)?.CancellationToken == cancellationToken;
        }

        internal static Diagnostic CreateAnalyzerExceptionDiagnostic(DiagnosticAnalyzer analyzer, Exception e)
        {
            var analyzerName = analyzer.ToString();
            var title = CodeAnalysisResources.CompilerAnalyzerFailure;
            var messageFormat = CodeAnalysisResources.CompilerAnalyzerThrows;
            var messageArguments = new[] { analyzerName, e.GetType().ToString(), e.Message };
            var description = string.Format(CodeAnalysisResources.CompilerAnalyzerThrowsDescription, analyzerName, e.CreateDiagnosticDescription());
            var descriptor = GetAnalyzerExceptionDiagnosticDescriptor(AnalyzerExceptionDiagnosticId, title, description, messageFormat);
            return Diagnostic.Create(descriptor, Location.None, messageArguments);
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

        private static Action<Diagnostic> GetAddDiagnostic(ISymbol contextSymbol, Compilation compilation, DiagnosticAnalyzer analyzer, Action<Diagnostic> addDiagnostic, Action<Diagnostic, DiagnosticAnalyzer, bool> addLocalDiagnostic, Action<Diagnostic, DiagnosticAnalyzer> addNonLocalDiagnostic, Func<ISymbol, SyntaxReference, Compilation, SyntaxNode> getTopMostNodeForAnalysis)
        {
            if (addLocalDiagnostic == null)
            {
                return addDiagnostic;
            }

            return diagnostic =>
            {
                if (diagnostic.Location.IsInSource)
                {
                    foreach (var syntaxRef in contextSymbol.DeclaringSyntaxReferences)
                    {
                        if (syntaxRef.SyntaxTree == diagnostic.Location.SourceTree)
                        {
                            var syntax = getTopMostNodeForAnalysis(contextSymbol, syntaxRef, compilation);
                            if (diagnostic.Location.SourceSpan.IntersectsWith(syntax.FullSpan))
                            {
                                addLocalDiagnostic(diagnostic, analyzer, false);
                                return;
                            }
                        }
                    }
                }

                addNonLocalDiagnostic(diagnostic, analyzer);
            };
        }

        private Action<Diagnostic> GetAddCompilationDiagnostic(DiagnosticAnalyzer analyzer)
        {
            if (_addNonLocalDiagnosticOpt == null)
            {
                return _addDiagnostic;
            }

            return diagnostic =>
            {
                _addNonLocalDiagnosticOpt(diagnostic, analyzer);
            };
        }

        private Action<Diagnostic> GetAddDiagnostic(SyntaxTree tree, DiagnosticAnalyzer analyzer, bool isSyntaxDiagnostic)
        {
            return GetAddDiagnostic(tree, null, analyzer, isSyntaxDiagnostic, _addDiagnostic, _addLocalDiagnosticOpt, _addNonLocalDiagnosticOpt);
        }

        private Action<Diagnostic> GetAddDiagnostic(SyntaxTree tree, TextSpan? span, DiagnosticAnalyzer analyzer, bool isSyntaxDiagnostic)
        {
            return GetAddDiagnostic(tree, span, analyzer, false, _addDiagnostic, _addLocalDiagnosticOpt, _addNonLocalDiagnosticOpt);
        }

        private static Action<Diagnostic> GetAddDiagnostic(SyntaxTree contextTree, TextSpan? span, DiagnosticAnalyzer analyzer, bool isSyntaxDiagnostic, Action<Diagnostic> addDiagnostic, Action<Diagnostic, DiagnosticAnalyzer, bool> addLocalDiagnostic, Action<Diagnostic, DiagnosticAnalyzer> addNonLocalDiagnostic)
        {
            if (addLocalDiagnostic == null)
            {
                return addDiagnostic;
            }

            return diagnostic =>
            {
                if (diagnostic.Location.IsInSource &&
                    contextTree == diagnostic.Location.SourceTree &&
                    (!span.HasValue || span.Value.IntersectsWith(diagnostic.Location.SourceSpan)))
                {
                    addLocalDiagnostic(diagnostic, analyzer, isSyntaxDiagnostic);
                }
                else
                {
                    addNonLocalDiagnostic(diagnostic, analyzer);
                }
            };
        }

        private static bool ShouldExecuteAction(AnalyzerStateData analyzerStateOpt, AnalyzerAction action)
        {
            return analyzerStateOpt == null || !analyzerStateOpt.ProcessedActions.Contains(action);
        }

        private static bool ShouldExecuteNode(SyntaxNodeAnalyzerStateData analyzerStateOpt, SyntaxNode node)
        {
            return analyzerStateOpt == null || !analyzerStateOpt.ProcessedNodes.Contains(node);
        }

        private static bool ShouldExecuteOperation(OperationAnalyzerStateData analyzerStateOpt, IOperation operation)
        {
            return analyzerStateOpt == null || !analyzerStateOpt.ProcessedOperations.Contains(operation);
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

        private static async Task<AnalyzerStateData> TryStartProcessingEventAsync(CompilationEvent nonSymbolCompilationEvent, DiagnosticAnalyzer analyzer, AnalysisScope analysisScope, AnalysisState analysisStateOpt, CancellationToken cancellationToken)
        {
            Debug.Assert(!(nonSymbolCompilationEvent is SymbolDeclaredCompilationEvent));
            Debug.Assert(analysisScope.Analyzers.Contains(analyzer));

            if (analysisStateOpt == null)
            {
                return null;
            }

            return await analysisStateOpt.TryStartProcessingEventAsync(nonSymbolCompilationEvent, analyzer, cancellationToken).ConfigureAwait(false);
        }

        private static async Task<AnalyzerStateData> TryStartSyntaxAnalysis(SyntaxTree tree, DiagnosticAnalyzer analyzer, AnalysisScope analysisScope, AnalysisState analysisStateOpt, CancellationToken cancellationToken)
        {
            Debug.Assert(analysisScope.Analyzers.Contains(analyzer));

            if (analysisStateOpt == null)
            {
                return null;
            }

            return await analysisStateOpt.TryStartSyntaxAnalysisAsync(tree, analyzer, cancellationToken).ConfigureAwait(false);
        }

        private static async Task<AnalyzerStateData> TryStartAnalyzingSymbolAsync(ISymbol symbol, DiagnosticAnalyzer analyzer, AnalysisScope analysisScope, AnalysisState analysisStateOpt, CancellationToken cancellationToken)
        {
            Debug.Assert(analysisScope.Analyzers.Contains(analyzer));

            if (analysisStateOpt == null)
            {
                return null;
            }

            return await analysisStateOpt.TryStartAnalyzingSymbolAsync(symbol, analyzer, cancellationToken).ConfigureAwait(false);
        }

        private static async Task<DeclarationAnalyzerStateData> TryStartAnalyzingSyntaxReferenceAsync(SyntaxReference syntaxReference, DiagnosticAnalyzer analyzer, AnalysisScope analysisScope, AnalysisState analysisStateOpt, CancellationToken cancellationToken)
        {
            Debug.Assert(analysisScope.Analyzers.Contains(analyzer));

            if (analysisStateOpt == null)
            {
                return null;
            }

            return await analysisStateOpt.TryStartAnalyzingDeclarationAsync(syntaxReference, analyzer, cancellationToken).ConfigureAwait(false);
        }

        private static async Task<OperationAnalyzerStateData> TryStartAnalyzingOperationReferenceAsync(SyntaxReference syntaxReference, DiagnosticAnalyzer analyzer, AnalysisScope analysisScope, AnalysisState analysisStateOpt, CancellationToken cancellationToken)
        {
            Debug.Assert(analysisScope.Analyzers.Contains(analyzer));

            if (analysisStateOpt == null)
            {
                return null;
            }

            var declarationAnalyzerStateOpt = await analysisStateOpt.TryStartAnalyzingDeclarationAsync(syntaxReference, analyzer, cancellationToken).ConfigureAwait(false);
            return declarationAnalyzerStateOpt?.OperationBlockAnalysisState.ExecutableNodesAnalysisState;
        }

        internal TimeSpan ResetAnalyzerExecutionTime(DiagnosticAnalyzer analyzer)
        {
            Debug.Assert(_analyzerExecutionTimeMapOpt != null);
            TimeSpan executionTime;
            if (!_analyzerExecutionTimeMapOpt.TryRemove(analyzer, out executionTime))
            {
                executionTime = default(TimeSpan);
            }

            return executionTime;
        }
    }
}
