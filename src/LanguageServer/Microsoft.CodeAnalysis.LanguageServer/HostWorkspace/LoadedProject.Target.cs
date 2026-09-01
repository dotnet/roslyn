// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.ProjectTelemetry;
using Microsoft.CodeAnalysis.ProjectSystem;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Workspaces.ProjectSystem;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.Logging;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

internal sealed partial class LoadedProject
{
    /// <summary>
    /// A single target of a (potentially) multi-targeted project. This type has no locking -- since it's private it's expected that the containing <see cref="LoadedProject" /> will
    /// acquire its own lock before using this type.
    /// </summary>
    private sealed class Target : IDisposable
    {
        private readonly LoadedProject _project;

        private readonly ProjectSystemProject _projectSystemProject;
        public bool NeedsRestore { get; private set; }
        public ProjectSystemProjectFactory ProjectFactory { get; }
        private readonly ProjectSystemProjectOptionsProcessor _optionsProcessor;
        private readonly IFileChangeContext _assetsFileChangeContext;

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

        public Target(LoadedProject project, ProjectSystemProject projectSystemProject, ProjectSystemProjectFactory projectFactory)
        {
            _project = project;

            _projectSystemProject = projectSystemProject;
            ProjectFactory = projectFactory;
            _optionsProcessor = new ProjectSystemProjectOptionsProcessor(projectSystemProject, projectFactory.Workspace.CurrentSolution.Services);

            _assetsFileChangeContext = _project._fileWatcher.CreateContext([]);
            _assetsFileChangeContext.FileChanged += AssetsFileChangeContext_FileChanged;
        }

        private void AssetsFileChangeContext_FileChanged(object? sender, FileChangedEventArgs e)
        {
            var checksum = GetAssetsFileChecksum(e.FilePath);

            if (_mostRecentProjectAssetsFileChecksum != checksum)
            {
                _mostRecentProjectAssetsFileChecksum = checksum;
                _project.NeedsReload?.Invoke(_project, e.FilePath);
            }
        }

        private static Checksum GetAssetsFileChecksum(string assetsFilePath)
        {
            return Shared.Utilities.IOUtilities.PerformIO(() =>
            {
                // We only want to trigger design time build if the assets file content actually changed from the last time this handler was called.
                // Sometimes we can get a change event where no content changed (e.g. for a failed restore).
                // In such cases, proceeding with design-time build can put us in a restore loop (since the design-time build notices that assets are missing).
                using var assetsFileStream = File.OpenRead(assetsFilePath);
                return Checksum.Create(assetsFileStream);
            }, defaultValue: Checksum.Null);
        }

        public string? GetTargetFramework()
        {
            Contract.ThrowIfNull(_mostRecentFileInfo, "We haven't been given a loaded project yet, so we can't provide the existing TFM.");
            return _mostRecentFileInfo.TargetFramework;
        }

        public ProjectId ProjectId => _projectSystemProject.Id;

        /// <summary>
        /// Unloads the target and removes it from the workspace.
        /// </summary>
        public void Dispose()
        {
            Contract.ThrowIfFalse(_project._gate.CurrentCount == 0, $"We should be holding {nameof(LoadedProject)}.{nameof(LoadedProject._gate)} for all methods in this class.");

            _mostRecentProjectAssetsFileWatcher?.Dispose();
            _assetsFileChangeContext.Dispose();
            _optionsProcessor.Dispose();
            _projectSystemProject.RemoveFromWorkspace();
        }

        public async ValueTask UpdateWithNewProjectInfoAsync(ProjectFileInfo newProjectInfo, bool isMiscellaneousFile, bool hasAllInformation, ProjectTargetFrameworkManager targetFrameworkManager, ILogger logger)
        {
            Contract.ThrowIfFalse(_project._gate.CurrentCount == 0, $"We should be holding {nameof(LoadedProject)}.{nameof(LoadedProject._gate)} for all methods in this class.");

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
            _projectSystemProject.HasAllInformation = hasAllInformation;

            // TODO: It's not clear to me why we set this here rather than just when we created the project, since the target framework is part of identity of this target
            if (newProjectInfo.TargetFrameworkIdentifier != null)
            {
                targetFrameworkManager.UpdateIdentifierForProject(_projectSystemProject.Id, newProjectInfo.TargetFrameworkIdentifier);
            }

            _optionsProcessor.SetCommandLine([.. newProjectInfo.CommandLineArgs]);
            var commandLineArguments = _optionsProcessor.GetParsedCommandLineArguments();

            UpdateProjectSystemProjectCollection(
                newProjectInfo.Documents,
                _mostRecentFileInfo?.Documents,
                DocumentFileInfoComparer.Instance,
                document =>
                {
                    if (PathUtilities.IsAbsolute(document.FilePath))
                        _projectSystemProject.AddSourceFile(document.FilePath, folders: [.. document.Folders]);
                    else
                        // When the file doesn't have an absolute path, then we think it doesn't exist on disk.
                        // e.g. it is a virtual document for an unsaved file or similar.
                        // In this case we just put a SourceTextContainer with empty text for it and rely on the LSP's solution forking to ensure it has up to date text.
                        _projectSystemProject.AddSourceTextContainer(SourceText.From("").Container, document.FilePath, folders: [.. document.Folders]);
                },
                document =>
                {
                    Contract.ThrowIfFalse(PathUtilities.IsAbsolute(document.FilePath), "We do not expect to remove a file which is not on disk from the project.");
                    _projectSystemProject.RemoveSourceFile(document.FilePath);
                },
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
                document => _projectSystemProject.AddAdditionalFile(document.FilePath, folders: [.. document.Folders]),
                document => _projectSystemProject.RemoveAdditionalFile(document.FilePath),
                "Project {0} now has {1} additional file(s). ({2} added, {3} removed.)");

            UpdateProjectSystemProjectCollection(
                newProjectInfo.AnalyzerConfigDocuments,
                _mostRecentFileInfo?.AnalyzerConfigDocuments,
                DocumentFileInfoComparer.Instance,
                document => _projectSystemProject.AddAnalyzerConfigFile(document.FilePath),
                document => _projectSystemProject.RemoveAnalyzerConfigFile(document.FilePath),
                "Project {0} now has {1} analyzer config file(s). ({2} added, {3} removed.)");

            WatchProjectAssetsFile(newProjectInfo);

            NeedsRestore = ProjectDependencyHelper.NeedsRestore(newProjectInfo, _mostRecentFileInfo, logger);

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
            return;

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

                if (currentProjectInfo.ProjectAssetsFilePath is { } assetsFilePath)
                {
                    _mostRecentProjectAssetsFileWatcher = _assetsFileChangeContext.EnqueueWatchingFile(assetsFilePath);

                    // Update the checksum we keep -- otherwise the first restore or build that touches this (even if it doesn't change the contents)
                    // would retrigger design time builds.
                    _mostRecentProjectAssetsFileChecksum = GetAssetsFileChecksum(assetsFilePath);
                }
                else
                {
                    _mostRecentProjectAssetsFileWatcher = null;
                }
            }
        }

        public bool FilePathIsIncludedInFileGlobs(string filePath)
        {
            Contract.ThrowIfFalse(_project._gate.CurrentCount == 0, $"We should be holding {nameof(LoadedProject)}.{nameof(LoadedProject._gate)} for all methods in this class.");

            if (_project._projectDirectory is null)
                return false;

            var matchers = _mostRecentFileMatchers?.Value;
            if (matchers is null)
                return false;

            // Check if the file path matches any of the globs in the project file.
            foreach (var matcher in matchers)
            {
                // CPS re-creates the msbuild globs from the includes/excludes/removes and the project XML directory and
                // ignores the MSBuildGlob.FixedDirectoryPart.  We'll do the same here and match using the project directory as the relative path.
                // See https://devdiv.visualstudio.com/DevDiv/_git/CPS?path=/src/Microsoft.VisualStudio.ProjectSystem/Build/MsBuildGlobFactory.cs
                var relativeDirectory = _project._projectDirectory;

                var matches = matcher.Match(relativeDirectory, filePath);
                if (matches.HasMatches)
                    return true;
            }

            return false;
        }

        public (ProjectFileInfo, ImmutableArray<CommandLineReference>, OutputKind) GetTelemetryInfo()
        {
            Contract.ThrowIfFalse(_project._gate.CurrentCount == 0, $"We should be holding {nameof(LoadedProject)}.{nameof(LoadedProject._gate)} for all methods in this class.");

            Contract.ThrowIfNull(_mostRecentFileInfo);
            Contract.ThrowIfNull(_projectSystemProject.CompilationOptions, "We previously asserted CompilationOptions should be non-null when we applied the previous update.");

            return (_mostRecentFileInfo, _mostRecentMetadataReferences, _projectSystemProject.CompilationOptions.OutputKind);

        }
    }
}
