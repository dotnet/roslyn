// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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

            private HostAnalyzerStateSets GetOrCreateHostStateSets(string language)
            {
                static HostAnalyzerStateSets CreateLanguageSpecificAnalyzerMap(string language, DiagnosticAnalyzerInfoCache analyzerInfoCache)
                {
                    var analyzersPerReference = analyzerInfoCache.GetOrCreateHostDiagnosticAnalyzersPerReference(language);

                    var analyzerMap = CreateStateSetMap(analyzerInfoCache, language, analyzersPerReference.Values);
                    VerifyUniqueStateNames(analyzerMap.Values);

                    return new HostAnalyzerStateSets(analyzerInfoCache, language, analyzerMap);
                }

                return ImmutableInterlocked.GetOrAdd(ref _hostAnalyzerStateMap, language, CreateLanguageSpecificAnalyzerMap, _analyzerInfoCache);
            }

            private sealed class HostAnalyzerStateSets
            {
                private const int BuiltInCompilerPriority = -2;
                private const int RegularDiagnosticAnalyzerPriority = -1;

                private readonly DiagnosticAnalyzer? _compilerAnalyzer;

                // ordered by priority
                public readonly ImmutableArray<StateSet> OrderedStateSets;

                public readonly ImmutableDictionary<DiagnosticAnalyzer, StateSet> StateSetMap;

                public HostAnalyzerStateSets(DiagnosticAnalyzerInfoCache analyzerInfoCache, string language, ImmutableDictionary<DiagnosticAnalyzer, StateSet> analyzerMap)
                {
                    StateSetMap = analyzerMap;

                    _compilerAnalyzer = analyzerInfoCache.GetCompilerDiagnosticAnalyzer(language);

                    // order statesets
                    // order will be in this order
                    // BuiltIn Compiler Analyzer (C#/VB) < Regular DiagnosticAnalyzers < Document/ProjectDiagnosticAnalyzers
                    OrderedStateSets = StateSetMap.Values.OrderBy(PriorityComparison).ToImmutableArray();
                }

                private int PriorityComparison(StateSet state1, StateSet state2)
                {
                    return GetPriority(state1) - GetPriority(state2);
                }

                private int GetPriority(StateSet state)
                {
                    // compiler gets highest priority
                    if (state.Analyzer == _compilerAnalyzer)
                    {
                        return BuiltInCompilerPriority;
                    }

                    switch (state.Analyzer)
                    {
                        case DocumentDiagnosticAnalyzer analyzer:
                            return Math.Max(0, analyzer.Priority);
                        case ProjectDiagnosticAnalyzer analyzer:
                            return Math.Max(0, analyzer.Priority);
                        default:
                            // regular analyzer get next priority after compiler analyzer
                            return RegularDiagnosticAnalyzerPriority;
                    }
                }
            }
        }
    }
}
