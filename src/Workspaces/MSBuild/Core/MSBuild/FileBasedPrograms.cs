// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.DotNet.FileBasedPrograms;
using IProjectInstance = Microsoft.DotNet.FileBasedPrograms.IProjectInstance;

namespace Microsoft.CodeAnalysis.MSBuild;

#pragma warning disable CS9113, IDE0060, CA1822 // TODO

internal sealed class FileBasedProgramsBuildHost(RemoteBuildHost buildHost) : Microsoft.DotNet.FileBasedPrograms.IBuildHost
{
    public IProjectCollection ProjectCollection => Microsoft.CodeAnalysis.MSBuild.ProjectCollection.Instance;

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

internal static class FileBasedProgramsProjectLoader
{
    public static async Task<RemoteProjectFile> LoadFileBasedAppProjectAsync(
        RemoteBuildHost buildHost,
        string entryPointFilePath,
        string languageName,
        Action<string> reportError,
        CancellationToken cancellationToken)
    {
        var buildHostBridge = new FileBasedProgramsBuildHost(buildHost);
        var entryPointFileFullPath = Path.GetFullPath(entryPointFilePath);
        // TODO: don't hardcode TFM
        var virtualProjectBuilder = new VirtualProjectBuilder(buildHostBridge, entryPointFileFullPath, "net10.0");
        virtualProjectBuilder.CreateProjectInstance(
            buildHostBridge.ProjectCollection,
            (text, path, textSpan, message, innerException) => reportError($"{new SourceFile(path, text).GetLocationString(textSpan)}: {message}"),
            project: out _,
            out var projectRootElement,
            evaluatedDirectives: out _);

        return await buildHost.LoadProjectAsync(projectRootElement.FullPath!, projectRootElement.GetRawXml(), languageName, fileBasedApp: true, cancellationToken).ConfigureAwait(false);
    }
}

// TODO: don't allow customizing project collections?
file sealed class ProjectCollection : IProjectCollection
{
    public static ProjectCollection Instance { get; } = new();

    private ProjectCollection() { }

    // TODO: do we even need this if it's just empty?
    public IDictionary<string, string> GlobalProperties => new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

file sealed class ProjectInstance(RemoteProjectInstance remoteProjectInstance) : IProjectInstance
{
    public static ProjectInstance FromProjectRootElement(
        RemoteBuildHost buildHost,
        ProjectRootElement projectRoot,
        ProjectCollection projectCollection,
        IDictionary<string, string> globalProperties)
    {
        // TODO: pass global properties
        var remoteProjectInstance = buildHost.LoadProjectInstanceAsync(projectRoot.FullPath!, projectRoot.GetRawXml(), CancellationToken.None).Result;
        return new ProjectInstance(remoteProjectInstance);
    }

    public IEnumerable<IProjectItemInstance> GetItems(string itemType) => []; // TODO
    public string GetPropertyValue(string propertyName) => remoteProjectInstance.GetPropertyValueAsync(propertyName, CancellationToken.None).Result;
    public string ExpandString(string value) => value; // TODO
}

file sealed class ProjectItemInstance
{
    public string GetMetadataValue(string name) => throw new NotImplementedException(); // TODO
}

file sealed class ProjectRootElement(string content) : IProjectRootElement
{
    public string? FullPath { get; set; }
    public string GetRawXml() => content;
}
