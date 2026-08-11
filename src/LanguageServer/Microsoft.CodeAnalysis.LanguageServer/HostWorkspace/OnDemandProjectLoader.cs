// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.Extensions.Logging;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

[ExportCSharpVisualBasicLspServiceFactory(typeof(IOnDemandProjectLoader)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class OnDemandProjectLoaderFactory(
    IGlobalOptionService globalOptionService,
    IAsynchronousOperationListenerProvider listenerProvider) : ILspServiceFactory
{
    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
        => new OnDemandProjectLoader(
            lspServices.GetRequiredService<WorkspaceProjectDiscoveryService>(),
            lspServices.GetRequiredService<LanguageServerProjectSystem>(),
            globalOptionService,
            listenerProvider.GetListener(FeatureAttribute.Workspace),
            lspServices.GetRequiredService<ILoggerFactory>());
}

internal sealed class OnDemandProjectLoader : IOnDemandProjectLoader, IDisposable
{
    private readonly WorkspaceProjectDiscoveryService _discoveryService;
    private readonly Func<string, Task<LanguageServerProjectLoadHandle>> _beginLoadingProjectAsync;
    private readonly Func<ImmutableArray<ProjectId>, ImmutableArray<string>> _getProjectReferences;
    private readonly Func<ImmutableArray<LanguageServerProjectLoadHandle>> _getPendingProjectLoadHandles;
    private readonly Func<bool> _isEnabled;
    private readonly Func<bool> _isUsingDevKit;
    private readonly IAsynchronousOperationListener _listener;
    private readonly ILogger _logger;
    private readonly CancellationTokenSource _shutdownSource = new();
    private readonly object _gate = new();
    private readonly Dictionary<DocumentKey, OnDemandProjectLoadOperation> _operations = new();

    public OnDemandProjectLoader(
        WorkspaceProjectDiscoveryService discoveryService,
        LanguageServerProjectSystem projectSystem,
        IGlobalOptionService globalOptionService,
        IAsynchronousOperationListener listener,
        ILoggerFactory loggerFactory)
        : this(
            discoveryService,
            projectSystem.BeginLoadingProjectAsync,
            projectSystem.GetProjectReferences,
            projectSystem.GetPendingProjectLoadHandles,
            () => globalOptionService.GetOption(LanguageServerProjectSystemOptionsStorage.LoadProjectsOnDemand),
            () => globalOptionService.GetOption(LspOptionsStorage.LspUsingDevkitFeatures),
            listener,
            loggerFactory)
    {
    }

    internal OnDemandProjectLoader(
        WorkspaceProjectDiscoveryService discoveryService,
        Func<string, Task<LanguageServerProjectLoadHandle>> beginLoadingProjectAsync,
        Func<ImmutableArray<ProjectId>, ImmutableArray<string>> getProjectReferences,
        Func<ImmutableArray<LanguageServerProjectLoadHandle>> getPendingProjectLoadHandles,
        Func<bool> isEnabled,
        Func<bool> isUsingDevKit,
        IAsynchronousOperationListener listener,
        ILoggerFactory loggerFactory)
    {
        _discoveryService = discoveryService;
        _beginLoadingProjectAsync = beginLoadingProjectAsync;
        _getProjectReferences = getProjectReferences;
        _getPendingProjectLoadHandles = getPendingProjectLoadHandles;
        _isEnabled = isEnabled;
        _isUsingDevKit = isUsingDevKit;
        _listener = listener;
        _logger = loggerFactory.CreateLogger<OnDemandProjectLoader>();
    }

    public OnDemandProjectLoadOperation StartLoading(DocumentUri uri)
    {
        if (!_isEnabled() ||
            _isUsingDevKit() ||
            !string.Equals(uri.ParsedUri?.Scheme, Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase))
        {
            return OnDemandProjectLoadOperation.Completed;
        }

        var filePath = uri.GetDocumentFilePathFromUri();
        if (!_discoveryService.TryGetWorkspaceFolder(filePath, out var normalizedFilePath, out var workspaceFolder))
            return OnDemandProjectLoadOperation.Completed;

        var key = new DocumentKey(normalizedFilePath, workspaceFolder!);
        lock (_gate)
        {
            if (_operations.TryGetValue(key, out var operation))
                return operation;

            var candidateProjectsTask = Task.Run(() => GetCandidateProjectsAsync(normalizedFilePath), CancellationToken.None);
            var rootCompletion = LoadProjectsAsync(candidateProjectsTask, includeDependencies: false, normalizedFilePath);
            var projectCompletion = GetLoadResultAsync(rootCompletion);
            operation = new(projectCompletion, () => LoadDependenciesAsync(candidateProjectsTask, rootCompletion, normalizedFilePath));
            _operations.Add(key, operation);
            _ = projectCompletion.ContinueWith(
                _ => RemoveOperation(key),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
            return operation;
        }
    }

    public OnDemandProjectLoadOperation GetWorkspaceLoadOperation()
    {
        var completion = WaitForPendingProjectLoadsAsync(_shutdownSource.Token);
        return new OnDemandProjectLoadOperation(completion, dependencyCompletionFactory: null);
    }

    private async Task<OnDemandProjectLoadResult> WaitForPendingProjectLoadsAsync(CancellationToken cancellationToken)
    {
        var handles = _getPendingProjectLoadHandles();
        await Task.WhenAll(handles.SelectAsArray(handle => handle.Completion.WaitAsync(cancellationToken))).ConfigureAwait(false);
        return OnDemandProjectLoadResult.Empty;
    }

    private void RemoveOperation(DocumentKey key)
    {
        lock (_gate)
            _operations.Remove(key);
    }

    private async Task<ImmutableArray<string>> GetCandidateProjectsAsync(string filePath)
        => await _discoveryService.GetCandidateProjectsAsync(filePath, _shutdownSource.Token).ConfigureAwait(false);

    private static async Task<OnDemandProjectLoadResult> GetLoadResultAsync(Task<ProjectClosureLoadResult> completion)
        => (await completion.ConfigureAwait(false)).LoadResult;

    private async Task<OnDemandProjectLoadResult> LoadDependenciesAsync(
        Task<ImmutableArray<string>> candidateProjectsTask,
        Task<ProjectClosureLoadResult> rootCompletion,
        string filePath)
    {
        var rootResult = await rootCompletion.ConfigureAwait(false);
        var dependencyResult = await LoadProjectsAsync(
            candidateProjectsTask, includeDependencies: true, filePath, rootResult.ProjectResults).ConfigureAwait(false);
        return dependencyResult.LoadResult;
    }

    private async Task<ProjectClosureLoadResult> LoadProjectsAsync(
        Task<ImmutableArray<string>> candidateProjectsTask,
        bool includeDependencies,
        string filePath,
        ImmutableDictionary<string, LanguageServerProjectLoadResult>? initialProjectResults = null)
    {
        using var token = _listener.BeginAsyncOperation(nameof(LoadProjectsAsync));
        try
        {
            var candidateProjects = await candidateProjectsTask.ConfigureAwait(false);
            if (candidateProjects.IsEmpty)
                return ProjectClosureLoadResult.Empty;

            _logger.LogInformation("Loading {ProjectCount} project(s) on demand for '{DocumentPath}'.", candidateProjects.Length, filePath);
            return await LoadProjectClosureAsync(
                candidateProjects, includeDependencies, initialProjectResults, _shutdownSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (_shutdownSource.IsCancellationRequested)
        {
            return ProjectClosureLoadResult.Empty;
        }
        catch (Exception exception) when (FatalError.ReportAndCatch(exception))
        {
            _logger.LogError(exception, "Failed to load projects on demand for '{DocumentPath}'.", filePath);
            return ProjectClosureLoadResult.Empty;
        }
    }

    private async Task<ProjectClosureLoadResult> LoadProjectClosureAsync(
        ImmutableArray<string> projectFilePaths,
        bool includeDependencies,
        ImmutableDictionary<string, LanguageServerProjectLoadResult>? initialProjectResults,
        CancellationToken cancellationToken)
    {
        var pendingLoads = new List<Task<(string ProjectPath, LanguageServerProjectLoadResult Result)>>();
        var visitedPaths = new HashSet<string>(PathUtilities.Comparer);
        var loadedProjects = new Dictionary<string, (bool Loaded, ImmutableArray<string> References)>(PathUtilities.Comparer);
        var projectResults = new Dictionary<string, LanguageServerProjectLoadResult>(PathUtilities.Comparer);

        if (initialProjectResults is not null)
        {
            foreach (var projectPath in initialProjectResults.Keys)
                visitedPaths.Add(projectPath);

            foreach (var (projectPath, result) in initialProjectResults)
                RecordResult(projectPath, result);
        }

        foreach (var projectFilePath in projectFilePaths)
            QueueProject(projectFilePath);

        while (pendingLoads.Count > 0)
        {
            // Process loads as they complete, rather than in enqueue order, so a slow project doesn't hold up
            // ones that already finished from expanding the dependency closure.
            var completedTask = await Task.WhenAny(pendingLoads).WaitAsync(cancellationToken).ConfigureAwait(false);
            pendingLoads.Remove(completedTask);

            var (projectPath, result) = await completedTask.ConfigureAwait(false);
            RecordResult(projectPath, result);
        }

        var completeness = ImmutableDictionary.CreateBuilder<string, bool>(PathUtilities.Comparer);
        var loadedRoots = ImmutableHashSet.CreateBuilder<string>(PathUtilities.Comparer);
        foreach (var projectFilePath in projectFilePaths)
        {
            var normalizedPath = Path.GetFullPath(projectFilePath);
            completeness[normalizedPath] = IsComplete(normalizedPath, []);
            if (loadedProjects.TryGetValue(normalizedPath, out var project) && project.Loaded)
                loadedRoots.Add(normalizedPath);
        }

        var loadResult = new OnDemandProjectLoadResult(completeness.ToImmutable(), loadedRoots.ToImmutable());
        return new(loadResult, ImmutableDictionary.CreateRange(PathUtilities.Comparer, projectResults));

        void RecordResult(string projectPath, LanguageServerProjectLoadResult result)
        {
            var references = includeDependencies && result.Status == LanguageServerProjectLoadStatus.Loaded
                ? _getProjectReferences(result.ProjectIds)
                : [];
            loadedProjects.Add(projectPath, (result.Status == LanguageServerProjectLoadStatus.Loaded, references));
            projectResults.Add(projectPath, result);

            foreach (var reference in references)
                QueueProject(reference);
        }

        void QueueProject(string projectFilePath)
        {
            projectFilePath = Path.GetFullPath(projectFilePath);
            if (visitedPaths.Add(projectFilePath))
                pendingLoads.Add(LoadProjectAsync(projectFilePath));
        }

        async Task<(string, LanguageServerProjectLoadResult)> LoadProjectAsync(string projectFilePath)
        {
            var handle = await _beginLoadingProjectAsync(projectFilePath).ConfigureAwait(false);
            return (projectFilePath, await handle.Completion.WaitAsync(cancellationToken).ConfigureAwait(false));
        }

        bool IsComplete(string projectFilePath, HashSet<string> visiting)
        {
            if (!loadedProjects.TryGetValue(projectFilePath, out var project) || !project.Loaded)
                return false;

            if (!visiting.Add(projectFilePath))
                return true;

            foreach (var reference in project.References)
            {
                if (!IsComplete(reference, visiting))
                    return false;
            }

            visiting.Remove(projectFilePath);
            return true;
        }
    }

    private readonly record struct ProjectClosureLoadResult(
        OnDemandProjectLoadResult LoadResult,
        ImmutableDictionary<string, LanguageServerProjectLoadResult> ProjectResults)
    {
        public static ProjectClosureLoadResult Empty { get; } = new(
            OnDemandProjectLoadResult.Empty,
            ImmutableDictionary<string, LanguageServerProjectLoadResult>.Empty.WithComparers(PathUtilities.Comparer));
    }

    public void Dispose()
    {
        _shutdownSource.Cancel();
        _shutdownSource.Dispose();
    }

    private readonly record struct DocumentKey(string FilePath, string WorkspaceFolder)
    {
        public bool Equals(DocumentKey other)
            => PathUtilities.Comparer.Equals(FilePath, other.FilePath) && PathUtilities.Comparer.Equals(WorkspaceFolder, other.WorkspaceFolder);

        public override int GetHashCode()
            => Hash.Combine(PathUtilities.Comparer.GetHashCode(FilePath), PathUtilities.Comparer.GetHashCode(WorkspaceFolder));
    }
}
