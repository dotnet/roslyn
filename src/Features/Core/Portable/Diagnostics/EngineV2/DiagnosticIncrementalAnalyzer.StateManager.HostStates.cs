// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    internal partial class DiagnosticIncrementalAnalyzer
    {
        private partial class StateManager
        {
            public IEnumerable<StateSet> GetAllHostStateSets()
                => _hostAnalyzerStateMap.Values.SelectMany(v => v.OrderedStateSets);

            private HostAnalyzerStateSets GetOrCreateHostStateSets(Project project, ProjectAnalyzerStateSets projectStateSets)
            {
                var hostStateSets = ImmutableInterlocked.GetOrAdd(ref _hostAnalyzerStateMap, project.Language, CreateLanguageSpecificAnalyzerMap, project.Solution.State.Analyzers);
                return hostStateSets.WithExcludedAnalyzers(projectStateSets.SkippedAnalyzersInfo.SkippedAnalyzers);

                static HostAnalyzerStateSets CreateLanguageSpecificAnalyzerMap(string language, HostDiagnosticAnalyzers hostAnalyzers)
                {
                    var analyzersPerReference = hostAnalyzers.GetOrCreateHostDiagnosticAnalyzersPerReference(language);

                    var analyzerMap = CreateStateSetMap(language, analyzersPerReference.Values, includeFileContentLoadAnalyzer: true);
                    VerifyUniqueStateNames(analyzerMap.Values);

                    return new HostAnalyzerStateSets(analyzerMap);
                }
            }

            private sealed class HostAnalyzerStateSets
            {
                private const int FileContentLoadAnalyzerPriority = -3;
                private const int BuiltInCompilerPriority = -2;
                private const int RegularDiagnosticAnalyzerPriority = -1;

                // ordered by priority
                public readonly ImmutableArray<StateSet> OrderedStateSets;

                public readonly ImmutableDictionary<DiagnosticAnalyzer, StateSet> StateSetMap;

                private HostAnalyzerStateSets(ImmutableDictionary<DiagnosticAnalyzer, StateSet> stateSetMap, ImmutableArray<StateSet> orderedStateSets)
                {
                    StateSetMap = stateSetMap;
                    OrderedStateSets = orderedStateSets;
                }

                public HostAnalyzerStateSets(ImmutableDictionary<DiagnosticAnalyzer, StateSet> analyzerMap)
                {
                    StateSetMap = analyzerMap;

                    // order statesets
                    // order will be in this order
                    // BuiltIn Compiler Analyzer (C#/VB) < Regular DiagnosticAnalyzers < Document/ProjectDiagnosticAnalyzers
                    OrderedStateSets = StateSetMap.Values.OrderBy(PriorityComparison).ToImmutableArray();
                }

                public HostAnalyzerStateSets WithExcludedAnalyzers(ImmutableHashSet<DiagnosticAnalyzer> excludedAnalyzers)
                {
                    if (excludedAnalyzers.IsEmpty)
                    {
                        return this;
                    }

                    var stateSetMap = StateSetMap.Where(kvp => !excludedAnalyzers.Contains(kvp.Key)).ToImmutableDictionary();
                    var orderedStateSets = OrderedStateSets.WhereAsArray(stateSet => !excludedAnalyzers.Contains(stateSet.Analyzer));
                    return new HostAnalyzerStateSets(stateSetMap, orderedStateSets);
                }

                private int PriorityComparison(StateSet state1, StateSet state2)
                    => GetPriority(state1) - GetPriority(state2);

                private static int GetPriority(StateSet state)
                {
                    // compiler gets highest priority
                    if (state.Analyzer.IsCompilerAnalyzer())
                    {
                        return BuiltInCompilerPriority;
                    }

                    return state.Analyzer switch
                    {
                        FileContentLoadAnalyzer _ => FileContentLoadAnalyzerPriority,
                        DocumentDiagnosticAnalyzer analyzer => Math.Max(0, analyzer.Priority),
                        ProjectDiagnosticAnalyzer analyzer => Math.Max(0, analyzer.Priority),
                        _ => RegularDiagnosticAnalyzerPriority,
                    };
                }
            }
        }
    }
}
