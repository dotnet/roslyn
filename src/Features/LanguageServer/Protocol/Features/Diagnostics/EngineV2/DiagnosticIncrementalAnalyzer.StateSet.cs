// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    internal partial class DiagnosticIncrementalAnalyzer
    {
        /// <summary>
        /// this contains all states regarding a <see cref="DiagnosticAnalyzer"/>
        /// </summary>
        private sealed class StateSet
        {
            public readonly string Language;
            public readonly DiagnosticAnalyzer Analyzer;

            private ImmutableDictionary<DocumentId, ActiveFileState> _activeFileStates;
            private ImmutableDictionary<ProjectId, ProjectState> _projectStates;

            // Used to protect writes of _activeFileStates or _projectStates
            private readonly object _lock;

            public StateSet(string language, DiagnosticAnalyzer analyzer)
            {
                Language = language;
                Analyzer = analyzer;

                _activeFileStates = ImmutableDictionary<DocumentId, ActiveFileState>.Empty;
                _projectStates = ImmutableDictionary<ProjectId, ProjectState>.Empty;

                _lock = new object();
            }

            [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/34761", AllowCaptures = false, AllowGenericEnumeration = false)]
            public bool ContainsAnyDocumentOrProjectDiagnostics(ProjectId projectId)
            {
                foreach (var (documentId, state) in _activeFileStates)
                {
                    if (documentId.ProjectId == projectId && !state.IsEmpty)
                    {
                        return true;
                    }
                }

                return _projectStates.TryGetValue(projectId, out var projectState) && !projectState.IsEmpty();
            }

            public IEnumerable<ProjectId> GetProjectsWithDiagnostics()
            {
                // Placed in locals because accessed multiple times in this method
                var activeFileStates = _activeFileStates;
                var projectStates = _projectStates;

                // quick bail out
                if (activeFileStates.IsEmpty && projectStates.IsEmpty)
                {
                    return SpecializedCollections.EmptyEnumerable<ProjectId>();
                }

                if (activeFileStates.Count == 1 && projectStates.IsEmpty)
                {
                    // see whether we actually have diagnostics
                    var (documentId, state) = activeFileStates.First();
                    if (state.IsEmpty)
                    {
                        return SpecializedCollections.EmptyEnumerable<ProjectId>();
                    }

                    // we do have diagnostics
                    return SpecializedCollections.SingletonEnumerable(documentId.ProjectId);
                }

                return new HashSet<ProjectId>(
                    activeFileStates.Where(kv => !kv.Value.IsEmpty)
                                    .Select(kv => kv.Key.ProjectId)
                                    .Concat(projectStates.Where(kv => !kv.Value.IsEmpty())
                                                         .Select(kv => kv.Key)));
            }

            [PerformanceSensitive("https://github.com/dotnet/roslyn/issues/34761", AllowCaptures = false, AllowGenericEnumeration = false)]
            public void CollectDocumentsWithDiagnostics(ProjectId projectId, HashSet<DocumentId> set)
            {
                RoslynDebug.Assert(set != null);

                // Collect active documents with diagnostics

                foreach (var (documentId, state) in _activeFileStates)
                {
                    if (documentId.ProjectId == projectId && !state.IsEmpty)
                    {
                        set.Add(documentId);
                    }
                }

                if (_projectStates.TryGetValue(projectId, out var projectState) && !projectState.IsEmpty())
                {
                    set.UnionWith(projectState.GetDocumentsWithDiagnostics());
                }
            }

            public bool IsActiveFile(DocumentId documentId)
                => _activeFileStates.ContainsKey(documentId);

            public bool FromBuild(ProjectId projectId)
                => _projectStates.TryGetValue(projectId, out var projectState) && projectState.FromBuild;

            public bool TryGetActiveFileState(DocumentId documentId, [NotNullWhen(true)] out ActiveFileState? state)
                => _activeFileStates.TryGetValue(documentId, out state);

            public bool TryGetProjectState(ProjectId projectId, [NotNullWhen(true)] out ProjectState? state)
                => _projectStates.TryGetValue(projectId, out state);

            public ActiveFileState GetOrCreateActiveFileState(DocumentId documentId)
            {
                if (!_activeFileStates.TryGetValue(documentId, out var value))
                {
                    lock (_lock)
                    {
                        value = new ActiveFileState(documentId);
                        _activeFileStates = _activeFileStates.SetItem(documentId, value);
                    }
                }

                return value;
            }

            public ProjectState GetOrCreateProjectState(ProjectId projectId)
            {
                if (!_projectStates.TryGetValue(projectId, out var value))
                {
                    lock (_lock)
                    {
                        value = new ProjectState(this, projectId);
                        _projectStates = _projectStates.SetItem(projectId, value);
                    }
                }

                return value;
            }

            public async Task<bool> OnDocumentOpenedAsync(TextDocument document)
            {
                // can not be cancelled
                if (!TryGetProjectState(document.Project.Id, out var projectState) ||
                    projectState.IsEmpty(document.Id))
                {
                    // nothing to do
                    return false;
                }

                var result = await projectState.GetAnalysisDataAsync(document, avoidLoadingData: false, CancellationToken.None).ConfigureAwait(false);
                var text = await document.GetTextAsync(CancellationToken.None).ConfigureAwait(false);

                // store analysis result to active file state:
                var activeFileState = GetOrCreateActiveFileState(document.Id);

                activeFileState.Save(AnalysisKind.Syntax, new DocumentAnalysisData(result.Version, text.Lines.Count, result.GetDocumentDiagnostics(document.Id, AnalysisKind.Syntax)));
                activeFileState.Save(AnalysisKind.Semantic, new DocumentAnalysisData(result.Version, text.Lines.Count, result.GetDocumentDiagnostics(document.Id, AnalysisKind.Semantic)));

                return true;
            }

            public async Task<bool> OnDocumentClosedAsync(TextDocument document, IGlobalOptionService globalOptions)
            {
                // can not be cancelled
                // remove active file state and put it in project state
                if (!_activeFileStates.TryGetValue(document.Id, out var activeFileState))
                {
                    return false;
                }

                lock (_lock)
                {
                    _activeFileStates = _activeFileStates.Remove(document.Id);
                }

                // active file exist, put it in the project state
                var projectState = GetOrCreateProjectState(document.Project.Id);
                await projectState.MergeAsync(activeFileState, document, globalOptions).ConfigureAwait(false);
                return true;
            }

            public bool OnDocumentReset(TextDocument document)
            {
                var changed = false;
                // can not be cancelled
                // remove active file state and put it in project state
                if (TryGetActiveFileState(document.Id, out var activeFileState))
                {
                    activeFileState.ResetVersion();
                    changed |= true;
                }

                if (TryGetProjectState(document.Project.Id, out var projectState))
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
                if (_activeFileStates.ContainsKey(id))
                {
                    lock (_lock)
                    {
                        _activeFileStates = _activeFileStates.Remove(id);
                    }

                    removed = true;
                }

                // remove state for the file that got removed.
                if (_projectStates.TryGetValue(id.ProjectId, out var state))
                {
                    lock(_lock)
                    {
                        _projectStates = _projectStates.Remove(id.ProjectId);
                    }

                    removed |= state.OnDocumentRemoved(id);
                }

                return removed;
            }

            public bool OnProjectRemoved(ProjectId id)
            {
                // remove state for project that got removed.
                if (_projectStates.TryGetValue(id, out var state))
                {
                    lock (_lock)
                    {
                        _projectStates = _projectStates.Remove(id);
                    }

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
        }
    }
}
