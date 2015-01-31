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
                /// Maintains all workspace diagnostic analyzers (with diagnostic states), which are enabled for all projects in the workspace.
                /// </summary>
                private partial class WorkspaceAnalyzersAndStates
                {
                    private readonly ImmutableArray<AnalyzerReference> workspaceAnalyzers;
                    private ImmutableDictionary<string, PerLanguageAnalyzersAndStates> perLanguageAnalyzersAndStatesMap;

                    public WorkspaceAnalyzersAndStates(ImmutableArray<AnalyzerReference> workspaceAnalyzers)
                    {
                        this.workspaceAnalyzers = workspaceAnalyzers;
                        this.perLanguageAnalyzersAndStatesMap = ImmutableDictionary<string, PerLanguageAnalyzersAndStates>.Empty;
                    }

                    private static PerLanguageAnalyzersAndStates CreatePerLanguageAnalyzersAndStates(string language, WorkspaceAnalyzersAndStates @this)
                    {
                        return new PerLanguageAnalyzersAndStates(@this.workspaceAnalyzers, language);
                    }

                    private PerLanguageAnalyzersAndStates GetOrCreatePerLanguageAnalyzersAndStates(string language)
                    {
                        return ImmutableInterlocked.GetOrAdd(ref this.perLanguageAnalyzersAndStatesMap, language, CreatePerLanguageAnalyzersAndStates, this);
                    }

                    public int GetAnalyzerCount(string language)
                    {
                        var analyzersAndStates = this.GetOrCreatePerLanguageAnalyzersAndStates(language);
                        return analyzersAndStates.AnalyzerCount;
                    }

                    public bool HasAnalyzerReference(AnalyzerReference analyzerReference, string language)
                    {
                        var analyzersAndStates = this.GetOrCreatePerLanguageAnalyzersAndStates(language);
                        return analyzersAndStates.HasAnalyzerReference(analyzerReference);
                    }

                    public bool HasAnalyzer(DiagnosticAnalyzer analyzer, string language)
                    {
                        if (analyzer == null)
                        {
                            return false;
                        }

                        var analyzersAndStates = this.GetOrCreatePerLanguageAnalyzersAndStates(language);
                        return analyzersAndStates.HasAnalyzer(analyzer);
                    }

                    public IEnumerable<Tuple<DiagnosticState, ProviderId, StateType>> GetAllExistingDiagnosticStates(string languageOpt)
                    {
                        var current = SpecializedCollections.EmptyEnumerable<Tuple<DiagnosticState, ProviderId, StateType>>();
                        foreach (var type in DocumentScopeStateTypes)
                        {
                            current = current.Concat(GetAllExistingDiagnosticStates(type, languageOpt));
                        }

                        return current;
                    }

                    public IEnumerable<Tuple<DiagnosticState, ProviderId, StateType>> GetAllExistingDiagnosticStates(StateType type, string languageOpt)
                    {
                        if (languageOpt != null)
                        {
                            PerLanguageAnalyzersAndStates analyzersStates;
                            if (this.perLanguageAnalyzersAndStatesMap.TryGetValue(languageOpt, out analyzersStates))
                            {
                                return analyzersStates.GetAllExistingDiagnosticStates(type);
                            }
                            else
                            {
                                return SpecializedCollections.EmptyEnumerable<Tuple<DiagnosticState, ProviderId, StateType>>();
                            }
                        }

                        // This might be a removed or closed document/project/solution.
                        // Return all existing states.
                        var current = SpecializedCollections.EmptyEnumerable<Tuple<DiagnosticState, ProviderId, StateType>>();
                        foreach (var analyzersAndStates in perLanguageAnalyzersAndStatesMap.Values)
                        {
                            current = current.Concat(analyzersAndStates.GetAllExistingDiagnosticStates(type));
                        }

                        return current;
                    }

                    public IEnumerable<KeyValuePair<DiagnosticAnalyzer, ProviderId>> GetAllProviderAndIds(string language)
                    {
                        var analyzersAndStates = this.GetOrCreatePerLanguageAnalyzersAndStates(language);
                        return analyzersAndStates.GetAllProviderAndIds();
                    }

                    public ImmutableDictionary<string, IEnumerable<DiagnosticAnalyzer>> GetAllDiagnosticAnalyzers(string language)
                    {
                        var analyzersAndStates = this.GetOrCreatePerLanguageAnalyzersAndStates(language);
                        return analyzersAndStates.GetAllDiagnosticAnalyzers();
                    }

                    public DiagnosticState GetOrCreateDiagnosticState(StateType stateType, ProviderId providerId, DiagnosticAnalyzer provider, string language)
                    {
                        var analyzersAndStates = this.GetOrCreatePerLanguageAnalyzersAndStates(language);
                        return analyzersAndStates.GetOrCreateDiagnosticState(stateType, providerId, provider);
                    }

                    public DiagnosticState GetDiagnosticState(StateType stateType, ProviderId providerId, string language)
                    {
                        var analyzersAndStates = this.GetOrCreatePerLanguageAnalyzersAndStates(language);
                        return analyzersAndStates.GetDiagnosticState(stateType, providerId);
                    }
                }
            }
        }
    }
}
