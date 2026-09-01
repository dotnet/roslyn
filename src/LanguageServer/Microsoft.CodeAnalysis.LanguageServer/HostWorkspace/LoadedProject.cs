// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.ProjectTelemetry;
using Microsoft.CodeAnalysis.ProjectSystem;
using Microsoft.CodeAnalysis.Workspaces.ProjectSystem;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.Extensions.Logging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

/// <summary>
/// Represents a single loaded project file on disk, which may contain multiple <see cref="Target"/> when we are multi-targeting.
/// </summary>
internal sealed partial class LoadedProject : IAsyncDisposable
{
    /// <summary>
    /// The file path or URI of a the project file. This may include virtual files; if you need a file path for purposes of file watching or file APIs,
    /// call <see cref="TryGetAbsoluteFilePath"/> to get a file path that exists on disk.
    /// </summary>
    public string ProjectFilePath { get; }
    private readonly string? _projectDirectory;
    private readonly IFileChangeWatcher _fileWatcher;

    /// <summary>
    /// A single gate to synchronize all use of this instance. This lock is expected to be held any time a <see cref="Target" /> is used as well. The expectation is no
    /// fancy trickery is happening in this type -- just acquire the lock at public methods.
    /// </summary>
    private readonly SemaphoreSlim _gate = new SemaphoreSlim(initialCount: 1);

    /// <summary>
    /// Whether we have been disposed or not. Once we're disposed, it's allowable for any methods on this type to no-op if they are called; this allows a project to
    /// be unloaded by one task and an async load to be in process somewhere else. Fundamentally the single <see cref="_gate"/> will decide who wins.
    /// </summary>
    private bool _disposed = false;

    /// <summary>
    /// A <see cref="IFileChangeContext"/> used to watch for source files being added or removed under the project directory.
    /// </summary>
    private readonly IFileChangeContext? _sourceFileCreatedOrDeletedChangeContext;

    /// <summary>
    /// A <see cref="IFileChangeContext"/> used to watch for changes to the project file itself.
    /// </summary>
    private readonly IFileChangeContext _projectFileChangeContext;

    private readonly List<Target> _targets = [];
    private (ProjectSystemProjectFactory ProjectFactory, ProjectId Id)? _primordialProjectInfo;

    private bool _reportedTelemetry = false;
    private Guid? _projectGuidForTelemetry = null;

    public LoadedProject(string projectFilePath, IFileChangeWatcher fileWatcher)
    {
        ProjectFilePath = projectFilePath;
        _fileWatcher = fileWatcher;

        _projectFileChangeContext = fileWatcher.CreateContext([]);
        _projectFileChangeContext.FileChanged += ProjectFileChangeContext_FileChanged;

        if (TryGetAbsoluteFilePath() is string absoluteFilePath)
        {
            _projectFileChangeContext.EnqueueWatchingFile(absoluteFilePath);
            _projectDirectory = Path.GetDirectoryName(absoluteFilePath);

            if (_projectDirectory is not null)
            {
                // We'll watch the directory for all source file changes
                _sourceFileCreatedOrDeletedChangeContext = fileWatcher.CreateContext([new(_projectDirectory, [".cs", ".cshtml", ".razor"])]);
                _sourceFileCreatedOrDeletedChangeContext.FileChanged += SourceFileCreatedOrDeletedChangeContext_FileChanged;
            }
        }
    }

    /// <summary>
    /// Raised any time this project (or any of its targets) needs a reload. The parameter includes the file path that triggered a reload.
    /// </summary>
    public event EventHandler<string>? NeedsReload;

    private void ProjectFileChangeContext_FileChanged(object? sender, FileChangedEventArgs e)
    {
        NeedsReload?.Invoke(this, e.FilePath);
    }

    private string? TryGetAbsoluteFilePath()
    {
        return PathUtilities.IsAbsolute(ProjectFilePath) && File.Exists(ProjectFilePath) ? ProjectFilePath : null;
    }

#pragma warning disable VSTHRD100 // Avoid async void methods -- async void because it's being used by an event handler
    private async void SourceFileCreatedOrDeletedChangeContext_FileChanged(object? sender, FileChangedEventArgs e)
#pragma warning restore VSTHRD100 // Avoid async void methods
    {
        // We only need to handle file adds/removes -- the changes are handled in the ProjectSystemProjectFactory for us
        if (e.ChangeKind == FileChangeKind.Changed)
            return;

        bool needsReload = false;

        using (await _gate.DisposableWaitAsync())
        {
            foreach (var target in _targets)
            {
                if (target.FilePathIsIncludedInFileGlobs(e.FilePath))
                {
                    needsReload = true;
                    break;
                }
            }
        }

        // Invoke this outside the lock
        if (needsReload)
            NeedsReload?.Invoke(this, e.FilePath);
    }

    /// <summary>
    /// Creates a primordial project for this <see cref="LoadedProject"/> and remembers it so it can be removed later.
    /// </summary>
    /// <param name="projectFactory"></param>
    /// <param name="projectInfo"></param>
    /// <returns></returns>
    public async ValueTask<Project> CreatePrimordialProjectAsync(ProjectSystemProjectFactory projectFactory, ProjectInfo projectInfo)
    {
        using (await _gate.DisposableWaitAsync())
        {
            Contract.ThrowIfTrue(_primordialProjectInfo.HasValue, $"This {nameof(LoadedProject)} already has a primordial project.");
            Contract.ThrowIfTrue(_targets.Count > 0, "We should not be creating primordial projects once we have real projects.");
            projectFactory.ApplyChangeToWorkspace(workspace => workspace.OnProjectAdded(projectInfo));

            _primordialProjectInfo = (projectFactory, projectInfo.Id);
            return projectFactory.Workspace.CurrentSolution.GetRequiredProject(projectInfo.Id);
        }
    }

    public async ValueTask<ImmutableArray<Project>> GetExistingProjectsAsync()
    {
        using (await _gate.DisposableWaitAsync())
        {
            if (_primordialProjectInfo.HasValue)
            {
                return [_primordialProjectInfo.Value.ProjectFactory.Workspace.CurrentSolution.GetRequiredProject(_primordialProjectInfo.Value.Id)];
            }
            else
            {
                var builder = ImmutableArray.CreateBuilder<Project>(_targets.Count);

                foreach (var target in _targets)
                    builder.Add(target.ProjectFactory.Workspace.CurrentSolution.GetRequiredProject(target.ProjectId));

                return builder.ToImmutable();
            }
        }
    }

    public async ValueTask<bool> UsesProjectFactoryAsync(ProjectSystemProjectFactory projectFactory)
    {
        using (await _gate.DisposableWaitAsync())
        {
            if (_primordialProjectInfo.HasValue)
            {
                return _primordialProjectInfo.Value.ProjectFactory == projectFactory;
            }
            else
            {
                // Assumption: All targets will use the same project factory.
                return _targets.Any(static (target, factory) => target.ProjectFactory == factory, projectFactory);
            }
        }
    }

    /// <summary>
    /// Updates the list of targets for this project given the set of loaded project infos, creating and removing targets as necessary. If there is a primordial project
    /// for this loaded project, it will be removed from the workspace once all targets have been created and added to the workspace.
    /// </summary>
    /// <param name="cancellationToken">
    /// A cancellation token that if cancelled will cancel the updating of the projects. There is no particular guarantee of what would be left and not left if the cancellation is
    /// raised, but the expectation is a later called to this method with the same data should still be allowed and bring everything up to date.
    /// </param>
    /// <returns>
    /// True if it was applied, or false if the project wasn't in a state to apply it -- most likely because the project had been unloaded.
    /// </returns>
    public async ValueTask<bool> TryApplyLoadedProjectInfosAsync(
        ImmutableArray<ProjectFileInfo> loadedProjectInfos,
        bool isMiscellaneousFile,
        bool hasAllInformation,
        ProjectSystemProjectFactory projectFactory,
        ProjectTargetFrameworkManager targetFrameworkManager,
        LanguageServerWorkspaceFactory workspaceFactory,
        ILogger logger,
        CancellationToken cancellationToken,
        bool onlyIfNoTargets = false)
    {
        using (await _gate.DisposableWaitAsync(cancellationToken))
        {
            // We already unloaded this project -- we're not going to resurrect it.
            if (_disposed)
                return false;

            if (onlyIfNoTargets && _targets.Count > 0)
                return false;

            var staleTargets = new List<Target>(_targets);

            foreach (var loadedProjectInfo in loadedProjectInfos)
            {
                var target = await GetOrCreateProjectTargetAsync(loadedProjectInfo, projectFactory, workspaceFactory, cancellationToken);
                staleTargets.Remove(target);
                await target.UpdateWithNewProjectInfoAsync(loadedProjectInfo, isMiscellaneousFile, hasAllInformation, targetFrameworkManager, logger);
            }

            // Now that we've created or updated projects, we can now remove any old projects that went away
            foreach (var staleTarget in staleTargets)
            {
                staleTarget.Dispose();
                _targets.Remove(staleTarget);
            }

            if (_primordialProjectInfo is not null)
            {
                // Remove the primordial project from the workspace now that the design-time build has produced real targets.
                await _primordialProjectInfo.Value.ProjectFactory.ApplyChangeToWorkspaceAsync(
                    workspace => workspace.OnProjectRemoved(_primordialProjectInfo.Value.Id),
                    cancellationToken);

                _primordialProjectInfo = null;
            }

            return true;
        }
    }

    private async Task<Target> GetOrCreateProjectTargetAsync(ProjectFileInfo loadedProjectInfo, ProjectSystemProjectFactory projectFactory, LanguageServerWorkspaceFactory workspaceFactory, CancellationToken cancellationToken)
    {
        Contract.ThrowIfFalse(_gate.CurrentCount == 0);

        var existingTarget = _targets.SingleOrDefault(p => p.GetTargetFramework() == loadedProjectInfo.TargetFramework && p.ProjectFactory == projectFactory);
        if (existingTarget != null)
            return existingTarget;

        var targetFramework = loadedProjectInfo.TargetFramework;
        var projectSystemName = targetFramework is null ? ProjectFilePath : $"{ProjectFilePath} (${targetFramework})";

        var projectCreationInfo = new ProjectSystemProjectCreationInfo
        {
            AssemblyName = projectSystemName,
            FilePath = TryGetAbsoluteFilePath(),
            CompilationOutputAssemblyFilePath = loadedProjectInfo.IntermediateOutputFilePath,
        };

        var projectSystemProject = await projectFactory.CreateAndAddToWorkspaceAsync(
            projectSystemName,
            loadedProjectInfo.Language,
            projectCreationInfo,
            workspaceFactory.ProjectSystemHostInfo,
            cancellationToken).ConfigureAwait(false);

        var target = new Target(this, projectSystemProject, projectFactory);
        _targets.Add(target);
        return target;
    }

    public async ValueTask SetProjectGuidForTelemetryAsync(Guid guid)
    {
        using (await _gate.DisposableWaitAsync())
        {
            _projectGuidForTelemetry = guid;
        }
    }
    public async ValueTask<bool> NeedsRestoreAsync()
    {
        using (await _gate.DisposableWaitAsync())
        {
            return _targets.Any(static t => t.NeedsRestore);
        }
    }

    public async ValueTask ReportTelemetryIfNotPreviouslyReportedAsync(ProjectLoadTelemetryReporter reporter, bool isSdkStyle, string? solutionPath, bool isMiscellaneousFile, bool isFileBasedProgram, bool hasFileBasedAppDirectives)
    {
        using (await _gate.DisposableWaitAsync())
        {
            if (TryGetAbsoluteFilePath() is null)
                return;

            // If we've already reported once, no need to report again
            if (_reportedTelemetry)
                return;

            _reportedTelemetry = true;

            var telemetryInfos = new Dictionary<ProjectFileInfo, ProjectLoadTelemetryReporter.TelemetryInfo>();

            foreach (var target in _targets)
            {
                var (projectFileInfo, metadataReferences, outputKind) = target.GetTelemetryInfo();
                telemetryInfos[projectFileInfo] =
                    new ProjectLoadTelemetryReporter.TelemetryInfo
                    {
                        MetadataReferences = metadataReferences,
                        OutputKind = outputKind,
                        IsSdkStyle = isSdkStyle,
                        HasSolutionFile = solutionPath is not null,
                        IsFileBasedProgram = isFileBasedProgram,
                        HasFileBasedAppDirectives = hasFileBasedAppDirectives,
                        IsMiscellaneousFile = isMiscellaneousFile,
                    };
            }

            await reporter.ReportProjectLoadTelemetryAsync(telemetryInfos, ProjectFilePath, _projectGuidForTelemetry, CancellationToken.None);
        }
    }

    /// <summary>
    /// Removes the project from the workspace and disposes of all resources.
    /// </summary>
    /// <returns></returns>
    public async ValueTask DisposeAsync()
    {
        using (await _gate.DisposableWaitAsync())
        {
            if (_disposed)
                return;

            _sourceFileCreatedOrDeletedChangeContext?.Dispose();
            _projectFileChangeContext.Dispose();

            foreach (var target in _targets)
                target.Dispose();

            _targets.Clear();

            if (_primordialProjectInfo.HasValue)
            {
                _primordialProjectInfo.Value.ProjectFactory.ApplyChangeToWorkspace(workspace => workspace.OnProjectRemoved(_primordialProjectInfo.Value.Id));
                _primordialProjectInfo = null;
            }

            _disposed = true;
        }
    }

    private sealed class DocumentFileInfoComparer : IEqualityComparer<DocumentFileInfo>
    {
        public static IEqualityComparer<DocumentFileInfo> Instance = new DocumentFileInfoComparer();

        private DocumentFileInfoComparer()
        {
        }

        public bool Equals(DocumentFileInfo? x, DocumentFileInfo? y)
        {
            return StringComparer.Ordinal.Equals(x?.FilePath, y?.FilePath);
        }

        public int GetHashCode(DocumentFileInfo obj)
        {
            return StringComparer.Ordinal.GetHashCode(obj.FilePath);
        }
    }
}
