// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            public IEnumerable<StateSet> GetHostStateSets()
            {
                return _hostStateMap.Values.SelectMany(v => v.GetStateSets());
            }

            public IEnumerable<StateSet> GetOrCreateHostStateSets(string language)
            {
                return GetHostAnalyzerMap(language).GetStateSets();
            }

            public StateSet GetOrCreateHostStateSet(string language, DiagnosticAnalyzer analyzer)
            {
                return GetHostAnalyzerMap(language).GetStateSet(analyzer);
            }

            private HostAnalyzerStateSets GetHostAnalyzerMap(string language)
            {
                static HostAnalyzerStateSets CreateLanguageSpecificAnalyzerMap(string language, HostAnalyzerManager analyzerManager)
                {
                    var analyzersPerReference = analyzerManager.GetHostDiagnosticAnalyzersPerReference(language);

                    var analyzerMap = CreateAnalyzerMap(analyzerManager, language, analyzersPerReference.Values);
                    VerifyUniqueStateNames(analyzerMap.Values);

                    return new HostAnalyzerStateSets(analyzerManager, language, analyzerMap);
                }

                return ImmutableInterlocked.GetOrAdd(ref _hostStateMap, language, CreateLanguageSpecificAnalyzerMap, _analyzerManager);
            }

            private sealed class HostAnalyzerStateSets
            {
                private const int BuiltInCompilerPriority = -2;
                private const int RegularDiagnosticAnalyzerPriority = -1;

                private readonly DiagnosticAnalyzer _compilerAnalyzer;

                private readonly ImmutableArray<StateSet> _orderedSet;
                private readonly ImmutableDictionary<DiagnosticAnalyzer, StateSet> _map;

                public HostAnalyzerStateSets(HostAnalyzerManager analyzerManager, string language, ImmutableDictionary<DiagnosticAnalyzer, StateSet> analyzerMap)
                {
                    _map = analyzerMap;

                    _compilerAnalyzer = analyzerManager.GetCompilerDiagnosticAnalyzer(language);

                    // order statesets
                    // order will be in this order
                    // BuiltIn Compiler Analyzer (C#/VB) < Regular DiagnosticAnalyzers < Document/ProjectDiagnosticAnalyzers
                    _orderedSet = _map.Values.OrderBy(PriorityComparison).ToImmutableArray();
                }

                public ImmutableArray<StateSet> GetStateSets() => _orderedSet;

                public StateSet GetStateSet(DiagnosticAnalyzer analyzer)
                {
                    if (_map.TryGetValue(analyzer, out var stateSet))
                    {
                        return stateSet;
                    }

                    return null;
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
