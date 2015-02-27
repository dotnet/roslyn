// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV1
{
    internal partial class DiagnosticIncrementalAnalyzer
    {
        private static readonly int s_stateTypeCount = Enum.GetNames(typeof(StateType)).Count();

        /// <summary>
        /// This is in charge of anything related to <see cref="DiagnosticState"/>
        /// </summary>
        private partial class StateManager
        {
            private readonly HostAnalyzerManager _analyzerManager;

            private readonly HostStates _hostStates;
            private readonly ProjectStates _projectStates;

            public StateManager(HostAnalyzerManager analyzerManager)
            {
                _analyzerManager = analyzerManager;

                _hostStates = new HostStates(this);
                _projectStates = new ProjectStates(this);
            }

            private HostAnalyzerManager AnalyzerManager { get { return _analyzerManager; } }

            /// <summary>
            /// This will be raised whenever <see cref="StateManager"/> finds <see cref="Project.AnalyzerReferences"/> change
            /// </summary>
            public event EventHandler<ProjectAnalyzerReferenceChangedEventArgs> ProjectAnalyzerReferenceChanged;

            /// <summary>
            /// Return <see cref="StateSet"/>s for the given <see cref="ProjectId"/>. 
            /// This will never create new <see cref="StateSet"/> but will return ones already created.
            /// </summary>
            public IEnumerable<StateSet> GetStateSets(ProjectId projectId)
            {
                return _hostStates.GetStateSets().Concat(_projectStates.GetStateSets(projectId));
            }

            /// <summary>
            /// Return <see cref="StateSet"/>s for the given <see cref="Project"/>.
            /// This will never create new <see cref="StateSet"/> but will return ones already created.
            /// Difference with <see cref="GetStateSets(ProjectId)"/> is that 
            /// this will only return <see cref="StateSet"/>s that have same language as <paramref name="project"/>.
            /// </summary>
            public IEnumerable<StateSet> GetStateSets(Project project)
            {
                return GetStateSets(project.Id).Where(s => s.Language == project.Language);
            }

            /// <summary>
            /// Return <see cref="StateSet"/>s for the given <see cref="Project"/>. 
            /// This will either return already created <see cref="StateSet"/>s for the specific snapshot of <see cref="Project"/> or
            /// It will create new <see cref="StateSet"/>s for the <see cref="Project"/> and update internal state.
            /// 
            /// since this has a side-effect, this should never be called concurrently. and incremental analyzer (solution crawler) should guarantee that.
            /// </summary>
            public IEnumerable<StateSet> GetOrUpdateStateSets(Project project)
            {
                return _hostStates.GetOrCreateStateSets(project.Language).Concat(_projectStates.GetOrUpdateStateSets(project));
            }

            /// <summary>
            /// Return <see cref="StateSet"/>s for the given <see cref="Project"/>. 
            /// This will either return already created <see cref="StateSet"/>s for the specific snapshot of <see cref="Project"/> or
            /// It will create new <see cref="StateSet"/>s for the <see cref="Project"/>.
            /// Unlike <see cref="GetOrUpdateStateSets(Project)"/>, this has no side effect.
            /// </summary>
            public IEnumerable<StateSet> GetOrCreateStateSets(Project project)
            {
                return _hostStates.GetOrCreateStateSets(project.Language).Concat(_projectStates.GetOrCreateStateSets(project));
            }

            /// <summary>
            /// Return <see cref="StateSet"/> for the given <see cref="DiagnosticAnalyzer"/> in the context of <see cref="Project"/>.
            /// This will either return already created <see cref="StateSet"/> for the specific snapshot of <see cref="Project"/> or
            /// It will create new <see cref="StateSet"/> for the <see cref="Project"/>.
            /// This will not have any side effect.
            /// </summary>
            public StateSet GetOrCreateStateSet(Project project, DiagnosticAnalyzer analyzer)
            {
                var stateSet = _hostStates.GetOrCreateStateSet(project.Language, analyzer);
                if (stateSet != null)
                {
                    return stateSet;
                }

                return _projectStates.GetOrCreateStateSet(project, analyzer);
            }

            /// <summary>
            /// Remove given <see cref="ProjectId"/>.
            /// </summary>
            public void RemoveStateSet(ProjectId projectId)
            {
                _projectStates.RemoveStateSet(projectId);
            }

            private void RaiseProjectAnalyzerReferenceChanged(ProjectAnalyzerReferenceChangedEventArgs args)
            {
                ProjectAnalyzerReferenceChanged?.Invoke(this, args);
            }

            private static ImmutableDictionary<DiagnosticAnalyzer, StateSet> CreateAnalyzerMap(
                    string language, IEnumerable<ImmutableArray<DiagnosticAnalyzer>> analyzerCollection)
            {
                var builder = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, StateSet>();
                foreach (var analyzers in analyzerCollection)
                {
                    foreach (var analyzer in analyzers)
                    {
                        // TODO: 
                        // #1, all de -duplication should move to HostAnalyzerManager
                        // #2, not sure whether de-duplicatoin of analyzer itself makes sense. this can only happen
                        //     if user delibrately put same analyzer twice.
                        if (builder.ContainsKey(analyzer))
                        {
                            continue;
                        }

                        builder.Add(analyzer, new StateSet(language, analyzer));
                    }
                }

                return builder.ToImmutable();
            }

            [Conditional("DEBUG")]
            private static void VerifyDiagnosticStates(IEnumerable<StateSet> stateSets)
            {
                // Ensure diagnostic state name is indeed unique.
                for (var i = 0; i < s_stateTypeCount; i++)
                {
                    var set = new HashSet<ValueTuple<string, string>>();

                    foreach (var stateSet in stateSets)
                    {
                        var state = stateSet.GetState((StateType)i);

                        Contract.Requires(set.Add(ValueTuple.Create(state.Language, state.Name)));
                    }
                }
            }
        }

        private class ProjectAnalyzerReferenceChangedEventArgs : EventArgs
        {
            public readonly Project Project;
            public readonly ImmutableArray<StateSet> Added;
            public readonly ImmutableArray<StateSet> Removed;

            public ProjectAnalyzerReferenceChangedEventArgs(Project project, ImmutableArray<StateSet> added, ImmutableArray<StateSet> removed)
            {
                Project = project;
                Added = added;
                Removed = removed;
            }
        }

        private class StateSet
        {
            private const string UserDiagnosticsPrefixTableName = "<UserDiagnostics>";

            private readonly string _language;
            private readonly DiagnosticAnalyzer _analyzer;

            private readonly DiagnosticState[] _state;

            public StateSet(string language, DiagnosticAnalyzer analyzer)
            {
                _language = language;
                _analyzer = analyzer;

                _state = CreateDiagnosticStates(language, analyzer);
            }

            public string Language => _language;
            public DiagnosticAnalyzer Analyzer => _analyzer;

            public DiagnosticState GetState(StateType stateType)
            {
                return _state[(int)stateType];
            }

            public void Remove(object key)
            {
                for (var stateType = 0; stateType < s_stateTypeCount; stateType++)
                {
                    _state[stateType].Remove(key);
                }
            }

            private static DiagnosticState[] CreateDiagnosticStates(string language, DiagnosticAnalyzer analyzer)
            {
                var states = new DiagnosticState[s_stateTypeCount];

                for (int stateType = 0; stateType < s_stateTypeCount; stateType++)
                {
                    var nameAndVersion = GetNameAndVersion(analyzer, (StateType)stateType);

                    var name = nameAndVersion.Item1;
                    var version = nameAndVersion.Item2;

                    states[stateType] = new DiagnosticState(name, version, language);
                }

                return states;
            }

            /// <summary>
            /// Get the unique state name for the given {type, provider} tuple.
            /// Note that this name is used by the underlying persistence stream of the corresponding <see cref="DiagnosticState"/> to Read/Write diagnostic data into the stream.
            /// If any two distinct {type, provider} tuples have the same diagnostic state name, we will end up sharing the persistence stream between them, leading to duplicate/missing/incorrect diagnostic data.
            /// </summary>
            private static ValueTuple<string, VersionStamp> GetNameAndVersion(DiagnosticAnalyzer analyzer, StateType type)
            {
                Contract.ThrowIfNull(analyzer);

                // Get the unique ID for given diagnostic analyzer.
                // note that we also put version stamp so that we can detect changed analyzer.
                var tuple = analyzer.GetUniqueIdForAnalyzer();
                return ValueTuple.Create(UserDiagnosticsPrefixTableName + "_" + type.ToString() + "_" + tuple.Item1, tuple.Item2);
            }
        }
    }
}
