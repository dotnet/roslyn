// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal partial class AnalyzerDriver<TLanguageKindEnum> : AnalyzerDriver where TLanguageKindEnum : struct
    {
        /// <summary>
        /// <see cref="AnalyzerActions"/> grouped by <see cref="DiagnosticAnalyzer"/>, and possibly other entities, such as <see cref="OperationKind"/>, <see cref="SymbolKind"/>, etc.
        /// </summary>
        private sealed class GroupedAnalyzerActions : IGroupedAnalyzerActions
        {
            public static readonly GroupedAnalyzerActions Empty = new GroupedAnalyzerActions(
                ImmutableArray<(DiagnosticAnalyzer, GroupedAnalyzerActionsForAnalyzer)>.Empty,
                ImmutableSegmentedDictionary<TLanguageKindEnum, ImmutableArray<DiagnosticAnalyzer>>.Empty,
                AnalyzerActions.Empty);

            private GroupedAnalyzerActions(
                ImmutableArray<(DiagnosticAnalyzer, GroupedAnalyzerActionsForAnalyzer)> groupedActionsAndAnalyzers,
                ImmutableSegmentedDictionary<TLanguageKindEnum, ImmutableArray<DiagnosticAnalyzer>> analyzersByKind,
                in AnalyzerActions analyzerActions)
            {
                GroupedActionsByAnalyzer = groupedActionsAndAnalyzers;
                AnalyzerActions = analyzerActions;
                AnalyzersByKind = analyzersByKind;
            }

            public ImmutableArray<(DiagnosticAnalyzer analyzer, GroupedAnalyzerActionsForAnalyzer groupedActions)> GroupedActionsByAnalyzer { get; }

            public ImmutableSegmentedDictionary<TLanguageKindEnum, ImmutableArray<DiagnosticAnalyzer>> AnalyzersByKind { get; }

            public AnalyzerActions AnalyzerActions { get; }

            public bool IsEmpty
            {
                get
                {
                    var isEmpty = ReferenceEquals(this, Empty);
                    Debug.Assert(isEmpty || !GroupedActionsByAnalyzer.IsEmpty);
                    return isEmpty;
                }
            }

            public static GroupedAnalyzerActions Create(DiagnosticAnalyzer analyzer, in AnalyzerActions analyzerActions)
            {
                if (analyzerActions.IsEmpty)
                {
                    return Empty;
                }

                var groupedActions = new GroupedAnalyzerActionsForAnalyzer(analyzer, analyzerActions, analyzerActionsNeedFiltering: false);
                var groupedActionsAndAnalyzers = ImmutableArray<(DiagnosticAnalyzer, GroupedAnalyzerActionsForAnalyzer)>.Empty.Add((analyzer, groupedActions));
                var analyzersByKind = CreateAnalyzersByKind(groupedActionsAndAnalyzers);
                return new GroupedAnalyzerActions(groupedActionsAndAnalyzers, analyzersByKind, in analyzerActions);
            }

            public static GroupedAnalyzerActions Create(ImmutableArray<DiagnosticAnalyzer> analyzers, in AnalyzerActions analyzerActions)
            {
                Debug.Assert(!analyzers.IsDefaultOrEmpty);

                var groups = analyzers.SelectAsArray(
                    (analyzer, analyzerActions) => (analyzer, new GroupedAnalyzerActionsForAnalyzer(analyzer, analyzerActions, analyzerActionsNeedFiltering: true)),
                    analyzerActions);
                var analyzersByKind = CreateAnalyzersByKind(groups);
                return new GroupedAnalyzerActions(groups, analyzersByKind, in analyzerActions);
            }

            IGroupedAnalyzerActions IGroupedAnalyzerActions.Append(IGroupedAnalyzerActions igroupedAnalyzerActions)
            {
                var groupedAnalyzerActions = (GroupedAnalyzerActions)igroupedAnalyzerActions;

#if DEBUG
                var inputAnalyzers = groupedAnalyzerActions.GroupedActionsByAnalyzer.Select(a => a.analyzer);
                var myAnalyzers = GroupedActionsByAnalyzer.Select(a => a.analyzer);
                var intersected = inputAnalyzers.Intersect(myAnalyzers);
                Debug.Assert(intersected.IsEmpty());
#endif

                var newGroupedActions = GroupedActionsByAnalyzer.AddRange(groupedAnalyzerActions.GroupedActionsByAnalyzer);
                var newAnalyzerActions = AnalyzerActions.Append(groupedAnalyzerActions.AnalyzerActions);
                var analyzersByKind = CreateAnalyzersByKind(newGroupedActions);
                return new GroupedAnalyzerActions(newGroupedActions, analyzersByKind, newAnalyzerActions);
            }

            private static ImmutableSegmentedDictionary<TLanguageKindEnum, ImmutableArray<DiagnosticAnalyzer>> CreateAnalyzersByKind(ImmutableArray<(DiagnosticAnalyzer, GroupedAnalyzerActionsForAnalyzer)> groupedActionsAndAnalyzers)
            {
                var analyzersByKind = PooledDictionary<TLanguageKindEnum, ArrayBuilder<DiagnosticAnalyzer>>.GetInstance();
                foreach (var (analyzer, groupedActionsForAnalyzer) in groupedActionsAndAnalyzers)
                {
                    foreach (var (kind, _) in groupedActionsForAnalyzer.NodeActionsByAnalyzerAndKind)
                    {
                        analyzersByKind.AddPooled(kind, analyzer);
                    }
                }

                return analyzersByKind.ToImmutableSegmentedDictionaryAndFree();
            }
        }
    }
}
