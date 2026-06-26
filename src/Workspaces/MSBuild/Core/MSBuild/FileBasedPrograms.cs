// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.CodeAnalysis.FileBasedPrograms;
using Microsoft.DotNet.FileBasedPrograms;
using IProjectInstance = Microsoft.DotNet.FileBasedPrograms.IProjectInstance;
using IProjectItemInstance = Microsoft.DotNet.FileBasedPrograms.IProjectItemInstance;

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
        var buildService = new FileBasedProgramsBuildService(buildHost);
        var projectRootElement = fileBasedProgramService.LoadFileBasedAppProject(
            buildService,
            FileBasedProgramsBuildService.ProjectCollection,
            entryPointFilePath,
            reportError);
        return await buildHost.LoadProjectAsync(
            projectRootElement.FullPath!,
            projectRootElement.GetRawXml(),
            LanguageNames.CSharp,
            fileBasedApp: true,
            cancellationToken).ConfigureAwait(false);
    }
}

file sealed class FileBasedProgramsBuildService(RemoteBuildHost buildHost) : Microsoft.DotNet.FileBasedPrograms.IBuildService
{
    public static IProjectCollection ProjectCollection => Microsoft.CodeAnalysis.MSBuild.ProjectCollection.Instance;

    public IProjectInstance CreateProjectInstanceFromProjectRootElement(
        IProjectRootElement projectRoot,
        IProjectCollection projectCollection,
        IDictionary<string, string> globalProperties)
    {
        return ProjectInstance.FromProjectRootElement(buildHost, (ProjectRootElement)projectRoot, (ProjectCollection)projectCollection, globalProperties);
    }

    public IProjectRootElement CreateProjectRootElement(XmlReader xmlReader, IProjectCollection projectCollection)
    {
        xmlReader.MoveToContent();
        return new ProjectRootElement(xmlReader.ReadOuterXml());
    }
}

file sealed class ProjectCollection : IProjectCollection
{
    public static ProjectCollection Instance { get; } = new();

    private ProjectCollection() { }

    public IDictionary<string, string> GlobalProperties => ImmutableDictionary<string, string>.Empty;
}

file sealed class ProjectInstance(RemoteProjectInstance remoteProjectInstance) : IProjectInstance
{
    public static ProjectInstance FromProjectRootElement(
        RemoteBuildHost buildHost,
        ProjectRootElement projectRoot,
        ProjectCollection projectCollection,
        IDictionary<string, string> globalProperties)
    {
        Debug.Assert(projectCollection == ProjectCollection.Instance);
        var remoteProjectInstance = buildHost.LoadProjectInstanceAsync(projectRoot.FullPath!, projectRoot.GetRawXml(), globalProperties, CancellationToken.None).Result;
        return new ProjectInstance(remoteProjectInstance);
    }

    public IEnumerable<IProjectItemInstance> GetItems(string itemType) => remoteProjectInstance.GetItemsAsync(itemType, CancellationToken.None).Result.Select(i => new ProjectItemInstance(itemType, i));
    public string GetPropertyValue(string propertyName) => remoteProjectInstance.GetPropertyValueAsync(propertyName, CancellationToken.None).Result;
    public string ExpandString(string value) => remoteProjectInstance.ExpandStringAsync(value, CancellationToken.None).Result;
}

file sealed class ProjectItemInstance(string itemType, RemoteProjectItemInstance remoteProjectItemInstance) : IProjectItemInstance
{
    public string ItemType => itemType;
    public string GetMetadataValue(string name) => remoteProjectItemInstance.GetMetadataValueAsync(name, CancellationToken.None).Result;
}

file sealed class ProjectRootElement(string content) : IProjectRootElement
{
    public string? FullPath { get; set; }
    public string GetRawXml() => content;
}
