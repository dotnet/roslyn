// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal partial class AnalyzerDriver<TLanguageKindEnum> : AnalyzerDriver where TLanguageKindEnum : struct
    {
        /// <summary>
        /// <see cref="AnalyzerActions"/> grouped by <see cref="DiagnosticAnalyzer"/>, and possibly other entities, such as <see cref="OperationKind"/>, <see cref="SymbolKind"/>, etc.
        /// </summary>
        private sealed class GroupedAnalyzerActions
        {
            private static readonly GroupedAnalyzerActions s_Default = new GroupedAnalyzerActions(new AnalyzerActions());

            private readonly AnalyzerActions _analyzerActions;

            private ImmutableDictionary<DiagnosticAnalyzer, ImmutableDictionary<TLanguageKindEnum, ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>>> _lazyNodeActionsByKind;
            private ImmutableDictionary<DiagnosticAnalyzer, ImmutableDictionary<OperationKind, ImmutableArray<OperationAnalyzerAction>>> _lazyOperationActionsByKind;
            private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<CodeBlockStartAnalyzerAction<TLanguageKindEnum>>> _lazyCodeBlockStartActionsByAnalyzer;
            // Code block actions and code block end actions are kept separate so that it is easy to
            // execute the code block actions before the code block end actions.
            private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<CodeBlockAnalyzerAction>> _lazyCodeBlockEndActionsByAnalyzer;
            private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<CodeBlockAnalyzerAction>> _lazyCodeBlockActionsByAnalyzer;
            private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<OperationBlockStartAnalyzerAction>> _lazyOperationBlockStartActionsByAnalyzer;
            private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<OperationBlockAnalyzerAction>> _lazyOperationBlockActionsByAnalyzer;
            private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<OperationBlockAnalyzerAction>> _lazyOperationBlockEndActionsByAnalyzer;

            private GroupedAnalyzerActions(AnalyzerActions analyzerActions)
            {
                _analyzerActions = analyzerActions;
            }

            public static GroupedAnalyzerActions Create(AnalyzerActions analyzerActionsOpt)
            {
                if (analyzerActionsOpt == null || analyzerActionsOpt.IsEmpty)
                {
                    return s_Default;
                }

                return new GroupedAnalyzerActions(analyzerActionsOpt);
            }

            public ImmutableDictionary<DiagnosticAnalyzer, ImmutableDictionary<TLanguageKindEnum, ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>>> NodeActionsByAnalyzerAndKind
            {
                get
                {
                    if (_lazyNodeActionsByKind == null)
                    {
                        var analyzerActionsByKind = CreateNodeActionsByKind(this._analyzerActions);
                        Interlocked.CompareExchange(ref _lazyNodeActionsByKind, analyzerActionsByKind, null);
                    }

                    return _lazyNodeActionsByKind;
                }
            }

            private static ImmutableDictionary<DiagnosticAnalyzer, ImmutableDictionary<TLanguageKindEnum, ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>>> CreateNodeActionsByKind(
                AnalyzerActions analyzerActions)
            {
                var nodeActions = analyzerActions.GetSyntaxNodeActions<TLanguageKindEnum>();
                ImmutableDictionary<DiagnosticAnalyzer, ImmutableDictionary<TLanguageKindEnum, ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>>> analyzerActionsByKind;
                if (!nodeActions.IsEmpty)
                {
                    var nodeActionsByAnalyzers = nodeActions.GroupBy(a => a.Analyzer);
                    var builder = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, ImmutableDictionary<TLanguageKindEnum, ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>>>();
                    foreach (var analyzerAndActions in nodeActionsByAnalyzers)
                    {
                        ImmutableDictionary<TLanguageKindEnum, ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>> actionsByKind;
                        if (analyzerAndActions.Any())
                        {
                            actionsByKind = AnalyzerExecutor.GetNodeActionsByKind(analyzerAndActions);
                        }
                        else
                        {
                            actionsByKind = ImmutableDictionary<TLanguageKindEnum, ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>>.Empty;
                        }

                        builder.Add(analyzerAndActions.Key, actionsByKind);
                    }

                    analyzerActionsByKind = builder.ToImmutable();
                }
                else
                {
                    analyzerActionsByKind = ImmutableDictionary<DiagnosticAnalyzer, ImmutableDictionary<TLanguageKindEnum, ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>>>.Empty;
                }

                return analyzerActionsByKind;
            }

            public ImmutableDictionary<DiagnosticAnalyzer, ImmutableDictionary<OperationKind, ImmutableArray<OperationAnalyzerAction>>> OperationActionsByAnalyzerAndKind
            {
                get
                {
                    if (_lazyOperationActionsByKind == null)
                    {
                        var analyzerActionsByKind = CreateOperationActionsByKind(this._analyzerActions);
                        Interlocked.CompareExchange(ref _lazyOperationActionsByKind, analyzerActionsByKind, null);
                    }

                    return _lazyOperationActionsByKind;
                }
            }

            private static ImmutableDictionary<DiagnosticAnalyzer, ImmutableDictionary<OperationKind, ImmutableArray<OperationAnalyzerAction>>> CreateOperationActionsByKind(
                AnalyzerActions analyzerActions)
            {
                var operationActions = analyzerActions.OperationActions;
                ImmutableDictionary<DiagnosticAnalyzer, ImmutableDictionary<OperationKind, ImmutableArray<OperationAnalyzerAction>>> analyzerActionsByKind;
                if (!operationActions.IsEmpty)
                {
                    var operationActionsByAnalyzers = operationActions.GroupBy(a => a.Analyzer);
                    var builder = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, ImmutableDictionary<OperationKind, ImmutableArray<OperationAnalyzerAction>>>();
                    foreach (var analyzerAndActions in operationActionsByAnalyzers)
                    {
                        ImmutableDictionary<OperationKind, ImmutableArray<OperationAnalyzerAction>> actionsByKind;
                        if (analyzerAndActions.Any())
                        {
                            actionsByKind = AnalyzerExecutor.GetOperationActionsByKind(analyzerAndActions);
                        }
                        else
                        {
                            actionsByKind = ImmutableDictionary<OperationKind, ImmutableArray<OperationAnalyzerAction>>.Empty;
                        }

                        builder.Add(analyzerAndActions.Key, actionsByKind);
                    }

                    analyzerActionsByKind = builder.ToImmutable();
                }
                else
                {
                    analyzerActionsByKind = ImmutableDictionary<DiagnosticAnalyzer, ImmutableDictionary<OperationKind, ImmutableArray<OperationAnalyzerAction>>>.Empty;
                }

                return analyzerActionsByKind;
            }

            private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<CodeBlockStartAnalyzerAction<TLanguageKindEnum>>> CodeBlockStartActionsByAnalyzer
            {
                get { return GetBlockActionsByAnalyzer(ref _lazyCodeBlockStartActionsByAnalyzer, analyzerActions => analyzerActions.GetCodeBlockStartActions<TLanguageKindEnum>(), this._analyzerActions); }
            }

            private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<CodeBlockAnalyzerAction>> CodeBlockEndActionsByAnalyzer
            {
                get { return GetBlockActionsByAnalyzer(ref _lazyCodeBlockEndActionsByAnalyzer, analyzerActions => analyzerActions.CodeBlockEndActions, this._analyzerActions); }
            }

            private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<CodeBlockAnalyzerAction>> CodeBlockActionsByAnalyzer
            {
                get { return GetBlockActionsByAnalyzer(ref _lazyCodeBlockActionsByAnalyzer, analyzerActions => analyzerActions.CodeBlockActions, this._analyzerActions); }
            }

            private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<OperationBlockStartAnalyzerAction>> OperationBlockStartActionsByAnalyzer
            {
                get { return GetBlockActionsByAnalyzer(ref _lazyOperationBlockStartActionsByAnalyzer, analyzerActions => analyzerActions.OperationBlockStartActions, this._analyzerActions); }
            }

            private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<OperationBlockAnalyzerAction>> OperationBlockEndActionsByAnalyzer
            {
                get { return GetBlockActionsByAnalyzer(ref _lazyOperationBlockEndActionsByAnalyzer, analyzerActions => analyzerActions.OperationBlockEndActions, this._analyzerActions); }
            }

            private ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<OperationBlockAnalyzerAction>> OperationBlockActionsByAnalyzer
            {
                get { return GetBlockActionsByAnalyzer(ref _lazyOperationBlockActionsByAnalyzer, analyzerActions => analyzerActions.OperationBlockActions, this._analyzerActions); }
            }

            private static ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<ActionType>> GetBlockActionsByAnalyzer<ActionType>(
                ref ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<ActionType>> lazyCodeBlockActionsByAnalyzer,
                Func<AnalyzerActions, ImmutableArray<ActionType>> codeBlockActionsFactory,
                AnalyzerActions analyzerActions)
                where ActionType : AnalyzerAction
            {
                if (lazyCodeBlockActionsByAnalyzer == null)
                {
                    var codeBlockActionsByAnalyzer = CreateBlockActionsByAnalyzer(codeBlockActionsFactory, analyzerActions);
                    Interlocked.CompareExchange(ref lazyCodeBlockActionsByAnalyzer, codeBlockActionsByAnalyzer, null);
                }

                return lazyCodeBlockActionsByAnalyzer;
            }

            private static ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<ActionType>> CreateBlockActionsByAnalyzer<ActionType>(
                Func<AnalyzerActions, ImmutableArray<ActionType>> codeBlockActionsFactory,
                AnalyzerActions analyzerActions)
                where ActionType : AnalyzerAction
            {
                ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<ActionType>> codeBlockActionsByAnalyzer;
                var codeBlockActions = codeBlockActionsFactory(analyzerActions);
                if (!codeBlockActions.IsEmpty)
                {
                    var builder = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, ImmutableArray<ActionType>>();
                    var actionsByAnalyzer = codeBlockActions.GroupBy(action => action.Analyzer);
                    foreach (var analyzerAndActions in actionsByAnalyzer)
                    {
                        builder.Add(analyzerAndActions.Key, analyzerAndActions.ToImmutableArrayOrEmpty());
                    }

                    codeBlockActionsByAnalyzer = builder.ToImmutable();
                }
                else
                {
                    codeBlockActionsByAnalyzer = ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<ActionType>>.Empty;
                }

                return codeBlockActionsByAnalyzer;
            }

            public bool ShouldExecuteSyntaxNodeActions(AnalysisScope analysisScope)
            {
                if (!this.NodeActionsByAnalyzerAndKind.IsEmpty)
                {
                    foreach (var analyzer in analysisScope.Analyzers)
                    {
                        if (this.NodeActionsByAnalyzerAndKind.ContainsKey(analyzer))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            public bool ShouldExecuteOperationActions(AnalysisScope analysisScope)
            {
                if (!this.OperationActionsByAnalyzerAndKind.IsEmpty)
                {
                    foreach (var analyzer in analysisScope.Analyzers)
                    {
                        if (this.OperationActionsByAnalyzerAndKind.ContainsKey(analyzer))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            public bool ShouldExecuteBlockActions<T0, T1>(ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<T0>> blockStartActions, ImmutableDictionary<DiagnosticAnalyzer, ImmutableArray<T1>> blockActions, AnalysisScope analysisScope, ISymbol symbol)
            {
                if ((!blockStartActions.IsEmpty || !blockActions.IsEmpty) &&
                    AnalyzerExecutor.CanHaveExecutableCodeBlock(symbol))
                {
                    foreach (var analyzer in analysisScope.Analyzers)
                    {
                        if (blockStartActions.ContainsKey(analyzer) ||
                            blockActions.ContainsKey(analyzer))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            public bool ShouldExecuteCodeBlockActions(AnalysisScope analysisScope, ISymbol symbol)
            {
                return ShouldExecuteBlockActions(this.CodeBlockStartActionsByAnalyzer, this.CodeBlockActionsByAnalyzer, analysisScope, symbol);
            }

            public bool ShouldExecuteOperationBlockActions(AnalysisScope analysisScope, ISymbol symbol)
            {
                return ShouldExecuteBlockActions(this.OperationBlockStartActionsByAnalyzer, this.OperationBlockActionsByAnalyzer, analysisScope, symbol);
            }

            public IEnumerable<CodeBlockAnalyzerActions> GetCodeBlockActions(AnalysisScope analysisScope)
            {
                if (_analyzerActions.IsEmpty)
                {
                    yield break;
                }

                foreach (var analyzer in analysisScope.Analyzers)
                {
                    ImmutableArray<CodeBlockStartAnalyzerAction<TLanguageKindEnum>> codeBlockStartActions;
                    if (!this.CodeBlockStartActionsByAnalyzer.TryGetValue(analyzer, out codeBlockStartActions))
                    {
                        codeBlockStartActions = ImmutableArray<CodeBlockStartAnalyzerAction<TLanguageKindEnum>>.Empty;
                    }

                    ImmutableArray<CodeBlockAnalyzerAction> codeBlockActions;
                    if (!this.CodeBlockActionsByAnalyzer.TryGetValue(analyzer, out codeBlockActions))
                    {
                        codeBlockActions = ImmutableArray<CodeBlockAnalyzerAction>.Empty;
                    }

                    ImmutableArray<CodeBlockAnalyzerAction> codeBlockEndActions;
                    if (!this.CodeBlockEndActionsByAnalyzer.TryGetValue(analyzer, out codeBlockEndActions))
                    {
                        codeBlockEndActions = ImmutableArray<CodeBlockAnalyzerAction>.Empty;
                    }

                    ImmutableArray<OperationBlockStartAnalyzerAction> operationBlockStartActions;
                    if (!this.OperationBlockStartActionsByAnalyzer.TryGetValue(analyzer, out operationBlockStartActions))
                    {
                        operationBlockStartActions = ImmutableArray<OperationBlockStartAnalyzerAction>.Empty;
                    }

                    ImmutableArray<OperationBlockAnalyzerAction> operationBlockActions;
                    if (!this.OperationBlockActionsByAnalyzer.TryGetValue(analyzer, out operationBlockActions))
                    {
                        operationBlockActions = ImmutableArray<OperationBlockAnalyzerAction>.Empty;
                    }

                    ImmutableArray<OperationBlockAnalyzerAction> operationBlockEndActions;
                    if (!this.OperationBlockEndActionsByAnalyzer.TryGetValue(analyzer, out operationBlockEndActions))
                    {
                        operationBlockEndActions = ImmutableArray<OperationBlockAnalyzerAction>.Empty;
                    }

                    if (!codeBlockStartActions.IsEmpty || !codeBlockActions.IsEmpty || !codeBlockEndActions.IsEmpty || !operationBlockStartActions.IsEmpty || !operationBlockActions.IsEmpty || !operationBlockEndActions.IsEmpty)
                    {
                        yield return
                            new CodeBlockAnalyzerActions
                            {
                                Analyzer = analyzer,
                                CodeBlockStartActions = codeBlockStartActions,
                                CodeBlockActions = codeBlockActions,
                                CodeBlockEndActions = codeBlockEndActions,
                                OperationBlockStartActions = operationBlockStartActions,
                                OperationBlockActions = operationBlockActions,
                                OperationBlockEndActions = operationBlockEndActions
                            };
                    }
                }
            }
        }
    }
}
