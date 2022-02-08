// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    internal partial class DiagnosticIncrementalAnalyzer
    {
        /// <summary>
        /// This is in charge of anything related to <see cref="StateSet"/>
        /// </summary>
        private partial class StateManager
        {
            private readonly Workspace _workspace;
            private readonly DiagnosticAnalyzerInfoCache _analyzerInfoCache;

            /// <summary>
            /// Analyzers supplied by the host (IDE). These are built-in to the IDE, the compiler, or from an installed IDE extension (VSIX). 
            /// Maps language name to the analyzers and their state.
            /// </summary>
            private ImmutableDictionary<HostAnalyzerStateSetKey, HostAnalyzerStateSets> _hostAnalyzerStateMap;

            /// <summary>
            /// Analyzers referenced by the project via a PackageReference.
            /// </summary>
            private readonly ConcurrentDictionary<ProjectId, ProjectAnalyzerStateSets> _projectAnalyzerStateMap;

            /// <summary>
            /// This will be raised whenever <see cref="StateManager"/> finds <see cref="Project.AnalyzerReferences"/> change
            /// </summary>
            public event EventHandler<ProjectAnalyzerReferenceChangedEventArgs>? ProjectAnalyzerReferenceChanged;

            public StateManager(Workspace workspace, DiagnosticAnalyzerInfoCache analyzerInfoCache)
            {
                _workspace = workspace;
                _analyzerInfoCache = analyzerInfoCache;

                _hostAnalyzerStateMap = ImmutableDictionary<HostAnalyzerStateSetKey, HostAnalyzerStateSets>.Empty;
                _projectAnalyzerStateMap = new ConcurrentDictionary<ProjectId, ProjectAnalyzerStateSets>(concurrencyLevel: 2, capacity: 10);
            }

            /// <summary>
            /// Return all <see cref="StateSet"/>.
            /// This will never create new <see cref="StateSet"/> but will return ones already created.
            /// </summary>
            public IEnumerable<StateSet> GetAllStateSets()
                => GetAllHostStateSets().Concat(GetAllProjectStateSets());

            /// <summary>
            /// Return <see cref="StateSet"/>s for the given <see cref="ProjectId"/>. 
            /// This will never create new <see cref="StateSet"/> but will return ones already created.
            /// </summary>
            public IEnumerable<StateSet> GetStateSets(ProjectId projectId)
            {
                var hostStateSets = GetAllHostStateSets();

                return _projectAnalyzerStateMap.TryGetValue(projectId, out var entry) ?
                    hostStateSets.Concat(entry.StateSetMap.Values) :
                    hostStateSets;
            }

            /// <summary>
            /// Return <see cref="StateSet"/>s for the given <see cref="Project"/>.
            /// This will never create new <see cref="StateSet"/> but will return ones already created.
            /// Difference with <see cref="GetStateSets(ProjectId)"/> is that 
            /// this will only return <see cref="StateSet"/>s that have same language as <paramref name="project"/>.
            /// </summary>
            public IEnumerable<StateSet> GetStateSets(Project project)
                => GetStateSets(project.Id).Where(s => s.Language == project.Language);

            /// <summary>
            /// Return <see cref="StateSet"/>s for the given <see cref="Project"/>. 
            /// This will either return already created <see cref="StateSet"/>s for the specific snapshot of <see cref="Project"/> or
            /// It will create new <see cref="StateSet"/>s for the <see cref="Project"/> and update internal state.
            /// 
            /// since this has a side-effect, this should never be called concurrently. and incremental analyzer (solution crawler) should guarantee that.
            /// </summary>
            public IEnumerable<StateSet> GetOrUpdateStateSets(Project project)
            {
                var projectStateSets = GetOrUpdateProjectStateSets(project);
                return GetOrCreateHostStateSets(project, projectStateSets).OrderedStateSets.Concat(projectStateSets.StateSetMap.Values);
            }

            /// <summary>
            /// Return <see cref="StateSet"/>s for the given <see cref="Project"/>. 
            /// This will either return already created <see cref="StateSet"/>s for the specific snapshot of <see cref="Project"/> or
            /// It will create new <see cref="StateSet"/>s for the <see cref="Project"/>.
            /// Unlike <see cref="GetOrUpdateStateSets(Project)"/>, this has no side effect.
            /// </summary>
            public IEnumerable<StateSet> GetOrCreateStateSets(Project project)
            {
                var projectStateSets = GetOrCreateProjectStateSets(project);
                return GetOrCreateHostStateSets(project, projectStateSets).OrderedStateSets.Concat(projectStateSets.StateSetMap.Values);
            }

            /// <summary>
            /// Return <see cref="StateSet"/> for the given <see cref="DiagnosticAnalyzer"/> in the context of <see cref="Project"/>.
            /// This will either return already created <see cref="StateSet"/> for the specific snapshot of <see cref="Project"/> or
            /// It will create new <see cref="StateSet"/> for the <see cref="Project"/>.
            /// This will not have any side effect.
            /// </summary>
            public StateSet? GetOrCreateStateSet(Project project, DiagnosticAnalyzer analyzer)
            {
                var projectStateSets = GetOrCreateProjectStateSets(project);
                if (projectStateSets.StateSetMap.TryGetValue(analyzer, out var stateSet))
                {
                    return stateSet;
                }

                var hostStateSetMap = GetOrCreateHostStateSets(project, projectStateSets).StateSetMap;
                if (hostStateSetMap.TryGetValue(analyzer, out stateSet))
                {
                    return stateSet;
                }

                return null;
            }

            /// <summary>
            /// Return <see cref="StateSet"/>s that are added as the given <see cref="Project"/>'s AnalyzerReferences.
            /// This will never create new <see cref="StateSet"/> but will return ones already created.
            /// </summary>
            public ImmutableArray<StateSet> CreateBuildOnlyProjectStateSet(Project project)
            {
                var projectStateSets = project.SupportsCompilation ?
                    GetOrUpdateProjectStateSets(project) :
                    ProjectAnalyzerStateSets.Default;
                var hostStateSets = GetOrCreateHostStateSets(project, projectStateSets);

                if (!project.SupportsCompilation)
                {
                    // languages which don't use our compilation model but diagnostic framework,
                    // all their analyzer should be host analyzers. return all host analyzers
                    // for the language
                    return hostStateSets.OrderedStateSets;
                }

                var hostStateSetMap = hostStateSets.StateSetMap;

                // create project analyzer reference identity map
                var projectAnalyzerReferenceIds = project.AnalyzerReferences.Select(r => r.Id).ToSet();

                // create build only stateSet array
                var stateSets = ImmutableArray.CreateBuilder<StateSet>();

                // include compiler analyzer in build only state, if available
                StateSet? compilerStateSet = null;
                var hostAnalyzers = project.Solution.State.Analyzers;
                var compilerAnalyzer = hostAnalyzers.GetCompilerDiagnosticAnalyzer(project.Language);
                if (compilerAnalyzer != null && hostStateSetMap.TryGetValue(compilerAnalyzer, out compilerStateSet))
                {
                    stateSets.Add(compilerStateSet);
                }

                // now add all project analyzers
                stateSets.AddRange(projectStateSets.StateSetMap.Values);

                // now add analyzers that exist in both host and project
                var hostAnalyzersById = hostAnalyzers.GetOrCreateHostDiagnosticAnalyzersPerReference(project.Language);
                foreach (var (identity, analyzers) in hostAnalyzersById)
                {
                    if (!projectAnalyzerReferenceIds.Contains(identity))
                    {
                        // it is from host analyzer package rather than project analyzer reference
                        // which build doesn't have
                        continue;
                    }

                    // if same analyzer exists both in host (vsix) and in analyzer reference,
                    // we include it in build only analyzer.
                    foreach (var analyzer in analyzers)
                    {
                        if (hostStateSetMap.TryGetValue(analyzer, out var stateSet) && stateSet != compilerStateSet)
                        {
                            stateSets.Add(stateSet);
                        }
                    }
                }

                return stateSets.ToImmutable();
            }

            /// <summary>
            /// Determines if any of the state sets in <see cref="GetAllHostStateSets()"/> match a specified predicate.
            /// </summary>
            /// <remarks>
            /// This method avoids the performance overhead of calling <see cref="GetAllHostStateSets()"/> for the
            /// specific case where the result is only used for testing if any element meets certain conditions.
            /// </remarks>
            public bool HasAnyHostStateSet<TArg>(Func<StateSet, TArg, bool> match, TArg arg)
            {
                foreach (var (_, hostStateSet) in _hostAnalyzerStateMap)
                {
                    foreach (var stateSet in hostStateSet.OrderedStateSets)
                    {
                        if (match(stateSet, arg))
                            return true;
                    }
                }

                return false;
            }

            /// <summary>
            /// Determines if any of the state sets in <see cref="_projectAnalyzerStateMap"/> for a specific project
            /// match a specified predicate.
            /// </summary>
            /// <remarks>
            /// <para>This method avoids the performance overhead of calling <see cref="GetStateSets(Project)"/> for the
            /// specific case where the result is only used for testing if any element meets certain conditions.</para>
            ///
            /// <para>Note that host state sets (i.e. ones retured by <see cref="GetAllHostStateSets()"/> are not tested
            /// by this method.</para>
            /// </remarks>
            public bool HasAnyProjectStateSet<TArg>(ProjectId projectId, Func<StateSet, TArg, bool> match, TArg arg)
            {
                if (_projectAnalyzerStateMap.TryGetValue(projectId, out var entry))
                {
                    foreach (var (_, stateSet) in entry.StateSetMap)
                    {
                        if (match(stateSet, arg))
                            return true;
                    }
                }

                return false;
            }

            public bool OnProjectRemoved(IEnumerable<StateSet> stateSets, ProjectId projectId)
            {
                var removed = false;
                foreach (var stateSet in stateSets)
                {
                    removed |= stateSet.OnProjectRemoved(projectId);
                }

                _projectAnalyzerStateMap.TryRemove(projectId, out _);
                return removed;
            }

            private void RaiseProjectAnalyzerReferenceChanged(ProjectAnalyzerReferenceChangedEventArgs args)
                => ProjectAnalyzerReferenceChanged?.Invoke(this, args);

            private static ImmutableDictionary<DiagnosticAnalyzer, StateSet> CreateStateSetMap(
                string language,
                IEnumerable<ImmutableArray<DiagnosticAnalyzer>> analyzerCollection,
                bool includeFileContentLoadAnalyzer)
            {
                var builder = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, StateSet>();

                if (includeFileContentLoadAnalyzer)
                {
                    builder.Add(FileContentLoadAnalyzer.Instance, new StateSet(language, FileContentLoadAnalyzer.Instance, PredefinedBuildTools.Live));
                }

                foreach (var analyzers in analyzerCollection)
                {
                    foreach (var analyzer in analyzers)
                    {
                        Debug.Assert(analyzer != FileContentLoadAnalyzer.Instance);

                        // TODO: 
                        // #1, all de-duplication should move to DiagnosticAnalyzerInfoCache
                        // #2, not sure whether de-duplication of analyzer itself makes sense. this can only happen
                        //     if user deliberately put same analyzer twice.
                        if (builder.ContainsKey(analyzer))
                        {
                            continue;
                        }

                        var buildToolName = analyzer.IsBuiltInAnalyzer() ?
                            PredefinedBuildTools.Live : analyzer.GetAnalyzerAssemblyName();

                        builder.Add(analyzer, new StateSet(language, analyzer, buildToolName));
                    }
                }

                return builder.ToImmutable();
            }

            [Conditional("DEBUG")]
            private static void VerifyUniqueStateNames(IEnumerable<StateSet> stateSets)
            {
                // Ensure diagnostic state name is indeed unique.
                var set = new HashSet<ValueTuple<string, string>>();

                foreach (var stateSet in stateSets)
                {
                    Contract.ThrowIfFalse(set.Add((stateSet.Language, stateSet.StateName)));
                }
            }

            [Conditional("DEBUG")]
            private void VerifyProjectDiagnosticStates(IEnumerable<StateSet> stateSets)
            {
                // We do not de-duplicate analyzer instances across host and project analyzers.
                var projectAnalyzers = stateSets.Select(state => state.Analyzer).ToImmutableHashSet();

                var hostStates = GetAllHostStateSets().Where(state => !projectAnalyzers.Contains(state.Analyzer));

                VerifyUniqueStateNames(hostStates.Concat(stateSets));
            }

            private readonly struct HostAnalyzerStateSetKey : IEquatable<HostAnalyzerStateSetKey>
            {
                public HostAnalyzerStateSetKey(string language, IReadOnlyList<AnalyzerReference> analyzerReferences)
                {
                    Language = language;
                    AnalyzerReferences = analyzerReferences;
                }

                public string Language { get; }
                public IReadOnlyList<AnalyzerReference> AnalyzerReferences { get; }

                public bool Equals(HostAnalyzerStateSetKey other)
                    => Language == other.Language && AnalyzerReferences == other.AnalyzerReferences;

                public override bool Equals(object? obj)
                    => obj is HostAnalyzerStateSetKey key && Equals(key);

                public override int GetHashCode()
                    => Hash.Combine(Language.GetHashCode(), AnalyzerReferences.GetHashCode());
            }
        }
    }
}
