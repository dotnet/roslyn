﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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
        private partial class StateManager
        {
            private readonly struct ProjectAnalyzerStateSets
            {
                public readonly IReadOnlyList<AnalyzerReference> AnalyzerReferences;

                // maps analyzer reference id to list of analyzers loaded from the reference
                public readonly ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>> MapPerReferences;

                public readonly ImmutableDictionary<DiagnosticAnalyzer, StateSet> StateSetMap;

                public ProjectAnalyzerStateSets(
                    IReadOnlyList<AnalyzerReference> analyzerReferences,
                    ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>> mapPerReferences,
                    ImmutableDictionary<DiagnosticAnalyzer, StateSet> analyzerMap)
                {
                    Contract.ThrowIfNull(analyzerReferences);
                    Contract.ThrowIfNull(mapPerReferences);
                    Contract.ThrowIfNull(analyzerMap);

                    AnalyzerReferences = analyzerReferences;
                    MapPerReferences = mapPerReferences;
                    StateSetMap = analyzerMap;
                }
            }

            public IEnumerable<StateSet> GetAllProjectStateSets()
            {
                // return existing state sets
                return _projectAnalyzerStateMap.Values.SelectMany(e => e.StateSetMap.Values).ToImmutableArray();
            }

            private ImmutableDictionary<DiagnosticAnalyzer, StateSet>? TryGetProjectStateSetMap(Project project)
            {
                // check if the analyzer references have changed since the last time we updated the map:
                if (_projectAnalyzerStateMap.TryGetValue(project.Id, out var entry) &&
                    entry.AnalyzerReferences.Equals(project.AnalyzerReferences))
                {
                    return entry.StateSetMap;
                }

                return null;
            }

            private ImmutableDictionary<DiagnosticAnalyzer, StateSet> GetOrCreateProjectStateSetMap(Project project)
            {
                // if we can't use cached one, we will create a new analyzer map. which is a bit of waste since
                // we will create new StateSet for all analyzers. but since this only happens when project analyzer references
                // are changed, I believe it is acceptable to have a bit of waste for simplicity.
                return TryGetProjectStateSetMap(project) ?? CreateProjectStateSetMap(project);
            }

            private ImmutableDictionary<DiagnosticAnalyzer, StateSet> CreateProjectStateSetMap(Project project)
            {
                if (project.AnalyzerReferences.Count == 0)
                {
                    return ImmutableDictionary<DiagnosticAnalyzer, StateSet>.Empty;
                }

                var analyzersPerReference = _analyzerInfoCache.CreateProjectDiagnosticAnalyzersPerReference(project);
                if (analyzersPerReference.Count == 0)
                {
                    return ImmutableDictionary<DiagnosticAnalyzer, StateSet>.Empty;
                }

                return CreateStateSetMap(project.Language, analyzersPerReference.Values, includeFileContentLoadAnalyzer: false);
            }

            private ImmutableDictionary<DiagnosticAnalyzer, StateSet> GetOrUpdateProjectAnalyzerMap(Project project)
                => TryGetProjectStateSetMap(project) ?? UpdateProjectAnalyzerMap(project);

            /// <summary>
            /// Updates the map to the given project snapshot.
            /// </summary>
            private ImmutableDictionary<DiagnosticAnalyzer, StateSet> UpdateProjectAnalyzerMap(Project project)
            {
                var newAnalyzersPerReference = _analyzerInfoCache.CreateProjectDiagnosticAnalyzersPerReference(project);
                var newMap = CreateStateSetMap(project.Language, newAnalyzersPerReference.Values, includeFileContentLoadAnalyzer: false);

                RaiseProjectAnalyzerReferenceChangedIfNeeded(project, newAnalyzersPerReference, newMap);

                // update cache. 
                _projectAnalyzerStateMap[project.Id] = new ProjectAnalyzerStateSets(project.AnalyzerReferences, newAnalyzersPerReference, newMap);

                VerifyProjectDiagnosticStates(newMap.Values);

                return newMap;
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

            private ImmutableArray<StateSet> DiffStateSets(
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
                    var referenceIdentity = _analyzerInfoCache.GetAnalyzerReferenceIdentity(reference);
                    // check duplication
                    if (!mapPerReference.TryGetValue(referenceIdentity, out var analyzers))
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
