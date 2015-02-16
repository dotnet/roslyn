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
    internal class AnalyzerDriverHelper
    {
        private const string DiagnosticId = "AD0001";
        private const string DiagnosticCategory = "Compiler";

        /// <summary>
        /// Executes the <see cref="DiagnosticAnalyzer.Initialize(AnalysisContext)"/> for the given analyzer.
        /// </summary>
        /// <param name="analyzer">Analyzer to get session wide analyzer actions.</param>
        /// <param name="sessionScope">Session scope to store register session wide analyzer actions.</param>
        /// <param name="addDiagnostic">Delegate to add diagnostics.</param>
        /// <param name="continueOnAnalyzerException">Predicate to decide if exceptions from the action should be handled or not.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <remarks>
        /// Note that this API doesn't execute any <see cref="CompilationStartAnalyzerAction"/> registered by the Initialize invocation.
        /// Use <see cref="ExecuteCompilationStartActions(ImmutableArray{CompilationStartAnalyzerAction}, HostCompilationStartAnalysisScope, Compilation, AnalyzerOptions, Action{Diagnostic}, Func{Exception, DiagnosticAnalyzer, bool}, CancellationToken)"/> API
        /// to get execute these actions to get the per-compilation analyzer actions.
        /// </remarks>
        public static void ExecuteInitializeMethod(
            DiagnosticAnalyzer analyzer,
            HostSessionStartAnalysisScope sessionScope,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException,
            CancellationToken cancellationToken)
        {
            ExecuteAndCatchIfThrows(analyzer, addDiagnostic, continueOnAnalyzerException, () =>
                {
                    // The Initialize method should be run asynchronously in case it is not well behaved, e.g. does not terminate.
                    analyzer.Initialize(new AnalyzerAnalysisContext(analyzer, sessionScope));
                }, cancellationToken);
        }

        /// <summary>
        /// Executes the compilation start actions.
        /// </summary>
        /// <param name="actions"><see cref="AnalyzerActions"/> whose compilation start actions are to be executed.</param>
        /// <param name="compilationScope">Compilation scope to store the analyzer actions.</param>
        /// <param name="compilation">Compilation to be used in the analysis.</param>
        /// <param name="analyzerOptions">Analyzer options.</param>
        /// <param name="addDiagnostic">Delegate to add diagnostics.</param>
        /// <param name="continueOnAnalyzerException">Predicate to decide if exceptions from the action should be handled or not.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static void ExecuteCompilationStartActions(
            ImmutableArray<CompilationStartAnalyzerAction> actions,
            HostCompilationStartAnalysisScope compilationScope,
            Compilation compilation,
            AnalyzerOptions analyzerOptions,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException,
            CancellationToken cancellationToken)
        {
            foreach (var startAction in actions)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ExecuteAndCatchIfThrows(startAction.Analyzer, addDiagnostic, continueOnAnalyzerException, () =>
                {
                    startAction.Action(new AnalyzerCompilationStartAnalysisContext(startAction.Analyzer, compilationScope, compilation, analyzerOptions, cancellationToken));
                }, cancellationToken);
            }
        }

        /// <summary>
        /// Executes the compilation end actions.
        /// </summary>
        /// <param name="actions"><see cref="AnalyzerActions"/> whose compilation end actions are to be executed.</param>
        /// <param name="compilation">Compilation to be used in the analysis.</param>
        /// <param name="analyzerOptions">Analyzer options.</param>
        /// <param name="addDiagnostic">Delegate to add diagnostics.</param>
        /// <param name="continueOnAnalyzerException">Predicate to decide if exceptions from the action should be handled or not.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static void ExecuteCompilationEndActions(
            AnalyzerActions actions,
            Compilation compilation,
            AnalyzerOptions analyzerOptions,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException,
            CancellationToken cancellationToken)
        {
            HostCompilationStartAnalysisScope compilationScope = new HostCompilationStartAnalysisScope(new HostSessionStartAnalysisScope());

            foreach (var endAction in actions.CompilationEndActions)
            {
                cancellationToken.ThrowIfCancellationRequested();
                ExecuteAndCatchIfThrows(endAction.Analyzer, addDiagnostic, continueOnAnalyzerException, () =>
                {
                    var context = new CompilationEndAnalysisContext(compilation, analyzerOptions, addDiagnostic, cancellationToken);
                    endAction.Action(context);
                }, cancellationToken);
            }
        }

        /// <summary>
        /// Executes the symbol actions on the given symbols.
        /// </summary>
        /// <param name="actions"><see cref="AnalyzerActions"/> whose symbol actions are to be executed.</param>
        /// <param name="symbols">Symbols to be analyzed.</param>
        /// <param name="compilation">Compilation to be used in the analysis.</param>
        /// <param name="analyzerOptions">Analyzer options.</param>
        /// <param name="addDiagnostic">Delegate to add diagnostics.</param>
        /// <param name="continueOnAnalyzerException">Predicate to decide if exceptions from the action should be handled or not.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static void ExecuteSymbolActions(
            AnalyzerActions actions,
            IEnumerable<ISymbol> symbols,
            Compilation compilation,
            AnalyzerOptions analyzerOptions,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException,
            CancellationToken cancellationToken)
        {
            foreach (var symbol in symbols)
            {
                foreach (var symbolAction in actions.SymbolActions)
                {
                    var action = symbolAction.Action;
                    var kinds = symbolAction.Kinds;
                    if (kinds.Contains(symbol.Kind))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        var symbolContext = new SymbolAnalysisContext(symbol, compilation, analyzerOptions, addDiagnostic, cancellationToken);

                        // Catch Exception from action.
                        ExecuteAndCatchIfThrows(symbolAction.Analyzer, addDiagnostic, continueOnAnalyzerException, () => action(symbolContext), cancellationToken);
                    }
                }
            }
        }

        /// <summary>
        /// Executes the semantic model actions on the given semantic model.
        /// </summary>
        /// <param name="actions"><see cref="AnalyzerActions"/> whose semantic model actions are to be executed.</param>
        /// <param name="semanticModel">Semantic model to analyze.</param>
        /// <param name="analyzerOptions">Analyzer options.</param>
        /// <param name="addDiagnostic">Delegate to add diagnostics.</param>
        /// <param name="continueOnAnalyzerException">Predicate to decide if exceptions from the action should be handled or not.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static void ExecuteSemanticModelActions(
            AnalyzerActions actions,
            SemanticModel semanticModel,
            AnalyzerOptions analyzerOptions,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException,
            CancellationToken cancellationToken)
        {
            foreach (var semanticModelAction in actions.SemanticModelActions)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Catch Exception from action.
                ExecuteAndCatchIfThrows(semanticModelAction.Analyzer, addDiagnostic, continueOnAnalyzerException, () =>
                {
                    var context = new SemanticModelAnalysisContext(semanticModel, analyzerOptions, addDiagnostic, cancellationToken);
                    semanticModelAction.Action(context);
                }, cancellationToken);
            }
        }

        /// <summary>
        /// Executes the syntax tree actions on the given tree.
        /// </summary>
        /// <param name="actions"><see cref="AnalyzerActions"/> whose syntax tree actions are to be executed.</param>
        /// <param name="syntaxTree">Syntax tree to analyze.</param>
        /// <param name="analyzerOptions">Analyzer options.</param>
        /// <param name="addDiagnostic">Delegate to add diagnostics.</param>
        /// <param name="continueOnAnalyzerException">Predicate to decide if exceptions from the action should be handled or not.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static void ExecuteSyntaxTreeActions(
            AnalyzerActions actions,
            SyntaxTree syntaxTree,
            AnalyzerOptions analyzerOptions,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException,
            CancellationToken cancellationToken)
        {
            foreach (var syntaxTreeAction in actions.SyntaxTreeActions)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Catch Exception from action.
                ExecuteAndCatchIfThrows(syntaxTreeAction.Analyzer, addDiagnostic, continueOnAnalyzerException, () =>
                {
                    var context = new SyntaxTreeAnalysisContext(syntaxTree, analyzerOptions, addDiagnostic, cancellationToken);
                    syntaxTreeAction.Action(context);
                }, cancellationToken);
            }
        }

        /// <summary>
        /// Executes the syntax node actions on the given syntax nodes.
        /// </summary>
        /// <param name="actions"><see cref="AnalyzerActions"/> whose code block start actions and end actions are to be executed.</param>
        /// <param name="nodes">Syntax nodes to be analyzed.</param>
        /// <param name="semanticModel">SemanticModel to be used in the analysis.</param>
        /// <param name="analyzerOptions">Analyzer options.</param>
        /// <param name="addDiagnostic">Delegate to add diagnostics.</param>
        /// <param name="continueOnAnalyzerException">Predicate to decide if exceptions from the action should be handled or not.</param>
        /// <param name="getKind">Delegate to compute language specific syntax kind for a syntax node.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static void ExecuteSyntaxNodeActions<TLanguageKindEnum>(
            AnalyzerActions actions,
            IEnumerable<SyntaxNode> nodes,
            SemanticModel semanticModel,
            AnalyzerOptions analyzerOptions,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException,
            Func<SyntaxNode, TLanguageKindEnum> getKind,
            CancellationToken cancellationToken)
            where TLanguageKindEnum : struct
        {
            var syntaxNodeActions = actions.GetSyntaxNodeActions<TLanguageKindEnum>();

            foreach (var syntaxNodeAction in syntaxNodeActions)
            {
                var action = syntaxNodeAction.Action;
                var kinds = syntaxNodeAction.Kinds;

                foreach (var node in nodes)
                {
                    if (kinds.Contains(getKind(node)))
                    {
                        ExecuteSyntaxNodeAction(action, node, syntaxNodeAction.Analyzer, semanticModel, analyzerOptions, addDiagnostic, continueOnAnalyzerException, cancellationToken);
                    }
                }
            }
        }

        internal static void ExecuteSyntaxNodeAction(
            Action<SyntaxNodeAnalysisContext> syntaxNodeAction,
            SyntaxNode node,
            DiagnosticAnalyzer analyzer,
            SemanticModel semanticModel,
            AnalyzerOptions analyzerOptions,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException,
            CancellationToken cancellationToken)
        {
            var syntaxNodeContext = new SyntaxNodeAnalysisContext(node, semanticModel, analyzerOptions, addDiagnostic, cancellationToken);

            // Catch Exception from action.
            ExecuteAndCatchIfThrows(analyzer, addDiagnostic, continueOnAnalyzerException, () => syntaxNodeAction(syntaxNodeContext), cancellationToken);
        }

        /// <summary>
        /// Executes the given code block actions on all the executable code blocks for each declaration info in <paramref name="declarationsInNode"/>.
        /// </summary>
        /// <param name="actions"><see cref="AnalyzerActions"/> whose code block start actions and end actions are to be executed.</param>
        /// <param name="declarationsInNode">Declarations to be analyzed.</param>
        /// <param name="semanticModel">SemanticModel to be shared amongst all actions.</param>
        /// <param name="analyzerOptions">Analyzer options.</param>
        /// <param name="addDiagnostic">Delegate to add diagnostics.</param>
        /// <param name="continueOnAnalyzerException">Predicate to decide if exceptions from any action should be handled or not.</param>
        /// <param name="getKind">Delegate to compute language specific syntax kind for a syntax node.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static void ExecuteCodeBlockActions<TLanguageKindEnum>(
            AnalyzerActions actions,
            IEnumerable<DeclarationInfo> declarationsInNode,
            SemanticModel semanticModel,
            AnalyzerOptions analyzerOptions,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException,
            Func<SyntaxNode, TLanguageKindEnum> getKind,
            CancellationToken cancellationToken)
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
                    ExecuteCodeBlockActions(codeBlockStartActions, codeBlockEndActions, declaredNode, declaredSymbol,
                        executableCodeBlocks, analyzerOptions, semanticModel, addDiagnostic, continueOnAnalyzerException, getKind, cancellationToken);
                }
            }
        }

        internal static void ExecuteCodeBlockActions<TLanguageKindEnum>(
            IEnumerable<CodeBlockStartAnalyzerAction<TLanguageKindEnum>> codeBlockStartActions,
            IEnumerable<CodeBlockEndAnalyzerAction> codeBlockEndActions,
            SyntaxNode declaredNode,
            ISymbol declaredSymbol,
            ImmutableArray<SyntaxNode> executableCodeBlocks,
            AnalyzerOptions analyzerOptions,
            SemanticModel semanticModel,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException,
            Func<SyntaxNode, TLanguageKindEnum> getKind,
            CancellationToken cancellationToken)
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
                ExecuteAndCatchIfThrows(da.Analyzer, addDiagnostic, continueOnAnalyzerException, () =>
                {
                    HostCodeBlockStartAnalysisScope<TLanguageKindEnum> codeBlockScope = new HostCodeBlockStartAnalysisScope<TLanguageKindEnum>();
                    CodeBlockStartAnalysisContext<TLanguageKindEnum> blockStartContext = new AnalyzerCodeBlockStartAnalysisContext<TLanguageKindEnum>(da.Analyzer, codeBlockScope, declaredNode, declaredSymbol, semanticModel, analyzerOptions, cancellationToken);
                    da.Action(blockStartContext);
                    endedActions.AddAll(codeBlockScope.CodeBlockEndActions);
                    executableNodeActions.AddRange(codeBlockScope.SyntaxNodeActions);
                }, cancellationToken);
            }

            // Execute stateful executable node analyzers, if any.
            if (executableNodeActions.Any())
            {
                var executableNodeActionsByKind = GetNodeActionsByKind(executableNodeActions, addDiagnostic);

                var nodesToAnalyze = executableCodeBlocks.SelectMany(cb => cb.DescendantNodesAndSelf());
                ExecuteSyntaxNodeActions(nodesToAnalyze, executableNodeActionsByKind, semanticModel,
                    analyzerOptions, addDiagnostic, continueOnAnalyzerException, getKind, cancellationToken);
            }

            // Execute code block end actions.
            foreach (var a in endedActions)
            {
                // Catch Exception from a.OnCodeBlockEnded
                ExecuteAndCatchIfThrows(a.Analyzer, addDiagnostic, continueOnAnalyzerException, () => a.Action(new CodeBlockEndAnalysisContext(declaredNode, declaredSymbol, semanticModel, analyzerOptions, addDiagnostic, cancellationToken)), cancellationToken);
            }

            endedActions.Free();
            executableNodeActions.Free();
        }

        internal static ImmutableDictionary<TLanguageKindEnum, ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>> GetNodeActionsByKind<TLanguageKindEnum>(
            IEnumerable<SyntaxNodeAnalyzerAction<TLanguageKindEnum>> nodeActions,
            Action<Diagnostic> addDiagnostic)
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

        internal static void ExecuteSyntaxNodeActions<TLanguageKindEnum>(
            IEnumerable<SyntaxNode> nodesToAnalyze,
            IDictionary<TLanguageKindEnum, ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>> nodeActionsByKind,
            SemanticModel model,
            AnalyzerOptions analyzerOptions,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnException,
            Func<SyntaxNode, TLanguageKindEnum> getKind,
            CancellationToken cancellationToken)
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
                        ExecuteSyntaxNodeAction(action.Action, child, action.Analyzer, model, analyzerOptions, addDiagnostic, continueOnException, cancellationToken);
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

        internal static void ExecuteAndCatchIfThrows(DiagnosticAnalyzer analyzer, Action<Diagnostic> addDiagnostic, Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException, Action analyze, CancellationToken cancellationToken)
        {
            try
            {
                analyze();
            }
            catch (OperationCanceledException oce) when (continueOnAnalyzerException(oce, analyzer))
            {
                if (oce.CancellationToken != cancellationToken)
                {
                    // Create a info diagnostic saying that the analyzer failed
                    addDiagnostic(GetAnalyzerDiagnostic(analyzer, oce));
                }
            }
            catch (Exception e) when (continueOnAnalyzerException(e, analyzer))
            {
                // Create a info diagnostic saying that the analyzer failed
                addDiagnostic(GetAnalyzerDiagnostic(analyzer, e));
            }
        }

        internal static Diagnostic GetAnalyzerDiagnostic(DiagnosticAnalyzer analyzer, Exception e)
        {
            return Diagnostic.Create(GetDiagnosticDescriptor(analyzer.GetType().ToString(), e.Message), Location.None);
        }

        internal static DiagnosticDescriptor GetDiagnosticDescriptor(string analyzerName, string message)
        {
            return new DiagnosticDescriptor(DiagnosticId,
                AnalyzerDriverResources.AnalyzerFailure,
                string.Format(AnalyzerDriverResources.AnalyzerThrows, analyzerName, message),
                category: DiagnosticCategory,
                defaultSeverity: DiagnosticSeverity.Info,
                isEnabledByDefault: true,
                customTags: WellKnownDiagnosticTags.AnalyzerException);
        }

        internal static bool IsAnalyzerExceptionDiagnostic(string diagnosticId, IEnumerable<string> customTags)
        {
            if (diagnosticId == DiagnosticId)
            {
                foreach (var tag in customTags)
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
