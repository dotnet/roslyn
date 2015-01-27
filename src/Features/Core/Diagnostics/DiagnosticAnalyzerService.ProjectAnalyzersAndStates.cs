// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    using ProviderId = Int32;

    internal partial class DiagnosticAnalyzerService
    {
        internal partial class DiagnosticIncrementalAnalyzer
        {
            private partial class DiagnosticAnalyzersAndStates
            {
                /// <summary>
                /// Maintains per-project diagnostic analyzers and the corresponding diagnostic states.
                /// </summary>
                private class ProjectAnalyzersAndStates
                {
                    private readonly ImmutableDictionary<string, ImmutableDictionary<DiagnosticAnalyzer, ProviderId>> analyzerIdMap;
                    private readonly DiagnosticState[,] diagnosticStateMaps;
                    private readonly int startAnalyzerId;
                    private readonly int analyzerCount;
                    private readonly string projectLanguage;

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

                        this.startAnalyzerId = sharedWorkspaceAnalyzersCount;
                        this.projectLanguage = projectLanguage;
                        this.analyzerIdMap = CreateAnalyzerIdMap(projectSpecificAnalyzers, this.startAnalyzerId);
                        analyzerCount = analyzerIdMap.Values.Flatten().Count();
                        Contract.ThrowIfFalse(analyzerCount > 0);

                        this.diagnosticStateMaps = new DiagnosticState[stateTypeCount, analyzerCount];
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
                            Contract.ThrowIfFalse(analyzerCount > 0);
                            return analyzerCount;
                        }
                    }

                    public IEnumerable<Tuple<DiagnosticState, ProviderId, StateType>> GetAllExistingDiagnosticStates()
                    {
                        var current = SpecializedCollections.EmptyEnumerable<Tuple<DiagnosticState, ProviderId, StateType>>();
                        foreach (var type in DocumentScopeStateTypes)
                        {
                            current = current.Concat(GetAllExistingDiagnosticStates(type));
                        }

                        return current;
                    }

                    public IEnumerable<Tuple<DiagnosticState, ProviderId, StateType>> GetAllExistingDiagnosticStates(StateType type)
                    {
                        foreach (var analyzerAndId in this.analyzerIdMap.Values.Flatten())
                        {
                            var state = this.diagnosticStateMaps[(int)type, analyzerAndId.Value - startAnalyzerId];
                            yield return Tuple.Create(state, analyzerAndId.Value, type);
                        }
                    }

                    public IEnumerable<KeyValuePair<DiagnosticAnalyzer, ProviderId>> GetAllProviderAndIds()
                    {
                        return this.analyzerIdMap.Values.Flatten();
                    }

                    public ImmutableDictionary<string, IEnumerable<DiagnosticAnalyzer>> GetAllDiagnosticAnalyzers()
                    {
                        var analyzers = ImmutableDictionary.CreateBuilder<string, IEnumerable<DiagnosticAnalyzer>>();

                        foreach (var item in this.analyzerIdMap)
                        {
                            analyzers.Add(item.Key, item.Value.Keys);
                        }

                        return analyzers.ToImmutable();
                    }

                    public DiagnosticState GetOrCreateDiagnosticState(StateType stateType, ProviderId providerId, DiagnosticAnalyzer provider)
                    {
                        Contract.ThrowIfFalse(providerId >= this.startAnalyzerId);
                        Contract.ThrowIfFalse(providerId < this.startAnalyzerId + this.AnalyzerCount);

                        return DiagnosticAnalyzersAndStates.GetOrCreateDiagnosticState(this.diagnosticStateMaps, stateType, providerId - startAnalyzerId, providerId, provider, this.projectLanguage);
                    }

                    public DiagnosticState GetDiagnosticState(StateType stateType, ProviderId providerId)
                    {
                        Contract.ThrowIfFalse(providerId >= this.startAnalyzerId);
                        Contract.ThrowIfFalse(providerId < this.startAnalyzerId + this.AnalyzerCount);

                        return this.diagnosticStateMaps[(int)stateType, providerId - startAnalyzerId];
                    }

                    public bool HasAnalyzer(DiagnosticAnalyzer analyzer)
                    {
                        Contract.ThrowIfNull(analyzer);
                        return this.analyzerIdMap.Values.Any(dict => dict.ContainsKey(analyzer));
                    }
                }
            }
        }
    }
}
