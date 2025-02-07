﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics;

internal partial class DiagnosticAnalyzerService
{
    private partial class DiagnosticIncrementalAnalyzer
    {
        /// <summary>
        /// State for diagnostics that belong to a project at given time.
        /// </summary>
        private sealed class ProjectState
        {
            private const string SyntaxStateName = nameof(SyntaxStateName);
            private const string SemanticStateName = nameof(SemanticStateName);
            private const string NonLocalStateName = nameof(NonLocalStateName);

            // project id of this state
            private readonly StateSet _owner;

            // last aggregated analysis result for this project saved
            private DiagnosticAnalysisResult _lastResult;

            public ProjectState(StateSet owner, ProjectId projectId)
            {
                _owner = owner;
                _lastResult = DiagnosticAnalysisResult.CreateInitialResult(projectId);
            }

            /// <summary>
            /// Return all diagnostics for the given project stored in this state
            /// </summary>
            public async Task<DiagnosticAnalysisResult> GetAnalysisDataAsync(Project project, CancellationToken cancellationToken)
            {
                // make a copy of last result.
                var lastResult = _lastResult;
                Contract.ThrowIfFalse(lastResult.ProjectId == project.Id);

                var version = await GetDiagnosticVersionAsync(project, cancellationToken).ConfigureAwait(false);
                if (lastResult.IsDefault)
                    return LoadInitialAnalysisData(project, version, cancellationToken);

                RoslynDebug.Assert(lastResult.DocumentIds != null);

                // PERF: avoid loading data if version is not right one.
                // avoid loading data flag is there as a strictly perf optimization.
                if (lastResult.Version != version)
                {
                    return lastResult;
                }

                // if given project doesnt have any diagnostics, return empty.
                if (lastResult.IsEmpty)
                {
                    return DiagnosticAnalysisResult.CreateEmpty(lastResult.ProjectId, lastResult.Version);
                }

                // loading data can be canceled any time.
                var serializerVersion = lastResult.Version;
                var builder = new Builder(project, lastResult.Version, lastResult.DocumentIds);

                foreach (var documentId in lastResult.DocumentIds)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var document = project.GetDocument(documentId);
                    if (document == null)
                        continue;

                    if (!TryGetDiagnosticsFromInMemoryStorage(serializerVersion, document, builder))
                    {
                        Debug.Assert(lastResult.Version == VersionStamp.Default);

                        // this can happen if we merged back active file diagnostics back to project state but
                        // project state didn't have diagnostics for the file yet. (since project state was staled)
                        continue;
                    }
                }

                if (!TryGetProjectDiagnosticsFromInMemoryStorage(serializerVersion, project, builder))
                {
                    // this can happen if SaveAsync is not yet called but active file merge happened. one of case is if user did build before the very first
                    // analysis happened.
                }

                return builder.ToResult();
            }

            /// <summary>
            /// Return all diagnostics for the given document stored in this state including non local diagnostics for this document
            /// </summary>
            public async Task<DiagnosticAnalysisResult> GetAnalysisDataAsync(TextDocument document, CancellationToken cancellationToken)
            {
                // make a copy of last result.
                var lastResult = _lastResult;
                Contract.ThrowIfFalse(lastResult.ProjectId == document.Project.Id);

                var version = await GetDiagnosticVersionAsync(document.Project, cancellationToken).ConfigureAwait(false);
                if (lastResult.IsDefault)
                    return LoadInitialAnalysisData(document, version);

                // if given document doesnt have any diagnostics, return empty.
                if (IsEmpty(lastResult, document.Id))
                {
                    return DiagnosticAnalysisResult.CreateEmpty(lastResult.ProjectId, lastResult.Version);
                }

                // loading data can be canceled any time.
                var serializerVersion = lastResult.Version;
                var builder = new Builder(document.Project, lastResult.Version);

                if (!TryGetDiagnosticsFromInMemoryStorage(serializerVersion, document, builder))
                {
                    Debug.Assert(lastResult.Version == VersionStamp.Default);

                    // this can happen if we merged back active file diagnostics back to project state but
                    // project state didn't have diagnostics for the file yet. (since project state was staled)
                }

                return builder.ToResult();
            }

            /// <summary>
            /// Return all no location diagnostics for the given project stored in this state
            /// </summary>
            public async Task<DiagnosticAnalysisResult> GetProjectAnalysisDataAsync(Project project, CancellationToken cancellationToken)
            {
                // make a copy of last result.
                var lastResult = _lastResult;
                Contract.ThrowIfFalse(lastResult.ProjectId == project.Id);

                var version = await GetDiagnosticVersionAsync(project, cancellationToken).ConfigureAwait(false);
                if (lastResult.IsDefault)
                    return LoadInitialProjectAnalysisData(project, version);

                // if given document doesn't have any diagnostics, return empty.
                if (lastResult.IsEmpty)
                {
                    return DiagnosticAnalysisResult.CreateEmpty(lastResult.ProjectId, lastResult.Version);
                }

                // loading data can be canceled any time.
                var serializerVersion = lastResult.Version;
                var builder = new Builder(project, lastResult.Version);

                if (!TryGetProjectDiagnosticsFromInMemoryStorage(serializerVersion, project, builder))
                {
                    // this can happen if SaveAsync is not yet called but active file merge happened. one of case is if user did build before the very first
                    // analysis happened.
                }

                return builder.ToResult();
            }

            public async ValueTask SaveToInMemoryStorageAsync(Project project, DiagnosticAnalysisResult result)
            {
                Contract.ThrowIfTrue(result.IsAggregatedForm);
                Contract.ThrowIfNull(result.DocumentIds);

                RemoveInMemoryCache(_lastResult);

                using var _ = PooledHashSet<DocumentId>.GetInstance(out var documentIdsToProcess);
                documentIdsToProcess.AddRange(_lastResult.DocumentIdsOrEmpty);
                documentIdsToProcess.AddRange(result.DocumentIdsOrEmpty);

                // save last aggregated form of analysis result
                _lastResult = result.ToAggregatedForm();

                // serialization can't be canceled.
                var serializerVersion = result.Version;

                foreach (var documentId in documentIdsToProcess)
                {
                    var document = project.GetTextDocument(documentId);

                    // If we couldn't find a normal document, and all features are enabled for source generated
                    // documents, attempt to locate a matching source generated document in the project.
                    if (document is null
                        && project.Solution.Services.GetService<ISolutionCrawlerOptionsService>()?.EnableDiagnosticsInSourceGeneratedFiles == true)
                    {
                        document = await project.GetSourceGeneratedDocumentAsync(documentId, CancellationToken.None).ConfigureAwait(false);
                    }

                    if (document == null)
                    {
                        // it can happen with build synchronization since, in build case, 
                        // we don't have actual snapshot (we have no idea what sources out of proc build has picked up)
                        // so we might be out of sync.
                        // example of such cases will be changing anything about solution while building is going on.
                        // it can be user explicit actions such as unloading project, deleting a file, but also it can be 
                        // something project system or roslyn workspace does such as populating workspace right after
                        // solution is loaded.
                        continue;
                    }

                    AddToInMemoryStorage(serializerVersion, new(document.Id), SyntaxStateName, result.GetDocumentDiagnostics(document.Id, AnalysisKind.Syntax));
                    AddToInMemoryStorage(serializerVersion, new(document.Id), SemanticStateName, result.GetDocumentDiagnostics(document.Id, AnalysisKind.Semantic));
                    AddToInMemoryStorage(serializerVersion, new(document.Id), NonLocalStateName, result.GetDocumentDiagnostics(document.Id, AnalysisKind.NonLocal));
                }

                AddToInMemoryStorage(serializerVersion, new(result.ProjectId), NonLocalStateName, result.GetOtherDiagnostics());
            }

            private DiagnosticAnalysisResult LoadInitialAnalysisData(
                Project project, VersionStamp version, CancellationToken cancellationToken)
            {
                // loading data can be canceled any time.
                var builder = new Builder(project, version);

                foreach (var document in project.Documents)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!TryGetDiagnosticsFromInMemoryStorage(version, document, builder))
                        continue;
                }

                if (!TryGetProjectDiagnosticsFromInMemoryStorage(version, project, builder))
                    return DiagnosticAnalysisResult.CreateEmpty(project.Id, VersionStamp.Default);

                return builder.ToResult();
            }

            private DiagnosticAnalysisResult LoadInitialAnalysisData(
                TextDocument document, VersionStamp version)
            {
                // loading data can be canceled any time.
                var project = document.Project;

                var builder = new Builder(project, version);

                if (!TryGetDiagnosticsFromInMemoryStorage(version, document, builder))
                    return DiagnosticAnalysisResult.CreateEmpty(project.Id, VersionStamp.Default);

                return builder.ToResult();
            }

            private DiagnosticAnalysisResult LoadInitialProjectAnalysisData(
                Project project, VersionStamp version)
            {
                // loading data can be canceled any time.
                var builder = new Builder(project, version);

                if (!TryGetProjectDiagnosticsFromInMemoryStorage(version, project, builder))
                    return DiagnosticAnalysisResult.CreateEmpty(project.Id, VersionStamp.Default);

                return builder.ToResult();
            }

            private void AddToInMemoryStorage(
                VersionStamp serializerVersion, ProjectOrDocumentId key, string stateKey, ImmutableArray<DiagnosticData> diagnostics)
            {
                InMemoryStorage.Cache(_owner.Analyzer, (key, stateKey), new CacheEntry(serializerVersion, diagnostics));
            }

            private bool TryGetDiagnosticsFromInMemoryStorage(VersionStamp serializerVersion, TextDocument document, Builder builder)
            {
                var success = true;
                var project = document.Project;
                var documentId = document.Id;

                var diagnostics = GetDiagnosticsFromInMemoryStorage(serializerVersion, new(documentId), SyntaxStateName);
                if (!diagnostics.IsDefault)
                {
                    builder.AddSyntaxLocals(documentId, diagnostics);
                }
                else
                {
                    success = false;
                }

                diagnostics = GetDiagnosticsFromInMemoryStorage(serializerVersion, new(documentId), SemanticStateName);
                if (!diagnostics.IsDefault)
                {
                    builder.AddSemanticLocals(documentId, diagnostics);
                }
                else
                {
                    success = false;
                }

                diagnostics = GetDiagnosticsFromInMemoryStorage(serializerVersion, new(documentId), NonLocalStateName);
                if (!diagnostics.IsDefault)
                {
                    builder.AddNonLocals(documentId, diagnostics);
                }
                else
                {
                    success = false;
                }

                return success;
            }

            private bool TryGetProjectDiagnosticsFromInMemoryStorage(VersionStamp serializerVersion, Project project, Builder builder)
            {
                var diagnostics = GetDiagnosticsFromInMemoryStorage(serializerVersion, new(project.Id), NonLocalStateName);
                if (!diagnostics.IsDefault)
                {
                    builder.AddOthers(diagnostics);
                    return true;
                }

                return false;
            }

            private ImmutableArray<DiagnosticData> GetDiagnosticsFromInMemoryStorage(
                VersionStamp serializerVersion, ProjectOrDocumentId key, string stateKey)
            {
                return InMemoryStorage.TryGetValue(_owner.Analyzer, (key, stateKey), out var entry) && serializerVersion == entry.Version
                    ? entry.Diagnostics
                    : default;
            }

            private void RemoveInMemoryCache(DiagnosticAnalysisResult lastResult)
            {
                // remove old cache
                foreach (var documentId in lastResult.DocumentIdsOrEmpty)
                {
                    RemoveInMemoryCacheEntries(documentId);
                }
            }

            private void RemoveInMemoryCacheEntries(DocumentId id)
            {
                RemoveInMemoryCacheEntry(new(id), SyntaxStateName);
                RemoveInMemoryCacheEntry(new(id), SemanticStateName);
                RemoveInMemoryCacheEntry(new(id), NonLocalStateName);
            }

            private void RemoveInMemoryCacheEntry(ProjectOrDocumentId key, string stateKey)
            {
                // remove in memory cache if entry exist
                InMemoryStorage.Remove(_owner.Analyzer, (key, stateKey));
            }

            private static bool IsEmpty(DiagnosticAnalysisResult result, DocumentId documentId)
                => !result.DocumentIdsOrEmpty.Contains(documentId);

            // we have this builder to avoid allocating collections unnecessarily.
            private sealed class Builder
            {
                private readonly Project _project;
                private readonly VersionStamp _version;
                private readonly ImmutableHashSet<DocumentId>? _documentIds;

                private ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>.Builder? _syntaxLocals;
                private ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>.Builder? _semanticLocals;
                private ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>.Builder? _nonLocals;
                private ImmutableArray<DiagnosticData> _others;

                public Builder(Project project, VersionStamp version, ImmutableHashSet<DocumentId>? documentIds = null)
                {
                    _project = project;
                    _version = version;
                    _documentIds = documentIds;
                }

                public void AddSyntaxLocals(DocumentId documentId, ImmutableArray<DiagnosticData> diagnostics)
                    => Add(ref _syntaxLocals, documentId, diagnostics);

                public void AddSemanticLocals(DocumentId documentId, ImmutableArray<DiagnosticData> diagnostics)
                    => Add(ref _semanticLocals, documentId, diagnostics);

                public void AddNonLocals(DocumentId documentId, ImmutableArray<DiagnosticData> diagnostics)
                    => Add(ref _nonLocals, documentId, diagnostics);

                public void AddOthers(ImmutableArray<DiagnosticData> diagnostics)
                    => _others = diagnostics;

                private void Add(ref ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>.Builder? locals, DocumentId documentId, ImmutableArray<DiagnosticData> diagnostics)
                {
                    if (_project.GetDocument(documentId)?.SupportsDiagnostics() == false)
                    {
                        return;
                    }

                    locals ??= ImmutableDictionary.CreateBuilder<DocumentId, ImmutableArray<DiagnosticData>>();
                    locals.Add(documentId, diagnostics);
                }

                public DiagnosticAnalysisResult ToResult()
                {
                    return DiagnosticAnalysisResult.Create(_project, _version,
                        _syntaxLocals?.ToImmutable() ?? ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>.Empty,
                        _semanticLocals?.ToImmutable() ?? ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>.Empty,
                        _nonLocals?.ToImmutable() ?? ImmutableDictionary<DocumentId, ImmutableArray<DiagnosticData>>.Empty,
                        _others.NullToEmpty(),
                        _documentIds);
                }
            }
        }
    }
}
