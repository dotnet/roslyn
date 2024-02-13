// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServer.Handler.DebugConfiguration;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.ProjectSystem;
using Microsoft.CodeAnalysis.Workspaces.ProjectSystem;
using Microsoft.Extensions.Logging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

/// <summary>
/// Represents a single project loaded for a single target.
/// </summary>
internal sealed class LoadedProject : IDisposable
{
    private readonly ProjectSystemProject _projectSystemProject;
    private readonly ProjectSystemProjectOptionsProcessor _optionsProcessor;
    private readonly IFileChangeContext _fileChangeContext;
    private readonly ProjectTargetFrameworkManager _targetFrameworkManager;

    /// <summary>
    /// The most recent version of the project design time build information; held onto so the next reload we can diff against this.
    /// </summary>
    private ProjectFileInfo? _mostRecentFileInfo;
    private IWatchedFile? _mostRecentProjectAssetsFileWatcher;
    private ImmutableArray<CommandLineReference> _mostRecentMetadataReferences = ImmutableArray<CommandLineReference>.Empty;
    private ImmutableArray<CommandLineAnalyzerReference> _mostRecentAnalyzerReferences = ImmutableArray<CommandLineAnalyzerReference>.Empty;

    public LoadedProject(ProjectSystemProject projectSystemProject, SolutionServices solutionServices, IFileChangeWatcher fileWatcher, ProjectTargetFrameworkManager targetFrameworkManager)
    {
        Contract.ThrowIfNull(projectSystemProject.FilePath);

        _projectSystemProject = projectSystemProject;
        _optionsProcessor = new ProjectSystemProjectOptionsProcessor(projectSystemProject, solutionServices);
        _targetFrameworkManager = targetFrameworkManager;

        // We'll watch the directory for all source file changes
        // TODO: we only should listen for add/removals here, but we can't specify such a filter now
        var projectDirectory = Path.GetDirectoryName(projectSystemProject.FilePath)!;
        var watchedDirectories = new WatchedDirectory[]
        {
            new(projectDirectory, ".cs"),
            new(projectDirectory, ".cshtml"),
            new(projectDirectory, ".razor")
        };

        _fileChangeContext = fileWatcher.CreateContext(watchedDirectories);
        _fileChangeContext.FileChanged += FileChangedContext_FileChanged;

        // Start watching for file changes for the project file as well
        _fileChangeContext.EnqueueWatchingFile(projectSystemProject.FilePath);
    }

    private void FileChangedContext_FileChanged(object? sender, string filePath)
    {
        NeedsReload?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? NeedsReload;

    public string? GetTargetFramework()
    {
        Contract.ThrowIfNull(_mostRecentFileInfo, "We haven't been given a loaded project yet, so we can't provide the existing TFM.");
        return _mostRecentFileInfo.TargetFramework;
    }

    public void Dispose()
    {
        _optionsProcessor.Dispose();
        _projectSystemProject.RemoveFromWorkspace();
    }

    public async ValueTask<(ImmutableArray<CommandLineReference>, OutputKind, bool)> UpdateWithNewProjectInfoAsync(ProjectFileInfo newProjectInfo, ILogger logger)
    {
        if (_mostRecentFileInfo != null)
        {
            // We should never be changing the fundamental identity of this project; if this happens we really should have done a full unload/reload.
            Contract.ThrowIfFalse(newProjectInfo.FilePath == _mostRecentFileInfo.FilePath);
            Contract.ThrowIfFalse(newProjectInfo.TargetFramework == _mostRecentFileInfo.TargetFramework);
        }

        await using var batch = _projectSystemProject.CreateBatchScope();

        var projectDisplayName = Path.GetFileNameWithoutExtension(newProjectInfo.FilePath)!;
        var projectFullPathWithTargetFramework = newProjectInfo.FilePath;

        if (newProjectInfo.TargetFramework != null)
        {
            var targetFrameworkSuffix = " (" + newProjectInfo.TargetFramework + ")";
            projectDisplayName += targetFrameworkSuffix;
            projectFullPathWithTargetFramework += targetFrameworkSuffix;
        }

        _projectSystemProject.DisplayName = projectDisplayName;
        _projectSystemProject.OutputFilePath = newProjectInfo.OutputFilePath;
        _projectSystemProject.OutputRefFilePath = newProjectInfo.OutputRefFilePath;

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
            "Project {0} now has {1} source file(s).");

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
        }).Where(static cr => cr.Reference is not null).ToImmutableArray();

        UpdateProjectSystemProjectCollection(
            metadataReferences,
            _mostRecentMetadataReferences,
            EqualityComparer<CommandLineReference>.Default, // CommandLineReference already implements equality
            reference => _projectSystemProject.AddMetadataReference(reference.Reference, reference.Properties),
            reference => _projectSystemProject.RemoveMetadataReference(reference.Reference, reference.Properties),
            "Project {0} now has {1} reference(s).");

        // Now that we've updated it hold onto the old list of references so we can remove them if there's a later update
        _mostRecentMetadataReferences = metadataReferences;

        var analyzerReferences = commandLineArguments.AnalyzerReferences.Select(cr =>
        {
            // Note that unlike regular references, we do not resolve these with the relative path resolver that searches reference paths
            var absolutePath = FileUtilities.ResolveRelativePath(cr.FilePath, commandLineArguments.BaseDirectory);
            return absolutePath is not null ? new CommandLineAnalyzerReference(absolutePath) : default;
        }).Where(static cr => cr.FilePath is not null).ToImmutableArray();

        UpdateProjectSystemProjectCollection(
            analyzerReferences,
            _mostRecentAnalyzerReferences,
            EqualityComparer<CommandLineAnalyzerReference>.Default, // CommandLineAnalyzerReference already implements equality
            reference => _projectSystemProject.AddAnalyzerReference(reference.FilePath),
            reference => _projectSystemProject.RemoveAnalyzerReference(reference.FilePath),
            "Project {0} now has {1} analyzer reference(s).");

        _mostRecentAnalyzerReferences = analyzerReferences;

        UpdateProjectSystemProjectCollection(
            newProjectInfo.AdditionalDocuments,
            _mostRecentFileInfo?.AdditionalDocuments,
            DocumentFileInfoComparer.Instance,
            document => _projectSystemProject.AddAdditionalFile(document.FilePath, folders: document.Folders),
            document => _projectSystemProject.RemoveAdditionalFile(document.FilePath),
            "Project {0} now has {1} additional file(s).");

        UpdateProjectSystemProjectCollection(
            newProjectInfo.AnalyzerConfigDocuments,
            _mostRecentFileInfo?.AnalyzerConfigDocuments,
            DocumentFileInfoComparer.Instance,
            document => _projectSystemProject.AddAnalyzerConfigFile(document.FilePath),
            document => _projectSystemProject.RemoveAnalyzerConfigFile(document.FilePath),
            "Project {0} now has {1} analyzer config file(s).");

        UpdateProjectSystemProjectCollection(
            newProjectInfo.AdditionalDocuments.Where(TreatAsIsDynamicFile),
            _mostRecentFileInfo?.AdditionalDocuments.Where(TreatAsIsDynamicFile),
            DocumentFileInfoComparer.Instance,
            document => _projectSystemProject.AddDynamicSourceFile(document.FilePath, folders: ImmutableArray<string>.Empty),
            document => _projectSystemProject.RemoveDynamicSourceFile(document.FilePath),
            "Project {0} now has {1} dynamic file(s).");

        WatchProjectAssetsFile(newProjectInfo, _fileChangeContext);

        var needsRestore = ProjectDependencyHelper.NeedsRestore(newProjectInfo, _mostRecentFileInfo, logger);

        _mostRecentFileInfo = newProjectInfo;

        Contract.ThrowIfNull(_projectSystemProject.CompilationOptions, "Compilation options cannot be null for C#/VB project");
        var outputKind = _projectSystemProject.CompilationOptions.OutputKind;
        return (metadataReferences, outputKind, needsRestore);

        // logMessage should be a string with two placeholders; the first is the project name, the second is the number of items.
        void UpdateProjectSystemProjectCollection<T>(IEnumerable<T> loadedCollection, IEnumerable<T>? oldLoadedCollection, IEqualityComparer<T> comparer, Action<T> addItem, Action<T> removeItem, string logMessage)
        {
            var newItems = new HashSet<T>(loadedCollection, comparer);
            var oldItems = new HashSet<T>(comparer);
            var oldItemsCount = oldItems.Count;

            if (oldLoadedCollection != null)
            {
                foreach (var item in oldLoadedCollection)
                    oldItems.Add(item);
            }

            foreach (var newItem in newItems)
            {
                // If oldItems already has this, we don't need to add it again. We'll remove it, and what is left in oldItems is stuff to remove
                if (!oldItems.Remove(newItem))
                    addItem(newItem);
            }

            foreach (var oldItem in oldItems)
            {
                removeItem(oldItem);
            }

            if (newItems.Count != oldItemsCount)
                logger.LogTrace(logMessage, projectFullPathWithTargetFramework, newItems.Count);
        }

        void WatchProjectAssetsFile(ProjectFileInfo currentProjectInfo, IFileChangeContext fileChangeContext)
        {
            if (_mostRecentFileInfo?.ProjectAssetsFilePath == currentProjectInfo.ProjectAssetsFilePath)
            {
                // The file path hasn't changed, just keep using the same watcher.
                return;
            }

            // Dispose of the last once since we're changing the file we're watching.
            _mostRecentProjectAssetsFileWatcher?.Dispose();

            IWatchedFile? currentWatcher = null;
            if (currentProjectInfo.ProjectAssetsFilePath != null)
            {
                currentWatcher = fileChangeContext.EnqueueWatchingFile(currentProjectInfo.ProjectAssetsFilePath);
            }

            _mostRecentProjectAssetsFileWatcher = currentWatcher;
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
