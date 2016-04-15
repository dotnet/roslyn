// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

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

                public IEnumerable<DiagnosticAnalyzer> GetAnalyzers(string language)
                {
                    var map = GetAnalyzerMap(language);
                    return map.GetAnalyzers();
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
                    private readonly DiagnosticAnalyzer _compilerAnalyzer;
                    private readonly StateSet _compilerStateSet;

                    private readonly ImmutableDictionary<DiagnosticAnalyzer, StateSet> _map;

                    public DiagnosticAnalyzerMap(HostAnalyzerManager analyzerManager, string language, ImmutableDictionary<DiagnosticAnalyzer, StateSet> analyzerMap)
                    {
                        // hold directly on to compiler analyzer
                        _compilerAnalyzer = analyzerManager.GetCompilerDiagnosticAnalyzer(language);

                        // in test case, we might not have the compiler analyzer.
                        if (_compilerAnalyzer == null)
                        {
                            _map = analyzerMap;
                            return;
                        }

                        _compilerStateSet = analyzerMap[_compilerAnalyzer];

                        // hold rest of analyzers
                        _map = analyzerMap.Remove(_compilerAnalyzer);
                    }

                    public IEnumerable<DiagnosticAnalyzer> GetAnalyzers()
                    {
                        // always return compiler one first if it exists.
                        // it might not exist in test environment.
                        if (_compilerAnalyzer != null)
                        {
                            yield return _compilerAnalyzer;
                        }

                        foreach (var analyzer in _map.Keys)
                        {
                            yield return analyzer;
                        }
                    }

                    public IEnumerable<StateSet> GetStateSets()
                    {
                        // always return compiler one first if it exists.
                        // it might not exist in test environment.
                        if (_compilerAnalyzer != null)
                        {
                            yield return _compilerStateSet;
                        }

                        // TODO: for now, this is static, but in future, we might consider making this a dynamic so that we process cheaper analyzer first.
                        foreach (var set in _map.Values)
                        {
                            yield return set;
                        }
                    }

                    public StateSet GetStateSet(DiagnosticAnalyzer analyzer)
                    {
                        if (_compilerAnalyzer == analyzer)
                        {
                            return _compilerStateSet;
                        }

                        StateSet set;
                        if (_map.TryGetValue(analyzer, out set))
                        {
                            return set;
                        }

                        return null;
                    }
                }
            }
        }
    }
}
