// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
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

                return _projectAnalyzerStateMap.TryGetValue(projectId, out var entry)
                    ? hostStateSets.Concat(entry.StateSetMap.Values)
                    : hostStateSets;
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
            public ImmutableArray<StateSet> GetOrUpdateStateSets(Project project)
            {
                var projectStateSets = GetOrUpdateProjectStateSets(project);
                return GetOrCreateHostStateSets(project, projectStateSets).OrderedStateSets.AddRange(projectStateSets.StateSetMap.Values);
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
                bool includeWorkspacePlaceholderAnalyzers)
            {
                var builder = ImmutableDictionary.CreateBuilder<DiagnosticAnalyzer, StateSet>();

                if (includeWorkspacePlaceholderAnalyzers)
                {
                    builder.Add(FileContentLoadAnalyzer.Instance, new StateSet(language, FileContentLoadAnalyzer.Instance));
                    builder.Add(GeneratorDiagnosticsPlaceholderAnalyzer.Instance, new StateSet(language, GeneratorDiagnosticsPlaceholderAnalyzer.Instance));
                }

                foreach (var analyzers in analyzerCollection)
                {
                    foreach (var analyzer in analyzers)
                    {
                        Debug.Assert(analyzer != FileContentLoadAnalyzer.Instance && analyzer != GeneratorDiagnosticsPlaceholderAnalyzer.Instance);

                        // TODO: 
                        // #1, all de-duplication should move to DiagnosticAnalyzerInfoCache
                        // #2, not sure whether de-duplication of analyzer itself makes sense. this can only happen
                        //     if user deliberately put same analyzer twice.
                        if (builder.ContainsKey(analyzer))
                        {
                            continue;
                        }

                        builder.Add(analyzer, new StateSet(language, analyzer));
                    }
                }

                return builder.ToImmutable();
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
