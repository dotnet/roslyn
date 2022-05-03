// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Microsoft.CodeAnalysis.Workspaces.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics.EngineV2
{
    internal partial class DiagnosticIncrementalAnalyzer
    {
        /// <summary>
        /// State for diagnostics that belong to a project at given time.
        /// </summary>
        private sealed class ProjectState
        {
            // project id of this state
            private readonly StateSet _owner;

            // last aggregated analysis result for this project saved
            private DiagnosticAnalysisResult _lastResult;

            public ProjectState(StateSet owner, ProjectId projectId)
            {
                _owner = owner;
                _lastResult = DiagnosticAnalysisResult.CreateInitialResult(projectId);
            }

            public bool FromBuild => _lastResult.FromBuild;

            public ImmutableHashSet<DocumentId> GetDocumentsWithDiagnostics()
                => _lastResult.DocumentIdsOrEmpty;

            public bool IsEmpty()
                => _lastResult.IsEmpty;

            public bool IsEmpty(DocumentId documentId)
                => IsEmpty(_lastResult, documentId);

            /// <summary>
            /// Return all diagnostics for the given project stored in this state
            /// </summary>
            public async Task<DiagnosticAnalysisResult> GetAnalysisDataAsync(Project project, bool avoidLoadingData, CancellationToken cancellationToken)
            {
                // make a copy of last result.
                var lastResult = _lastResult;
                Contract.ThrowIfFalse(lastResult.ProjectId == project.Id);

                if (lastResult.IsDefault)
                {
                    return await LoadInitialAnalysisDataAsync(project, cancellationToken).ConfigureAwait(false);
                }

                RoslynDebug.Assert(lastResult.DocumentIds != null);

                // PERF: avoid loading data if version is not right one.
                // avoid loading data flag is there as a strictly perf optimization.
                var version = await GetDiagnosticVersionAsync(project, cancellationToken).ConfigureAwait(false);
                if (avoidLoadingData && lastResult.Version != version)
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

                    if (!await TryGetDiagnosticsFromInMemoryStorageAsync(serializerVersion, document, builder, cancellationToken).ConfigureAwait(false))
                    {
                        Debug.Assert(lastResult.Version == VersionStamp.Default);

                        // this can happen if we merged back active file diagnostics back to project state but
                        // project state didn't have diagnostics for the file yet. (since project state was staled)
                        continue;
                    }
                }

                if (!await TryGetProjectDiagnosticsFromInMemoryStorageAsync(serializerVersion, project, builder, cancellationToken).ConfigureAwait(false))
                {
                    // this can happen if SaveAsync is not yet called but active file merge happened. one of case is if user did build before the very first
                    // analysis happened.
                }

                return builder.ToResult();
            }

            /// <summary>
            /// Return all diagnostics for the given document stored in this state including non local diagnostics for this document
            /// </summary>
            public async Task<DiagnosticAnalysisResult> GetAnalysisDataAsync(TextDocument document, bool avoidLoadingData, CancellationToken cancellationToken)
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
                    return DiagnosticAnalysisResult.CreateEmpty(lastResult.ProjectId, lastResult.Version);
                }

                // loading data can be canceled any time.
                var serializerVersion = lastResult.Version;
                var builder = new Builder(document.Project, lastResult.Version);

                if (!await TryGetDiagnosticsFromInMemoryStorageAsync(serializerVersion, document, builder, cancellationToken).ConfigureAwait(false))
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
            public async Task<DiagnosticAnalysisResult> GetProjectAnalysisDataAsync(Project project, bool avoidLoadingData, CancellationToken cancellationToken)
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

                // if given document doesn't have any diagnostics, return empty.
                if (lastResult.IsEmpty)
                {
                    return DiagnosticAnalysisResult.CreateEmpty(lastResult.ProjectId, lastResult.Version);
                }

                // loading data can be canceled any time.
                var serializerVersion = lastResult.Version;
                var builder = new Builder(project, lastResult.Version);

                if (!await TryGetProjectDiagnosticsFromInMemoryStorageAsync(serializerVersion, project, builder, cancellationToken).ConfigureAwait(false))
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

                // save last aggregated form of analysis result
                _lastResult = result.ToAggregatedForm();

                // serialization can't be canceled.
                var serializerVersion = result.Version;
                foreach (var documentId in result.DocumentIds)
                {
                    var document = project.GetTextDocument(documentId);

                    // If we couldn't find a normal document, and all features are enabled for source generated
                    // documents, attempt to locate a matching source generated document in the project.
                    if (document is null
                        && project.Solution.Workspace.Services.GetService<IWorkspaceConfigurationService>()?.Options.EnableOpeningSourceGeneratedFiles == true)
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

                    await AddToInMemoryStorageAsync(serializerVersion, project, document, document.Id, _owner.SyntaxStateName, result.GetDocumentDiagnostics(document.Id, AnalysisKind.Syntax)).ConfigureAwait(false);
                    await AddToInMemoryStorageAsync(serializerVersion, project, document, document.Id, _owner.SemanticStateName, result.GetDocumentDiagnostics(document.Id, AnalysisKind.Semantic)).ConfigureAwait(false);
                    await AddToInMemoryStorageAsync(serializerVersion, project, document, document.Id, _owner.NonLocalStateName, result.GetDocumentDiagnostics(document.Id, AnalysisKind.NonLocal)).ConfigureAwait(false);
                }

                await AddToInMemoryStorageAsync(serializerVersion, project, document: null, result.ProjectId, _owner.NonLocalStateName, result.GetOtherDiagnostics()).ConfigureAwait(false);
            }

            public void ResetVersion()
            {
                // reset version of cached data so that we can recalculate new data (ex, OnDocumentReset)
                _lastResult = _lastResult.Reset();
            }

            public async ValueTask MergeAsync(ActiveFileState state, TextDocument document, IGlobalOptionService globalOptions)
            {
                Contract.ThrowIfFalse(state.DocumentId == document.Id);

                // merge active file state to project state
                var lastResult = _lastResult;

                var syntax = state.GetAnalysisData(AnalysisKind.Syntax);
                var semantic = state.GetAnalysisData(AnalysisKind.Semantic);

                var project = document.Project;

                // if project didn't successfully loaded, then it is same as FSA off
                var fullAnalysis = globalOptions.GetBackgroundAnalysisScope(project.Language) == BackgroundAnalysisScope.FullSolution &&
                                   await project.HasSuccessfullyLoadedAsync(CancellationToken.None).ConfigureAwait(false);

                // keep from build flag if full analysis is off
                var fromBuild = fullAnalysis ? false : lastResult.FromBuild;

                var languageServices = document.Project.LanguageServices;
                var simplifierOptions = (languageServices.GetService<ISimplifierOptionsStorage>() != null) ? globalOptions.GetSimplifierOptions(languageServices) : null;
                var openFileOnlyAnalyzer = _owner.Analyzer.IsOpenFileOnly(simplifierOptions);

                // if it is allowed to keep project state, check versions and if they are same, bail out.
                // if full solution analysis is off or we are asked to reset document state, we always merge.
                if (fullAnalysis && !openFileOnlyAnalyzer &&
                    syntax.Version != VersionStamp.Default &&
                    syntax.Version == semantic.Version &&
                    syntax.Version == lastResult.Version)
                {
                    // all data is in sync already.
                    return;
                }

                // we have mixed versions or full analysis is off, set it to default so that it can be re-calculated next time so data can be in sync.
                var version = VersionStamp.Default;

                // serialization can't be canceled.
                var serializerVersion = version;

                // save active file diagnostics back to project state
                await AddToInMemoryStorageAsync(serializerVersion, project, document, document.Id, _owner.SyntaxStateName, syntax.Items).ConfigureAwait(false);
                await AddToInMemoryStorageAsync(serializerVersion, project, document, document.Id, _owner.SemanticStateName, semantic.Items).ConfigureAwait(false);

                // save last aggregated form of analysis result
                _lastResult = _lastResult.UpdateAggregatedResult(version, state.DocumentId, fromBuild);
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

            private async Task<DiagnosticAnalysisResult> LoadInitialAnalysisDataAsync(Project project, CancellationToken cancellationToken)
            {
                // loading data can be canceled any time.
                var version = await GetDiagnosticVersionAsync(project, cancellationToken).ConfigureAwait(false);
                var serializerVersion = version;
                var builder = new Builder(project, version);

                foreach (var document in project.Documents)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!await TryGetDiagnosticsFromInMemoryStorageAsync(serializerVersion, document, builder, cancellationToken).ConfigureAwait(false))
                        continue;
                }

                if (!await TryGetProjectDiagnosticsFromInMemoryStorageAsync(serializerVersion, project, builder, cancellationToken).ConfigureAwait(false))
                    return DiagnosticAnalysisResult.CreateEmpty(project.Id, VersionStamp.Default);

                return builder.ToResult();
            }

            private async Task<DiagnosticAnalysisResult> LoadInitialAnalysisDataAsync(TextDocument document, CancellationToken cancellationToken)
            {
                // loading data can be canceled any time.
                var project = document.Project;

                var version = await GetDiagnosticVersionAsync(project, cancellationToken).ConfigureAwait(false);
                var serializerVersion = version;
                var builder = new Builder(project, version);

                if (!await TryGetDiagnosticsFromInMemoryStorageAsync(serializerVersion, document, builder, cancellationToken).ConfigureAwait(false))
                {
                    return DiagnosticAnalysisResult.CreateEmpty(project.Id, VersionStamp.Default);
                }

                return builder.ToResult();
            }

            private async Task<DiagnosticAnalysisResult> LoadInitialProjectAnalysisDataAsync(Project project, CancellationToken cancellationToken)
            {
                // loading data can be canceled any time.
                var version = await GetDiagnosticVersionAsync(project, cancellationToken).ConfigureAwait(false);
                var serializerVersion = version;
                var builder = new Builder(project, version);

                if (!await TryGetProjectDiagnosticsFromInMemoryStorageAsync(serializerVersion, project, builder, cancellationToken).ConfigureAwait(false))
                    return DiagnosticAnalysisResult.CreateEmpty(project.Id, VersionStamp.Default);

                return builder.ToResult();
            }

            private ValueTask AddToInMemoryStorageAsync(VersionStamp serializerVersion, Project project, TextDocument? document, object key, string stateKey, ImmutableArray<DiagnosticData> diagnostics)
            {
                Contract.ThrowIfFalse(document == null || document.Project == project);

                // if serialization fail, hold it in the memory
                InMemoryStorage.Cache(_owner.Analyzer, (key, stateKey), new CacheEntry(serializerVersion, diagnostics));
                return default;
            }

            private async ValueTask<bool> TryGetDiagnosticsFromInMemoryStorageAsync(VersionStamp serializerVersion, TextDocument document, Builder builder, CancellationToken cancellationToken)
            {
                var success = true;
                var project = document.Project;
                var documentId = document.Id;

                var diagnostics = await GetDiagnosticsFromInMemoryStorageAsync(serializerVersion, project, document, documentId, _owner.SyntaxStateName, cancellationToken).ConfigureAwait(false);
                if (!diagnostics.IsDefault)
                {
                    builder.AddSyntaxLocals(documentId, diagnostics);
                }
                else
                {
                    success = false;
                }

                diagnostics = await GetDiagnosticsFromInMemoryStorageAsync(serializerVersion, project, document, documentId, _owner.SemanticStateName, cancellationToken).ConfigureAwait(false);
                if (!diagnostics.IsDefault)
                {
                    builder.AddSemanticLocals(documentId, diagnostics);
                }
                else
                {
                    success = false;
                }

                diagnostics = await GetDiagnosticsFromInMemoryStorageAsync(serializerVersion, project, document, documentId, _owner.NonLocalStateName, cancellationToken).ConfigureAwait(false);
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

            private async ValueTask<bool> TryGetProjectDiagnosticsFromInMemoryStorageAsync(VersionStamp serializerVersion, Project project, Builder builder, CancellationToken cancellationToken)
            {
                var diagnostics = await GetDiagnosticsFromInMemoryStorageAsync(serializerVersion, project, document: null, project.Id, _owner.NonLocalStateName, cancellationToken).ConfigureAwait(false);
                if (!diagnostics.IsDefault)
                {
                    builder.AddOthers(diagnostics);
                    return true;
                }

                return false;
            }

            private ValueTask<ImmutableArray<DiagnosticData>> GetDiagnosticsFromInMemoryStorageAsync(
                VersionStamp serializerVersion, Project project, TextDocument? document, object key, string stateKey, CancellationToken _)
            {
                Contract.ThrowIfFalse(document == null || document.Project == project);

                return InMemoryStorage.TryGetValue(_owner.Analyzer, (key, stateKey), out var entry) && serializerVersion == entry.Version
                    ? new(entry.Diagnostics)
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
                RemoveInMemoryCacheEntry(id, _owner.SyntaxStateName);
                RemoveInMemoryCacheEntry(id, _owner.SemanticStateName);
                RemoveInMemoryCacheEntry(id, _owner.NonLocalStateName);
            }

            private void RemoveInMemoryCacheEntry(object key, string stateKey)
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
