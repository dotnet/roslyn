// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
            public IEnumerable<StateSet> GetProjectStateSets()
            {
                // return existing state sets
                return _projectStateMap.Values.SelectMany(e => e.AnalyzerMap.Values).ToImmutableArray();
            }

            public IEnumerable<StateSet> GetProjectStateSets(ProjectId projectId)
            {
                var map = GetCachedProjectAnalyzerMap(projectId);
                return map.Values;
            }

            public IEnumerable<DiagnosticAnalyzer> GetOrCreateProjectAnalyzers(Project project)
            {
                var map = GetOrCreateProjectAnalyzerMap(project);
                return map.Keys;
            }

            public IEnumerable<StateSet> GetOrUpdateProjectStateSets(Project project)
            {
                var map = GetOrUpdateProjectAnalyzerMap(project);
                return map.Values;
            }

            public IEnumerable<StateSet> GetOrCreateProjectStateSets(Project project)
            {
                var map = GetOrCreateProjectAnalyzerMap(project);
                return map.Values;
            }

            public StateSet GetOrCreateProjectStateSet(Project project, DiagnosticAnalyzer analyzer)
            {
                var map = GetOrCreateProjectAnalyzerMap(project);
                if (map.TryGetValue(analyzer, out var set))
                {
                    return set;
                }

                return null;
            }

            public void RemoveProjectStateSet(ProjectId projectId)
            {
                if (projectId == null)
                {
                    return;
                }

                _projectStateMap.TryRemove(projectId, out _);
            }

            private ImmutableDictionary<DiagnosticAnalyzer, StateSet> GetOrUpdateProjectAnalyzerMap(Project project)
            {
                var map = GetProjectAnalyzerMap(project);
                if (map != null)
                {
                    return map;
                }

                var newAnalyzersPerReference = _analyzerManager.CreateProjectDiagnosticAnalyzersPerReference(project);
                var newMap = CreateAnalyzerMap(_analyzerManager, project.Language, newAnalyzersPerReference.Values);

                RaiseProjectAnalyzerReferenceChangedIfNeeded(project, newAnalyzersPerReference, newMap);

                // update cache. 
                _projectStateMap[project.Id] = new ProjectAnalyzerStateSets(project.AnalyzerReferences, newAnalyzersPerReference, newMap);

                VerifyProjectDiagnosticStates(newMap.Values);

                return newMap;
            }

            private ImmutableDictionary<DiagnosticAnalyzer, StateSet> GetCachedProjectAnalyzerMap(ProjectId projectId)
            {
                if (_projectStateMap.TryGetValue(projectId, out var entry))
                {
                    return entry.AnalyzerMap;
                }

                return ImmutableDictionary<DiagnosticAnalyzer, StateSet>.Empty;
            }

            private ImmutableDictionary<DiagnosticAnalyzer, StateSet> GetOrCreateProjectAnalyzerMap(Project project)
            {
                // if we can't use cached one, we will create a new analyzer map. which is a bit of waste since
                // we will create new StateSet for all analyzers. but since this only happens when project analyzer references
                // are changed, I believe it is acceptable to have a bit of waste for simplicity.
                return GetProjectAnalyzerMap(project) ?? CreateProjectAnalyzerMap(project);
            }

            private ImmutableDictionary<DiagnosticAnalyzer, StateSet> GetProjectAnalyzerMap(Project project)
            {
                if (_projectStateMap.TryGetValue(project.Id, out var entry) && entry.AnalyzerReferences.Equals(project.AnalyzerReferences))
                {
                    return entry.AnalyzerMap;
                }

                return null;
            }

            private ImmutableDictionary<DiagnosticAnalyzer, StateSet> CreateProjectAnalyzerMap(Project project)
            {
                if (project.AnalyzerReferences.Count == 0)
                {
                    return ImmutableDictionary<DiagnosticAnalyzer, StateSet>.Empty;
                }

                var analyzersPerReference = _analyzerManager.CreateProjectDiagnosticAnalyzersPerReference(project);
                if (analyzersPerReference.Count == 0)
                {
                    return ImmutableDictionary<DiagnosticAnalyzer, StateSet>.Empty;
                }

                return CreateAnalyzerMap(_analyzerManager, project.Language, analyzersPerReference.Values);
            }

            private void RaiseProjectAnalyzerReferenceChangedIfNeeded(
                Project project,
                ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>> newMapPerReference,
                ImmutableDictionary<DiagnosticAnalyzer, StateSet> newMap)
            {
                if (!_projectStateMap.TryGetValue(project.Id, out var entry))
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
                var removedStates = DiffStateSets(entry.AnalyzerReferences.Except(project.AnalyzerReferences), entry.MapPerReferences, entry.AnalyzerMap);

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
                    var referenceIdentity = _analyzerManager.GetAnalyzerReferenceIdentity(reference);
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

            private readonly struct ProjectAnalyzerStateSets
            {
                public readonly IReadOnlyList<AnalyzerReference> AnalyzerReferences;
                public readonly ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>> MapPerReferences;
                public readonly ImmutableDictionary<DiagnosticAnalyzer, StateSet> AnalyzerMap;

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
                    AnalyzerMap = analyzerMap;
                }
            }
        }
    }
}
