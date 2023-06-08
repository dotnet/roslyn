// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServer.Handler.DebugConfiguration;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.ProjectSystem;
using Microsoft.CodeAnalysis.Workspaces.ProjectSystem;
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
    private ImmutableArray<CommandLineReference> _mostRecentMetadataReferences = ImmutableArray<CommandLineReference>.Empty;

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

    public async ValueTask UpdateWithNewProjectInfoAsync(ProjectFileInfo newProjectInfo)
    {
        if (_mostRecentFileInfo != null)
        {
            // We should never be changing the fundamental identity of this project; if this happens we really should have done a full unload/reload.
            Contract.ThrowIfFalse(newProjectInfo.FilePath == _mostRecentFileInfo.FilePath);
            Contract.ThrowIfFalse(newProjectInfo.TargetFramework == _mostRecentFileInfo.TargetFramework);
        }

        await using var batch = _projectSystemProject.CreateBatchScope();

        var projectDisplayName = Path.GetFileNameWithoutExtension(newProjectInfo.FilePath);

        if (newProjectInfo.TargetFramework != null)
        {
            projectDisplayName += " (" + newProjectInfo.TargetFramework + ")";
        }

        _projectSystemProject.OutputFilePath = newProjectInfo.OutputFilePath;
        _projectSystemProject.OutputRefFilePath = newProjectInfo.OutputRefFilePath;

        if (newProjectInfo.TargetFrameworkIdentifier != null)
        {
            _targetFrameworkManager.UpdateIdentifierForProject(_projectSystemProject.Id, newProjectInfo.TargetFrameworkIdentifier);
        }

        _optionsProcessor.SetCommandLine(newProjectInfo.CommandLineArgs);

        UpdateProjectSystemProjectCollection(
            newProjectInfo.Documents,
            _mostRecentFileInfo?.Documents,
            DocumentFileInfoComparer.Instance,
            document => _projectSystemProject.AddSourceFile(document.FilePath),
            document => _projectSystemProject.RemoveSourceFile(document.FilePath));

        var metadataReferences = _optionsProcessor.GetParsedCommandLineArguments().MetadataReferences.Distinct();

        UpdateProjectSystemProjectCollection(
            metadataReferences,
            _mostRecentMetadataReferences,
            EqualityComparer<CommandLineReference>.Default, // CommandLineReference already implements equality
            reference => _projectSystemProject.AddMetadataReference(reference.Reference, reference.Properties),
            reference => _projectSystemProject.RemoveMetadataReference(reference.Reference, reference.Properties));

        // Now that we've updated it hold onto the old list of references so we can remove them if there's a later update
        _mostRecentMetadataReferences = metadataReferences;

        UpdateProjectSystemProjectCollection(
            newProjectInfo.AdditionalDocuments.Distinct(DocumentFileInfoComparer.Instance), // TODO: figure out why we have duplicates
            _mostRecentFileInfo?.AdditionalDocuments.Distinct(DocumentFileInfoComparer.Instance),
            DocumentFileInfoComparer.Instance,
            document => _projectSystemProject.AddAdditionalFile(document.FilePath),
            document => _projectSystemProject.RemoveAdditionalFile(document.FilePath));

        UpdateProjectSystemProjectCollection(
            newProjectInfo.AnalyzerConfigDocuments,
            _mostRecentFileInfo?.AnalyzerConfigDocuments,
            DocumentFileInfoComparer.Instance,
            document => _projectSystemProject.AddAnalyzerConfigFile(document.FilePath),
            document => _projectSystemProject.RemoveAnalyzerConfigFile(document.FilePath));

        UpdateProjectSystemProjectCollection(
            newProjectInfo.AdditionalDocuments.Where(TreatAsIsDynamicFile).Distinct(DocumentFileInfoComparer.Instance), // TODO: figure out why we have duplicates
            _mostRecentFileInfo?.AdditionalDocuments.Where(TreatAsIsDynamicFile).Distinct(DocumentFileInfoComparer.Instance),
            DocumentFileInfoComparer.Instance,
            document => _projectSystemProject.AddDynamicSourceFile(document.FilePath, folders: ImmutableArray<string>.Empty),
            document => _projectSystemProject.RemoveDynamicSourceFile(document.FilePath));

        _mostRecentFileInfo = newProjectInfo;

        return;

        static void UpdateProjectSystemProjectCollection<T>(IEnumerable<T> loadedCollection, IEnumerable<T>? oldLoadedCollection, IEqualityComparer<T> comparer, Action<T> addItem, Action<T> removeItem)
        {
            var oldItems = new HashSet<T>(comparer);

            if (oldLoadedCollection != null)
            {
                foreach (var item in oldLoadedCollection)
                    oldItems.Add(item);
            }

            foreach (var newItem in loadedCollection)
            {
                // If oldItems already has this, we don't need to add it again. We'll remove it, and what is left in oldItems is stuff to remove
                if (!oldItems.Remove(newItem))
                    addItem(newItem);
            }

            foreach (var oldItem in oldItems)
            {
                removeItem(oldItem);
            }
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
