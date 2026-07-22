// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.CodeAnalysis.FileBasedPrograms;
using Microsoft.DotNet.FileBasedPrograms;

namespace Microsoft.CodeAnalysis.MSBuild;

internal static class FileBasedProgramsProjectLoader
{
    public static async Task<RemoteProjectFile> LoadFileBasedAppProjectAsync(
        RemoteBuildHost buildHost,
        IFileBasedProgramService fileBasedProgramService,
        string entryPointFilePath,
        Action<string> reportError,
        CancellationToken cancellationToken)
    {
        var buildService = new FileBasedProgramsBuildService(buildHost, cancellationToken);
        await using var _ = buildService.ConfigureAwait(false);
        var projectRootElement = fileBasedProgramService.LoadFileBasedAppProject(
            buildService,
            FileBasedProgramsBuildService.ProjectCollection,
            entryPointFilePath,
            reportError);
        return await buildHost.LoadProjectAsync(
            projectRootElement.FullPath!,
            physicalFilePath: entryPointFilePath,
            projectRootElement.GetRawXml(),
            LanguageNames.CSharp,
            globalProperties: fileBasedProgramService.GetGlobalBuildProperties(),
            cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// An implementation of <see cref="IBuildService"/> which uses MSBuild over RPC (via <see cref="RemoteBuildHost"/>).
/// </summary>
file sealed class FileBasedProgramsBuildService(RemoteBuildHost buildHost, CancellationToken cancellationToken) : IBuildService, IAsyncDisposable
{
    public static IProjectCollection ProjectCollection => Microsoft.CodeAnalysis.MSBuild.ProjectCollection.Instance;

    public ConcurrentBag<IAsyncDisposable> Disposables { get; } = [];

    public Microsoft.DotNet.FileBasedPrograms.IProjectInstance CreateProjectInstanceFromProjectRootElement(
        IProjectRootElement projectRoot,
        IProjectCollection projectCollection,
        IDictionary<string, string> globalProperties)
    {
        return ProjectInstance.FromProjectRootElement(this, buildHost, (ProjectRootElement)projectRoot, (ProjectCollection)projectCollection, globalProperties, cancellationToken);
    }

    public IProjectRootElement CreateProjectRootElement(XmlReader xmlReader, IProjectCollection projectCollection, string entryPointFilePath)
    {
        xmlReader.MoveToContent();
        return new ProjectRootElement(xmlReader.ReadOuterXml());
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

    public IDictionary<string, string> GlobalProperties => ImmutableDictionary<string, string>.Empty;
}

/// <summary>
/// Our adapter for MSBuild's <c>ProjectInstance</c> in <see cref="FileBasedProgramsBuildService"/> abstraction.
/// </summary>
file sealed class ProjectInstance(RemoteProjectInstance remoteProjectInstance, CancellationToken cancellationToken) : Microsoft.DotNet.FileBasedPrograms.IProjectInstance
{
    public static ProjectInstance FromProjectRootElement(
        FileBasedProgramsBuildService service,
        RemoteBuildHost buildHost,
        ProjectRootElement projectRoot,
        ProjectCollection projectCollection,
        IDictionary<string, string> globalProperties,
        CancellationToken cancellationToken)
    {
        Debug.Assert(projectCollection == ProjectCollection.Instance);
        var remoteProjectInstance = buildHost.LoadProjectInstanceAsync(projectRoot.FullPath!, projectRoot.GetRawXml(), globalProperties, cancellationToken).GetAwaiter().GetResult();
        service.Disposables.Add(remoteProjectInstance);
        return new ProjectInstance(remoteProjectInstance, cancellationToken);
    }

    public IEnumerable<Microsoft.DotNet.FileBasedPrograms.IProjectItemInstance> GetItems(string itemType) => remoteProjectInstance.GetItemsAsync(itemType, cancellationToken).GetAwaiter().GetResult().Select(i => new ProjectItemInstance(itemType, i, cancellationToken));
    public string GetPropertyValue(string propertyName) => remoteProjectInstance.GetPropertyValueAsync(propertyName, cancellationToken).GetAwaiter().GetResult();
    public string ExpandString(string value) => remoteProjectInstance.ExpandStringAsync(value, cancellationToken).GetAwaiter().GetResult();
}

/// <summary>
/// Our adapter for MSBuild's <c>ProjectItemInstance</c> in <see cref="FileBasedProgramsBuildService"/> abstraction.
/// </summary>
file sealed class ProjectItemInstance(string itemType, RemoteProjectItemInstance remoteProjectItemInstance, CancellationToken cancellationToken) : Microsoft.DotNet.FileBasedPrograms.IProjectItemInstance
{
    public string ItemType => itemType;
    public string GetMetadataValue(string name) => remoteProjectItemInstance.GetMetadataValueAsync(name, cancellationToken).GetAwaiter().GetResult();
}

/// <summary>
/// Our adapter for MSBuild's <c>ProjectRootElement</c> in <see cref="FileBasedProgramsBuildService"/> abstraction.
/// </summary>
file sealed class ProjectRootElement(string content) : IProjectRootElement
{
    public string? FullPath { get; set; }
    public string GetRawXml() => content;
}
