// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    internal partial class DiagnosticIncrementalAnalyzer
    {
        /// <summary>
        /// State for diagnostics that belong to a project at given time.
        /// </summary>
        private class ProjectState
        {
            // project id of this state
            private readonly StateSet _owner;

            // last aggregated analysis result for this project saved
            private AnalysisResult _lastResult;

            public ProjectState(StateSet owner, ProjectId projectId)
            {
                _owner = owner;
                _lastResult = new AnalysisResult(projectId, VersionStamp.Default, documentIds: null, isEmpty: true);
            }

            public ImmutableHashSet<DocumentId> GetDocumentsWithDiagnostics()
            {
                return _lastResult.DocumentIdsOrEmpty;
            }

            public bool IsEmpty()
            {
                return _lastResult.IsEmpty;
            }

            public bool IsEmpty(DocumentId documentId)
            {
                return IsEmpty(_lastResult, documentId);
            }

            public async Task<AnalysisResult> GetAnalysisDataAsync(Project project, bool avoidLoadingData, CancellationToken cancellationToken)
            {
                // make a copy of last result.
                var lastResult = _lastResult;
                Contract.ThrowIfFalse(lastResult.ProjectId == project.Id);

                if (lastResult.IsDefault)
                {
                    return await LoadInitialAnalysisDataAsync(project, cancellationToken).ConfigureAwait(false);
                }

                // PERF: avoid loading data if version is not right one.
                // avoid loading data flag is there as a strickly perf optimization.
                var version = await GetDiagnosticVersionAsync(project, cancellationToken).ConfigureAwait(false);
                if (avoidLoadingData && lastResult.Version != version)
                {
                    return lastResult;
                }

                // if given document doesnt have any diagnostics, return empty.
                if (lastResult.IsEmpty)
                {
                    return new AnalysisResult(lastResult.ProjectId, lastResult.Version);
                }

                // loading data can be cancelled any time.
                var serializer = new DiagnosticDataSerializer(_owner.AnalyzerVersion, lastResult.Version);
                var builder = new Builder(project.Id, lastResult.Version, lastResult.DocumentIds);

                foreach (var documentId in lastResult.DocumentIds)
                {
                    var document = project.GetDocument(documentId);
                    if (document == null)
                    {
                        continue;
                    }

                    if (!await TryDeserializeDocumentAsync(serializer, document, builder, cancellationToken).ConfigureAwait(false))
                    {
                        Contract.Requires(false, "How this can happen?");
                        continue;
                    }
                }

                if (!await TryDeserializeAsync(serializer, project, project.Id, _owner.NonLocalStateName, builder.AddOthers, cancellationToken).ConfigureAwait(false))
                {
                    Contract.Requires(false, "How this can happen?");
                }

                return builder.ToResult();
            }

            public async Task<AnalysisResult> GetAnalysisDataAsync(Document document, bool avoidLoadingData, CancellationToken cancellationToken)
            {
                // make a copy of last result.
                var lastResult = _lastResult;
                Contract.ThrowIfFalse(lastResult.ProjectId == document.Project.Id);

                if (lastResult.IsDefault)
                {
                    return await LoadInitialAnalysisDataAsync(document, cancellationToken).ConfigureAwait(false);
                }

                var version = await GetDiagnosticVersionAsync(document.Project, cancellationToken).ConfigureAwait(false);
                if (avoidLoadingData && lastResult.Version != version)
                {
                    return lastResult;
                }

                // if given document doesnt have any diagnostics, return empty.
                if (IsEmpty(lastResult, document.Id))
                {
                    return new AnalysisResult(lastResult.ProjectId, lastResult.Version);
                }

                // loading data can be cancelled any time.
                var serializer = new DiagnosticDataSerializer(_owner.AnalyzerVersion, lastResult.Version);
                var builder = new Builder(document.Project.Id, lastResult.Version);

                if (!await TryDeserializeDocumentAsync(serializer, document, builder, cancellationToken).ConfigureAwait(false))
                {
                    Contract.Requires(false, "How this can happen?");
                }

                return builder.ToResult();
            }

            public async Task<AnalysisResult> GetProjectAnalysisDataAsync(Project project, bool avoidLoadingData, CancellationToken cancellationToken)
            {
                // make a copy of last result.
                var lastResult = _lastResult;
                Contract.ThrowIfFalse(lastResult.ProjectId == project.Id);

                if (lastResult.IsDefault)
                {
                    return await LoadInitialProjectAnalysisDataAsync(project, cancellationToken).ConfigureAwait(false);
                }

                var version = await GetDiagnosticVersionAsync(project, cancellationToken).ConfigureAwait(false);
                if (avoidLoadingData && lastResult.Version != version)
                {
                    return lastResult;
                }

                // if given document doesnt have any diagnostics, return empty.
                if (lastResult.IsEmpty)
                {
                    return new AnalysisResult(lastResult.ProjectId, lastResult.Version);
                }

                // loading data can be cancelled any time.
                var serializer = new DiagnosticDataSerializer(_owner.AnalyzerVersion, lastResult.Version);
                var builder = new Builder(project.Id, lastResult.Version);

                if (!await TryDeserializeAsync(serializer, project, project.Id, _owner.NonLocalStateName, builder.AddOthers, cancellationToken).ConfigureAwait(false))
                {
                    Contract.Requires(false, "How this can happen?");
                }

                return builder.ToResult();
            }

            public async Task SaveAsync(Project project, AnalysisResult result)
            {
                Contract.ThrowIfTrue(result.IsAggregatedForm);

                RemoveInMemoryCache(_lastResult);

                // save last aggregated form of analysis result
                _lastResult = result.ToAggregatedForm();

                // serialization can't be cancelled.
                var serializer = new DiagnosticDataSerializer(_owner.AnalyzerVersion, result.Version);
                foreach (var documentId in result.DocumentIds)
                {
                    var document = project.GetDocument(documentId);
                    Contract.ThrowIfNull(document);

                    await SerializeAsync(serializer, document, document.Id, _owner.SyntaxStateName, GetResult(result, AnalysisKind.Syntax, document.Id)).ConfigureAwait(false);
                    await SerializeAsync(serializer, document, document.Id, _owner.SemanticStateName, GetResult(result, AnalysisKind.Semantic, document.Id)).ConfigureAwait(false);
                    await SerializeAsync(serializer, document, document.Id, _owner.NonLocalStateName, GetResult(result, AnalysisKind.NonLocal, document.Id)).ConfigureAwait(false);
                }

                await SerializeAsync(serializer, project, result.ProjectId, _owner.NonLocalStateName, result.Others).ConfigureAwait(false);
            }

            public bool OnDocumentRemoved(DocumentId id)
            {
                RemoveInMemoryCacheEntries(id);
                return !IsEmpty(id);
            }

            public bool OnProjectRemoved(ProjectId id)
            {
                RemoveInMemoryCacheEntry(id, _owner.NonLocalStateName);
                return !IsEmpty();
            }

            private async Task<AnalysisResult> LoadInitialAnalysisDataAsync(Project project, CancellationToken cancellationToken)
            {
                // loading data can be cancelled any time.
                var version = await GetDiagnosticVersionAsync(project, cancellationToken).ConfigureAwait(false);
                var serializer = new DiagnosticDataSerializer(_owner.AnalyzerVersion, version);
                var builder = new Builder(project.Id, version);

                foreach (var document in project.Documents)
                {
                    if (!await TryDeserializeDocumentAsync(serializer, document, builder, cancellationToken).ConfigureAwait(false))
                    {
                        continue;
                    }
                }

                if (!await TryDeserializeAsync(serializer, project, project.Id, _owner.NonLocalStateName, builder.AddOthers, cancellationToken).ConfigureAwait(false))
                {
                    return new AnalysisResult(project.Id, VersionStamp.Default, ImmutableHashSet<DocumentId>.Empty, isEmpty: true);
                }

                return builder.ToResult();
            }

            private async Task<AnalysisResult> LoadInitialAnalysisDataAsync(Document document, CancellationToken cancellationToken)
            {
                // loading data can be cancelled any time.
                var project = document.Project;

                var version = await GetDiagnosticVersionAsync(project, cancellationToken).ConfigureAwait(false);
                var serializer = new DiagnosticDataSerializer(_owner.AnalyzerVersion, version);
                var builder = new Builder(project.Id, version);

                if (!await TryDeserializeDocumentAsync(serializer, document, builder, cancellationToken).ConfigureAwait(false))
                {
                    return new AnalysisResult(project.Id, VersionStamp.Default, ImmutableHashSet<DocumentId>.Empty, isEmpty: true);
                }

                return builder.ToResult();
            }

            private async Task<AnalysisResult> LoadInitialProjectAnalysisDataAsync(Project project, CancellationToken cancellationToken)
            {
                // loading data can be cancelled any time.
                var version = await GetDiagnosticVersionAsync(project, cancellationToken).ConfigureAwait(false);
                var serializer = new DiagnosticDataSerializer(_owner.AnalyzerVersion, version);
                var builder = new Builder(project.Id, version);

                if (!await TryDeserializeAsync(serializer, project, project.Id, _owner.NonLocalStateName, builder.AddOthers, cancellationToken).ConfigureAwait(false))
                {
                    return new AnalysisResult(project.Id, VersionStamp.Default, ImmutableHashSet<DocumentId>.Empty, isEmpty: true);
                }

                return builder.ToResult();
            }

            private async Task SerializeAsync(DiagnosticDataSerializer serializer, object documentOrProject, object key, string stateKey, ImmutableArray<DiagnosticData> diagnostics)
            {
                // try to serialize it
                if (await serializer.SerializeAsync(documentOrProject, stateKey, diagnostics, CancellationToken.None).ConfigureAwait(false))
                {
                    // we succeeded saving it to persistent storage. remove it from in memory cache if it exists
                    RemoveInMemoryCacheEntry(key, stateKey);
                    return;
                }

                // if serialization fail, hold it in the memory
                InMemoryStorage.Cache(_owner.Analyzer, ValueTuple.Create(key, stateKey), new CacheEntry(serializer.Version, diagnostics));
            }

            private async Task<bool> TryDeserializeDocumentAsync(DiagnosticDataSerializer serializer, Document document, Builder builder, CancellationToken cancellationToken)
            {
                var result = true;

                result &= await TryDeserializeAsync(serializer, document, document.Id, _owner.SyntaxStateName, builder.AddSyntaxLocals, cancellationToken).ConfigureAwait(false);
                result &= await TryDeserializeAsync(serializer, document, document.Id, _owner.SemanticStateName, builder.AddSemanticLocals, cancellationToken).ConfigureAwait(false);
                result &= await TryDeserializeAsync(serializer, document, document.Id, _owner.NonLocalStateName, builder.AddNonLocals, cancellationToken).ConfigureAwait(false);

                return result;
            }

            private async Task<bool> TryDeserializeAsync<T>(
                DiagnosticDataSerializer serializer,
                object documentOrProject, T key, string stateKey,
                Action<T, ImmutableArray<DiagnosticData>> add,
                CancellationToken cancellationToken) where T : class
            {
                var diagnostics = await DeserializeAsync(serializer, documentOrProject, key, stateKey, cancellationToken).ConfigureAwait(false);
                if (diagnostics.IsDefault)
                {
                    return false;
                }

                add(key, diagnostics);
                return true;
            }

            private async Task<ImmutableArray<DiagnosticData>> DeserializeAsync(DiagnosticDataSerializer serializer, object documentOrProject, object key, string stateKey, CancellationToken cancellationToken)
            {
                // check cache first
                CacheEntry entry;
                if (InMemoryStorage.TryGetValue(_owner.Analyzer, ValueTuple.Create(key, stateKey), out entry) && serializer.Version == entry.Version)
                {
                    return entry.Diagnostics;
                }

                // try to deserialize it
                return await serializer.DeserializeAsync(documentOrProject, stateKey, cancellationToken).ConfigureAwait(false);
            }

            private void RemoveInMemoryCache(AnalysisResult lastResult)
            {
                // remove old cache
                foreach (var documentId in lastResult.DocumentIdsOrEmpty)
                {
                    RemoveInMemoryCacheEntries(documentId);
                }
            }

            private void RemoveInMemoryCacheEntries(DocumentId id)
            {
                RemoveInMemoryCacheEntry(id, _owner.SyntaxStateName);
                RemoveInMemoryCacheEntry(id, _owner.SemanticStateName);
                RemoveInMemoryCacheEntry(id, _owner.NonLocalStateName);
            }

            private void RemoveInMemoryCacheEntry(object key, string stateKey)
            {
                // remove in memory cache if entry exist
                InMemoryStorage.Remove(_owner.Analyzer, ValueTuple.Create(key, stateKey));
            }

            private bool IsEmpty(AnalysisResult result, DocumentId documentId)
            {
                return !result.DocumentIdsOrEmpty.Contains(documentId);
            }

            // we have this builder to avoid allocating collections unnecessarily.
            private class Builder
            {
                private readonly ProjectId _projectId;
                private readonly VersionStamp _version;
                private readonly ImmutableHashSet<DocumentId> _documentIds;

                private ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>.Builder _syntaxLocals;
                private ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>.Builder _semanticLocals;
                private ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>.Builder _nonLocals;
                private ImmutableArray<DiagnosticData> _others;

                public Builder(ProjectId projectId, VersionStamp version, ImmutableHashSet<DocumentId> documentIds = null)
                {
                    _projectId = projectId;
                    _version = version;
                    _documentIds = documentIds;
                }

                public void AddSyntaxLocals(DocumentId documentId, ImmutableArray<DiagnosticData> diagnostics)
                {
                    Add(ref _syntaxLocals, documentId, diagnostics);
                }

                public void AddSemanticLocals(DocumentId documentId, ImmutableArray<DiagnosticData> diagnostics)
                {
                    Add(ref _semanticLocals, documentId, diagnostics);
                }

                public void AddNonLocals(DocumentId documentId, ImmutableArray<DiagnosticData> diagnostics)
                {
                    Add(ref _nonLocals, documentId, diagnostics);
                }

                public void AddOthers(ProjectId unused, ImmutableArray<DiagnosticData> diagnostics)
                {
                    _others = diagnostics;
                }

                private void Add(ref ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>.Builder locals, DocumentId documentId, ImmutableArray<DiagnosticData> diagnostics)
                {
                    locals = locals ?? ImmutableDictionary.CreateBuilder<DocumentId, ImmutableArray<DiagnosticData>>();
                    locals.Add(documentId, diagnostics);
                }

                public AnalysisResult ToResult()
                {
                    return new AnalysisResult(_projectId, _version,
                        _syntaxLocals?.ToImmutable() ?? ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>.Empty,
                        _semanticLocals?.ToImmutable() ?? ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>.Empty,
                        _nonLocals?.ToImmutable() ?? ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>.Empty,
                        _others.IsDefault ? ImmutableArray<DiagnosticData>.Empty : _others,
                        _documentIds);
                }
            }
        }
    }
}
