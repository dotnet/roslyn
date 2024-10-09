// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    internal partial class DiagnosticIncrementalAnalyzer
    {
        private partial class StateManager
        {
            private readonly struct ProjectAnalyzerStateSets
            {
                public static readonly ProjectAnalyzerStateSets Default = new(
                    [],
                    ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>>.Empty,
                    ImmutableDictionary<DiagnosticAnalyzer, StateSet>.Empty,
                    SkippedHostAnalyzersInfo.Empty);

                public readonly IReadOnlyList<AnalyzerReference> AnalyzerReferences;

                // maps analyzer reference id to list of analyzers loaded from the reference
                public readonly ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>> MapPerReferences;

                public readonly ImmutableDictionary<DiagnosticAnalyzer, StateSet> StateSetMap;

                public readonly SkippedHostAnalyzersInfo SkippedAnalyzersInfo;

                internal ProjectAnalyzerStateSets(
                    IReadOnlyList<AnalyzerReference> analyzerReferences,
                    ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>> mapPerReferences,
                    ImmutableDictionary<DiagnosticAnalyzer, StateSet> stateSetMap,
                    SkippedHostAnalyzersInfo skippedAnalyzersInfo)
                {
                    AnalyzerReferences = analyzerReferences;
                    MapPerReferences = mapPerReferences;
                    StateSetMap = stateSetMap;
                    SkippedAnalyzersInfo = skippedAnalyzersInfo;
                }
            }

            public IEnumerable<StateSet> GetAllProjectStateSets()
            {
                // return existing state sets
                // No need to use _projectAnalyzerStateMapGuard during reads of _projectAnalyzerStateMap
                return _projectAnalyzerStateMap.Values.SelectManyAsArray(e => e.StateSetMap.Values);
            }

            private ProjectAnalyzerStateSets? TryGetProjectStateSets(Project project)
            {
                // check if the analyzer references have changed since the last time we updated the map:
                // No need to use _projectAnalyzerStateMapGuard during reads of _projectAnalyzerStateMap
                if (_projectAnalyzerStateMap.TryGetValue(project.Id, out var entry) &&
                    entry.AnalyzerReferences.SequenceEqual(project.AnalyzerReferences))
                {
                    return entry;
                }

                return null;
            }

            private async Task<ProjectAnalyzerStateSets> GetOrCreateProjectStateSetsAsync(Project project, CancellationToken cancellationToken)
                => TryGetProjectStateSets(project) ?? await UpdateProjectStateSetsAsync(project, cancellationToken).ConfigureAwait(false);

            /// <summary>
            /// Creates a new project state sets.
            /// </summary>
            private ProjectAnalyzerStateSets CreateProjectStateSets(Project project)
            {
                if (project.AnalyzerReferences.Count == 0)
                {
                    return ProjectAnalyzerStateSets.Default;
                }

                var hostAnalyzers = project.Solution.SolutionState.Analyzers;
                var analyzersPerReference = hostAnalyzers.CreateProjectDiagnosticAnalyzersPerReference(project);
                if (analyzersPerReference.Count == 0)
                {
                    return ProjectAnalyzerStateSets.Default;
                }

                var newMap = CreateStateSetMap(project.Language, analyzersPerReference.Values, includeWorkspacePlaceholderAnalyzers: false);
                var skippedAnalyzersInfo = project.GetSkippedAnalyzersInfo(_analyzerInfoCache);
                return new ProjectAnalyzerStateSets(project.AnalyzerReferences, analyzersPerReference, newMap, skippedAnalyzersInfo);
            }

            /// <summary>
            /// Updates the map to the given project snapshot.
            /// </summary>
            private async Task<ProjectAnalyzerStateSets> UpdateProjectStateSetsAsync(Project project, CancellationToken cancellationToken)
            {
                ProjectAnalyzerReferenceChangedEventArgs? analyzerReferenceChangedEventArgs = null;
                ProjectAnalyzerStateSets? projectStateSets;

                // This code is called concurrently for a project, so the guard prevents duplicated effort calculating StateSets.
                using (await _projectAnalyzerStateMapGuard.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
                {
                    projectStateSets = TryGetProjectStateSets(project);

                    if (projectStateSets == null)
                    {
                        projectStateSets = CreateProjectStateSets(project);

                        analyzerReferenceChangedEventArgs = GetProjectAnalyzerReferenceChangedEventArgs(project, projectStateSets.Value.MapPerReferences, projectStateSets.Value.StateSetMap);

                        // update cache. 
                        _projectAnalyzerStateMap = _projectAnalyzerStateMap.SetItem(project.Id, projectStateSets.Value);
                    }
                }

                if (analyzerReferenceChangedEventArgs != null)
                    RaiseProjectAnalyzerReferenceChanged(analyzerReferenceChangedEventArgs);

                return projectStateSets.Value;
            }

            private ProjectAnalyzerReferenceChangedEventArgs? GetProjectAnalyzerReferenceChangedEventArgs(
                Project project,
                ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>> newMapPerReference,
                ImmutableDictionary<DiagnosticAnalyzer, StateSet> newMap)
            {
                // No need to use _projectAnalyzerStateMapGuard during reads of _projectAnalyzerStateMap
                if (!_projectAnalyzerStateMap.TryGetValue(project.Id, out var entry))
                {
                    // no previous references and we still don't have any references
                    if (newMap.Count == 0)
                    {
                        return null;
                    }

                    // new reference added
                    return new ProjectAnalyzerReferenceChangedEventArgs(project, newMap.Values.ToImmutableArrayOrEmpty(), []);
                }

                Debug.Assert(!entry.AnalyzerReferences.Equals(project.AnalyzerReferences));

                // there has been change. find out what has changed
                var addedStates = DiffStateSets(project.AnalyzerReferences.Except(entry.AnalyzerReferences), newMapPerReference, newMap);
                var removedStates = DiffStateSets(entry.AnalyzerReferences.Except(project.AnalyzerReferences), entry.MapPerReferences, entry.StateSetMap);

                // nothing has changed
                if (addedStates.Length == 0 && removedStates.Length == 0)
                {
                    return null;
                }

                return new ProjectAnalyzerReferenceChangedEventArgs(project, addedStates, removedStates);
            }

            private static ImmutableArray<StateSet> DiffStateSets(
                IEnumerable<AnalyzerReference> references,
                ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>> mapPerReference,
                ImmutableDictionary<DiagnosticAnalyzer, StateSet> map)
            {
                if (mapPerReference.Count == 0 || map.Count == 0)
                {
                    // nothing to diff
                    return [];
                }

                var builder = ImmutableArray.CreateBuilder<StateSet>();
                foreach (var reference in references)
                {
                    // check duplication
                    if (!mapPerReference.TryGetValue(reference.Id, out var analyzers))
                    {
                        continue;
                    }

                    // okay, this is real reference. get stateset
                    foreach (var analyzer in analyzers)
                    {
                        if (!map.TryGetValue(analyzer, out var set))
                        {
                            continue;
                        }

                        builder.Add(set);
                    }
                }

                return builder.ToImmutableAndClear();
            }
        }
    }
}
