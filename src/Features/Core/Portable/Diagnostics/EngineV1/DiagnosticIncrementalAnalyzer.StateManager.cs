// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV1
{
    internal partial class DiagnosticIncrementalAnalyzer
    {
        private const string RoslynLanguageServices = "Roslyn Language Services";

        private static readonly int s_stateTypeCount = Enum.GetNames(typeof(StateType)).Length;

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
            /// Return <see cref="DiagnosticAnalyzer"/>s for the given <see cref="Project"/>.
            /// </summary>
            public IEnumerable<DiagnosticAnalyzer> GetAnalyzers(Project project)
            {
                return _hostStates.GetAnalyzers(project.Language).Concat(_projectStates.GetAnalyzers(project));
            }

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
            /// Return <see cref="StateSet"/>s that are added as the given <see cref="Project"/>'s AnalyzerReferences.
            /// This will never create new <see cref="StateSet"/> but will return ones already created.
            /// </summary>
            public ImmutableArray<StateSet> GetBuildOnlyStateSets(object cache, Project project)
            {
                var stateSetCache = (IDictionary<Project, ImmutableArray<StateSet>>)cache;
                return stateSetCache.GetOrAdd(project, CreateBuildOnlyProjectStateSet);
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

            private ImmutableArray<StateSet> CreateBuildOnlyProjectStateSet(Project project)
            {
                var referenceIdentities = project.AnalyzerReferences.Select(r => _analyzerManager.GetAnalyzerReferenceIdentity(r)).ToSet();
                var stateSetMap = GetStateSets(project).ToDictionary(s => s.Analyzer, s => s);

                var stateSets = ImmutableArray.CreateBuilder<StateSet>();

                // we always include compiler analyzer in build only state
                var compilerAnalyzer = _analyzerManager.GetCompilerDiagnosticAnalyzer(project.Language);
                StateSet compilerStateSet;
                if (stateSetMap.TryGetValue(compilerAnalyzer, out compilerStateSet))
                {
                    stateSets.Add(compilerStateSet);
                }

                var analyzerMap = _analyzerManager.GetHostDiagnosticAnalyzersPerReference(project.Language);
                foreach (var kv in analyzerMap)
                {
                    var identity = kv.Key;
                    if (!referenceIdentities.Contains(identity))
                    {
                        // it is from host analyzer package rather than project analyzer reference
                        // which build doesn't have
                        continue;
                    }

                    // if same analyzer exists both in host (vsix) and in analyzer reference,
                    // we include it in build only analyzer.
                    foreach (var analyzer in kv.Value)
                    {
                        StateSet stateSet;
                        if (stateSetMap.TryGetValue(analyzer, out stateSet) && stateSet != compilerStateSet)
                        {
                            stateSets.Add(stateSet);
                        }
                    }
                }

                return stateSets.ToImmutable();
            }

            private void RaiseProjectAnalyzerReferenceChanged(ProjectAnalyzerReferenceChangedEventArgs args)
            {
                ProjectAnalyzerReferenceChanged?.Invoke(this, args);
            }

            private static ImmutableDictionary<DiagnosticAnalyzer, StateSet> CreateAnalyzerMap(
                HostAnalyzerManager analyzerManager, string language, IEnumerable<ImmutableArray<DiagnosticAnalyzer>> analyzerCollection)
            {
                var compilerAnalyzer = analyzerManager.GetCompilerDiagnosticAnalyzer(language);

                var builder = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, StateSet>();
                foreach (var analyzers in analyzerCollection)
                {
                    foreach (var analyzer in analyzers)
                    {
                        // TODO: 
                        // #1, all de -duplication should move to HostAnalyzerManager
                        // #2, not sure whether de-duplication of analyzer itself makes sense. this can only happen
                        //     if user deliberately put same analyzer twice.
                        if (builder.ContainsKey(analyzer))
                        {
                            continue;
                        }

                        var buildToolName = analyzer == compilerAnalyzer ?
                            PredefinedBuildTools.Live : GetBuildToolName(analyzerManager, language, analyzer);

                        builder.Add(analyzer, new StateSet(language, analyzer, buildToolName));
                    }
                }

                return builder.ToImmutable();
            }

            private static string GetBuildToolName(HostAnalyzerManager analyzerManager, string language, DiagnosticAnalyzer analyzer)
            {
                var packageName = analyzerManager.GetDiagnosticAnalyzerPackageName(language, analyzer);
                if (packageName == null)
                {
                    return null;
                }

                if (packageName == RoslynLanguageServices)
                {
                    return PredefinedBuildTools.Live;
                }

                return $"{analyzer.GetAnalyzerAssemblyName()} [{packageName}]";
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

                        if (!(set.Add(ValueTuple.Create(state.Language_TestingOnly, state.Name_TestingOnly))))
                        {
                            Contract.Fail();
                        }
                    }
                }
            }
        }
    }
}
