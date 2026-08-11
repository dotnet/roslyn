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
    private readonly Func<ImmutableArray<string>, CancellationToken, Task> _loadProjectsAsync;
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
            (projectFilePaths, cancellationToken) => projectSystem.LoadProjectsAsync(projectFilePaths, progressTracker: null, cancellationToken),
            () => globalOptionService.GetOption(LanguageServerProjectSystemOptionsStorage.LoadProjectsOnDemand),
            () => globalOptionService.GetOption(LspOptionsStorage.LspUsingDevkitFeatures),
            listener,
            loggerFactory)
    {
    }

    internal OnDemandProjectLoader(
        WorkspaceProjectDiscoveryService discoveryService,
        Func<ImmutableArray<string>, CancellationToken, Task> loadProjectsAsync,
        Func<bool> isEnabled,
        Func<bool> isUsingDevKit,
        IAsynchronousOperationListener listener,
        ILoggerFactory loggerFactory)
    {
        _discoveryService = discoveryService;
        _loadProjectsAsync = loadProjectsAsync;
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

            var task = Task.Run(() => LoadProjectsAsync(normalizedFilePath), CancellationToken.None);
            operation = new(task);
            _operations.Add(key, operation);
            _ = task.ContinueWith(
                _ => RemoveOperation(key),
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
            return operation;
        }
    }

    private void RemoveOperation(DocumentKey key)
    {
        lock (_gate)
            _operations.Remove(key);
    }

    private async Task LoadProjectsAsync(string filePath)
    {
        using var token = _listener.BeginAsyncOperation(nameof(LoadProjectsAsync));
        try
        {
            var candidateProjects = await _discoveryService.GetCandidateProjectsAsync(filePath, _shutdownSource.Token).ConfigureAwait(false);
            if (candidateProjects.IsEmpty)
                return;

            _logger.LogInformation("Loading {ProjectCount} project(s) on demand for '{DocumentPath}'.", candidateProjects.Length, filePath);
            await _loadProjectsAsync(candidateProjects, _shutdownSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (_shutdownSource.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (FatalError.ReportAndCatch(exception))
        {
            _logger.LogError(exception, "Failed to load projects on demand for '{DocumentPath}'.", filePath);
        }
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
