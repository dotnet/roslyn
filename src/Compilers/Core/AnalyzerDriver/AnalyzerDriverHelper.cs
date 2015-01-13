// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        internal const string DiagnosticId = "AD0001";
        private const string DiagnosticCategory = "Compiler";

        /// <summary>
        /// Executes the <see cref="DiagnosticAnalyzer.Initialize(AnalysisContext)"/> for the given analyzer and returns the set of registered sessions.
        /// </summary>
        /// <param name="analyzer">Analyzer to get session wide analyzer actions.</param>
        /// <param name="addDiagnostic">Delegate to add diagnostics.</param>
        /// <param name="continueOnAnalyzerException">Predicate to decide if exceptions from the action should be handled or not.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <remarks>
        /// Note that this API doesn't execute any <see cref="CompilationStartAnalyzerAction"/> registered by the Initialize invocation.
        /// Use <see cref="ExecuteCompilationStartActions(AnalyzerActions, DiagnosticAnalyzer, Compilation, AnalyzerOptions, Action{Diagnostic}, Func{Exception, DiagnosticAnalyzer, bool}, CancellationToken)"/> API
        /// to get execute these actions to get the per-compilation analyzer actions.
        /// </remarks>
        public static AnalyzerActions GetSessionAnalyzerActions(
            DiagnosticAnalyzer analyzer,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException,
            CancellationToken cancellationToken)
        {
            VerifyArguments(analyzer, addDiagnostic, continueOnAnalyzerException);

            HostSessionStartAnalysisScope sessionScope = new HostSessionStartAnalysisScope();

            ExecuteAndCatchIfThrows(analyzer, addDiagnostic, continueOnAnalyzerException, () =>
            {
                // The Initialize method should be run asynchronously in case it is not well behaved, e.g. does not terminate.
                analyzer.Initialize(new AnalyzerAnalysisContext(analyzer, sessionScope));
            }, cancellationToken);

            return sessionScope.GetAnalyzerActions(analyzer);
        }

        /// <summary>
        /// Executes the compilation start actions and returns the per-compilation analyzer actions added by these actions.
        /// </summary>
        /// <param name="actions"><see cref="AnalyzerActions"/> whose compilation start actions are to be executed.</param>
        /// <param name="analyzer">Analyzer on which compilation start actions have to be executed.</param>
        /// <param name="compilation">Compilation to be used in the analysis.</param>
        /// <param name="analyzerOptions">Analyzer options.</param>
        /// <param name="addDiagnostic">Delegate to add diagnostics.</param>
        /// <param name="continueOnAnalyzerException">Predicate to decide if exceptions from the action should be handled or not.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        public static AnalyzerActions ExecuteCompilationStartActions(
            AnalyzerActions actions,
            DiagnosticAnalyzer analyzer,
            Compilation compilation,
            AnalyzerOptions analyzerOptions,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException,
            CancellationToken cancellationToken)
        {
            VerifyArguments(compilation, actions, analyzerOptions, analyzer, addDiagnostic, continueOnAnalyzerException);

            HostCompilationStartAnalysisScope compilationScope = new HostCompilationStartAnalysisScope(new HostSessionStartAnalysisScope());

            foreach (var startAction in actions.CompilationStartActions)
            {
                if (startAction.Analyzer == analyzer)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    ExecuteAndCatchIfThrows(analyzer, addDiagnostic, continueOnAnalyzerException, () =>
                    {
                        startAction.Action(new AnalyzerCompilationStartAnalysisContext(analyzer, compilationScope, compilation, analyzerOptions, cancellationToken));
                    }, cancellationToken);
                }
            }

            return compilationScope.GetAnalyzerActions(analyzer);
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
            VerifyArguments(compilation, actions, analyzerOptions, addDiagnostic, continueOnAnalyzerException);

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
            VerifyArguments(symbols, compilation, actions, analyzerOptions, addDiagnostic, continueOnAnalyzerException);

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
            VerifyArguments(semanticModel, actions, analyzerOptions, addDiagnostic, continueOnAnalyzerException);

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
            VerifyArguments(syntaxTree, actions, analyzerOptions, addDiagnostic, continueOnAnalyzerException);

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
            VerifyArguments(nodes, getKind, semanticModel, actions, analyzerOptions, addDiagnostic, continueOnAnalyzerException);

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
            VerifyArguments(declarationsInNode, getKind, semanticModel, actions, analyzerOptions, addDiagnostic, continueOnAnalyzerException);

            var codeBlockStartActions = actions.GetCodeBlockStartActions<TLanguageKindEnum>();
            var codeBlockEndActions = actions.GetCodeBlockEndActions<TLanguageKindEnum>();

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
            IEnumerable<CodeBlockEndAnalyzerAction<TLanguageKindEnum>> codeBlockEndActions,
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
            var endedActions = PooledHashSet<CodeBlockEndAnalyzerAction<TLanguageKindEnum>>.GetInstance();
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
                var executableNodeActionsByKind = PooledDictionary<TLanguageKindEnum, ArrayBuilder<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>>.GetInstance();
                GetNodeActionsByKind(executableNodeActions, executableNodeActionsByKind, addDiagnostic);

                var nodesToAnalyze = executableCodeBlocks.SelectMany(cb => cb.DescendantNodesAndSelf());
                ExecuteSyntaxNodeActions(nodesToAnalyze, executableNodeActionsByKind, semanticModel,
                    analyzerOptions, addDiagnostic, continueOnAnalyzerException, getKind, cancellationToken);

                foreach (var b in executableNodeActionsByKind.Values)
                {
                    b.Free();
                }

                executableNodeActionsByKind.Free();
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

        internal static void GetNodeActionsByKind<TLanguageKindEnum>(
            IEnumerable<SyntaxNodeAnalyzerAction<TLanguageKindEnum>> nodeActions,
            PooledDictionary<TLanguageKindEnum, ArrayBuilder<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>> nodeActionsByKind,
            Action<Diagnostic> addDiagnostic)
            where TLanguageKindEnum : struct
        {
            Debug.Assert(nodeActions != null && nodeActions.Any());
            Debug.Assert(nodeActionsByKind != null && !nodeActionsByKind.Any());

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
        }

        internal static void ExecuteSyntaxNodeActions<TLanguageKindEnum>(
            IEnumerable<SyntaxNode> nodesToAnalyze,
            IDictionary<TLanguageKindEnum, ArrayBuilder<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>> nodeActionsByKind,
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
                ArrayBuilder<SyntaxNodeAnalyzerAction<TLanguageKindEnum>> actionsForKind;
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

        private static void VerifyArguments(
            IEnumerable<ISymbol> symbols,
            Compilation compilation,
            AnalyzerActions actions,
            AnalyzerOptions analyzerOptions,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException)
        {
            if (symbols == null)
            {
                throw new ArgumentNullException(nameof(symbols));
            }

            if (symbols.Any(s => s == null))
            {
                throw new ArgumentException(AnalyzerDriverResources.ArgumentElementCannotBeNull, nameof(symbols));
            }

            VerifyArguments(compilation, actions, analyzerOptions, addDiagnostic, continueOnAnalyzerException);
        }

        private static void VerifyArguments(
            Compilation compilation,
            AnalyzerActions actions,
            AnalyzerOptions analyzerOptions,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException)
        {
            if (compilation == null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }

            VerifyArguments(actions, analyzerOptions, addDiagnostic, continueOnAnalyzerException);
        }

        internal protected static void VerifyArguments(
            SemanticModel semanticModel,
            AnalyzerActions actions,
            AnalyzerOptions analyzerOptions,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException)
        {
            if (semanticModel == null)
            {
                throw new ArgumentNullException(nameof(semanticModel));
            }

            VerifyArguments(actions, analyzerOptions, addDiagnostic, continueOnAnalyzerException);
        }

        private static void VerifyArguments(
            SyntaxTree syntaxTree,
            AnalyzerActions actions,
            AnalyzerOptions analyzerOptions,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException)
        {
            if (syntaxTree == null)
            {
                throw new ArgumentNullException(nameof(syntaxTree));
            }

            VerifyArguments(actions, analyzerOptions, addDiagnostic, continueOnAnalyzerException);
        }

        private static void VerifyArguments(
            Compilation compilation,
            AnalyzerActions actions,
            AnalyzerOptions analyzerOptions,
            DiagnosticAnalyzer analyzer,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException)
        {
            if (compilation == null)
            {
                throw new ArgumentNullException(nameof(compilation));
            }

            if (analyzer == null)
            {
                throw new ArgumentNullException(nameof(analyzer));
            }

            VerifyArguments(actions, analyzerOptions, addDiagnostic, continueOnAnalyzerException);
        }

        internal protected static void VerifyArguments(
            AnalyzerActions actions,
            AnalyzerOptions analyzerOptions,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException)
        {
            if (actions == null)
            {
                throw new ArgumentNullException(nameof(actions));
            }

            VerifyArguments(analyzerOptions, addDiagnostic, continueOnAnalyzerException);
        }

        internal protected static void VerifyArguments(
            AnalyzerOptions analyzerOptions,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException)
        {
            if (analyzerOptions == null)
            {
                throw new ArgumentNullException(nameof(analyzerOptions));
            }

            if (addDiagnostic == null)
            {
                throw new ArgumentNullException(nameof(addDiagnostic));
            }

            if (continueOnAnalyzerException == null)
            {
                throw new ArgumentNullException(nameof(continueOnAnalyzerException));
            }
        }

        internal protected static void VerifyArguments(
            DiagnosticAnalyzer analyzer,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException)
        {
            if (analyzer == null)
            {
                throw new ArgumentNullException(nameof(analyzer));
            }

            if (addDiagnostic == null)
            {
                throw new ArgumentNullException(nameof(addDiagnostic));
            }

            if (continueOnAnalyzerException == null)
            {
                throw new ArgumentNullException(nameof(continueOnAnalyzerException));
            }
        }


        private static void VerifyArguments<TLanguageKindEnum>(
            IEnumerable<DeclarationInfo> declarationsInNode,
            Func<SyntaxNode, TLanguageKindEnum> getKind,
            SemanticModel semanticModel,
            AnalyzerActions actions,
            AnalyzerOptions analyzerOptions,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException)
        {
            if (declarationsInNode == null)
            {
                throw new ArgumentNullException(nameof(declarationsInNode));
            }

            VerifyArguments(getKind, semanticModel, actions, analyzerOptions, addDiagnostic, continueOnAnalyzerException);
        }

        private static void VerifyArguments<TLanguageKindEnum>(
            IEnumerable<SyntaxNode> nodes,
            Func<SyntaxNode, TLanguageKindEnum> getKind,
            SemanticModel semanticModel,
            AnalyzerActions actions,
            AnalyzerOptions analyzerOptions,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException)
        {
            if (nodes == null)
            {
                throw new ArgumentNullException(nameof(nodes));
            }

            if (nodes.Any(n => n == null))
            {
                throw new ArgumentException(AnalyzerDriverResources.ArgumentElementCannotBeNull, nameof(nodes));
            }

            VerifyArguments(getKind, semanticModel, actions, analyzerOptions, addDiagnostic, continueOnAnalyzerException);
        }

        private static void VerifyArguments<TLanguageKindEnum>(
            Func<SyntaxNode, TLanguageKindEnum> getKind,
            SemanticModel semanticModel,
            AnalyzerActions actions,
            AnalyzerOptions analyzerOptions,
            Action<Diagnostic> addDiagnostic,
            Func<Exception, DiagnosticAnalyzer, bool> continueOnAnalyzerException)
        {
            if (getKind == null)
            {
                throw new ArgumentNullException(nameof(getKind));
            }

            VerifyArguments(semanticModel, actions, analyzerOptions, addDiagnostic, continueOnAnalyzerException);
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
    }
}
