// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServer.Handler.DebugConfiguration;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.FileWatching;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.ProjectTelemetry;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Workspaces.ProjectSystem;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Logging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

/// <summary>
/// Represents a single project loaded for a single target.
/// </summary>
internal sealed class LoadedProject : IDisposable
{
    private readonly string _projectFilePath;
    private readonly string _projectDirectory;

    private readonly ProjectSystemProject _projectSystemProject;
    public ProjectSystemProjectFactory ProjectFactory { get; }
    private readonly ProjectSystemProjectOptionsProcessor _optionsProcessor;
    private readonly IFileChangeContext _sourceFileChangeContext;
    private readonly IFileChangeContext _projectFileChangeContext;
    private readonly IFileChangeContext _assetsFileChangeContext;
    private readonly ProjectTargetFrameworkManager _targetFrameworkManager;

    /// <summary>
    /// The most recent version of the project design time build information; held onto so the next reload we can diff against this.
    /// </summary>
    private ProjectFileInfo? _mostRecentFileInfo;
    /// <summary>
    /// The most recent version of the file glob matcher.  Held onto 
    /// </summary>
    private Lazy<ImmutableArray<Matcher>>? _mostRecentFileMatchers;
    private IWatchedFile? _mostRecentProjectAssetsFileWatcher;
    private Checksum _mostRecentProjectAssetsFileChecksum;
    private ImmutableArray<CommandLineReference> _mostRecentMetadataReferences = [];
    private ImmutableArray<CommandLineAnalyzerReference> _mostRecentAnalyzerReferences = [];

    public LoadedProject(ProjectSystemProject projectSystemProject, ProjectSystemProjectFactory projectFactory, IFileChangeWatcher fileWatcher, ProjectTargetFrameworkManager targetFrameworkManager)
    {
        Contract.ThrowIfNull(projectSystemProject.FilePath);
        _projectFilePath = projectSystemProject.FilePath;

        _projectSystemProject = projectSystemProject;
        ProjectFactory = projectFactory;
        _optionsProcessor = new ProjectSystemProjectOptionsProcessor(projectSystemProject, projectFactory.Workspace.CurrentSolution.Services);
        _targetFrameworkManager = targetFrameworkManager;

        // We'll watch the directory for all source file changes
        // TODO: we only should listen for add/removals here, but we can't specify such a filter now
        _projectDirectory = Path.GetDirectoryName(_projectFilePath)!;

        _sourceFileChangeContext = fileWatcher.CreateContext([new(_projectDirectory, [".cs", ".cshtml", ".razor"])]);
        _sourceFileChangeContext.FileChanged += SourceFileChangeContext_FileChanged;

        _projectFileChangeContext = fileWatcher.CreateContext([]);
        _projectFileChangeContext.FileChanged += ProjectFileChangeContext_FileChanged;
        _projectFileChangeContext.EnqueueWatchingFile(_projectFilePath);

        _assetsFileChangeContext = fileWatcher.CreateContext([]);
        _assetsFileChangeContext.FileChanged += AssetsFileChangeContext_FileChanged;
    }

    private void SourceFileChangeContext_FileChanged(object? sender, string filePath)
    {
        var matchers = _mostRecentFileMatchers?.Value;
        if (matchers is null)
        {
            return;
        }

        // Check if the file path matches any of the globs in the project file.
        foreach (var matcher in matchers)
        {
            // CPS re-creates the msbuild globs from the includes/excludes/removes and the project XML directory and
            // ignores the MSBuildGlob.FixedDirectoryPart.  We'll do the same here and match using the project directory as the relative path.
            // See https://devdiv.visualstudio.com/DevDiv/_git/CPS?path=/src/Microsoft.VisualStudio.ProjectSystem/Build/MsBuildGlobFactory.cs
            var relativeDirectory = _projectDirectory;

            var matches = matcher.Match(relativeDirectory, filePath);
            if (matches.HasMatches)
            {
                NeedsReload?.Invoke(this, EventArgs.Empty);
                return;
            }
        }
    }

    private void ProjectFileChangeContext_FileChanged(object? sender, string filePath)
    {
        NeedsReload?.Invoke(this, EventArgs.Empty);
    }

    private void AssetsFileChangeContext_FileChanged(object? sender, string filePath)
    {
        Shared.Utilities.IOUtilities.PerformIO(() =>
        {
            // We only want to trigger design time build if the assets file content actually changed from the last time this handler was called.
            // Sometimes we can get a change event where no content changed (e.g. for a failed restore).
            // In such cases, proceeding with design-time build can put us in a restore loop (since the design-time build notices that assets are missing).
            using var assetsFileStream = File.OpenRead(filePath);
            var checksum = Checksum.Create(assetsFileStream);
            if (_mostRecentProjectAssetsFileChecksum != checksum)
            {
                _mostRecentProjectAssetsFileChecksum = checksum;
                NeedsReload?.Invoke(this, EventArgs.Empty);
            }
        });
    }

    public event EventHandler? NeedsReload;

    public string? GetTargetFramework()
    {
        Contract.ThrowIfNull(_mostRecentFileInfo, "We haven't been given a loaded project yet, so we can't provide the existing TFM.");
        return _mostRecentFileInfo.TargetFramework;
    }

    /// <summary>
    /// Unloads the project and removes it from the workspace.
    /// </summary>
    public void Dispose()
    {
        _sourceFileChangeContext.Dispose();
        _projectFileChangeContext.Dispose();
        _optionsProcessor.Dispose();
        _projectSystemProject.RemoveFromWorkspace();
    }

    public async ValueTask<(OutputKind OutputKind, ImmutableArray<CommandLineReference> MetadataReferences, bool NeedsRestore)> UpdateWithNewProjectInfoAsync(ProjectFileInfo newProjectInfo, bool isMiscellaneousFile, ILogger logger)
    {
        if (_mostRecentFileInfo != null)
        {
            // We should never be changing the fundamental identity of this project; if this happens we really should have done a full unload/reload.
            Contract.ThrowIfFalse(newProjectInfo.FilePath == _mostRecentFileInfo.FilePath);
            Contract.ThrowIfFalse(newProjectInfo.TargetFramework == _mostRecentFileInfo.TargetFramework);
        }

        var disposableBatchScope = await _projectSystemProject.CreateBatchScopeAsync(CancellationToken.None).ConfigureAwait(false);
        await using var _ = disposableBatchScope.ConfigureAwait(false);

        var targetFrameworkSuffix = newProjectInfo.TargetFramework != null ? " (" + newProjectInfo.TargetFramework + ")" : "";
        var projectDisplayName = isMiscellaneousFile
            ? FeaturesResources.Miscellaneous_Files
            : Path.GetFileNameWithoutExtension(newProjectInfo.FilePath) + targetFrameworkSuffix;
        var projectFullPathWithTargetFramework = newProjectInfo.FilePath + targetFrameworkSuffix;

        _projectSystemProject.DisplayName = projectDisplayName;
        _projectSystemProject.OutputFilePath = newProjectInfo.OutputFilePath;
        _projectSystemProject.OutputRefFilePath = newProjectInfo.OutputRefFilePath;
        _projectSystemProject.GeneratedFilesOutputDirectory = newProjectInfo.GeneratedFilesOutputDirectory;
        _projectSystemProject.CompilationOutputAssemblyFilePath = newProjectInfo.IntermediateOutputFilePath;
        _projectSystemProject.DefaultNamespace = newProjectInfo.DefaultNamespace;
        _projectSystemProject.HasAllInformation = !isMiscellaneousFile;

        if (newProjectInfo.TargetFrameworkIdentifier != null)
        {
            _targetFrameworkManager.UpdateIdentifierForProject(_projectSystemProject.Id, newProjectInfo.TargetFrameworkIdentifier);
        }

        _optionsProcessor.SetCommandLine(newProjectInfo.CommandLineArgs);
        var commandLineArguments = _optionsProcessor.GetParsedCommandLineArguments();

        UpdateProjectSystemProjectCollection(
            newProjectInfo.Documents,
            _mostRecentFileInfo?.Documents,
            DocumentFileInfoComparer.Instance,
            document => _projectSystemProject.AddSourceFile(document.FilePath, folders: document.Folders),
            document => _projectSystemProject.RemoveSourceFile(document.FilePath),
            "Project {0} now has {1} source file(s). ({2} added, {3} removed.)");

        var relativePathResolver = new RelativePathResolver(commandLineArguments.ReferencePaths, commandLineArguments.BaseDirectory);
        var metadataReferences = commandLineArguments.MetadataReferences.Select(cr =>
        {
            // The relative path resolver calls File.Exists() to see if the path doesn't exist; it guarantees that generally the path returned
            // is to an actual file on disk. And it needs to call File.Exists() in some cases if there are reference paths to have to search. But as a fallback
            // we'll accept the resolved path since in the common case it's a file that just might not exist on disk yet.
            var absolutePath =
                relativePathResolver.ResolvePath(cr.Reference, baseFilePath: null) ??
                FileUtilities.ResolveRelativePath(cr.Reference, commandLineArguments.BaseDirectory);

            return absolutePath is not null ? new CommandLineReference(absolutePath, cr.Properties) : default;
        }).WhereAsArray(static cr => cr.Reference is not null);

        UpdateProjectSystemProjectCollection(
            metadataReferences,
            _mostRecentMetadataReferences,
            EqualityComparer<CommandLineReference>.Default, // CommandLineReference already implements equality
            reference => _projectSystemProject.AddMetadataReference(reference.Reference, reference.Properties),
            reference => _projectSystemProject.RemoveMetadataReference(reference.Reference, reference.Properties),
            "Project {0} now has {1} reference(s). ({2} added, {3} removed.)");

        // Now that we've updated it hold onto the old list of references so we can remove them if there's a later update
        _mostRecentMetadataReferences = metadataReferences;

        var analyzerReferences = commandLineArguments.AnalyzerReferences.Select(cr =>
        {
            // Note that unlike regular references, we do not resolve these with the relative path resolver that searches reference paths
            var absolutePath = FileUtilities.ResolveRelativePath(cr.FilePath, commandLineArguments.BaseDirectory);
            return absolutePath is not null ? new CommandLineAnalyzerReference(absolutePath) : default;
        }).WhereAsArray(static cr => cr.FilePath is not null);

        UpdateProjectSystemProjectCollection(
            analyzerReferences,
            _mostRecentAnalyzerReferences,
            EqualityComparer<CommandLineAnalyzerReference>.Default, // CommandLineAnalyzerReference already implements equality
            reference => _projectSystemProject.AddAnalyzerReference(reference.FilePath),
            reference => _projectSystemProject.RemoveAnalyzerReference(reference.FilePath),
            "Project {0} now has {1} analyzer reference(s). ({2} added, {3} removed.)");

        _mostRecentAnalyzerReferences = analyzerReferences;

        UpdateProjectSystemProjectCollection(
            newProjectInfo.AdditionalDocuments,
            _mostRecentFileInfo?.AdditionalDocuments,
            DocumentFileInfoComparer.Instance,
            document => _projectSystemProject.AddAdditionalFile(document.FilePath, folders: document.Folders),
            document => _projectSystemProject.RemoveAdditionalFile(document.FilePath),
            "Project {0} now has {1} additional file(s). ({2} added, {3} removed.)");

        UpdateProjectSystemProjectCollection(
            newProjectInfo.AnalyzerConfigDocuments,
            _mostRecentFileInfo?.AnalyzerConfigDocuments,
            DocumentFileInfoComparer.Instance,
            document => _projectSystemProject.AddAnalyzerConfigFile(document.FilePath),
            document => _projectSystemProject.RemoveAnalyzerConfigFile(document.FilePath),
            "Project {0} now has {1} analyzer config file(s). ({2} added, {3} removed.)");

        UpdateProjectSystemProjectCollection(
            newProjectInfo.AdditionalDocuments.Where(TreatAsIsDynamicFile),
            _mostRecentFileInfo?.AdditionalDocuments.Where(TreatAsIsDynamicFile),
            DocumentFileInfoComparer.Instance,
            document => _projectSystemProject.AddDynamicSourceFile(document.FilePath, folders: []),
            document => _projectSystemProject.RemoveDynamicSourceFile(document.FilePath),
            "Project {0} now has {1} dynamic file(s). ({2} added, {3} removed.)");

        WatchProjectAssetsFile(newProjectInfo);

        var needsRestore = ProjectDependencyHelper.NeedsRestore(newProjectInfo, _mostRecentFileInfo, logger);

        _mostRecentFileMatchers = new Lazy<ImmutableArray<Matcher>>(() =>
        {
            return [.. newProjectInfo.FileGlobs.Select(glob =>
            {
                var matcher = new Matcher();
                matcher.AddIncludePatterns(glob.Includes);
                matcher.AddExcludePatterns(glob.Excludes);
                matcher.AddExcludePatterns(glob.Removes);
                return matcher;
            })];
        });
        _mostRecentFileInfo = newProjectInfo;

        Contract.ThrowIfNull(_projectSystemProject.CompilationOptions, "Compilation options cannot be null for C#/VB project");
        return (_projectSystemProject.CompilationOptions.OutputKind, metadataReferences, needsRestore);

        // logMessage must have 4 placeholders: project name, number of items, added items count, and removed items count.
        void UpdateProjectSystemProjectCollection<T>(IEnumerable<T> loadedCollection, IEnumerable<T>? oldLoadedCollection, IEqualityComparer<T> comparer, Action<T> addItem, Action<T> removeItem, string logMessage)
        {
            var newItems = new HashSet<T>(loadedCollection, comparer);
            var oldItems = new HashSet<T>(oldLoadedCollection ?? [], comparer);

            var addedCount = 0;

            foreach (var newItem in newItems)
            {
                // If oldItems already has this, we don't need to add it again. We'll remove it, and what is left in oldItems is stuff to remove
                if (!oldItems.Remove(newItem))
                {
                    addItem(newItem);
                    addedCount++;
                }
            }

            var removedCount = oldItems.Count;
            foreach (var oldItem in oldItems)
            {
                removeItem(oldItem);
            }

            if (addedCount != 0 || removedCount != 0)
                logger.LogTrace(logMessage, projectFullPathWithTargetFramework, newItems.Count, addedCount, removedCount);
        }

        void WatchProjectAssetsFile(ProjectFileInfo currentProjectInfo)
        {
            if (_mostRecentFileInfo?.ProjectAssetsFilePath == currentProjectInfo.ProjectAssetsFilePath)
            {
                // The file path hasn't changed, just keep using the same watcher.
                return;
            }

            // Dispose of the last once since we're changing the file we're watching.
            _mostRecentProjectAssetsFileWatcher?.Dispose();
            _mostRecentProjectAssetsFileWatcher = currentProjectInfo.ProjectAssetsFilePath is { } assetsFilePath
                    ? _assetsFileChangeContext.EnqueueWatchingFile(assetsFilePath)
                    : null;
            _mostRecentProjectAssetsFileChecksum = default;
        }
    }

    private static bool TreatAsIsDynamicFile(DocumentFileInfo info)
    {
        var extension = Path.GetExtension(info.FilePath);
        return extension is ".cshtml" or ".razor";
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
