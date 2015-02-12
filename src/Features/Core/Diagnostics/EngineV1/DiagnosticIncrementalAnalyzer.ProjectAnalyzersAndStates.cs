// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV1
{
    using ProviderId = Int32;

    internal partial class DiagnosticIncrementalAnalyzer
    {
        private partial class DiagnosticAnalyzersAndStates
        {
            /// <summary>
            /// Maintains per-project diagnostic analyzers and the corresponding diagnostic states.
            /// </summary>
            private class ProjectAnalyzersAndStates
            {
                private readonly ImmutableDictionary<string, ImmutableDictionary<DiagnosticAnalyzer, ProviderId>> _analyzerIdMap;
                private readonly DiagnosticState[,] _diagnosticStateMaps;
                private readonly int _startAnalyzerId;
                private readonly int _analyzerCount;
                private readonly string _projectLanguage;

                public static ProjectAnalyzersAndStates CreateIfAnyAnalyzers(IEnumerable<KeyValuePair<string, IEnumerable<DiagnosticAnalyzer>>> projectSpecificAnalyzers, int sharedWorkspaceAnalyzersCount, string projectLanguage)
                {
                    // Make sure we have at least one analyzer.
                    if (projectSpecificAnalyzers == null || !projectSpecificAnalyzers.Any())
                    {
                        return null;
                    }

                    return new ProjectAnalyzersAndStates(projectSpecificAnalyzers, sharedWorkspaceAnalyzersCount, projectLanguage);
                }

                private ProjectAnalyzersAndStates(IEnumerable<KeyValuePair<string, IEnumerable<DiagnosticAnalyzer>>> projectSpecificAnalyzers, int sharedWorkspaceAnalyzersCount, string projectLanguage)
                {
                    Contract.ThrowIfFalse(projectSpecificAnalyzers != null && projectSpecificAnalyzers.Any());

                    _startAnalyzerId = sharedWorkspaceAnalyzersCount;
                    _projectLanguage = projectLanguage;
                    _analyzerIdMap = CreateAnalyzerIdMap(projectSpecificAnalyzers, _startAnalyzerId);
                    _analyzerCount = _analyzerIdMap.Values.Flatten().Count();
                    Contract.ThrowIfFalse(_analyzerCount > 0);

                    _diagnosticStateMaps = new DiagnosticState[s_stateTypeCount, _analyzerCount];
                }

                private static ImmutableDictionary<string, ImmutableDictionary<DiagnosticAnalyzer, ProviderId>> CreateAnalyzerIdMap(
                    IEnumerable<KeyValuePair<string, IEnumerable<DiagnosticAnalyzer>>> projectSpecificAnalyzers, int startAnalyzerId)
                {
                    var index = startAnalyzerId;
                    var map = ImmutableDictionary.CreateBuilder<string, ImmutableDictionary<DiagnosticAnalyzer, ProviderId>>();

                    foreach (var analyzerList in projectSpecificAnalyzers)
                    {
                        var analyzerMap = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, ProviderId>();

                        foreach (var analyzer in analyzerList.Value)
                        {
                            Contract.ThrowIfNull(analyzer);
                            analyzerMap.Add(analyzer, index++);
                        }

                        if (map.ContainsKey(analyzerList.Key))
                        {
                            map[analyzerList.Key] = map[analyzerList.Key].AddRange(analyzerMap);
                        }
                        else
                        {
                            map.Add(analyzerList.Key, analyzerMap.ToImmutable());
                        }
                    }

                    return map.ToImmutable();
                }

                public int AnalyzerCount
                {
                    get
                    {
                        Contract.ThrowIfFalse(_analyzerCount > 0);
                        return _analyzerCount;
                    }
                }

                public IEnumerable<Tuple<DiagnosticState, ProviderId, StateType>> GetAllExistingDiagnosticStates()
                {
                    var current = SpecializedCollections.EmptyEnumerable<Tuple<DiagnosticState, ProviderId, StateType>>();
                    foreach (var type in s_documentScopeStateTypes)
                    {
                        current = current.Concat(GetAllExistingDiagnosticStates(type));
                    }

                    return current;
                }

                public IEnumerable<Tuple<DiagnosticState, ProviderId, StateType>> GetAllExistingDiagnosticStates(StateType type)
                {
                    foreach (var analyzerAndId in _analyzerIdMap.Values.Flatten())
                    {
                        var state = _diagnosticStateMaps[(int)type, analyzerAndId.Value - _startAnalyzerId];
                        yield return Tuple.Create(state, analyzerAndId.Value, type);
                    }
                }

                public IEnumerable<KeyValuePair<DiagnosticAnalyzer, ProviderId>> GetAllProviderAndIds()
                {
                    return _analyzerIdMap.Values.Flatten();
                }

                public DiagnosticState GetOrCreateDiagnosticState(StateType stateType, ProviderId providerId, DiagnosticAnalyzer provider)
                {
                    Contract.ThrowIfFalse(providerId >= _startAnalyzerId);
                    Contract.ThrowIfFalse(providerId < _startAnalyzerId + this.AnalyzerCount);

                    return DiagnosticAnalyzersAndStates.GetOrCreateDiagnosticState(_diagnosticStateMaps, stateType, providerId - _startAnalyzerId, providerId, provider, _projectLanguage);
                }

                public DiagnosticState GetDiagnosticState(StateType stateType, ProviderId providerId)
                {
                    Contract.ThrowIfFalse(providerId >= _startAnalyzerId);
                    Contract.ThrowIfFalse(providerId < _startAnalyzerId + this.AnalyzerCount);

                    return _diagnosticStateMaps[(int)stateType, providerId - _startAnalyzerId];
                }

                public bool HasAnalyzer(DiagnosticAnalyzer analyzer)
                {
                    Contract.ThrowIfNull(analyzer);
                    return _analyzerIdMap.Values.Any(dict => dict.ContainsKey(analyzer));
                }
            }
        }
    }
}
