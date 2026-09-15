// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.CodeAnalysis.FileBasedPrograms;
using Microsoft.DotNet.FileBasedPrograms;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.MSBuild;

internal static class FileBasedProgramsProjectLoader
{
    internal sealed record ProjectLoadResult(
        string EntryPointFilePath,
        DateTime GraphLoadStartTimeUtc,
        DateTime EntryPointLastWriteTimeUtc,
        ImmutableArray<ProjectFileInfo> ProjectFileInfos,
        ImmutableArray<DiagnosticLogItem> DiagnosticLogItems);

    internal sealed record ProjectGraphLoadResult(
        ProjectLoadResult Root,
        ImmutableArray<ProjectLoadResult> ReferencedProjects);

    public static async Task<RemoteProjectFile> LoadFileBasedAppProjectAsync(
        RemoteBuildHost buildHost,
        IFileBasedProgramService fileBasedProgramService,
        string entryPointFilePath,
        Action<string> reportError,
        CancellationToken cancellationToken)
    {
        var buildService = new FileBasedProgramsBuildService(buildHost, cancellationToken);
        await using var _ = buildService.ConfigureAwait(false);
        var projectRootElement = await fileBasedProgramService.LoadFileBasedAppProjectAsync(
            buildService,
            FileBasedProgramsBuildService.ProjectCollection,
            entryPointFilePath,
            reportError).ConfigureAwait(false);
        return await buildHost.LoadProjectAsync(
            projectRootElement.FullPath!,
            physicalFilePath: entryPointFilePath,
            projectRootElement.GetRawXml(),
            LanguageNames.CSharp,
            globalProperties: fileBasedProgramService.GetGlobalBuildProperties(),
            cancellationToken).ConfigureAwait(false);
    }

    public static async Task<ProjectGraphLoadResult> LoadFileBasedAppProjectGraphAsync(
        RemoteBuildHost buildHost,
        IFileBasedProgramService fileBasedProgramService,
        string entryPointFilePath,
        Action<string> reportError,
        CancellationToken cancellationToken)
    {
        var buildService = new FileBasedProgramsBuildService(buildHost, cancellationToken);
        await using var _ = buildService.ConfigureAwait(false);
        var graphLoadStartTimeUtc = DateTime.UtcNow;
        var rootProject = (ProjectRootElement)await fileBasedProgramService.LoadFileBasedAppProjectAsync(
            buildService,
            FileBasedProgramsBuildService.ProjectCollection,
            entryPointFilePath,
            reportError).ConfigureAwait(false);

        var rootResult = await LoadProjectAsync(rootProject, entryPointFilePath, disposeAfterLoad: false).ConfigureAwait(false);
        var referencedProjectResults = ImmutableArray.CreateBuilder<ProjectLoadResult>(Math.Max(0, buildService.ProjectRoots.Count - 1));
        foreach (var projectRoot in buildService.GetFinalReferencedProjectRoots(entryPointFilePath))
            referencedProjectResults.Add(await LoadProjectAsync(projectRoot, projectRoot.EntryPointFilePath, disposeAfterLoad: true).ConfigureAwait(false));

        return new(rootResult, referencedProjectResults.DrainToImmutable());

        async Task<ProjectLoadResult> LoadProjectAsync(ProjectRootElement projectRoot, string physicalFilePath, bool disposeAfterLoad)
        {
            var entryPointLastWriteTimeUtc = File.GetLastWriteTimeUtc(physicalFilePath);
            var loadedFile = await buildHost.LoadProjectAsync(
                projectRoot.FullPath!,
                physicalFilePath,
                projectRoot.GetRawXml(),
                LanguageNames.CSharp,
                globalProperties: fileBasedProgramService.GetGlobalBuildProperties(),
                cancellationToken).ConfigureAwait(false);

            try
            {
                return new(
                    physicalFilePath,
                    graphLoadStartTimeUtc,
                    entryPointLastWriteTimeUtc,
                    await loadedFile.GetProjectFileInfosAsync(cancellationToken).ConfigureAwait(false),
                    await loadedFile.GetDiagnosticLogItemsAsync(cancellationToken).ConfigureAwait(false));
            }
            finally
            {
                if (disposeAfterLoad)
                    await loadedFile.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}

/// <summary>
/// An implementation of <see cref="IBuildService"/> which uses MSBuild over RPC (via <see cref="RemoteBuildHost"/>).
/// </summary>
file sealed class FileBasedProgramsBuildService(RemoteBuildHost buildHost, CancellationToken cancellationToken) : IBuildService, IAsyncDisposable
{
    public static IProjectCollection ProjectCollection => Microsoft.CodeAnalysis.MSBuild.ProjectCollection.Instance;

    public ConcurrentBag<IAsyncDisposable> Disposables { get; } = [];
    public ConcurrentDictionary<string, ProjectRootElement> ProjectRoots { get; } = new(PathUtilities.Comparer);

    public async ValueTask<Microsoft.DotNet.FileBasedPrograms.IProjectInstance> CreateProjectInstanceFromProjectRootElementAsync(
        IProjectRootElement projectRoot,
        IProjectCollection projectCollection,
        IDictionary<string, string>? additionalGlobalProperties)
    {
        var root = (ProjectRootElement)projectRoot;
        var projectInstance = await ProjectInstance.FromProjectRootElementAsync(
            this, buildHost, root, (ProjectCollection)projectCollection, additionalGlobalProperties, cancellationToken).ConfigureAwait(false);
        ProjectRoots[root.EntryPointFilePath] = root;
        return projectInstance;
    }

    public IProjectRootElement CreateProjectRootElement(XmlReader xmlReader, IProjectCollection projectCollection, string entryPointFilePath)
    {
        xmlReader.MoveToContent();
        return new ProjectRootElement(entryPointFilePath, xmlReader.ReadOuterXml());
    }

    public IEnumerable<ProjectRootElement> GetFinalReferencedProjectRoots(string rootEntryPointFilePath)
    {
        foreach (var projectRoot in ProjectRoots.Values)
        {
            if (!PathUtilities.Comparer.Equals(projectRoot.EntryPointFilePath, rootEntryPointFilePath))
                yield return projectRoot;
        }
    }

    public async ValueTask DisposeAsync()
    {
        while (Disposables.TryTake(out var disposable))
        {
            await disposable.DisposeAsync().ConfigureAwait(false);
        }
    }
}

/// <summary>
/// Our adapter for MSBuild's <c>ProjectCollection</c> in <see cref="FileBasedProgramsBuildService"/> abstraction.
/// </summary>
file sealed class ProjectCollection : IProjectCollection
{
    public static ProjectCollection Instance { get; } = new();

    private ProjectCollection() { }
}

/// <summary>
/// Our adapter for MSBuild's <c>ProjectInstance</c> in <see cref="FileBasedProgramsBuildService"/> abstraction.
/// </summary>
file sealed class ProjectInstance(RemoteProjectInstance remoteProjectInstance, CancellationToken cancellationToken) : Microsoft.DotNet.FileBasedPrograms.IProjectInstance
{
    public static async ValueTask<Microsoft.DotNet.FileBasedPrograms.IProjectInstance> FromProjectRootElementAsync(
        FileBasedProgramsBuildService service,
        RemoteBuildHost buildHost,
        ProjectRootElement projectRoot,
        ProjectCollection projectCollection,
        IDictionary<string, string>? additionalGlobalProperties,
        CancellationToken cancellationToken)
    {
        Debug.Assert(projectCollection == ProjectCollection.Instance);
        var remoteProjectInstance = await buildHost.LoadProjectInstanceAsync(projectRoot.FullPath!, projectRoot.GetRawXml(), additionalGlobalProperties, cancellationToken).ConfigureAwait(false);
        service.Disposables.Add(remoteProjectInstance);
        return new ProjectInstance(remoteProjectInstance, cancellationToken);
    }

    public ValueTask<ImmutableArray<ImmutableArray<string>>> GetItemMetadataValuesAsync(string itemType, ImmutableArray<string> metadataNames) => new(remoteProjectInstance.GetItemMetadataValuesAsync(itemType, metadataNames.ToArray(), cancellationToken));
    public ValueTask<string> GetPropertyValueAsync(string propertyName) => new(remoteProjectInstance.GetPropertyValueAsync(propertyName, cancellationToken));
    public ValueTask<string> ExpandStringAsync(string value) => new(remoteProjectInstance.ExpandStringAsync(value, cancellationToken));
}

/// <summary>
/// Our adapter for MSBuild's <c>ProjectRootElement</c> in <see cref="FileBasedProgramsBuildService"/> abstraction.
/// </summary>
file sealed class ProjectRootElement(string entryPointFilePath, string content) : IProjectRootElement
{
    public string EntryPointFilePath { get; } = entryPointFilePath;
    public string? FullPath { get; set; }
    public string GetRawXml() => content;
}
