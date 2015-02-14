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
            private partial class WorkspaceAnalyzersAndStates
            {
                /// <summary>
                /// Maintains all workspace diagnostic analyzers (with diagnostic states) for a specific language.
                /// </summary>
                private class PerLanguageAnalyzersAndStates
                {
                    private readonly string _language;
                    private readonly ImmutableDictionary<string, ImmutableDictionary<DiagnosticAnalyzer, ProviderId>> _diagnosticAnalyzerIdMap;
                    private readonly int _analyzerCount;
                    private readonly DiagnosticState[,] _diagnosticStateMaps;

                    public PerLanguageAnalyzersAndStates(AnalyzerManager analyzerManager, string language)
                    {
                        _language = language;

                        // TODO: dynamically re-order providers so that cheap one runs first and slower runs later.
                        _diagnosticAnalyzerIdMap = CreateAnalyzerIdMap(analyzerManager, language);
                        _analyzerCount = _diagnosticAnalyzerIdMap.Values.Flatten().Count();
                        _diagnosticStateMaps = new DiagnosticState[s_stateTypeCount, _analyzerCount];
                    }

                    private static ImmutableDictionary<string, ImmutableDictionary<DiagnosticAnalyzer, ProviderId>> CreateAnalyzerIdMap(
                        AnalyzerManager analyzerManager, string language)
                    {
                        var index = 0;
                        var map = ImmutableDictionary.CreateBuilder<string, ImmutableDictionary<DiagnosticAnalyzer, ProviderId>>();

                        foreach (var kv in analyzerManager.GetHostDiagnosticAnalyzersPerReference(language))
                        {
                            var perAnalyzerMap = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, ProviderId>();

                            var referenceIdentity = kv.Key;
                            var perLanguageAnalyzers = kv.Value;

                            if (perLanguageAnalyzers.Length > 0)
                            {
                                foreach (var analyzer in perLanguageAnalyzers)
                                {
                                    var analyzerId = index++;
                                    perAnalyzerMap.Add(analyzer, analyzerId);
                                }

                                map.Add(referenceIdentity, perAnalyzerMap.ToImmutable());
                            }
                        }

                        return map.ToImmutable();
                    }

                    public int AnalyzerCount
                    {
                        get { return _analyzerCount; }
                    }

                    public bool HasAnalyzerReference(AnalyzerReference analyzerReference)
                    {
                        Contract.ThrowIfNull(analyzerReference);

                        if (analyzerReference is AnalyzerFileReference)
                        {
                            // Filter out duplicate analyzer references with same assembly name/full path.
                            return analyzerReference.Display != null && _diagnosticAnalyzerIdMap.ContainsKey(analyzerReference.Display);
                        }
                        else
                        {
                            // For non-file references, we will check individual DiagnosticAnalyzer instances for duplicates.
                            return false;
                        }
                    }

                    public bool HasAnalyzer(DiagnosticAnalyzer analyzer)
                    {
                        Contract.ThrowIfNull(analyzer);
                        return _diagnosticAnalyzerIdMap.Values.Any(dict => dict.ContainsKey(analyzer));
                    }

                    public IEnumerable<Tuple<DiagnosticState, ProviderId, StateType>> GetAllExistingDiagnosticStates(StateType type)
                    {
                        foreach (var analyzerAndId in _diagnosticAnalyzerIdMap.Values.Flatten())
                        {
                            yield return Tuple.Create(_diagnosticStateMaps[(int)type, analyzerAndId.Value], analyzerAndId.Value, type);
                        }
                    }

                    public IEnumerable<KeyValuePair<DiagnosticAnalyzer, ProviderId>> GetAllProviderAndIds()
                    {
                        return _diagnosticAnalyzerIdMap.Values.Flatten();
                    }

                    public DiagnosticState GetOrCreateDiagnosticState(StateType stateType, ProviderId providerId, DiagnosticAnalyzer provider)
                    {
                        Contract.ThrowIfFalse(providerId >= 0);
                        Contract.ThrowIfFalse(providerId < this.AnalyzerCount);

                        return DiagnosticAnalyzersAndStates.GetOrCreateDiagnosticState(_diagnosticStateMaps, stateType, providerId, providerId, provider, _language);
                    }

                    public DiagnosticState GetDiagnosticState(StateType stateType, ProviderId providerId)
                    {
                        Contract.ThrowIfFalse(providerId >= 0);
                        Contract.ThrowIfFalse(providerId < this.AnalyzerCount);

                        return _diagnosticStateMaps[(int)stateType, providerId];
                    }
                }
            }
        }
    }
}
