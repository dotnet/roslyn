// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    /// <summary>
    /// Contains the core execution logic for callbacks into analyzers.
    /// </summary>
    internal class AnalyzerExecutor
    {
        private const string AnalyzerExceptionDiagnosticId = "AD0001";
        private const string DescriptorExceptionDiagnosticId = "AD0002";
        private const string DiagnosticCategory = "Compiler";

        private readonly Compilation _compilation;
        private readonly AnalyzerOptions _analyzerOptions;
        private readonly Action<Diagnostic> _addDiagnostic;
        private readonly Action<Exception, DiagnosticAnalyzer, Diagnostic> _onAnalyzerException;
        private readonly CancellationToken _cancellationToken;

        /// <summary>
        /// Creates AnalyzerActionsExecutor to execute analyzer actions with given arguments
        /// </summary>
        /// <param name="compilation">Compilation to be used in the analysis.</param>
        /// <param name="analyzerOptions">Analyzer options.</param>
        /// <param name="addDiagnostic">Delegate to add analyzer diagnostics.</param>
        /// <param name="onAnalyzerException">
        /// Optional delegate which is invoked when an analyzer throws an exception.
        /// Delegate can do custom tasks such as report the given analyzer exception diagnostic, report a non-fatal watson for the exception, etc.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static AnalyzerExecutor Create(
            Compilation compilation,
            AnalyzerOptions analyzerOptions,
            Action<Diagnostic> addDiagnostic,
            Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException,
            CancellationToken cancellationToken)
        {
            return new AnalyzerExecutor(compilation, analyzerOptions, addDiagnostic, onAnalyzerException, cancellationToken);
        }

        /// <summary>
        /// Creates AnalyzerActionsExecutor to fetch <see cref="DiagnosticAnalyzer.SupportedDiagnostics"/>.
        /// </summary>
        /// <param name="onAnalyzerException">
        /// Optional delegate which is invoked when an analyzer throws an exception.
        /// Delegate can do custom tasks such as report the given analyzer exception diagnostic, report a non-fatal watson for the exception, etc.
        /// </param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static AnalyzerExecutor CreateForSupportedDiagnostics(
            Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException,
            CancellationToken cancellationToken)
        {
            return new AnalyzerExecutor(
                compilation: null,
                analyzerOptions: null,
                addDiagnostic: null,
                onAnalyzerException: onAnalyzerException,
                cancellationToken: cancellationToken);
        }

        private AnalyzerExecutor(
            Compilation compilation,
            AnalyzerOptions analyzerOptions,
            Action<Diagnostic> addDiagnostic,
            Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException,
            CancellationToken cancellationToken)
        {
            _compilation = compilation;
            _analyzerOptions = analyzerOptions;
            _addDiagnostic = addDiagnostic;
            _onAnalyzerException = onAnalyzerException;
            _cancellationToken = cancellationToken;
        }

        internal Compilation Compilation => _compilation;
        internal CancellationToken CancellationToken => _cancellationToken;
        internal Action<Exception, DiagnosticAnalyzer, Diagnostic> OnAnalyzerException => _onAnalyzerException;

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
            // The Initialize method should be run asynchronously in case it is not well behaved, e.g. does not terminate.
            ExecuteAndCatchIfThrows(analyzer, () => analyzer.Initialize(new AnalyzerAnalysisContext(analyzer, sessionScope)));
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
                ExecuteAndCatchIfThrows(startAction.Analyzer,
                    () => startAction.Action(new AnalyzerCompilationStartAnalysisContext(startAction.Analyzer, compilationScope, _compilation, _analyzerOptions, _cancellationToken)));
            }
        }

        /// <summary>
        /// Executes the compilation end actions.
        /// </summary>
        /// <param name="actions"><see cref="AnalyzerActions"/> whose compilation end actions are to be executed.</param>
        public void ExecuteCompilationEndActions(AnalyzerActions actions)
        {
            ExecuteCompilationEndActions(actions.CompilationEndActions);
        }

        /// <summary>
        /// Executes the compilation end actions.
        /// </summary>
        /// <param name="compilationEndActions">Compilation end actions to be executed.</param>
        public void ExecuteCompilationEndActions(ImmutableArray<CompilationEndAnalyzerAction> compilationEndActions)
        {
            foreach (var endAction in compilationEndActions)
            {
                _cancellationToken.ThrowIfCancellationRequested();
                ExecuteAndCatchIfThrows(endAction.Analyzer,
                    () => endAction.Action(new CompilationEndAnalysisContext(_compilation, _analyzerOptions, _addDiagnostic, _cancellationToken)));
            }
        }

        /// <summary>
        /// Executes the symbol actions on the given symbols.
        /// </summary>
        /// <param name="actions"><see cref="AnalyzerActions"/> whose symbol actions are to be executed.</param>
        /// <param name="symbols">Symbols to be analyzed.</param>
        public void ExecuteSymbolActions(AnalyzerActions actions, IEnumerable<ISymbol> symbols)
        {
            ExecuteSymbolActions(actions.SymbolActions, symbols);
        }

        /// <summary>
        /// Executes the symbol actions on the given symbols.
        /// </summary>
        /// <param name="symbolActions">Symbol actions to be executed.</param>
        /// <param name="symbols">Symbols to be analyzed.</param>
        public void ExecuteSymbolActions(ImmutableArray<SymbolAnalyzerAction> symbolActions, IEnumerable<ISymbol> symbols)
        {
            foreach (var symbol in symbols)
            {
                ExecuteSymbolActions(symbolActions, symbol);
            }
        }

        /// <summary>
        /// Executes the symbol actions on the given symbol.
        /// </summary>
        /// <param name="symbolActions">Symbol actions to be executed.</param>
        /// <param name="symbol">Symbol to be analyzed.</param>
        /// <param name="overriddenAddDiagnostic">Optional overridden add diagnostic delegate.</param>
        public void ExecuteSymbolActions(ImmutableArray<SymbolAnalyzerAction> symbolActions, ISymbol symbol, Action<Diagnostic> overriddenAddDiagnostic = null)
        {
            var addDiagnostic = overriddenAddDiagnostic ?? _addDiagnostic;

            foreach (var symbolAction in symbolActions)
            {
                var action = symbolAction.Action;
                var kinds = symbolAction.Kinds;
                if (kinds.Contains(symbol.Kind))
                {
                    _cancellationToken.ThrowIfCancellationRequested();
                    ExecuteAndCatchIfThrows(symbolAction.Analyzer,
                        () => action(new SymbolAnalysisContext(symbol, _compilation, _analyzerOptions, addDiagnostic, _cancellationToken)));
                }
            }
        }

        /// <summary>
        /// Executes the semantic model actions on the given semantic model.
        /// </summary>
        /// <param name="actions"><see cref="AnalyzerActions"/> whose semantic model actions are to be executed.</param>
        /// <param name="semanticModel">Semantic model to analyze.</param>
        public void ExecuteSemanticModelActions(AnalyzerActions actions, SemanticModel semanticModel)
        {
            ExecuteSemanticModelActions(actions.SemanticModelActions, semanticModel);
        }

        /// <summary>
        /// Executes the semantic model actions on the given semantic model.
        /// </summary>
        /// <param name="semanticModelActions">Semantic model actions to be executed.</param>
        /// <param name="semanticModel">Semantic model to analyze.</param>
        public void ExecuteSemanticModelActions(ImmutableArray<SemanticModelAnalyzerAction> semanticModelActions, SemanticModel semanticModel)
        {
            foreach (var semanticModelAction in semanticModelActions)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                // Catch Exception from action.
                ExecuteAndCatchIfThrows(semanticModelAction.Analyzer,
                    () => semanticModelAction.Action(new SemanticModelAnalysisContext(semanticModel, _analyzerOptions, _addDiagnostic, _cancellationToken)));
            }
        }

        /// <summary>
        /// Executes the syntax tree actions on the given syntax tree.
        /// </summary>
        /// <param name="actions"><see cref="AnalyzerActions"/> whose syntax tree actions are to be executed.</param>
        /// <param name="tree">Syntax tree to analyze.</param>
        public void ExecuteSyntaxTreeActions(AnalyzerActions actions, SyntaxTree tree)
        {
            ExecuteSyntaxTreeActions(actions.SyntaxTreeActions, tree);
        }

        /// <summary>
        /// Executes the syntax tree actions on the given syntax tree.
        /// </summary>
        /// <param name="syntaxTreeActions">Syntax tree actions to be executed.</param>
        /// <param name="tree">Syntax tree to analyze.</param>
        public void ExecuteSyntaxTreeActions(ImmutableArray<SyntaxTreeAnalyzerAction> syntaxTreeActions, SyntaxTree tree)
        {
            foreach (var syntaxTreeAction in syntaxTreeActions)
            {
                _cancellationToken.ThrowIfCancellationRequested();

                // Catch Exception from action.
                ExecuteAndCatchIfThrows(syntaxTreeAction.Analyzer,
                    () => syntaxTreeAction.Action(new SyntaxTreeAnalysisContext(tree, _analyzerOptions, _addDiagnostic, _cancellationToken)));
            }
        }

        /// <summary>
        /// Executes the syntax node actions on the given syntax nodes.
        /// </summary>
        /// <param name="actions"><see cref="AnalyzerActions"/> whose code block start actions and end actions are to be executed.</param>
        /// <param name="nodes">Syntax nodes to be analyzed.</param>
        /// <param name="semanticModel">SemanticModel to be used in the analysis.</param>
        /// <param name="getKind">Delegate to compute language specific syntax kind for a syntax node.</param>
        public void ExecuteSyntaxNodeActions<TLanguageKindEnum>(
            AnalyzerActions actions,
            IEnumerable<SyntaxNode> nodes,
            SemanticModel semanticModel,
            Func<SyntaxNode, TLanguageKindEnum> getKind)
            where TLanguageKindEnum : struct
        {
            var syntaxNodeActions = actions.GetSyntaxNodeActions<TLanguageKindEnum>();

            foreach (var syntaxNodeAction in syntaxNodeActions)
            {
                foreach (var node in nodes)
                {
                    if (syntaxNodeAction.Kinds.Contains(getKind(node)))
                    {
                        ExecuteSyntaxNodeAction(syntaxNodeAction, node, semanticModel);
                    }
                }
            }
        }

        private void ExecuteSyntaxNodeAction<TLanguageKindEnum>(
            SyntaxNodeAnalyzerAction<TLanguageKindEnum> syntaxNodeAction,
            SyntaxNode node,
            SemanticModel semanticModel)
            where TLanguageKindEnum : struct
        {
            var syntaxNodeContext = new SyntaxNodeAnalysisContext(node, semanticModel, _analyzerOptions, _addDiagnostic, _cancellationToken);
            ExecuteAndCatchIfThrows(syntaxNodeAction.Analyzer, () => syntaxNodeAction.Action(syntaxNodeContext));
        }

        /// <summary>
        /// Executes the given code block actions on all the executable code blocks for each declaration info in <paramref name="declarationsInNode"/>.
        /// </summary>
        /// <param name="actions"><see cref="AnalyzerActions"/> whose code block start actions and end actions are to be executed.</param>
        /// <param name="declarationsInNode">Declarations to be analyzed.</param>
        /// <param name="semanticModel">SemanticModel to be shared amongst all actions.</param>
        /// <param name="getKind">Delegate to compute language specific syntax kind for a syntax node.</param>
        public void ExecuteCodeBlockActions<TLanguageKindEnum>(
            AnalyzerActions actions,
            IEnumerable<DeclarationInfo> declarationsInNode,
            SemanticModel semanticModel,
            Func<SyntaxNode, TLanguageKindEnum> getKind)
            where TLanguageKindEnum : struct
        {
            var codeBlockStartActions = actions.GetCodeBlockStartActions<TLanguageKindEnum>();
            var codeBlockEndActions = actions.CodeBlockEndActions;

            if (!codeBlockStartActions.Any() && !codeBlockEndActions.Any())
            {
                return;
            }

            foreach (var declInfo in declarationsInNode)
            {
                var declaredNode = declInfo.DeclaredNode;
                var declaredSymbol = declInfo.DeclaredSymbol;
                var executableCodeBlocks = declInfo.ExecutableCodeBlocks;

                if (declaredSymbol != null && declInfo.ExecutableCodeBlocks.Any())
                {
                    ExecuteCodeBlockActions(codeBlockStartActions, codeBlockEndActions,
                        declaredNode, declaredSymbol, executableCodeBlocks, semanticModel, getKind);
                }
            }
        }

        internal void ExecuteCodeBlockActions<TLanguageKindEnum>(
            IEnumerable<CodeBlockStartAnalyzerAction<TLanguageKindEnum>> codeBlockStartActions,
            IEnumerable<CodeBlockEndAnalyzerAction> codeBlockEndActions,
            SyntaxNode declaredNode,
            ISymbol declaredSymbol,
            ImmutableArray<SyntaxNode> executableCodeBlocks,
            SemanticModel semanticModel,
            Func<SyntaxNode, TLanguageKindEnum> getKind)
            where TLanguageKindEnum : struct
        {
            Debug.Assert(declaredNode != null);
            Debug.Assert(declaredSymbol != null);
            Debug.Assert(CanHaveExecutableCodeBlock(declaredSymbol));
            Debug.Assert(codeBlockStartActions.Any() || codeBlockEndActions.Any());
            Debug.Assert(executableCodeBlocks.Any());

            // Compute the sets of code block end and stateful syntax node actions.
            var endedActions = PooledHashSet<CodeBlockEndAnalyzerAction>.GetInstance();
            var executableNodeActions = ArrayBuilder<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>.GetInstance();

            // Include the stateless code block actions.
            endedActions.AddAll(codeBlockEndActions);

            // Include the stateful actions.
            foreach (var da in codeBlockStartActions)
            {
                // Catch Exception from the start action.
                ExecuteAndCatchIfThrows(da.Analyzer, () =>
                {
                    var codeBlockScope = new HostCodeBlockStartAnalysisScope<TLanguageKindEnum>();
                    var blockStartContext = new AnalyzerCodeBlockStartAnalysisContext<TLanguageKindEnum>(da.Analyzer,
                        codeBlockScope, declaredNode, declaredSymbol, semanticModel, _analyzerOptions, _cancellationToken);
                    da.Action(blockStartContext);
                    endedActions.AddAll(codeBlockScope.CodeBlockEndActions);
                    executableNodeActions.AddRange(codeBlockScope.SyntaxNodeActions);
                });
            }

            // Execute stateful executable node analyzers, if any.
            if (executableNodeActions.Any())
            {
                var executableNodeActionsByKind = GetNodeActionsByKind(executableNodeActions);

                var nodesToAnalyze = executableCodeBlocks.SelectMany(cb => cb.DescendantNodesAndSelf());
                ExecuteSyntaxNodeActions(nodesToAnalyze, executableNodeActionsByKind, semanticModel, getKind);
            }

            // Execute code block end actions.
            foreach (var endedAction in endedActions)
            {
                ExecuteAndCatchIfThrows(endedAction.Analyzer,
                    () => endedAction.Action(new CodeBlockEndAnalysisContext(declaredNode, declaredSymbol, semanticModel, _analyzerOptions, _addDiagnostic, _cancellationToken)));
            }

            endedActions.Free();
            executableNodeActions.Free();
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

        internal void ExecuteSyntaxNodeActions<TLanguageKindEnum>(
            IEnumerable<SyntaxNode> nodesToAnalyze,
            IDictionary<TLanguageKindEnum, ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>> nodeActionsByKind,
            SemanticModel model,
            Func<SyntaxNode, TLanguageKindEnum> getKind)
            where TLanguageKindEnum : struct
        {
            Debug.Assert(nodeActionsByKind != null);
            Debug.Assert(nodeActionsByKind.Any());

            foreach (var child in nodesToAnalyze)
            {
                ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>> actionsForKind;
                if (nodeActionsByKind.TryGetValue(getKind(child), out actionsForKind))
                {
                    foreach (var action in actionsForKind)
                    {
                        ExecuteSyntaxNodeAction(action, child, model);
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
                    return true;

                case SymbolKind.Field:
                    Debug.Assert(((IFieldSymbol)symbol).AssociatedSymbol == null);
                    return true;

                default:
                    return false;
            }
        }

        internal void ExecuteAndCatchIfThrows(DiagnosticAnalyzer analyzer, Action analyze)
        {
            ExecuteAndCatchIfThrows(analyzer, analyze, _onAnalyzerException, _cancellationToken);
        }

        private static void ExecuteAndCatchIfThrows(
            DiagnosticAnalyzer analyzer,
            Action analyze,
            Action<Exception, DiagnosticAnalyzer, Diagnostic> onAnalyzerException,
            CancellationToken cancellationToken)
        {
            try
            {
                analyze();
            }
            catch (OperationCanceledException oce)
            {
                if (oce.CancellationToken != cancellationToken)
                {
                    // Diagnostic for analyzer exception.
                    var diagnostic = GetAnalyzerDiagnostic(analyzer, oce);
                    onAnalyzerException(oce, analyzer, diagnostic);
                }
            }
            catch (Exception e)
            {
                // Diagnostic for analyzer exception.
                var diagnostic = GetAnalyzerDiagnostic(analyzer, e);
                onAnalyzerException(e, analyzer, diagnostic);
            }
        }

        internal static Diagnostic GetAnalyzerDiagnostic(DiagnosticAnalyzer analyzer, Exception e)
        {
            var descriptor = new DiagnosticDescriptor(AnalyzerExceptionDiagnosticId,
                AnalyzerDriverResources.AnalyzerFailure,
                AnalyzerDriverResources.AnalyzerThrows,
                category: DiagnosticCategory,
                defaultSeverity: DiagnosticSeverity.Info,
                isEnabledByDefault: true,
                customTags: WellKnownDiagnosticTags.AnalyzerException);
            return Diagnostic.Create(descriptor, Location.None, analyzer.GetType().ToString(), e.Message);
        }

        internal static Diagnostic GetDescriptorDiagnostic(string faultedDescriptorId, Exception e)
        {
            var descriptor = new DiagnosticDescriptor(DescriptorExceptionDiagnosticId,
                AnalyzerDriverResources.AnalyzerFailure,
                AnalyzerDriverResources.DiagnosticDescriptorThrows,
                category: DiagnosticCategory,
                defaultSeverity: DiagnosticSeverity.Info,
                isEnabledByDefault: true,
                customTags: WellKnownDiagnosticTags.AnalyzerException);
            return Diagnostic.Create(descriptor, Location.None, faultedDescriptorId, e.Message);
        }

        internal static bool IsAnalyzerExceptionDiagnostic(Diagnostic diagnostic)
        {
            if (diagnostic.Id == AnalyzerExceptionDiagnosticId || diagnostic.Id == DescriptorExceptionDiagnosticId)
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
    }
}
