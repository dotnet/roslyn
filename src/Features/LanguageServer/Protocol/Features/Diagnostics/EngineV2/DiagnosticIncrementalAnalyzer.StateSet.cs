// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
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
            public readonly string ErrorSourceName;

            private readonly PersistentNames _persistentNames;

            private readonly ConcurrentDictionary<DocumentId, ActiveFileState> _activeFileStates;
            private readonly ConcurrentDictionary<ProjectId, ProjectState> _projectStates;

            public StateSet(string language, DiagnosticAnalyzer analyzer, string errorSourceName)
            {
                Language = language;
                Analyzer = analyzer;
                ErrorSourceName = errorSourceName;

                _persistentNames = PersistentNames.Create(Analyzer);

                _activeFileStates = new ConcurrentDictionary<DocumentId, ActiveFileState>(concurrencyLevel: 2, capacity: 10);
                _projectStates = new ConcurrentDictionary<ProjectId, ProjectState>(concurrencyLevel: 2, capacity: 1);
            }

            public string StateName => _persistentNames.StateName;
            public string SyntaxStateName => _persistentNames.SyntaxStateName;
            public string SemanticStateName => _persistentNames.SemanticStateName;
            public string NonLocalStateName => _persistentNames.NonLocalStateName;

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
                // quick bail out
                if (_activeFileStates.IsEmpty && _projectStates.IsEmpty)
                {
                    return SpecializedCollections.EmptyEnumerable<ProjectId>();
                }

                if (_activeFileStates.Count == 1 && _projectStates.IsEmpty)
                {
                    // see whether we actually have diagnostics
                    var (documentId, state) = _activeFileStates.First();
                    if (state.IsEmpty)
                    {
                        return SpecializedCollections.EmptyEnumerable<ProjectId>();
                    }

                    // we do have diagnostics
                    return SpecializedCollections.SingletonEnumerable(documentId.ProjectId);
                }

                return new HashSet<ProjectId>(
                    _activeFileStates.Where(kv => !kv.Value.IsEmpty)
                                     .Select(kv => kv.Key.ProjectId)
                                     .Concat(_projectStates.Where(kv => !kv.Value.IsEmpty())
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
                => _activeFileStates.GetOrAdd(documentId, id => new ActiveFileState(id));

            public ProjectState GetOrCreateProjectState(ProjectId projectId)
                => _projectStates.GetOrAdd(projectId, id => new ProjectState(this, id));

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

                // store analysis result to active file state:
                var activeFileState = GetOrCreateActiveFileState(document.Id);

                activeFileState.Save(AnalysisKind.Syntax, new DocumentAnalysisData(result.Version, result.GetDocumentDiagnostics(document.Id, AnalysisKind.Syntax)));
                activeFileState.Save(AnalysisKind.Semantic, new DocumentAnalysisData(result.Version, result.GetDocumentDiagnostics(document.Id, AnalysisKind.Semantic)));

                return true;
            }

            public async Task<bool> OnDocumentClosedAsync(TextDocument document, IGlobalOptionService globalOptions)
            {
                // can not be cancelled
                // remove active file state and put it in project state
                if (!_activeFileStates.TryRemove(document.Id, out var activeFileState))
                {
                    return false;
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
                if (_activeFileStates.TryRemove(id, out _))
                {
                    removed = true;
                }
                // remove state for the file that got removed.
                if (_projectStates.TryGetValue(id.ProjectId, out var state))
                {
                    removed |= state.OnDocumentRemoved(id);
                }

                return removed;
            }

            public bool OnProjectRemoved(ProjectId id)
            {
                // remove state for project that got removed.
                if (_projectStates.TryRemove(id, out var state))
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

            private sealed class PersistentNames
            {
                private const string UserDiagnosticsPrefixTableName = "<UserDiagnostics2>";

                private static readonly ConcurrentDictionary<string, PersistentNames> s_analyzerStateNameCache
                    = new(concurrencyLevel: 2, capacity: 10);

                private PersistentNames(string assemblyQualifiedName)
                {
                    StateName = UserDiagnosticsPrefixTableName + "_" + assemblyQualifiedName;
                    SyntaxStateName = StateName + ".Syntax";
                    SemanticStateName = StateName + ".Semantic";
                    NonLocalStateName = StateName + ".NonLocal";
                }

                /// <summary>
                /// Get the unique state name for the given analyzer.
                /// Note that this name is used by the underlying persistence stream of the corresponding <see cref="ProjectState"/> to Read/Write diagnostic data into the stream.
                /// If any two distinct analyzer have the same diagnostic state name, we will end up sharing the persistence stream between them, leading to duplicate/missing/incorrect diagnostic data.
                /// </summary>
                public string StateName { get; }
                public string SyntaxStateName { get; }
                public string SemanticStateName { get; }
                public string NonLocalStateName { get; }

                public static PersistentNames Create(DiagnosticAnalyzer diagnosticAnalyzer)
                    => s_analyzerStateNameCache.GetOrAdd(diagnosticAnalyzer.GetAnalyzerId(), t => new PersistentNames(t));
            }
        }
    }
}
