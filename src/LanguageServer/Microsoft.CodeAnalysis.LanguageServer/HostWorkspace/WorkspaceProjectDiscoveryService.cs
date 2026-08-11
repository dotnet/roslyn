// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.ProjectSystem;
using Microsoft.CodeAnalysis.Shared.Utilities;
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
        => new WorkspaceProjectDiscoveryService(
            loggerFactory,
            lspServices.GetRequiredService<IFileChangeWatcher>(),
            lspServices.GetRequiredService<LanguageServerProjectSystem>().GetSupportedProjectFileExtensions());
}

internal sealed partial class WorkspaceProjectDiscoveryService : ILspService, IOnInitialized, IDisposable
{
    private static readonly StringComparison s_pathComparison = PathUtilities.IsUnixLikePlatform ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

    private readonly ILogger _logger;
    private readonly IFileChangeWatcher _fileChangeWatcher;
    private readonly ImmutableArray<string> _supportedProjectFileExtensions;
    private readonly Func<string, ImmutableArray<string>> _enumerateFiles;
    private readonly object _gate = new();

    private ImmutableArray<string> _workspaceFolders;
    private readonly Dictionary<string, ProjectDirectory> _projectDirectories = new(PathUtilities.Comparer);
    private readonly Dictionary<string, DirectoryEnumeration> _directoryEnumerations = new(PathUtilities.Comparer);
    private IInitializeManager? _initializeManager;
    private bool _isDisposed;

    internal WorkspaceProjectDiscoveryService(
        ILoggerFactory loggerFactory,
        IFileChangeWatcher fileChangeWatcher,
        ImmutableArray<string> supportedProjectFileExtensions,
        Func<string, ImmutableArray<string>>? enumerateFiles = null)
    {
        _logger = loggerFactory.CreateLogger<WorkspaceProjectDiscoveryService>();
        _fileChangeWatcher = fileChangeWatcher;
        _supportedProjectFileExtensions = supportedProjectFileExtensions;
        _enumerateFiles = enumerateFiles ?? EnumerateFiles;
    }

    public Task OnInitializedAsync(ClientCapabilities clientCapabilities, RequestContext context, CancellationToken cancellationToken)
    {
        var initializeManager = context.GetRequiredService<IInitializeManager>();
        initializeManager.WorkspaceFoldersChanged += OnWorkspaceFoldersChanged;

        lock (_gate)
        {
            Contract.ThrowIfTrue(_isDisposed);
            _initializeManager = initializeManager;
            _workspaceFolders = initializeManager.GetRequiredWorkspaceFolderPaths().SelectAsArray(NormalizePath);
        }

        return Task.CompletedTask;
    }

    private void OnWorkspaceFoldersChanged(object? sender, WorkspaceFoldersChangedEventArgs e)
    {
        foreach (var removedFolder in e.RemovedFolders)
            RemoveWorkspaceFolder(removedFolder);

        foreach (var addedFolder in e.AddedFolders)
            AddWorkspaceFolder(addedFolder);
    }

    private void AddWorkspaceFolder(string workspaceFolder)
    {
        workspaceFolder = NormalizePath(workspaceFolder);

        lock (_gate)
        {
            if (_isDisposed)
                return;

            Contract.ThrowIfTrue(_workspaceFolders.IsDefault, $"{nameof(OnInitializedAsync)} must be called before adding workspace folders.");
            if (!_workspaceFolders.Contains(workspaceFolder, PathUtilities.Comparer))
                _workspaceFolders = _workspaceFolders.Add(workspaceFolder);
        }
    }

    private void RemoveWorkspaceFolder(string workspaceFolder)
    {
        workspaceFolder = NormalizePath(workspaceFolder);
        List<IFileChangeContext>? watchersToDispose = null;
        List<DirectoryEnumeration>? enumerationsToAbandon = null;

        lock (_gate)
        {
            if (_isDisposed)
                return;

            Contract.ThrowIfTrue(_workspaceFolders.IsDefault, $"{nameof(OnInitializedAsync)} must be called before removing workspace folders.");
            _workspaceFolders = _workspaceFolders.Remove(workspaceFolder, PathUtilities.Comparer);

            foreach (var (directory, projectDirectory) in _projectDirectories)
            {
                if (PathUtilities.Comparer.Equals(projectDirectory.WorkspaceFolder, workspaceFolder))
                {
                    watchersToDispose ??= [];
                    watchersToDispose.Add(projectDirectory.Watcher);
                    _projectDirectories.Remove(directory);
                }
            }

            foreach (var (directory, enumeration) in _directoryEnumerations)
            {
                if (PathUtilities.Comparer.Equals(enumeration.WorkspaceFolder, workspaceFolder))
                {
                    enumerationsToAbandon ??= [];
                    enumerationsToAbandon.Add(enumeration);
                    _directoryEnumerations.Remove(directory);
                }
            }
        }

        if (watchersToDispose is not null)
        {
            foreach (var watcher in watchersToDispose)
                watcher.Dispose();
        }

        if (enumerationsToAbandon is not null)
        {
            foreach (var enumeration in enumerationsToAbandon)
                AbandonEnumeration(enumeration, cancel: false);
        }
    }

    internal async ValueTask<ImmutableArray<string>> GetCandidateProjectsAsync(string filePath, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            Contract.ThrowIfTrue(_isDisposed);
            Contract.ThrowIfTrue(_workspaceFolders.IsDefault, $"{nameof(OnInitializedAsync)} must be called before {nameof(GetCandidateProjectsAsync)}.");
        }

        if (!PathUtilities.IsAbsolute(filePath))
            return [];

        filePath = NormalizePath(filePath);
        var workspaceFolder = GetDeepestContainingWorkspaceFolder(filePath);
        if (workspaceFolder is null)
            return [];

        var directory = PathUtilities.GetDirectoryName(filePath);
        while (PathUtilities.IsSameDirectoryOrChildOf(directory, workspaceFolder, s_pathComparison))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var projects = await GetProjectsInDirectoryAsync(directory, workspaceFolder, cancellationToken).ConfigureAwait(false);
            if (!projects.IsEmpty)
                return projects;

            if (PathUtilities.Comparer.Equals(directory, workspaceFolder))
                break;

            directory = PathUtilities.GetDirectoryName(directory);
        }

        return [];
    }

    internal bool TryGetWorkspaceFolder(string filePath, out string normalizedFilePath, out string? workspaceFolder)
    {
        normalizedFilePath = filePath;
        workspaceFolder = null;
        if (!PathUtilities.IsAbsolute(filePath))
            return false;

        normalizedFilePath = NormalizePath(filePath);
        workspaceFolder = GetDeepestContainingWorkspaceFolder(normalizedFilePath);
        return workspaceFolder is not null;
    }

    private string? GetDeepestContainingWorkspaceFolder(string filePath)
    {
        lock (_gate)
            return GetDeepestContainingWorkspaceFolder_NoLock(filePath);
    }

    private string? GetDeepestContainingWorkspaceFolder_NoLock(string path)
    {
        string? deepestWorkspaceFolder = null;
        foreach (var workspaceFolder in _workspaceFolders)
        {
            if (PathUtilities.IsSameDirectoryOrChildOf(path, workspaceFolder, s_pathComparison) &&
                (deepestWorkspaceFolder is null || workspaceFolder.Length > deepestWorkspaceFolder.Length))
            {
                deepestWorkspaceFolder = workspaceFolder;
            }
        }

        return deepestWorkspaceFolder;
    }

    private async ValueTask<ImmutableArray<string>> GetProjectsInDirectoryAsync(
        string directory, string workspaceFolder, CancellationToken cancellationToken)
    {
        ProjectDirectory? projectDirectory;
        DirectoryEnumeration? enumeration;
        lock (_gate)
        {
            if (_isDisposed || !_workspaceFolders.Contains(workspaceFolder, PathUtilities.Comparer))
                return [];

            if (_projectDirectories.TryGetValue(directory, out var cachedDirectory))
            {
                projectDirectory = cachedDirectory;
                enumeration = null;
            }
            else
            {
                projectDirectory = null;
                if (!_directoryEnumerations.TryGetValue(directory, out enumeration))
                {
                    var watcher = CreateWatcher(directory);
                    enumeration = new DirectoryEnumeration(workspaceFolder, watcher);
                    _directoryEnumerations.Add(directory, enumeration);
                }
            }
        }

        if (projectDirectory is not null)
            return ValidateProjects(directory, projectDirectory);

        Contract.ThrowIfNull(enumeration);
        if (enumeration.TryStart())
        {
            // Enumeration is shared by all coalesced callers, so it must not inherit one caller's cancellation.
            _ = Task.Run(() => EnumerateDirectory(directory, enumeration), CancellationToken.None);
        }

        return await enumeration.Completion.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    private void EnumerateDirectory(string directory, DirectoryEnumeration enumeration)
    {
        try
        {
            EnumerateDirectoryCore(directory, enumeration);
        }
        catch (Exception ex)
        {
            lock (_gate)
            {
                if (_directoryEnumerations.TryGetValue(directory, out var currentEnumeration) && ReferenceEquals(currentEnumeration, enumeration))
                    _directoryEnumerations.Remove(directory);
            }

            enumeration.DisposeWatcher();
            enumeration.Completion.TrySetException(ex);
        }
    }

    private void EnumerateDirectoryCore(string directory, DirectoryEnumeration enumeration)
    {
        ImmutableArray<string> enumeratedProjects;
        try
        {
            enumeratedProjects = [.. _enumerateFiles(directory)
                .Where(IsSupportedProjectExtension)
                .Order(StringComparer.Ordinal)];
        }
        catch (Exception ex) when (IOUtilities.IsNormalIOException(ex))
        {
            _logger.LogWarning(
                "Could not enumerate project files in '{Directory}' while resolving workspace projects: {ExceptionMessage}",
                directory,
                ex.Message);
            enumeratedProjects = [];
        }

        // Validate the enumerated snapshot. Watcher changes are merged below while atomically transitioning
        // the directory from an in-flight enumeration to either a positive cache entry or no state.
        var projects = enumeratedProjects.Where(IsExistingSupportedProject).ToHashSet(PathUtilities.Comparer);
        var result = ImmutableArray<string>.Empty;
        var disposeWatcher = true;
        lock (_gate)
        {
            if (_directoryEnumerations.TryGetValue(directory, out var currentEnumeration) && ReferenceEquals(currentEnumeration, enumeration))
            {
                foreach (var (projectPath, exists) in enumeration.Changes)
                {
                    if (exists)
                        projects.Add(projectPath);
                    else
                        projects.Remove(projectPath);
                }

                result = [.. projects.Order(StringComparer.Ordinal)];
                _directoryEnumerations.Remove(directory);

                if (_isDisposed || !_workspaceFolders.Contains(enumeration.WorkspaceFolder, PathUtilities.Comparer))
                {
                    result = [];
                }
                else if (!result.IsEmpty)
                {
                    _projectDirectories.Add(directory, new ProjectDirectory(enumeration.WorkspaceFolder, result, enumeration.Watcher));
                    disposeWatcher = false;
                }
            }
        }

        if (disposeWatcher)
            enumeration.DisposeWatcher();

        enumeration.Completion.TrySetResult(result);
    }

    private static void AbandonEnumeration(DirectoryEnumeration enumeration, bool cancel)
    {
        if (cancel)
            enumeration.Completion.TrySetCanceled();
        else
            enumeration.Completion.TrySetResult([]);

        enumeration.DisposeWatcher();
    }

    private IFileChangeContext CreateWatcher(string directory)
    {
        var extensionFilters = _supportedProjectFileExtensions.SelectAsArray(static extension => "." + extension);
        var watcher = _fileChangeWatcher.CreateContext([new WatchedDirectory(directory, extensionFilters)]);
        watcher.FileChanged += OnProjectFileChanged;
        return watcher;
    }

    private void OnProjectFileChanged(object? sender, string projectFilePath)
    {
        if (!PathUtilities.IsAbsolute(projectFilePath) || !IsSupportedProjectExtension(projectFilePath))
            return;

        projectFilePath = NormalizePath(projectFilePath);
        var directory = PathUtilities.GetDirectoryName(projectFilePath);
        var exists = File.Exists(projectFilePath);
        IFileChangeContext? watcherToDispose = null;

        lock (_gate)
        {
            if (_directoryEnumerations.TryGetValue(directory, out var enumeration))
            {
                enumeration.Changes[projectFilePath] = exists;
                return;
            }

            if (_projectDirectories.TryGetValue(directory, out var projectDirectory))
            {
                // We already know whether 'projectFilePath' exists; no need to re-stat every other project
                // in the directory, so this stays free of blocking I/O while holding the lock.
                var projects = projectDirectory.Projects;
                var updatedProjects = exists
                    ? (projects.Contains(projectFilePath, PathUtilities.Comparer) ? projects : [.. projects.Add(projectFilePath).Order(StringComparer.Ordinal)])
                    : projects.Remove(projectFilePath, PathUtilities.Comparer);

                if (updatedProjects.IsEmpty)
                {
                    _projectDirectories.Remove(directory);
                    watcherToDispose = projectDirectory.Watcher;
                }
                else if (updatedProjects != projects)
                {
                    _projectDirectories[directory] = projectDirectory with { Projects = updatedProjects };
                }
            }
            else if (!exists || GetDeepestContainingWorkspaceFolder_NoLock(directory) is not { } workspaceFolder)
            {
                return;
            }
            else
            {
                var watcher = CreateWatcher(directory);
                _projectDirectories.Add(directory, new ProjectDirectory(workspaceFolder, [projectFilePath], watcher));
            }
        }

        watcherToDispose?.Dispose();
    }

    private ImmutableArray<string> ValidateProjects(string directory, ProjectDirectory projectDirectory)
    {
        var projects = projectDirectory.Projects.Where(IsExistingSupportedProject).Order(StringComparer.Ordinal).ToImmutableArray();

        IFileChangeContext? watcherToDispose = null;
        lock (_gate)
        {
            // Only reconcile if nothing else raced ahead of us and already replaced this entry.
            if (_projectDirectories.TryGetValue(directory, out var current) && ReferenceEquals(current, projectDirectory))
            {
                if (projects.IsEmpty)
                {
                    _projectDirectories.Remove(directory);
                    watcherToDispose = current.Watcher;
                }
                else if (projects.Length != current.Projects.Length)
                {
                    _projectDirectories[directory] = current with { Projects = projects };
                }
            }
        }

        watcherToDispose?.Dispose();
        return projects;
    }

    private bool IsExistingSupportedProject(string projectPath)
        => File.Exists(projectPath) && IsSupportedProjectExtension(projectPath);

    private bool IsSupportedProjectExtension(string path)
    {
        var extension = PathUtilities.GetExtension(path);
        if (extension is not ['.', .. var extensionWithoutDot])
            return false;

        foreach (var supported in _supportedProjectFileExtensions)
        {
            if (extensionWithoutDot.AsSpan().Equals(supported, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private ImmutableArray<string> EnumerateFiles(string directory)
    {
        // Filter by extension during enumeration itself so files that can't be projects never get a path string allocated.
        using var enumerator = new ProjectFileEnumerator(directory, _supportedProjectFileExtensions);
        var builder = ImmutableArray.CreateBuilder<string>();
        while (enumerator.MoveNext())
            builder.Add(enumerator.Current);

        return builder.ToImmutable();
    }

    private static string NormalizePath(string path)
        => Path.GetFullPath(path);

    public void Dispose()
    {
        IInitializeManager? initializeManager;
        List<IFileChangeContext> watchers;
        List<DirectoryEnumeration> enumerations;

        lock (_gate)
        {
            if (_isDisposed)
                return;

            _isDisposed = true;
            initializeManager = _initializeManager;
            _initializeManager = null;
            watchers = [.. _projectDirectories.Values.Select(static directory => directory.Watcher)];
            enumerations = [.. _directoryEnumerations.Values];
            _projectDirectories.Clear();
            _directoryEnumerations.Clear();
            _workspaceFolders = [];
        }

        initializeManager?.WorkspaceFoldersChanged -= OnWorkspaceFoldersChanged;

        foreach (var enumeration in enumerations)
            AbandonEnumeration(enumeration, cancel: true);

        foreach (var watcher in watchers)
            watcher.Dispose();
    }

    internal TestAccessor GetTestAccessor() => new(this);
}
