// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Collections;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal partial class AnalyzerDriver<TLanguageKindEnum> : AnalyzerDriver where TLanguageKindEnum : struct
    {
        private sealed class GroupedAnalyzerActionsForAnalyzer
        {
            private readonly DiagnosticAnalyzer _analyzer;
            private readonly bool _analyzerActionsNeedFiltering;

            private ImmutableSegmentedDictionary<TLanguageKindEnum, ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>> _lazyNodeActionsByKind;
            private ImmutableSegmentedDictionary<OperationKind, ImmutableArray<OperationAnalyzerAction>> _lazyOperationActionsByKind;
            private ImmutableArray<CodeBlockStartAnalyzerAction<TLanguageKindEnum>> _lazyCodeBlockStartActions;
            private ImmutableArray<CodeBlockAnalyzerAction> _lazyCodeBlockEndActions;
            private ImmutableArray<CodeBlockAnalyzerAction> _lazyCodeBlockActions;
            private ImmutableArray<OperationBlockStartAnalyzerAction> _lazyOperationBlockStartActions;
            private ImmutableArray<OperationBlockAnalyzerAction> _lazyOperationBlockActions;
            private ImmutableArray<OperationBlockAnalyzerAction> _lazyOperationBlockEndActions;

            public GroupedAnalyzerActionsForAnalyzer(DiagnosticAnalyzer analyzer, in AnalyzerActions analyzerActions, bool analyzerActionsNeedFiltering)
            {
                Debug.Assert(!analyzerActions.IsEmpty);

                _analyzer = analyzer;
                AnalyzerActions = analyzerActions;
                _analyzerActionsNeedFiltering = analyzerActionsNeedFiltering;
            }

            public AnalyzerActions AnalyzerActions { get; }

            [Conditional("DEBUG")]
            private static void VerifyActions<TAnalyzerAction>(in ImmutableArray<TAnalyzerAction> actions, DiagnosticAnalyzer analyzer)
                where TAnalyzerAction : AnalyzerAction
            {
                foreach (var action in actions)
                {
                    Debug.Assert(action.Analyzer == analyzer);
                }
            }

            private ImmutableArray<TAnalyzerAction> GetFilteredActions<TAnalyzerAction>(in ImmutableArray<TAnalyzerAction> actions)
                where TAnalyzerAction : AnalyzerAction
                => GetFilteredActions(actions, _analyzer, _analyzerActionsNeedFiltering);

            private static ImmutableArray<TAnalyzerAction> GetFilteredActions<TAnalyzerAction>(
                in ImmutableArray<TAnalyzerAction> actions,
                DiagnosticAnalyzer analyzer,
                bool analyzerActionsNeedFiltering)
                where TAnalyzerAction : AnalyzerAction
            {
                if (!analyzerActionsNeedFiltering)
                {
                    return actions;
                }

                return actions.WhereAsArray((action, analyzer) => action.Analyzer == analyzer, analyzer);
            }

            public ImmutableSegmentedDictionary<TLanguageKindEnum, ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>> NodeActionsByAnalyzerAndKind
            {
                get
                {
                    if (_lazyNodeActionsByKind == null)
                    {
                        var nodeActions = _analyzerActionsNeedFiltering ?
                            AnalyzerActions.GetSyntaxNodeActions<TLanguageKindEnum>(_analyzer) :
                            AnalyzerActions.GetSyntaxNodeActions<TLanguageKindEnum>();
                        VerifyActions(nodeActions, _analyzer);
                        var analyzerActionsByKind = !nodeActions.IsEmpty ?
                            AnalyzerExecutor.GetNodeActionsByKind(nodeActions) :
                            ImmutableSegmentedDictionary<TLanguageKindEnum, ImmutableArray<SyntaxNodeAnalyzerAction<TLanguageKindEnum>>>.Empty;
                        RoslynImmutableInterlocked.InterlockedInitialize(ref _lazyNodeActionsByKind, analyzerActionsByKind);
                    }

                    return _lazyNodeActionsByKind;
                }
            }

            public ImmutableSegmentedDictionary<OperationKind, ImmutableArray<OperationAnalyzerAction>> OperationActionsByAnalyzerAndKind
            {
                get
                {
                    if (_lazyOperationActionsByKind == null)
                    {
                        var operationActions = GetFilteredActions(AnalyzerActions.OperationActions);
                        VerifyActions(operationActions, _analyzer);
                        var analyzerActionsByKind = operationActions.Any() ?
                            AnalyzerExecutor.GetOperationActionsByKind(operationActions) :
                            ImmutableSegmentedDictionary<OperationKind, ImmutableArray<OperationAnalyzerAction>>.Empty;
                        RoslynImmutableInterlocked.InterlockedInitialize(ref _lazyOperationActionsByKind, analyzerActionsByKind);
                    }

                    return _lazyOperationActionsByKind;
                }
            }

            private ImmutableArray<CodeBlockStartAnalyzerAction<TLanguageKindEnum>> CodeBlockStartActions
            {
                get
                {
                    if (_lazyCodeBlockStartActions.IsDefault)
                    {
                        var codeBlockActions = GetFilteredActions(AnalyzerActions.GetCodeBlockStartActions<TLanguageKindEnum>());
                        VerifyActions(codeBlockActions, _analyzer);
                        ImmutableInterlocked.InterlockedInitialize(ref _lazyCodeBlockStartActions, codeBlockActions);
                    }

                    return _lazyCodeBlockStartActions;
                }
            }

            private ImmutableArray<CodeBlockAnalyzerAction> CodeBlockEndActions
                => GetExecutableCodeActions(ref _lazyCodeBlockEndActions, AnalyzerActions.CodeBlockEndActions, _analyzer, _analyzerActionsNeedFiltering);

            private ImmutableArray<CodeBlockAnalyzerAction> CodeBlockActions
                => GetExecutableCodeActions(ref _lazyCodeBlockActions, AnalyzerActions.CodeBlockActions, _analyzer, _analyzerActionsNeedFiltering);

            private ImmutableArray<OperationBlockStartAnalyzerAction> OperationBlockStartActions
                => GetExecutableCodeActions(ref _lazyOperationBlockStartActions, AnalyzerActions.OperationBlockStartActions, _analyzer, _analyzerActionsNeedFiltering);

            private ImmutableArray<OperationBlockAnalyzerAction> OperationBlockEndActions
                => GetExecutableCodeActions(ref _lazyOperationBlockEndActions, AnalyzerActions.OperationBlockEndActions, _analyzer, _analyzerActionsNeedFiltering);

            private ImmutableArray<OperationBlockAnalyzerAction> OperationBlockActions
                => GetExecutableCodeActions(ref _lazyOperationBlockActions, AnalyzerActions.OperationBlockActions, _analyzer, _analyzerActionsNeedFiltering);

            public bool HasCodeBlockStartActions => !CodeBlockStartActions.IsEmpty;
            public bool HasOperationBlockStartActions => !OperationBlockStartActions.IsEmpty;

            private static ImmutableArray<ActionType> GetExecutableCodeActions<ActionType>(
                ref ImmutableArray<ActionType> lazyCodeBlockActions,
                ImmutableArray<ActionType> codeBlockActions,
                DiagnosticAnalyzer analyzer,
                bool analyzerActionsNeedFiltering)
                where ActionType : AnalyzerAction
            {
                if (lazyCodeBlockActions.IsDefault)
                {
                    codeBlockActions = GetFilteredActions(codeBlockActions, analyzer, analyzerActionsNeedFiltering);
                    VerifyActions(codeBlockActions, analyzer);
                    ImmutableInterlocked.InterlockedInitialize(ref lazyCodeBlockActions, codeBlockActions);
                }

                return lazyCodeBlockActions;
            }

            public bool TryGetExecutableCodeBlockActions(out ExecutableCodeBlockAnalyzerActions actions)
            {
                if (!OperationBlockStartActions.IsEmpty ||
                    !OperationBlockActions.IsEmpty ||
                    !OperationBlockEndActions.IsEmpty ||
                    !CodeBlockStartActions.IsEmpty ||
                    !CodeBlockActions.IsEmpty ||
                    !CodeBlockEndActions.IsEmpty)
                {
                    actions = new ExecutableCodeBlockAnalyzerActions
                    {
                        Analyzer = _analyzer,
                        CodeBlockStartActions = CodeBlockStartActions,
                        CodeBlockActions = CodeBlockActions,
                        CodeBlockEndActions = CodeBlockEndActions,
                        OperationBlockStartActions = OperationBlockStartActions,
                        OperationBlockActions = OperationBlockActions,
                        OperationBlockEndActions = OperationBlockEndActions
                    };

                    return true;
                }

                actions = default;
                return false;
            }
        }
    }
}
