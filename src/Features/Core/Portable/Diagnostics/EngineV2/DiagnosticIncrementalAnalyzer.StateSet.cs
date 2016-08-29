// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    internal partial class DiagnosticIncrementalAnalyzer
    {
        /// <summary>
        /// this contains all states regarding a <see cref="DiagnosticAnalyzer"/>
        /// </summary>
        private class StateSet
        {
            private const string UserDiagnosticsPrefixTableName = "<UserDiagnostics2>";

            private readonly string _language;
            private readonly DiagnosticAnalyzer _analyzer;
            private readonly string _errorSourceName;

            // analyzer version this state belong to
            private readonly VersionStamp _analyzerVersion;

            // name of each analysis kind persistent storage
            private readonly string _stateName;
            private readonly string _syntaxStateName;
            private readonly string _semanticStateName;
            private readonly string _nonLocalStateName;

            private readonly ConcurrentDictionary<DocumentId, ActiveFileState> _activeFileStates;
            private readonly ConcurrentDictionary<ProjectId, ProjectState> _projectStates;

            public StateSet(string language, DiagnosticAnalyzer analyzer, string errorSourceName)
            {
                _language = language;
                _analyzer = analyzer;
                _errorSourceName = errorSourceName;

                var nameAndVersion = GetNameAndVersion(_analyzer);
                _analyzerVersion = nameAndVersion.Item2;

                _stateName = nameAndVersion.Item1;

                _syntaxStateName = _stateName + ".Syntax";
                _semanticStateName = _stateName + ".Semantic";
                _nonLocalStateName = _stateName + ".NonLocal";

                _activeFileStates = new ConcurrentDictionary<DocumentId, ActiveFileState>(concurrencyLevel: 2, capacity: 10);
                _projectStates = new ConcurrentDictionary<ProjectId, ProjectState>(concurrencyLevel: 2, capacity: 1);
            }

            public string StateName => _stateName;
            public string SyntaxStateName => _syntaxStateName;
            public string SemanticStateName => _semanticStateName;
            public string NonLocalStateName => _nonLocalStateName;

            public string Language => _language;
            public string ErrorSourceName => _errorSourceName;

            public DiagnosticAnalyzer Analyzer => _analyzer;
            public VersionStamp AnalyzerVersion => _analyzerVersion;

            public bool ContainsAnyDocumentOrProjectDiagnostics(ProjectId projectId)
            {
                foreach (var state in GetActiveFileStates(projectId))
                {
                    if (!state.IsEmpty)
                    {
                        return true;
                    }
                }

                ProjectState projectState;
                if (!_projectStates.TryGetValue(projectId, out projectState))
                {
                    return false;
                }

                return !projectState.IsEmpty();
            }

            public IEnumerable<ProjectId> GetProjectsWithDiagnostics()
            {
                // quick bail out
                if (_activeFileStates.IsEmpty && _projectStates.IsEmpty)
                {
                    return SpecializedCollections.EmptyEnumerable<ProjectId>();
                }

                if (_activeFileStates.Count == 1 && _projectStates.IsEmpty)
                {
                    // see whether we actually have diagnostics
                    var kv = _activeFileStates.First();
                    if (kv.Value.IsEmpty)
                    {
                        return SpecializedCollections.EmptyEnumerable<ProjectId>();
                    }

                    // we do have diagnostics
                    return SpecializedCollections.SingletonEnumerable(kv.Key.ProjectId);
                }

                return new HashSet<ProjectId>(
                    _activeFileStates.Where(kv => !kv.Value.IsEmpty)
                                     .Select(kv => kv.Key.ProjectId)
                                     .Concat(_projectStates.Where(kv => !kv.Value.IsEmpty())
                                                           .Select(kv => kv.Key)));
            }

            public IEnumerable<DocumentId> GetDocumentsWithDiagnostics(ProjectId projectId)
            {
                HashSet<DocumentId> set = null;
                foreach (var state in GetActiveFileStates(projectId))
                {
                    set = set ?? new HashSet<DocumentId>();
                    set.Add(state.DocumentId);
                }

                ProjectState projectState;
                if (!_projectStates.TryGetValue(projectId, out projectState) || projectState.IsEmpty())
                {
                    return set ?? SpecializedCollections.EmptyEnumerable<DocumentId>();
                }

                set = set ?? new HashSet<DocumentId>();
                set.UnionWith(projectState.GetDocumentsWithDiagnostics());

                return set;
            }

            private IEnumerable<ActiveFileState> GetActiveFileStates(ProjectId projectId)
            {
                return _activeFileStates.Where(kv => kv.Key.ProjectId == projectId).Select(kv => kv.Value);
            }

            public bool IsActiveFile(DocumentId documentId)
            {
                return _activeFileStates.ContainsKey(documentId);
            }

            public bool FromBuild(ProjectId projectId)
            {
                ProjectState projectState;
                if (!_projectStates.TryGetValue(projectId, out projectState))
                {
                    return false;
                }

                return projectState.FromBuild;
            }

            public bool TryGetActiveFileState(DocumentId documentId, out ActiveFileState state)
            {
                return _activeFileStates.TryGetValue(documentId, out state);
            }

            public bool TryGetProjectState(ProjectId projectId, out ProjectState state)
            {
                return _projectStates.TryGetValue(projectId, out state);
            }

            public ActiveFileState GetActiveFileState(DocumentId documentId)
            {
                return _activeFileStates.GetOrAdd(documentId, id => new ActiveFileState(id));
            }

            public ProjectState GetProjectState(ProjectId projectId)
            {
                return _projectStates.GetOrAdd(projectId, id => new ProjectState(this, id));
            }

            public async Task<bool> OnDocumentOpenedAsync(Document document)
            {
                // can not be cancelled
                ProjectState projectState;
                if (!TryGetProjectState(document.Project.Id, out projectState) ||
                    projectState.IsEmpty(document.Id))
                {
                    // nothing to do
                    return false;
                }

                // always load data
                var avoidLoadingData = false;
                var result = await projectState.GetAnalysisDataAsync(document, avoidLoadingData, CancellationToken.None).ConfigureAwait(false);

                // put project state to active file state
                var activeFileState = GetActiveFileState(document.Id);

                activeFileState.Save(AnalysisKind.Syntax, new DocumentAnalysisData(result.Version, result.GetResultOrEmpty(result.SyntaxLocals, document.Id)));
                activeFileState.Save(AnalysisKind.Semantic, new DocumentAnalysisData(result.Version, result.GetResultOrEmpty(result.SemanticLocals, document.Id)));

                return true;
            }

            public async Task<bool> OnDocumentClosedAsync(Document document)
            {
                // can not be cancelled
                // remove active file state and put it in project state
                ActiveFileState activeFileState;
                if (!_activeFileStates.TryRemove(document.Id, out activeFileState))
                {
                    return false;
                }

                // active file exist, put it in the project state
                var projectState = GetProjectState(document.Project.Id);
                await projectState.MergeAsync(activeFileState, document).ConfigureAwait(false);
                return true;
            }

            public bool OnDocumentReset(Document document)
            {
                var changed = false;

                // can not be cancelled
                // remove active file state and put it in project state
                ActiveFileState activeFileState;
                if (TryGetActiveFileState(document.Id, out activeFileState))
                {
                    activeFileState.ResetVersion();
                    changed |= true;
                }

                ProjectState projectState;
                if (TryGetProjectState(document.Project.Id, out projectState))
                {
                    projectState.ResetVersion();
                    changed |= true;
                }

                return changed;
            }

            public bool OnDocumentRemoved(DocumentId id)
            {
                // remove active file state for removed document
                var removed = false;
                ActiveFileState activeFileState;
                if (_activeFileStates.TryRemove(id, out activeFileState))
                {
                    removed = true;
                }

                // remove state for the file that got removed.
                ProjectState state;
                if (_projectStates.TryGetValue(id.ProjectId, out state))
                {
                    removed |= state.OnDocumentRemoved(id);
                }

                return removed;
            }

            public bool OnProjectRemoved(ProjectId id)
            {
                // remove state for project that got removed.
                ProjectState state;
                if (_projectStates.TryRemove(id, out state))
                {
                    return state.OnProjectRemoved(id);
                }

                return false;
            }

            public void OnRemoved()
            {
                // ths stateset is being removed.
                // TODO: we do this since InMemoryCache is static type. we might consider making it instance object
                //       of something.
                InMemoryStorage.DropCache(Analyzer);
            }

            /// <summary>
            /// Get the unique state name for the given analyzer.
            /// Note that this name is used by the underlying persistence stream of the corresponding <see cref="ProjectState"/> to Read/Write diagnostic data into the stream.
            /// If any two distinct analyzer have the same diagnostic state name, we will end up sharing the persistence stream between them, leading to duplicate/missing/incorrect diagnostic data.
            /// </summary>
            private static ValueTuple<string, VersionStamp> GetNameAndVersion(DiagnosticAnalyzer analyzer)
            {
                Contract.ThrowIfNull(analyzer);

                // Get the unique ID for given diagnostic analyzer.
                // note that we also put version stamp so that we can detect changed analyzer.
                var tuple = analyzer.GetAnalyzerIdAndVersion();
                return ValueTuple.Create(UserDiagnosticsPrefixTableName + "_" + tuple.Item1, tuple.Item2);
            }
        }
    }
}
