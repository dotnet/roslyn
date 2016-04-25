// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private partial class StateManager
        {
            /// <summary>
            /// This class is responsible for anything related to <see cref="StateSet"/> for project level <see cref="DiagnosticAnalyzer"/>s.
            /// </summary>
            private class ProjectStates
            {
                private readonly StateManager _owner;
                private readonly ConcurrentDictionary<ProjectId, Entry> _stateMap;

                public ProjectStates(StateManager owner)
                {
                    _owner = owner;
                    _stateMap = new ConcurrentDictionary<ProjectId, Entry>(concurrencyLevel: 2, capacity: 10);
                }

                public IEnumerable<StateSet> GetStateSets(ProjectId projectId)
                {
                    var map = GetCachedAnalyzerMap(projectId);
                    return map.Values;
                }

                public IEnumerable<DiagnosticAnalyzer> GetOrCreateAnalyzers(Project project)
                {
                    var map = GetOrCreateAnalyzerMap(project);
                    return map.Keys;
                }

                public IEnumerable<StateSet> GetOrUpdateStateSets(Project project)
                {
                    var map = GetOrUpdateAnalyzerMap(project);
                    return map.Values;
                }

                public IEnumerable<StateSet> GetOrCreateStateSets(Project project)
                {
                    var map = GetOrCreateAnalyzerMap(project);
                    return map.Values;
                }

                public StateSet GetOrCreateStateSet(Project project, DiagnosticAnalyzer analyzer)
                {
                    var map = GetOrCreateAnalyzerMap(project);

                    StateSet set;
                    if (map.TryGetValue(analyzer, out set))
                    {
                        return set;
                    }

                    return null;
                }

                public void RemoveStateSet(ProjectId projectId)
                {
                    if (projectId == null)
                    {
                        return;
                    }

                    Entry unused;
                    _stateMap.TryRemove(projectId, out unused);
                }

                private ImmutableDictionary<DiagnosticAnalyzer, StateSet> GetOrUpdateAnalyzerMap(Project project)
                {
                    var map = GetAnalyzerMap(project);
                    if (map != null)
                    {
                        return map;
                    }

                    var newAnalyzersPerReference = _owner.AnalyzerManager.CreateProjectDiagnosticAnalyzersPerReference(project);
                    var newMap = StateManager.CreateAnalyzerMap(_owner.AnalyzerManager, project.Language, newAnalyzersPerReference.Values);

                    RaiseProjectAnalyzerReferenceChangedIfNeeded(project, newAnalyzersPerReference, newMap);

                    // update cache. 
                    // add and update is same since this method will not be called concurrently.
                    var entry = _stateMap.AddOrUpdate(project.Id,
                        _ => new Entry(project.AnalyzerReferences, newAnalyzersPerReference, newMap), (_1, _2) => new Entry(project.AnalyzerReferences, newAnalyzersPerReference, newMap));

                    VerifyDiagnosticStates(entry.AnalyzerMap.Values);

                    return entry.AnalyzerMap;
                }

                private ImmutableDictionary<DiagnosticAnalyzer, StateSet> GetCachedAnalyzerMap(ProjectId projectId)
                {
                    Entry entry;
                    if (_stateMap.TryGetValue(projectId, out entry))
                    {
                        return entry.AnalyzerMap;
                    }

                    return ImmutableDictionary<DiagnosticAnalyzer, StateSet>.Empty;
                }

                private ImmutableDictionary<DiagnosticAnalyzer, StateSet> GetOrCreateAnalyzerMap(Project project)
                {
                    // if we can't use cached one, we will create a new analyzer map. which is a bit of waste since
                    // we will create new StateSet for all analyzers. but since this only happens when project analyzer references
                    // are changed, I believe it is acceptable to have a bit of waste for simplicity.
                    return GetAnalyzerMap(project) ?? CreateAnalyzerMap(project);
                }

                private ImmutableDictionary<DiagnosticAnalyzer, StateSet> GetAnalyzerMap(Project project)
                {
                    Entry entry;
                    if (_stateMap.TryGetValue(project.Id, out entry) && entry.AnalyzerReferences.Equals(project.AnalyzerReferences))
                    {
                        return entry.AnalyzerMap;
                    }

                    return null;
                }

                private ImmutableDictionary<DiagnosticAnalyzer, StateSet> CreateAnalyzerMap(Project project)
                {
                    if (project.AnalyzerReferences.Count == 0)
                    {
                        return ImmutableDictionary<DiagnosticAnalyzer, StateSet>.Empty;
                    }

                    var analyzersPerReference = _owner.AnalyzerManager.CreateProjectDiagnosticAnalyzersPerReference(project);
                    if (analyzersPerReference.Count == 0)
                    {
                        return ImmutableDictionary<DiagnosticAnalyzer, StateSet>.Empty;
                    }

                    return StateManager.CreateAnalyzerMap(_owner.AnalyzerManager, project.Language, analyzersPerReference.Values);
                }

                private void RaiseProjectAnalyzerReferenceChangedIfNeeded(
                    Project project,
                    ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>> newMapPerReference,
                    ImmutableDictionary<DiagnosticAnalyzer, StateSet> newMap)
                {
                    Entry entry;
                    if (!_stateMap.TryGetValue(project.Id, out entry))
                    {
                        // no previous references and we still don't have any references
                        if (newMap.Count == 0)
                        {
                            return;
                        }

                        // new reference added
                        _owner.RaiseProjectAnalyzerReferenceChanged(
                            new ProjectAnalyzerReferenceChangedEventArgs(project, newMap.Values.ToImmutableArrayOrEmpty(), ImmutableArray<StateSet>.Empty));
                        return;
                    }

                    Contract.Requires(!entry.AnalyzerReferences.Equals(project.AnalyzerReferences));

                    // there has been change. find out what has changed
                    var addedStates = DiffStateSets(project.AnalyzerReferences.Except(entry.AnalyzerReferences), newMapPerReference, newMap);
                    var removedStates = DiffStateSets(entry.AnalyzerReferences.Except(project.AnalyzerReferences), entry.MapPerReferences, entry.AnalyzerMap);

                    // nothing has changed
                    if (addedStates.Length == 0 && removedStates.Length == 0)
                    {
                        return;
                    }

                    _owner.RaiseProjectAnalyzerReferenceChanged(
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
                        var referenceIdentity = _owner.AnalyzerManager.GetAnalyzerReferenceIdentity(reference);

                        // check duplication
                        ImmutableArray<DiagnosticAnalyzer> analyzers;
                        if (!mapPerReference.TryGetValue(referenceIdentity, out analyzers))
                        {
                            continue;
                        }

                        // okay, this is real reference. get stateset
                        foreach (var analyzer in analyzers)
                        {
                            StateSet set;
                            if (!map.TryGetValue(analyzer, out set))
                            {
                                continue;
                            }

                            builder.Add(set);
                        }
                    }

                    return builder.ToImmutable();
                }

                [Conditional("DEBUG")]
                private void VerifyDiagnosticStates(IEnumerable<StateSet> stateSets)
                {
                    // We do not de-duplicate analyzer instances across host and project analyzers.
                    var projectAnalyzers = stateSets.Select(state => state.Analyzer).ToImmutableHashSet();

                    var hostStates = _owner._hostStates.GetStateSets()
                        .Where(state => !projectAnalyzers.Contains(state.Analyzer));

                    StateManager.VerifyDiagnosticStates(hostStates.Concat(stateSets));
                }

                private struct Entry
                {
                    public readonly IReadOnlyList<AnalyzerReference> AnalyzerReferences;
                    public readonly ImmutableDictionary<object, ImmutableArray<DiagnosticAnalyzer>> MapPerReferences;
                    public readonly ImmutableDictionary<DiagnosticAnalyzer, StateSet> AnalyzerMap;

                    public Entry(
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
}
