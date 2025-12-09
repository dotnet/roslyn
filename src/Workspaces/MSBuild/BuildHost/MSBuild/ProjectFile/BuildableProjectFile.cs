// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;
using MSB = Microsoft.Build;

namespace Microsoft.CodeAnalysis.MSBuild;

internal sealed class BuildableProjectFile(ProjectFile projectFile, ProjectBuildManager buildManager, DiagnosticLog log) : IProjectFile
{
    public string FilePath
        => projectFile.FilePath;

    public string Language
        => projectFile.Language;

    public MSB.Evaluation.Project? Project
        => projectFile.Project;

    public ImmutableArray<DiagnosticLogItem> GetDiagnosticLogItems()
        => [.. log];

    /// <summary>
    /// Gets project file information asynchronously. Note that this can produce multiple
    /// instances of <see cref="ProjectFileInfo"/> if the project is multi-targeted: one for
    /// each target framework.
    /// </summary>
    public async Task<ImmutableArray<ProjectFileInfo>> GetProjectFileInfosAsync(CancellationToken cancellationToken)
    {
        if (projectFile.Project is null)
        {
            return [ProjectFileInfo.CreateEmpty(Language, filePath: null)];
        }

        var projectInstances = await buildManager.BuildProjectInstancesAsync(projectFile.Project, log, cancellationToken).ConfigureAwait(false);
        return projectInstances.SelectAsArray(projectFile.CreateProjectFileInfo);
    }

    public void AddDocument(string filePath, string? logicalPath = null)
    {
        if (Project is null)
        {
            return;
        }

        var relativePath = PathUtilities.GetRelativePath(Project.DirectoryPath, filePath);

        Dictionary<string, string>? metadata = null;
        if (logicalPath != null && relativePath != logicalPath)
        {
            metadata = new Dictionary<string, string>
            {
                { MetadataNames.Link, logicalPath }
            };

            relativePath = filePath; // link to full path
        }

        Project.AddItem(ItemNames.Compile, relativePath, metadata);
    }

    public void RemoveDocument(string filePath)
    {
        if (Project is null)
        {
            return;
        }

        var relativePath = PathUtilities.GetRelativePath(Project.DirectoryPath, filePath);

        var items = Project.GetItems(ItemNames.Compile);
        var item = items.FirstOrDefault(it => PathUtilities.PathsEqual(it.EvaluatedInclude, relativePath)
                                           || PathUtilities.PathsEqual(it.EvaluatedInclude, filePath));
        if (item != null)
        {
            Project.RemoveItem(item);
        }
    }

    public void AddMetadataReference(string metadataReferenceIdentity, ImmutableArray<string> aliases, string? hintPath)
    {
        if (Project is null)
        {
            return;
        }

        var metadata = new Dictionary<string, string>();
        if (!aliases.IsEmpty)
            metadata.Add(MetadataNames.Aliases, string.Join(",", aliases));

        if (hintPath is not null)
            metadata.Add(MetadataNames.HintPath, hintPath);

        Project.AddItem(ItemNames.Reference, metadataReferenceIdentity, metadata);
    }

    public void RemoveMetadataReference(string shortAssemblyName, string fullAssemblyName, string filePath)
    {
        if (Project is null)
        {
            return;
        }

        var item = FindReferenceItem(shortAssemblyName, fullAssemblyName, filePath);
        if (item != null)
        {
            Project.RemoveItem(item);
        }
    }

    private MSB.Evaluation.ProjectItem FindReferenceItem(string shortAssemblyName, string fullAssemblyName, string filePath)
    {
        Contract.ThrowIfNull(Project, "The project was not loaded.");

        var references = Project.GetItems(ItemNames.Reference);
        MSB.Evaluation.ProjectItem? item = null;

        var fileName = Path.GetFileNameWithoutExtension(filePath);

        // check for short name match
        item = references.FirstOrDefault(it => string.Compare(it.EvaluatedInclude, shortAssemblyName, StringComparison.OrdinalIgnoreCase) == 0);
        if (item is not null)
            return item;

        // check for full name match
        item = references.FirstOrDefault(it => string.Compare(it.EvaluatedInclude, fullAssemblyName, StringComparison.OrdinalIgnoreCase) == 0);
        if (item is not null)
            return item;

        // check for file path match
        var relativePath = PathUtilities.GetRelativePath(Project.DirectoryPath, filePath);

        item = references.FirstOrDefault(it => PathUtilities.PathsEqual(it.EvaluatedInclude, filePath)
                                                || PathUtilities.PathsEqual(it.EvaluatedInclude, relativePath)
                                                || PathUtilities.PathsEqual(GetHintPath(it), filePath)
                                                || PathUtilities.PathsEqual(GetHintPath(it), relativePath));

        if (item is not null)
            return item;

        var partialName = shortAssemblyName + ",";
        var items = references.Where(it => it.EvaluatedInclude.StartsWith(partialName, StringComparison.OrdinalIgnoreCase)).ToList();
        if (items.Count == 1)
        {
            return items[0];
        }

        throw new InvalidOperationException($"Unable to find reference item '{shortAssemblyName}'");
    }

    private static string GetHintPath(MSB.Evaluation.ProjectItem item)
        => item.Metadata.FirstOrDefault(m => string.Equals(m.Name, MetadataNames.HintPath, StringComparison.OrdinalIgnoreCase))?.EvaluatedValue ?? string.Empty;

    public void AddProjectReference(string projectName, ProjectFileReference reference)
    {
        if (Project is null)
        {
            return;
        }

        var metadata = new Dictionary<string, string>
        {
            { MetadataNames.Name, projectName }
        };

        if (!reference.Aliases.IsEmpty)
        {
            metadata.Add(MetadataNames.Aliases, string.Join(",", reference.Aliases));
        }

        var relativePath = PathUtilities.GetRelativePath(Project.DirectoryPath, reference.Path);
        Project.AddItem(ItemNames.ProjectReference, relativePath, metadata);
    }

    public void RemoveProjectReference(string projectName, string projectFilePath)
    {
        if (Project is null)
        {
            return;
        }

        var item = FindProjectReferenceItem(projectName, projectFilePath);
        if (item != null)
        {
            Project.RemoveItem(item);
        }
    }

    private MSB.Evaluation.ProjectItem? FindProjectReferenceItem(string projectName, string projectFilePath)
    {
        if (Project is null)
        {
            return null;
        }

        var references = Project.GetItems(ItemNames.ProjectReference);
        var relativePath = PathUtilities.GetRelativePath(Project.DirectoryPath, projectFilePath);

        MSB.Evaluation.ProjectItem? item = null;

        // find by project file path
        item = references.First(it => PathUtilities.PathsEqual(it.EvaluatedInclude, relativePath)
                                   || PathUtilities.PathsEqual(it.EvaluatedInclude, projectFilePath));

        // try to find by project name
        item ??= references.First(it => string.Compare(projectName, it.GetMetadataValue(MetadataNames.Name), StringComparison.OrdinalIgnoreCase) == 0);

        return item;
    }

    public void AddAnalyzerReference(string fullPath)
    {
        if (Project is null)
        {
            return;
        }

        var relativePath = PathUtilities.GetRelativePath(Project.DirectoryPath, fullPath);
        Project.AddItem(ItemNames.Analyzer, relativePath);
    }

    public void RemoveAnalyzerReference(string fullPath)
    {
        if (Project is null)
        {
            return;
        }

        var relativePath = PathUtilities.GetRelativePath(Project.DirectoryPath, fullPath);

        var analyzers = Project.GetItems(ItemNames.Analyzer);
        var item = analyzers.FirstOrDefault(it => PathUtilities.PathsEqual(it.EvaluatedInclude, relativePath)
                                                || PathUtilities.PathsEqual(it.EvaluatedInclude, fullPath));
        if (item != null)
        {
            Project.RemoveItem(item);
        }
    }

    public void Save()
    {
        if (Project is null)
        {
            return;
        }

        Project.Save();
    }
}
