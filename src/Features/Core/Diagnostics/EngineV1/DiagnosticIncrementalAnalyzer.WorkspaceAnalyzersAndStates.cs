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
            /// Maintains all workspace diagnostic analyzers (with diagnostic states), which are enabled for all projects in the workspace.
            /// </summary>
            private partial class WorkspaceAnalyzersAndStates
            {
                private readonly AnalyzerManager _analyzerManager;
                private ImmutableDictionary<string, PerLanguageAnalyzersAndStates> _perLanguageAnalyzersAndStatesMap;

                public WorkspaceAnalyzersAndStates(AnalyzerManager analyzerManager)
                {
                    _analyzerManager = analyzerManager;
                    _perLanguageAnalyzersAndStatesMap = ImmutableDictionary<string, PerLanguageAnalyzersAndStates>.Empty;
                }

                private static PerLanguageAnalyzersAndStates CreatePerLanguageAnalyzersAndStates(string language, WorkspaceAnalyzersAndStates @this)
                {
                    return new PerLanguageAnalyzersAndStates(@this._analyzerManager, language);
                }

                private PerLanguageAnalyzersAndStates GetOrCreatePerLanguageAnalyzersAndStates(string language)
                {
                    return ImmutableInterlocked.GetOrAdd(ref _perLanguageAnalyzersAndStatesMap, language, CreatePerLanguageAnalyzersAndStates, this);
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
                    foreach (var type in s_documentScopeStateTypes)
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
                        if (_perLanguageAnalyzersAndStatesMap.TryGetValue(languageOpt, out analyzersStates))
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
                    foreach (var analyzersAndStates in _perLanguageAnalyzersAndStatesMap.Values)
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
