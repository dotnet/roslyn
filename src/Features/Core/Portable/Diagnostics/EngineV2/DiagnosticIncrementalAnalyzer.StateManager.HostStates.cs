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
            /// <summary>
            /// This class is responsible for anything related to <see cref="StateSet"/> for host level <see cref="DiagnosticAnalyzer"/>s.
            /// </summary>
            private class HostStates
            {
                private readonly StateManager _owner;

                private ImmutableDictionary<string, DiagnosticAnalyzerMap> _stateMap;

                public HostStates(StateManager owner)
                {
                    _owner = owner;
                    _stateMap = ImmutableDictionary<string, DiagnosticAnalyzerMap>.Empty;
                }

                public IEnumerable<StateSet> GetStateSets()
                {
                    return _stateMap.Values.SelectMany(v => v.GetStateSets());
                }

                public IEnumerable<StateSet> GetOrCreateStateSets(string language)
                {
                    return GetAnalyzerMap(language).GetStateSets();
                }

                public StateSet GetOrCreateStateSet(string language, DiagnosticAnalyzer analyzer)
                {
                    return GetAnalyzerMap(language).GetStateSet(analyzer);
                }

                private DiagnosticAnalyzerMap GetAnalyzerMap(string language)
                {
                    return ImmutableInterlocked.GetOrAdd(ref _stateMap, language, CreateLanguageSpecificAnalyzerMap, this);
                }

                private DiagnosticAnalyzerMap CreateLanguageSpecificAnalyzerMap(string language, HostStates @this)
                {
                    var analyzersPerReference = _owner.AnalyzerManager.GetHostDiagnosticAnalyzersPerReference(language);

                    var analyzerMap = CreateAnalyzerMap(_owner.AnalyzerManager, language, analyzersPerReference.Values);
                    VerifyDiagnosticStates(analyzerMap.Values);

                    return new DiagnosticAnalyzerMap(_owner.AnalyzerManager, language, analyzerMap);
                }

                private class DiagnosticAnalyzerMap
                {
                    private const int BuiltInCompilerPriority = -2;
                    private const int RegularDiagnosticAnalyzerPriority = -1;

                    private readonly StateSet _compilerStateSet;

                    private readonly ImmutableArray<StateSet> _orderedSet;
                    private readonly ImmutableDictionary<DiagnosticAnalyzer, StateSet> _map;

                    public DiagnosticAnalyzerMap(HostAnalyzerManager analyzerManager, string language, ImmutableDictionary<DiagnosticAnalyzer, StateSet> analyzerMap)
                    {
                        _map = analyzerMap;

                        var compilerAnalyzer = analyzerManager.GetCompilerDiagnosticAnalyzer(language);

                        // in test case, we might not have the compiler analyzer.
                        if (compilerAnalyzer != null)
                        {
                            // hold onto stateSet for compiler analyzer
                            _compilerStateSet = analyzerMap[compilerAnalyzer];
                        }

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
                        if (state == _compilerStateSet)
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
}
