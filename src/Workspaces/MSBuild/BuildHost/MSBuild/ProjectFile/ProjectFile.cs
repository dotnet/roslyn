// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;
using MSB = Microsoft.Build;

namespace Microsoft.CodeAnalysis.MSBuild;

internal sealed class ProjectFile(
    string language,
    string projectFilePath,
    MSB.Evaluation.Project? project,
    ProjectBuildManager buildManager,
    RpcServer server,
    DiagnosticLog log) :
#if NETFRAMEWORK
    MarshalByRefObject, // We need this object to pass across the AppDomain boundary when on .NET Framework
#endif
    IProjectFile
{
    private readonly ProjectCommandLineProvider? _commandLineProvider = ProjectCommandLineProvider.TryCreate(language, buildManager.KnownCommandLineParserLanguages);

    public string FilePath
        => project?.FullPath ?? string.Empty;

    /// <summary>
    /// The original C# file path if this corresponds to a virtual project.
    /// </summary>
    public string? PhysicalFilePath { get; init; }

    public DiagnosticLogItem[] GetEvaluationDiagnosticLogItems()
        => [.. log];

    /// <summary>
    /// Gets project file information asynchronously. Note that this can produce multiple
    /// instances of <see cref="ProjectFileInfo"/> if the project is multi-targeted: one for
    /// each target framework.
    /// </summary>
    public async Task<ProjectFileInfo[]> GetProjectFileInfosAsync(CancellationToken cancellationToken)
    {
        if (project is null)
        {
            return [ProjectFileInfo.CreateEmpty(language, PhysicalFilePath ?? projectFilePath)];
        }

        var targetFrameworkValue = project.GetPropertyValue(PropertyNames.TargetFramework);
        var targetFrameworksValue = project.GetPropertyValue(PropertyNames.TargetFrameworks);

        if (!RoslynString.IsNullOrEmpty(targetFrameworkValue) || RoslynString.IsNullOrEmpty(targetFrameworksValue))
        {
            return [await BuildProjectFileInfoAsync().ConfigureAwait(false)];
        }

        // This project has a <TargetFrameworks> property, but does not specify a <TargetFramework>.
        // In this case, we need to iterate through the <TargetFrameworks>, set <TargetFramework> with
        // each value, and build the project.

        var targetFrameworks = targetFrameworksValue.Split(';');

        if (!project.GlobalProperties.TryGetValue(PropertyNames.TargetFramework, out var initialGlobalTargetFrameworkValue))
            initialGlobalTargetFrameworkValue = null;

        var results = new List<ProjectFileInfo>(targetFrameworks.Length);
        foreach (var targetFramework in targetFrameworks)
        {
            project.SetGlobalProperty(PropertyNames.TargetFramework, targetFramework);

            try
            {
                project.ReevaluateIfNecessary();
                results.Add(await BuildProjectFileInfoAsync().ConfigureAwait(false));
            }
            catch (MSB.Exceptions.InvalidProjectFileException e)
            {
                // The project only fails to evaluate for this target framework, so report that for just this target and still build the others.
                var failureLog = new DiagnosticLog();
                failureLog.Add(e, project.FullPath);
                results.Add(ProjectFileInfo.CreateEmpty(language, PhysicalFilePath ?? projectFilePath) with { TargetFramework = targetFramework, Diagnostics = [.. failureLog] });
            }
        }

        if (initialGlobalTargetFrameworkValue is null)
        {
            project.RemoveGlobalProperty(PropertyNames.TargetFramework);
        }
        else
        {
            project.SetGlobalProperty(PropertyNames.TargetFramework, initialGlobalTargetFrameworkValue);
        }

        // This puts the project back to how it was evaluated when it was loaded, which succeeded, so unlike in the loop this can't fail
        project.ReevaluateIfNecessary();

        return [.. results];

        async Task<ProjectFileInfo> BuildProjectFileInfoAsync()
        {
            var (projectInstance, log) = await buildManager.BuildProjectInstanceAsync(project, cancellationToken).ConfigureAwait(false);

            var projectFileInfo = new ProjectInstanceReader(language, _commandLineProvider, projectInstance, project, PhysicalFilePath).CreateProjectFileInfo();
            return projectFileInfo with { Diagnostics = [.. log] };
        }
    }

    public void AddDocument(string filePath, string? logicalPath = null)
    {
        if (project is null)
        {
            return;
        }

        var relativePath = PathUtilities.GetRelativePath(project.DirectoryPath, filePath);

        Dictionary<string, string>? metadata = null;
        if (logicalPath != null && relativePath != logicalPath)
        {
            metadata = new Dictionary<string, string>
            {
                { MetadataNames.Link, logicalPath }
            };

            relativePath = filePath; // link to full path
        }

        project.AddItem(ItemNames.Compile, relativePath, metadata);
    }

    public void RemoveDocument(string filePath)
    {
        if (project is null)
        {
            return;
        }

        var relativePath = PathUtilities.GetRelativePath(project.DirectoryPath, filePath);

        var items = project.GetItems(ItemNames.Compile);
        var item = items.FirstOrDefault(it => PathUtilities.PathsEqual(it.EvaluatedInclude, relativePath)
                                           || PathUtilities.PathsEqual(it.EvaluatedInclude, filePath));
        if (item != null)
        {
            project.RemoveItem(item);
        }
    }

    public void AddMetadataReference(string metadataReferenceIdentity, string[] aliases, string? hintPath)
    {
        if (project is null)
        {
            return;
        }

        var metadata = new Dictionary<string, string>();
        if (aliases.Length > 0)
            metadata.Add(MetadataNames.Aliases, string.Join(",", aliases));

        if (hintPath is not null)
            metadata.Add(MetadataNames.HintPath, hintPath);

        project.AddItem(ItemNames.Reference, metadataReferenceIdentity, metadata);
    }

    public void RemoveMetadataReference(string shortAssemblyName, string fullAssemblyName, string filePath)
    {
        if (project is null)
        {
            return;
        }

        var item = FindReferenceItem(shortAssemblyName, fullAssemblyName, filePath);
        if (item != null)
        {
            project.RemoveItem(item);
        }
    }

    private MSB.Evaluation.ProjectItem FindReferenceItem(string shortAssemblyName, string fullAssemblyName, string filePath)
    {
        Contract.ThrowIfNull(project, "The project was not loaded.");

        var references = project.GetItems(ItemNames.Reference);
        MSB.Evaluation.ProjectItem? item = null;

        var fileName = Path.GetFileNameWithoutExtension(filePath);

        // check for short name match
        item = references.FirstOrDefault(it => string.Equals(it.EvaluatedInclude, shortAssemblyName, StringComparison.OrdinalIgnoreCase));
        if (item is not null)
            return item;

        // check for full name match
        item = references.FirstOrDefault(it => string.Equals(it.EvaluatedInclude, fullAssemblyName, StringComparison.OrdinalIgnoreCase));
        if (item is not null)
            return item;

        // check for file path match
        var relativePath = PathUtilities.GetRelativePath(project.DirectoryPath, filePath);

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
        if (project is null)
        {
            return;
        }

        var metadata = new Dictionary<string, string>
        {
            { MetadataNames.Name, projectName }
        };

        if (reference.Aliases.Length > 0)
        {
            metadata.Add(MetadataNames.Aliases, string.Join(",", reference.Aliases));
        }

        var relativePath = PathUtilities.GetRelativePath(project.DirectoryPath, reference.Path);
        project.AddItem(ItemNames.ProjectReference, relativePath, metadata);
    }

    public void RemoveProjectReference(string projectName, string projectFilePath)
    {
        if (project is null)
        {
            return;
        }

        var item = FindProjectReferenceItem(projectName, projectFilePath);
        if (item != null)
        {
            project.RemoveItem(item);
        }
    }

    private MSB.Evaluation.ProjectItem? FindProjectReferenceItem(string projectName, string projectFilePath)
    {
        if (project is null)
        {
            return null;
        }

        var references = project.GetItems(ItemNames.ProjectReference);
        var relativePath = PathUtilities.GetRelativePath(project.DirectoryPath, projectFilePath);

        MSB.Evaluation.ProjectItem? item = null;

        // find by project file path
        item = references.First(it => PathUtilities.PathsEqual(it.EvaluatedInclude, relativePath)
                                   || PathUtilities.PathsEqual(it.EvaluatedInclude, projectFilePath));

        // try to find by project name
        item ??= references.First(it => string.Equals(projectName, it.GetMetadataValue(MetadataNames.Name), StringComparison.OrdinalIgnoreCase));

        return item;
    }

    public void AddAnalyzerReference(string fullPath)
    {
        if (project is null)
        {
            return;
        }

        var relativePath = PathUtilities.GetRelativePath(project.DirectoryPath, fullPath);
        project.AddItem(ItemNames.Analyzer, relativePath);
    }

    public void RemoveAnalyzerReference(string fullPath)
    {
        if (project is null)
        {
            return;
        }

        var relativePath = PathUtilities.GetRelativePath(project.DirectoryPath, fullPath);

        var analyzers = project.GetItems(ItemNames.Analyzer);
        var item = analyzers.FirstOrDefault(it => PathUtilities.PathsEqual(it.EvaluatedInclude, relativePath)
                                                || PathUtilities.PathsEqual(it.EvaluatedInclude, fullPath));
        if (item != null)
        {
            project.RemoveItem(item);
        }
    }

    public void Save()
    {
        if (project is null)
        {
            return;
        }

        project.Save();
    }

    public void Dispose()
    {
        server.RemoveTarget(this);

        if (project is not null)
            buildManager.UnloadProject(project);
    }
}
