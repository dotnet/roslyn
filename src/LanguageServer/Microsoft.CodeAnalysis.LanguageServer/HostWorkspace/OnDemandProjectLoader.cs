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
    private readonly Func<string, Task<LoadedProject>> _beginLoadingProjectAsync;
    private readonly Func<LoadedProject, CancellationToken, Task<bool>> _waitForLoadAsync;
    private readonly Func<LoadedProject, Task<ImmutableArray<string>>> _getProjectReferencesAsync;
    private readonly Func<CancellationToken, Task> _waitForActiveProjectLoadsAsync;
    private readonly Func<string, bool> _isDocumentInHostWorkspace;
    private readonly Func<bool> _isEnabled;
    private readonly Func<bool> _isUsingDevKit;
    private readonly IAsynchronousOperationListener _listener;
    private readonly ILogger _logger;
    private readonly CancellationTokenSource _shutdownSource = new();
    private readonly object _gate = new();
    private readonly Dictionary<string, OnDemandProjectLoadOperation> _operations = new(PathUtilities.Comparer);

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
            static (project, cancellationToken) => project.WaitForLoadAsync(cancellationToken).AsTask(),
            projectSystem.GetProjectReferencesAsync,
            projectSystem.WaitForActiveProjectLoadsAsync,
            isDocumentInHostWorkspace,
            () => globalOptionService.GetOption(LanguageServerProjectSystemOptionsStorage.LoadProjectsOnDemand),
            () => globalOptionService.GetOption(LspOptionsStorage.LspUsingDevkitFeatures),
            listener,
            loggerFactory)
    {
    }

    internal OnDemandProjectLoader(
        WorkspaceProjectDiscoveryService discoveryService,
        Func<string, Task<LoadedProject>> beginLoadingProjectAsync,
        Func<LoadedProject, CancellationToken, Task<bool>> waitForLoadAsync,
        Func<LoadedProject, Task<ImmutableArray<string>>> getProjectReferencesAsync,
        Func<CancellationToken, Task> waitForActiveProjectLoadsAsync,
        Func<string, bool> isDocumentInHostWorkspace,
        Func<bool> isEnabled,
        Func<bool> isUsingDevKit,
        IAsynchronousOperationListener listener,
        ILoggerFactory loggerFactory)
    {
        _discoveryService = discoveryService;
        _beginLoadingProjectAsync = beginLoadingProjectAsync;
        _waitForLoadAsync = waitForLoadAsync;
        _getProjectReferencesAsync = getProjectReferencesAsync;
        _waitForActiveProjectLoadsAsync = waitForActiveProjectLoadsAsync;
        _isDocumentInHostWorkspace = isDocumentInHostWorkspace;
        _isEnabled = isEnabled;
        _isUsingDevKit = isUsingDevKit;
        _listener = listener;
        _logger = loggerFactory.CreateLogger<OnDemandProjectLoader>();
    }

    public OnDemandProjectLoadOperation StartLoading(DocumentUri uri, ImmutableHashSet<string> workspaceFolders)
    {
        if (!_isEnabled() ||
            _isUsingDevKit() ||
            uri.ParsedDocumentUri?.IsFile != true)
        {
            return OnDemandProjectLoadOperation.Completed;
        }

        var filePath = uri.GetDocumentFilePathFromUri();
        if (_isDocumentInHostWorkspace(filePath))
            return OnDemandProjectLoadOperation.Completed;

        var discoveryTask = Task.Run(() => DiscoverProjects(filePath, workspaceFolders), CancellationToken.None);
        return new OnDemandProjectLoadOperation(LoadDiscoveredProjectsAsync(discoveryTask));
    }

    private async Task LoadDiscoveredProjectsAsync(Task<ImmutableArray<string>> discoveryTask)
    {
        var candidateProjects = await discoveryTask.ConfigureAwait(false);
        if (candidateProjects.IsEmpty)
            return;

        var operations = candidateProjects.SelectAsArray(GetOrCreateLoadOperation);
        await Task.WhenAll(operations.SelectAsArray(
            operation => operation.WaitAsync(_shutdownSource.Token))).ConfigureAwait(false);
    }

    private OnDemandProjectLoadOperation GetOrCreateLoadOperation(string projectPath)
    {
        projectPath = Path.GetFullPath(projectPath);
        lock (_gate)
        {
            if (_operations.TryGetValue(projectPath, out var operation))
                return operation;

            var projectCompletion = LoadProjectsAsync(projectPath);
            operation = new(projectCompletion);
            _operations.Add(projectPath, operation);
            _ = projectCompletion.ContinueWith(
                _ => RemoveOperation(projectPath, operation),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
            return operation;
        }
    }

    public OnDemandProjectLoadOperation GetWorkspaceLoadOperation()
    {
        var completion = _waitForActiveProjectLoadsAsync(_shutdownSource.Token);
        return new OnDemandProjectLoadOperation(completion);
    }

    private void RemoveOperation(string projectPath, OnDemandProjectLoadOperation operation)
    {
        lock (_gate)
        {
            if (_operations.TryGetValue(projectPath, out var currentOperation) && ReferenceEquals(currentOperation, operation))
                _operations.Remove(projectPath);
        }
    }

    private ImmutableArray<string> DiscoverProjects(string filePath, ImmutableHashSet<string> workspaceFolders)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = _discoveryService.DiscoverProjects(filePath, workspaceFolders, _shutdownSource.Token);
        _logger.LogDebug(
            "Discovered {ProjectCount} candidate project(s) for '{DocumentPath}' in {ElapsedMilliseconds} ms.",
            result.Length,
            filePath,
            stopwatch.ElapsedMilliseconds);
        return result;
    }

    private async Task LoadProjectsAsync(string projectPath)
    {
        using var token = _listener.BeginAsyncOperation(nameof(LoadProjectsAsync));
        try
        {
            _logger.LogInformation("Loading project on demand for '{ProjectPath}'.", projectPath);
            await LoadProjectClosureAsync([projectPath], _shutdownSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (_shutdownSource.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (FatalError.ReportAndCatch(exception))
        {
            _logger.LogError(exception, "Failed to load projects on demand for '{ProjectPath}'.", projectPath);
        }
    }

    private async Task LoadProjectClosureAsync(
        ImmutableArray<string> projectFilePaths,
        CancellationToken cancellationToken)
    {
        var pendingLoads = new List<Task<(LoadedProject project, bool loadedSuccessfully)>>();
        var visitedPaths = new HashSet<string>(PathUtilities.Comparer);

        foreach (var projectFilePath in projectFilePaths)
            QueueProject(projectFilePath);

        while (pendingLoads.Count > 0)
        {
            // Process loads as they complete, rather than in enqueue order, so a slow project doesn't hold up
            // ones that already finished from expanding the dependency closure.
            var completedTask = await Task.WhenAny(pendingLoads).WaitAsync(cancellationToken).ConfigureAwait(false);
            pendingLoads.Remove(completedTask);

            var (project, loadedSuccessfully) = await completedTask.ConfigureAwait(false);
            if (loadedSuccessfully)
            {
                foreach (var reference in await _getProjectReferencesAsync(project).ConfigureAwait(false))
                    QueueProject(reference);
            }
        }

        void QueueProject(string projectFilePath)
        {
            projectFilePath = Path.GetFullPath(projectFilePath);
            if (visitedPaths.Add(projectFilePath))
                pendingLoads.Add(LoadProjectAsync(projectFilePath));
        }

        async Task<(LoadedProject project, bool loadedSuccessfully)> LoadProjectAsync(string projectFilePath)
        {
            var project = await _beginLoadingProjectAsync(projectFilePath).ConfigureAwait(false);
            var loadedSuccessfully = await _waitForLoadAsync(project, cancellationToken).ConfigureAwait(false);
            return (project, loadedSuccessfully);
        }
    }

    public void Dispose()
    {
        _shutdownSource.Cancel();
        _shutdownSource.Dispose();
    }

}
