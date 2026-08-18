// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
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
    {
        var hostWorkspace = lspServices.GetRequiredService<LanguageServerWorkspaceFactory>().HostWorkspace;
        return new OnDemandProjectLoader(
            lspServices.GetRequiredService<WorkspaceProjectDiscoveryService>(),
            lspServices.GetRequiredService<LanguageServerProjectSystem>(),
            filePath => !hostWorkspace.CurrentSolution.GetDocumentIdsWithFilePath(filePath).IsEmpty,
            globalOptionService,
            listenerProvider.GetListener(FeatureAttribute.Workspace),
            lspServices.GetRequiredService<ILoggerFactory>());
    }
}

internal sealed class OnDemandProjectLoader : IOnDemandProjectLoader, IDisposable
{
    private readonly WorkspaceProjectDiscoveryService _discoveryService;
    private readonly Func<string, Task<LanguageServerProjectLoadHandle>> _beginLoadingProjectAsync;
    private readonly Func<ImmutableArray<ProjectId>, ImmutableArray<string>> _getProjectReferences;
    private readonly Func<ImmutableArray<LanguageServerProjectLoadHandle>> _getPendingProjectLoadHandles;
    private readonly Func<string, bool> _isDocumentInHostWorkspace;
    private readonly Func<bool> _isEnabled;
    private readonly Func<bool> _isUsingDevKit;
    private readonly IAsynchronousOperationListener _listener;
    private readonly ILogger _logger;
    private readonly CancellationTokenSource _shutdownSource = new();
    private readonly object _gate = new();
    private readonly Dictionary<ProjectKey, OnDemandProjectLoadOperation> _operations = new();

    public OnDemandProjectLoader(
        WorkspaceProjectDiscoveryService discoveryService,
        LanguageServerProjectSystem projectSystem,
        Func<string, bool> isDocumentInHostWorkspace,
        IGlobalOptionService globalOptionService,
        IAsynchronousOperationListener listener,
        ILoggerFactory loggerFactory)
        : this(
            discoveryService,
            projectSystem.BeginLoadingProjectAsync,
            projectSystem.GetProjectReferences,
            projectSystem.GetPendingProjectLoadHandles,
            isDocumentInHostWorkspace,
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
        Func<string, bool> isDocumentInHostWorkspace,
        Func<bool> isEnabled,
        Func<bool> isUsingDevKit,
        IAsynchronousOperationListener listener,
        ILoggerFactory loggerFactory)
    {
        _discoveryService = discoveryService;
        _beginLoadingProjectAsync = beginLoadingProjectAsync;
        _getProjectReferences = getProjectReferences;
        _getPendingProjectLoadHandles = getPendingProjectLoadHandles;
        _isDocumentInHostWorkspace = isDocumentInHostWorkspace;
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
        if (_isDocumentInHostWorkspace(filePath))
            return OnDemandProjectLoadOperation.Completed;

        var discoveryTask = Task.Run(() => DiscoverProjects(filePath), CancellationToken.None);
        return new OnDemandProjectLoadOperation(LoadDiscoveredProjectsAsync(discoveryTask));
    }

    private async Task LoadDiscoveredProjectsAsync(Task<ProjectDiscoveryResult> discoveryTask)
    {
        var (workspaceFolder, candidateProjects) = await discoveryTask.ConfigureAwait(false);
        if (workspaceFolder is null || candidateProjects.IsEmpty)
            return;

        var operations = candidateProjects.SelectAsArray(projectPath => GetOrCreateLoadOperation(projectPath, workspaceFolder));
        await Task.WhenAll(operations.SelectAsArray(
            operation => operation.WaitAsync(_shutdownSource.Token))).ConfigureAwait(false);
    }

    private OnDemandProjectLoadOperation GetOrCreateLoadOperation(string projectPath, string workspaceFolder)
    {
        projectPath = Path.GetFullPath(projectPath);
        var key = new ProjectKey(projectPath, workspaceFolder);
        lock (_gate)
        {
            if (_operations.TryGetValue(key, out var operation))
                return operation;

            var projectCompletion = LoadProjectsAsync(Task.FromResult(ImmutableArray.Create(projectPath)), projectPath);
            operation = new(projectCompletion);
            _operations.Add(key, operation);
            _ = projectCompletion.ContinueWith(
                _ => RemoveOperation(key, operation),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
            return operation;
        }
    }

    public OnDemandProjectLoadOperation GetWorkspaceLoadOperation()
    {
        var completion = WaitForPendingProjectLoadsAsync(_shutdownSource.Token);
        return new OnDemandProjectLoadOperation(completion);
    }

    private async Task WaitForPendingProjectLoadsAsync(CancellationToken cancellationToken)
    {
        var handles = _getPendingProjectLoadHandles();
        await Task.WhenAll(handles.SelectAsArray(handle => handle.Completion.WaitAsync(cancellationToken))).ConfigureAwait(false);
    }

    private void RemoveOperation(ProjectKey key, OnDemandProjectLoadOperation operation)
    {
        lock (_gate)
        {
            if (_operations.TryGetValue(key, out var currentOperation) && ReferenceEquals(currentOperation, operation))
                _operations.Remove(key);
        }
    }

    private ProjectDiscoveryResult DiscoverProjects(string filePath)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = _discoveryService.DiscoverProjects(filePath, _shutdownSource.Token);
        _logger.LogDebug(
            "Discovered {ProjectCount} candidate project(s) for '{DocumentPath}' in {ElapsedMilliseconds} ms.",
            result.Projects.Length,
            filePath,
            stopwatch.ElapsedMilliseconds);
        return result;
    }

    private async Task LoadProjectsAsync(
        Task<ImmutableArray<string>> candidateProjectsTask,
        string filePath)
    {
        using var token = _listener.BeginAsyncOperation(nameof(LoadProjectsAsync));
        try
        {
            var candidateProjects = await candidateProjectsTask.ConfigureAwait(false);
            if (candidateProjects.IsEmpty)
                return;

            _logger.LogInformation("Loading {ProjectCount} project(s) on demand for '{DocumentPath}'.", candidateProjects.Length, filePath);
            await LoadProjectClosureAsync(candidateProjects, _shutdownSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (_shutdownSource.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (FatalError.ReportAndCatch(exception))
        {
            _logger.LogError(exception, "Failed to load projects on demand for '{DocumentPath}'.", filePath);
        }
    }

    private async Task LoadProjectClosureAsync(
        ImmutableArray<string> projectFilePaths,
        CancellationToken cancellationToken)
    {
        var pendingLoads = new List<Task<(string ProjectPath, LanguageServerProjectLoadResult Result)>>();
        var visitedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var projectFilePath in projectFilePaths)
            QueueProject(projectFilePath);

        while (pendingLoads.Count > 0)
        {
            // Process loads as they complete, rather than in enqueue order, so a slow project doesn't hold up
            // ones that already finished from expanding the dependency closure.
            var completedTask = await Task.WhenAny(pendingLoads).WaitAsync(cancellationToken).ConfigureAwait(false);
            pendingLoads.Remove(completedTask);

            var (_, result) = await completedTask.ConfigureAwait(false);
            QueueReferences(result);
        }

        void QueueReferences(LanguageServerProjectLoadResult result)
        {
            if (result.Status == LanguageServerProjectLoadStatus.Loaded)
            {
                foreach (var reference in _getProjectReferences(result.ProjectIds))
                    QueueProject(reference);
            }
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
    }

    public void Dispose()
    {
        _shutdownSource.Cancel();
        _shutdownSource.Dispose();
    }

    private readonly record struct ProjectKey(string ProjectPath, string WorkspaceFolder)
    {
        public bool Equals(ProjectKey other)
            => StringComparer.OrdinalIgnoreCase.Equals(ProjectPath, other.ProjectPath) &&
               StringComparer.OrdinalIgnoreCase.Equals(WorkspaceFolder, other.WorkspaceFolder);

        public override int GetHashCode()
            => Hash.Combine(StringComparer.OrdinalIgnoreCase.GetHashCode(ProjectPath), StringComparer.OrdinalIgnoreCase.GetHashCode(WorkspaceFolder));
    }
}
