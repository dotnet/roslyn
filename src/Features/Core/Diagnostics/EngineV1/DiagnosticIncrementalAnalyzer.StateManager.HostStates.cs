// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV1
{
    internal partial class DiagnosticIncrementalAnalyzer
    {
        private partial class StateManager
        {
            /// <summary>
            /// This class is responsible for anything related to <see cref="DiagnosticState"/> for host level <see cref="DiagnosticAnalyzer"/>s.
            /// </summary>
            private class HostStates
            {
                private readonly StateManager _owner;

                private ImmutableDictionary<string, ImmutableDictionary<DiagnosticAnalyzer, StateSet>> _stateMap;

                public HostStates(StateManager owner)
                {
                    _owner = owner;
                    _stateMap = ImmutableDictionary<string, ImmutableDictionary<DiagnosticAnalyzer, StateSet>>.Empty;
                }

                public IEnumerable<StateSet> GetStateSets()
                {
                    return _stateMap.Values.SelectMany(v => v.Values);
                }

                public IEnumerable<StateSet> GetOrCreateStateSets(string language)
                {
                    var map = GetAnalyzerMap(language);
                    return map.Values;
                }

                public StateSet GetOrCreateStateSet(string language, DiagnosticAnalyzer analyzer)
                {
                    var map = GetAnalyzerMap(language);

                    StateSet set;
                    if (map.TryGetValue(analyzer, out set))
                    {
                        return set;
                    }

                    return null;
                }

                private ImmutableDictionary<DiagnosticAnalyzer, StateSet> GetAnalyzerMap(string language)
                {
                    return ImmutableInterlocked.GetOrAdd(ref _stateMap, language, CreateLanguageSpecificAnalyzerMap, this);
                }

                private ImmutableDictionary<DiagnosticAnalyzer, StateSet> CreateLanguageSpecificAnalyzerMap(string language, HostStates @this)
                {
                    var analyzersPerReference = _owner.AnalyzerManager.GetHostDiagnosticAnalyzersPerReference(language);

                    var analyzerMap = CreateAnalyzerMap(_owner.AnalyzerManager, language, analyzersPerReference.Values);
                    VerifyDiagnosticStates(analyzerMap.Values);

                    return analyzerMap;
                }
            }
        }
    }
}
