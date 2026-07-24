// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.ProjectSystem;
using Microsoft.Extensions.Logging;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

[ExportCSharpVisualBasicLspServiceFactory(typeof(WorkspaceProjectDiscoveryService)), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class WorkspaceProjectDiscoveryServiceFactory(
    ILoggerFactory loggerFactory) : ILspServiceFactory
{
    public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
        => new WorkspaceProjectDiscoveryService(loggerFactory, lspServices.GetRequiredService<IFileChangeWatcher>());
}

internal sealed class WorkspaceProjectDiscoveryService(
    ILoggerFactory loggerFactory,
    IFileChangeWatcher fileChangeWatcher) : ILspService, IOnInitialized
{
    private static readonly StringComparer s_pathComparer = StringComparer.OrdinalIgnoreCase;

    private readonly ILogger _logger = loggerFactory.CreateLogger<WorkspaceProjectDiscoveryService>();
    private readonly object _gate = new();
    private readonly IFileChangeWatcher _fileChangeWatcher = fileChangeWatcher;
    private readonly TaskCompletionSource _discoveryCompletionSource = new();

    private ImmutableArray<string> _workspaceFolders;
    private ImmutableDictionary<string, WorkspaceFolderProjectIndex> _discoveryIndexByWorkspaceFolder = ImmutableDictionary<string, WorkspaceFolderProjectIndex>.Empty.WithComparers(s_pathComparer);
    private ImmutableDictionary<string, IFileChangeContext> _watchersByWorkspaceFolder = ImmutableDictionary<string, IFileChangeContext>.Empty.WithComparers(s_pathComparer);

    public Task OnInitializedAsync(ClientCapabilities clientCapabilities, RequestContext context, CancellationToken cancellationToken)
    {
        var initializeManager = context.GetRequiredService<IInitializeManager>();
        // Subscribe before reading initial state so we cannot miss updates that race with initialization.
        initializeManager.WorkspaceFoldersChanged += OnWorkspaceFoldersChanged;

        _workspaceFolders = initializeManager.GetRequiredWorkspaceFolderPaths();

        if (_workspaceFolders.IsDefaultOrEmpty)
        {
            _discoveryCompletionSource.SetResult();
            return Task.CompletedTask;
        }

        foreach (var workspaceFolder in _workspaceFolders)
        {
            AddWorkspaceFolder(workspaceFolder);
        }

        _discoveryCompletionSource.SetResult();
        return Task.CompletedTask;
    }

    private void OnWorkspaceFoldersChanged(object? sender, WorkspaceFoldersChangedEventArgs e)
    {
        foreach (var addedFolder in e.AddedFolders)
        {
            AddWorkspaceFolder(addedFolder);
        }

        foreach (var removedFolder in e.RemovedFolders)
        {
            RemoveWorkspaceFolder(removedFolder);
        }
    }

    private void AddWorkspaceFolder(string workspaceFolder)
    {
        if (!Directory.Exists(workspaceFolder))
            return;

        lock (_gate)
        {
            if (_discoveryIndexByWorkspaceFolder.ContainsKey(workspaceFolder))
            {
                _logger.LogTrace("Workspace folder '{WorkspaceFolder}' is already being discovered.", workspaceFolder);
                return;
            }
        }

        var index = DiscoverProjectsInWorkspaceFolder(workspaceFolder);
        var watcher = CreateWatcher(workspaceFolder);

        lock (_gate)
        {
            if (_discoveryIndexByWorkspaceFolder.ContainsKey(workspaceFolder))
            {
                watcher.Dispose();
                _logger.LogTrace("Workspace folder '{WorkspaceFolder}' is already being discovered.", workspaceFolder);
                return;
            }

            _discoveryIndexByWorkspaceFolder = _discoveryIndexByWorkspaceFolder.Add(workspaceFolder, index);
            _watchersByWorkspaceFolder = _watchersByWorkspaceFolder.Add(workspaceFolder, watcher);
            _workspaceFolders = _workspaceFolders.IsDefault ? [workspaceFolder] : _workspaceFolders.Add(workspaceFolder);
            _logger.LogTrace("Started project discovery and watcher for workspace folder '{WorkspaceFolder}'.", workspaceFolder);
        }
    }

    private void RemoveWorkspaceFolder(string workspaceFolder)
    {
        lock (_gate)
        {
            if (_watchersByWorkspaceFolder.TryGetValue(workspaceFolder, out var watcher))
            {
                watcher.Dispose();
                _watchersByWorkspaceFolder = _watchersByWorkspaceFolder.Remove(workspaceFolder);
            }

            if (_discoveryIndexByWorkspaceFolder.ContainsKey(workspaceFolder))
            {
                _discoveryIndexByWorkspaceFolder = _discoveryIndexByWorkspaceFolder.Remove(workspaceFolder);
                _workspaceFolders = _workspaceFolders.Remove(workspaceFolder);
                _logger.LogTrace("Removed project discovery cache and watcher for workspace folder '{WorkspaceFolder}'.", workspaceFolder);
            }
        }
    }

    private IFileChangeContext CreateWatcher(string workspaceFolder)
    {
        var watcher = _fileChangeWatcher.CreateContext([new WatchedDirectory(workspaceFolder, [".csproj"])]);
        watcher.FileChanged += (_, projectFilePath) => OnProjectFileChanged(workspaceFolder, projectFilePath);
        return watcher;
    }

    private void OnProjectFileChanged(string workspaceFolder, string projectFilePath)
    {
        lock (_gate)
        {
            if (!_discoveryIndexByWorkspaceFolder.TryGetValue(workspaceFolder, out var index))
                return;

            var projectExists = File.Exists(projectFilePath);
            var updatedIndex = projectExists
                ? index.WithAddedProject(projectFilePath)
                : index.WithRemovedProject(projectFilePath);

            _discoveryIndexByWorkspaceFolder = _discoveryIndexByWorkspaceFolder.SetItem(workspaceFolder, updatedIndex);

            _logger.LogTrace("Updated project discovery cache for changed project file '{ProjectFilePath}' in workspace folder '{WorkspaceFolder}'.", projectFilePath, workspaceFolder);
        }
    }

    internal ValueTask<ImmutableArray<string>> GetCandidateProjectsAsync(string filePath, CancellationToken cancellationToken)
    {
        Contract.ThrowIfTrue(_workspaceFolders.IsDefault, $"{nameof(OnInitializedAsync)} must be called before {nameof(GetCandidateProjectsAsync)}.");

        ImmutableDictionary<string, WorkspaceFolderProjectIndex> discoveryIndexByWorkspaceFolder;
        lock (_gate)
        {
            discoveryIndexByWorkspaceFolder = _discoveryIndexByWorkspaceFolder;
        }

        if (!PathUtilities.IsAbsolute(filePath) || discoveryIndexByWorkspaceFolder.IsEmpty)
            return new([]);

        // Prefer the deepest workspace folder in case folders are nested.
        var candidateWorkspaceFolders = discoveryIndexByWorkspaceFolder.Keys
            .Where(workspaceFolder => PathUtilities.IsSameDirectoryOrChildOf(child: filePath, parent: workspaceFolder))
            .OrderByDescending(static workspaceFolder => workspaceFolder.Length)
            .ToImmutableArray();

        foreach (var workspaceFolder in candidateWorkspaceFolders)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var index = discoveryIndexByWorkspaceFolder[workspaceFolder];
            var projects = index.FindNearestProjects(filePath);
            if (!projects.IsDefaultOrEmpty)
                return new(projects);
        }

        return new([]);
    }

    private WorkspaceFolderProjectIndex DiscoverProjectsInWorkspaceFolder(string workspaceFolder)
    {
        var stopwatch = SharedStopwatch.StartNew();
        var projectsByDirectory = ImmutableDictionary.CreateBuilder<string, ImmutableArray<string>>(s_pathComparer);

        WorkspaceFolderWalker.Walk(workspaceFolder, (directory, csprojPaths) =>
        {
            projectsByDirectory[directory] = csprojPaths;
        });

        _logger.LogDebug(
            "Indexed {ProjectCount} projects across {DirectoryCount} directories in '{WorkspaceFolder}' in {ElapsedMilliseconds} ms.",
            projectsByDirectory.Values.Sum(static values => values.Length),
            projectsByDirectory.Count,
            workspaceFolder,
            Math.Round(stopwatch.Elapsed.TotalMilliseconds));

        return new WorkspaceFolderProjectIndex(workspaceFolder, projectsByDirectory.ToImmutable());
    }

    internal TestAccessor GetTestAccessor() => new(this);

    internal readonly struct TestAccessor
    {
        private readonly WorkspaceProjectDiscoveryService _instance;

        internal TestAccessor(WorkspaceProjectDiscoveryService instance)
            => _instance = instance;

        /// <summary>
        /// Returns a task that completes when all project discovery tasks are complete.
        /// Useful for tests to wait for discovery without hardcoding delays.
        /// </summary>
        internal Task WhenDiscoveryCompleteAsync()
        {
            return _instance._discoveryCompletionSource.Task;
        }

        internal void AddWorkspaceFolder(string workspaceFolder)
            => _instance.AddWorkspaceFolder(workspaceFolder);

        internal void RemoveWorkspaceFolder(string workspaceFolder)
            => _instance.RemoveWorkspaceFolder(workspaceFolder);

        internal void NotifyProjectFileChanged(string workspaceFolder, string projectFilePath)
            => _instance.OnProjectFileChanged(workspaceFolder, projectFilePath);

        internal ValueTask<ImmutableArray<string>> GetCandidateProjectsAsync(string filePath, CancellationToken cancellationToken)
            => _instance.GetCandidateProjectsAsync(filePath, cancellationToken);
    }

    private sealed record WorkspaceFolderProjectIndex(string WorkspaceFolder, ImmutableDictionary<string, ImmutableArray<string>> ProjectsByDirectory)
    {
        public WorkspaceFolderProjectIndex WithAddedProject(string projectFilePath)
        {
            var projectDirectory = PathUtilities.GetDirectoryName(projectFilePath);

            if (ProjectsByDirectory.TryGetValue(projectDirectory, out var projects))
            {
                if (projects.Any(project => s_pathComparer.Equals(project, projectFilePath)))
                    return this;

                return this with { ProjectsByDirectory = ProjectsByDirectory.SetItem(projectDirectory, projects.Add(projectFilePath)) };
            }

            return this with { ProjectsByDirectory = ProjectsByDirectory.Add(projectDirectory, [projectFilePath]) };
        }

        public WorkspaceFolderProjectIndex WithRemovedProject(string projectFilePath)
        {
            var projectDirectory = PathUtilities.GetDirectoryName(projectFilePath);
            if (!ProjectsByDirectory.TryGetValue(projectDirectory, out var projects))
                return this;

            var filteredProjects = projects.Where(project => !s_pathComparer.Equals(project, projectFilePath)).ToImmutableArray();
            if (filteredProjects.Length == projects.Length)
                return this;

            if (filteredProjects.IsEmpty)
                return this with { ProjectsByDirectory = ProjectsByDirectory.Remove(projectDirectory) };

            return this with { ProjectsByDirectory = ProjectsByDirectory.SetItem(projectDirectory, filteredProjects) };
        }

        public ImmutableArray<string> FindNearestProjects(string filePath)
        {
            var directory = PathUtilities.GetDirectoryName(filePath);
            while (PathUtilities.IsSameDirectoryOrChildOf(child: directory, parent: WorkspaceFolder))
            {
                if (ProjectsByDirectory.TryGetValue(directory, out var projects))
                    return projects;

                var parent = PathUtilities.GetDirectoryName(directory);
                if (directory.Equals(parent, StringComparison.OrdinalIgnoreCase))
                    break;

                directory = parent;
            }

            return [];
        }
    }
}
