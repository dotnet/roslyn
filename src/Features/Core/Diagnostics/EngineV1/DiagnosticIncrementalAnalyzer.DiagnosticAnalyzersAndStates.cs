// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV1
{
    using ProviderId = Int32;

    internal partial class DiagnosticIncrementalAnalyzer
    {
        /// <summary>
        /// Maintains all MEF-imported diagnostic analyzers (with diagnostic states), which are enabled for all projects in the workspace,
        /// and a map of per-project diagnostic analyzers (with diagnostic states).
        /// </summary>
        private partial class DiagnosticAnalyzersAndStates
        {
            private const string UserDiagnosticsPrefixTableName = "<UserDiagnostics>";

            private readonly DiagnosticIncrementalAnalyzer _owner;
            private readonly WorkspaceAnalyzersAndStates _sharedAnalyzersAndStates;
            private readonly ConcurrentDictionary<ProjectId, ProjectAnalyzersAndStates> _projectAnalyzersAndStatesMap;

            public readonly Workspace Workspace;

            public DiagnosticAnalyzersAndStates(DiagnosticIncrementalAnalyzer owner, Workspace workspace, AnalyzerManager analyzerManager)
            {
                _owner = owner;
                _sharedAnalyzersAndStates = new WorkspaceAnalyzersAndStates(analyzerManager);
                _projectAnalyzersAndStatesMap = new ConcurrentDictionary<ProjectId, ProjectAnalyzersAndStates>();

                this.Workspace = workspace;
            }

            public IEnumerable<Tuple<DiagnosticState, ProviderId, StateType>> GetAllExistingDiagnosticStates(ProjectId projectId)
            {
                var project = this.Workspace.CurrentSolution.GetProject(projectId);
                var language = project != null ? project.Language : null;
                return GetAllExistingDiagnosticStates(projectId, language);
            }

            public IEnumerable<Tuple<DiagnosticState, ProviderId, StateType>> GetAllExistingDiagnosticStates(ProjectId projectId, string language)
            {
                var current = _sharedAnalyzersAndStates.GetAllExistingDiagnosticStates(language);

                ProjectAnalyzersAndStates projectAnalyzersAndStates;
                if (_projectAnalyzersAndStatesMap.TryGetValue(projectId, out projectAnalyzersAndStates) &&
                    projectAnalyzersAndStates != null)
                {
                    current = current.Concat(projectAnalyzersAndStates.GetAllExistingDiagnosticStates());
                }

                return current;
            }

            public IEnumerable<Tuple<DiagnosticState, ProviderId, StateType>> GetAllExistingDiagnosticStates(ProjectId projectId, StateType type)
            {
                var project = this.Workspace.CurrentSolution.GetProject(projectId);
                var language = project != null ? project.Language : null;
                return GetAllExistingDiagnosticStates(projectId, type, language);
            }

            public IEnumerable<Tuple<DiagnosticState, ProviderId, StateType>> GetAllExistingDiagnosticStates(ProjectId projectId, StateType type, string language)
            {
                var current = _sharedAnalyzersAndStates.GetAllExistingDiagnosticStates(type, language);

                ProjectAnalyzersAndStates projectAnalyzersAndStates;
                if (_projectAnalyzersAndStatesMap.TryGetValue(projectId, out projectAnalyzersAndStates) &&
                    projectAnalyzersAndStates != null)
                {
                    current = current.Concat(projectAnalyzersAndStates.GetAllExistingDiagnosticStates(type));
                }

                return current;
            }

            public IEnumerable<KeyValuePair<DiagnosticAnalyzer, ProviderId>> GetAllExistingProviderAndIds(Project project)
            {
                var current = _sharedAnalyzersAndStates.GetAllProviderAndIds(project.Language);

                ProjectAnalyzersAndStates projectAnalyzersAndStates;
                if (_projectAnalyzersAndStatesMap.TryGetValue(project.Id, out projectAnalyzersAndStates) &&
                    projectAnalyzersAndStates != null)
                {
                    current = current.Concat(projectAnalyzersAndStates.GetAllProviderAndIds());
                }

                return current;
            }

            public async Task<IEnumerable<KeyValuePair<DiagnosticAnalyzer, ProviderId>>> GetAllProviderAndIdsAsync(Project project, CancellationToken cancellationToken)
            {
                var current = _sharedAnalyzersAndStates.GetAllProviderAndIds(project.Language);

                var projectAnalyzersAndStates = await GetOrCreateProjectAnalyzersAndStatesAsync(project, cancellationToken).ConfigureAwait(false);
                if (projectAnalyzersAndStates != null)
                {
                    current = current.Concat(projectAnalyzersAndStates.GetAllProviderAndIds());
                }

                return current;
            }

            public DiagnosticState GetOrCreateDiagnosticState(StateType stateType, ProviderId providerId, DiagnosticAnalyzer provider, ProjectId projectId, string language)
            {
                Contract.ThrowIfFalse(providerId >= 0);

                var sharedAnalyzersCount = _sharedAnalyzersAndStates.GetAnalyzerCount(language);
                if (providerId < sharedAnalyzersCount)
                {
                    return _sharedAnalyzersAndStates.GetOrCreateDiagnosticState(stateType, providerId, provider, language);
                }

                ProjectAnalyzersAndStates projectAnalyzersAndStates;
                if (!_projectAnalyzersAndStatesMap.TryGetValue(projectId, out projectAnalyzersAndStates) ||
                    projectAnalyzersAndStates == null)
                {
                    return null;
                }

                return projectAnalyzersAndStates.GetOrCreateDiagnosticState(stateType, providerId, provider);
            }

            internal static DiagnosticState GetOrCreateDiagnosticState(DiagnosticState[,] diagnosticStateMaps, StateType stateType, int providerIndex, ProviderId providerId, DiagnosticAnalyzer provider, string language)
            {
                Contract.ThrowIfFalse(providerIndex >= 0);
                Contract.ThrowIfFalse(providerIndex < diagnosticStateMaps.GetLength(1));

                if (diagnosticStateMaps[(int)stateType, providerIndex] == null)
                {
                    var nameAndVersion = GetUniqueDiagnosticStateNameAndVersion(stateType, providerId, provider);

                    var name = nameAndVersion.Item1;
                    var version = nameAndVersion.Item2;
                    diagnosticStateMaps[(int)stateType, providerIndex] = new DiagnosticState(name, version, language);

#if DEBUG
                    // Ensure diagnostic state name is indeed unique.
                    foreach (var type in s_documentScopeStateTypes)
                    {
                        for (var pId = 0; pId < diagnosticStateMaps.GetLength(1); pId++)
                        {
                            if (diagnosticStateMaps[(int)type, pId] != null)
                            {
                                Contract.ThrowIfFalse(name != diagnosticStateMaps[(int)type, pId].Name ||
                                    (stateType == type &&
                                     (pId == providerIndex || language != diagnosticStateMaps[(int)type, pId].Language)));
                            }
                        }
                    }
#endif
                }

                return diagnosticStateMaps[(int)stateType, providerIndex];
            }

            /// <summary>
            /// Get the unique state name for the given {type, provider} tuple.
            /// Note that this name is used by the underlying persistence stream of the corresponding <see cref="DiagnosticState"/> to Read/Write diagnostic data into the stream.
            /// If any two distinct {type, provider} tuples have the same diagnostic state name, we will end up sharing the persistence stream between them, leading to duplicate/missing/incorrect diagnostic data.
            /// </summary>
            private static ValueTuple<string, VersionStamp> GetUniqueDiagnosticStateNameAndVersion(StateType type, ProviderId providerId, DiagnosticAnalyzer provider)
            {
                Contract.ThrowIfNull(provider);

                // Get the unique ID for given diagnostic analyzer.
                // note that we also put version stamp so that we can detect changed provider
                var providerType = provider.GetType();
                var location = providerType.Assembly.Location;

                return ValueTuple.Create(UserDiagnosticsPrefixTableName + "_" + type.ToString() + "_" + providerType.AssemblyQualifiedName, GetProviderVersion(location));
            }

            private static VersionStamp GetProviderVersion(string path)
            {
                if (path == null || !File.Exists(path))
                {
                    return VersionStamp.Default;
                }

                return VersionStamp.Create(File.GetLastWriteTimeUtc(path));
            }

            public DiagnosticState GetDiagnosticState(StateType stateType, ProviderId providerId, ProjectId projectId, string language)
            {
                Contract.ThrowIfFalse(providerId >= 0);

                var sharedAnalyzersCount = _sharedAnalyzersAndStates.GetAnalyzerCount(language);
                if (providerId < sharedAnalyzersCount)
                {
                    return _sharedAnalyzersAndStates.GetDiagnosticState(stateType, providerId, language);
                }

                ProjectAnalyzersAndStates projectAnalyzersAndStates;
                if (!_projectAnalyzersAndStatesMap.TryGetValue(projectId, out projectAnalyzersAndStates) ||
                    projectAnalyzersAndStates == null)
                {
                    return null;
                }

                return projectAnalyzersAndStates.GetDiagnosticState(stateType, providerId);
            }

            public bool RemoveProjectAnalyzersAndStates(ProjectId projectId)
            {
                ProjectAnalyzersAndStates projectAnalyzersAndStates;
                return _projectAnalyzersAndStatesMap.TryRemove(projectId, out projectAnalyzersAndStates);
            }

            private async Task<ProjectAnalyzersAndStates> GetOrCreateProjectAnalyzersAndStatesAsync(Project project, CancellationToken cancellationToken)
            {
                // Update per-project analyzers if needed.
                IEnumerable<Tuple<DiagnosticState, ProviderId, StateType>> removedStates;
                ProjectAnalyzersAndStates projectAnalyzersAndStates = this.GetOrCreateProjectAnalyzersAndStates(project, out removedStates);
                if (removedStates.Any())
                {
                    // Analyzers got updated, so clear the existing persistent states.
                    await _owner.RemoveCacheDataAsync(project, removedStates, cancellationToken).ConfigureAwait(false);
                }

                return projectAnalyzersAndStates;
            }

            private ProjectAnalyzersAndStates GetOrCreateProjectAnalyzersAndStates(Project project,
                out IEnumerable<Tuple<DiagnosticState, ProviderId, StateType>> removedStates)
            {
                removedStates = SpecializedCollections.EmptyEnumerable<Tuple<DiagnosticState, ProviderId, StateType>>();

                var newAnalyzersBuilder = ImmutableArray.CreateBuilder<KeyValuePair<string, IEnumerable<DiagnosticAnalyzer>>>();
                foreach (var analyzerReference in project.AnalyzerReferences)
                {
                    // Filter out duplicate analyzer references.
                    if (_sharedAnalyzersAndStates.HasAnalyzerReference(analyzerReference, project.Language))
                    {
                        continue;
                    }

                    // Get analyzers in the analyzer reference for the given project language.
                    // Filter out duplicate analyzers.
                    // NOTE: The HasAnalyzerReference check above to filter out duplicate analyzer references works only for duplicate AnalyzerFileReference with the same underlying assembly.
                    // However, we can also have AnalyzerImageReference, which might contain the same DiagnosticAnalyzer instance across different AnalyzerImageReference instances
                    // and we want to avoid duplicate analyzers for that case. Hence we apply the HasAnalyzer filter here.
                    var analyzers = analyzerReference.GetAnalyzers(project.Language)
                        .Where(a => !_sharedAnalyzersAndStates.HasAnalyzer(a, project.Language));

                    if (analyzers.Any())
                    {
                        newAnalyzersBuilder.Add(KeyValuePair.Create(analyzerReference.Display, analyzers));
                    }
                }

                var newAnalyzers = newAnalyzersBuilder.ToImmutable();
                ProjectAnalyzersAndStates newProjectAnalyzersAndStates = null;

                ProjectAnalyzersAndStates currentProjectAnalyzersAndStates;
                if (_projectAnalyzersAndStatesMap.TryGetValue(project.Id, out currentProjectAnalyzersAndStates))
                {
                    var newAnalyzersCount = newAnalyzers.Sum(kv => kv.Value.Count());
                    if (currentProjectAnalyzersAndStates != null && currentProjectAnalyzersAndStates.AnalyzerCount == newAnalyzersCount)
                    {
                        Contract.ThrowIfFalse(currentProjectAnalyzersAndStates.AnalyzerCount > 0);

                        // Project still has the same number of analyzers, does the saved projectAnalyzersAndStates has the same set of analyzers?
                        var hasSameAnalyzers = true;
                        foreach (var analyzerPair in newAnalyzers)
                        {
                            foreach (var analyzer in analyzerPair.Value)
                            {
                                if (!currentProjectAnalyzersAndStates.HasAnalyzer(analyzer))
                                {
                                    hasSameAnalyzers = false;
                                    break;
                                }
                            }
                        }

                        if (hasSameAnalyzers)
                        {
                            return currentProjectAnalyzersAndStates;
                        }
                    }
                }
                else
                {
                    currentProjectAnalyzersAndStates = null;
                }

                if (currentProjectAnalyzersAndStates != null)
                {
                    removedStates = currentProjectAnalyzersAndStates.GetAllExistingDiagnosticStates();
                }

                var workspaceAnalyzersCount = _sharedAnalyzersAndStates.GetAnalyzerCount(project.Language);
                newProjectAnalyzersAndStates = ProjectAnalyzersAndStates.CreateIfAnyAnalyzers(newAnalyzers, workspaceAnalyzersCount, project.Language);
                return _projectAnalyzersAndStatesMap.AddOrUpdate(project.Id, newProjectAnalyzersAndStates, (k, c) => newProjectAnalyzersAndStates);
            }
        }
    }
}
