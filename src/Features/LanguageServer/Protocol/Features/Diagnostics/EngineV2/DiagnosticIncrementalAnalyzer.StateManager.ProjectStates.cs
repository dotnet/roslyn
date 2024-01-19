// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
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
                    ImmutableArray<AnalyzerReference>.Empty,
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
                return _projectAnalyzerStateMap.Values.SelectManyAsArray(e => e.StateSetMap.Values);
            }

            private ProjectAnalyzerStateSets? TryGetProjectStateSets(Project project)
            {
                // check if the analyzer references have changed since the last time we updated the map:
                if (_projectAnalyzerStateMap.TryGetValue(project.Id, out var entry) &&
                    entry.AnalyzerReferences.SequenceEqual(project.AnalyzerReferences))
                {
                    return entry;
                }

                return null;
            }

            private ProjectAnalyzerStateSets GetOrCreateProjectStateSets(Project project)
            {
                // if we can't use cached one, we will create a new analyzer map. which is a bit of waste since
                // we will create new StateSet for all analyzers. but since this only happens when project analyzer references
                // are changed, I believe it is acceptable to have a bit of waste for simplicity.
                return TryGetProjectStateSets(project) ?? CreateProjectStateSets(project);
            }

            private ProjectAnalyzerStateSets GetOrUpdateProjectStateSets(Project project)
                => TryGetProjectStateSets(project) ?? UpdateProjectStateSets(project);

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
            private ProjectAnalyzerStateSets UpdateProjectStateSets(Project project)
            {
                var projectStateSets = CreateProjectStateSets(project);

                RaiseProjectAnalyzerReferenceChangedIfNeeded(project, projectStateSets.MapPerReferences, projectStateSets.StateSetMap);

                // update cache. 
                _projectAnalyzerStateMap[project.Id] = projectStateSets;

                return projectStateSets;
            }

            private void RaiseProjectAnalyzerReferenceChangedIfNeeded(
                Project project,
                ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>> newMapPerReference,
                ImmutableDictionary<DiagnosticAnalyzer, StateSet> newMap)
            {
                if (!_projectAnalyzerStateMap.TryGetValue(project.Id, out var entry))
                {
                    // no previous references and we still don't have any references
                    if (newMap.Count == 0)
                    {
                        return;
                    }

                    // new reference added
                    RaiseProjectAnalyzerReferenceChanged(
                        new ProjectAnalyzerReferenceChangedEventArgs(project, newMap.Values.ToImmutableArrayOrEmpty(), ImmutableArray<StateSet>.Empty));
                    return;
                }

                Debug.Assert(!entry.AnalyzerReferences.Equals(project.AnalyzerReferences));

                // there has been change. find out what has changed
                var addedStates = DiffStateSets(project.AnalyzerReferences.Except(entry.AnalyzerReferences), newMapPerReference, newMap);
                var removedStates = DiffStateSets(entry.AnalyzerReferences.Except(project.AnalyzerReferences), entry.MapPerReferences, entry.StateSetMap);

                // nothing has changed
                if (addedStates.Length == 0 && removedStates.Length == 0)
                {
                    return;
                }

                RaiseProjectAnalyzerReferenceChanged(
                    new ProjectAnalyzerReferenceChangedEventArgs(project, addedStates, removedStates));
            }

            private static ImmutableArray<StateSet> DiffStateSets(
                IEnumerable<AnalyzerReference> references,
                ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>> mapPerReference,
                ImmutableDictionary<DiagnosticAnalyzer, StateSet> map)
            {
                if (mapPerReference.Count == 0 || map.Count == 0)
                {
                    // nothing to diff
                    return ImmutableArray<StateSet>.Empty;
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

                return builder.ToImmutable();
            }
        }
    }
}
