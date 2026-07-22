// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections.Generic;
using System.Xml;

namespace Microsoft.DotNet.FileBasedPrograms;

/// <summary>
/// The root interface for our abstraction over MSBuild APIs.
/// This is used by <c>VirtualProjectBuilder</c> so that it can be used
/// both by dotnet CLI (which uses MSBuild APIs directly)
/// and by roslyn IDE (which uses MSBuild through RPC).
/// </summary>
#if FILE_BASED_PROGRAMS_PUBLIC
public
#else
internal
#endif
interface IBuildService
{
    /// <summary>
    /// An abstraction over MSBuild's <c>ProjectInstance.FromProjectRootElement</c> method.
    /// </summary>
    IProjectInstance CreateProjectInstanceFromProjectRootElement(
        IProjectRootElement projectRoot,
        IProjectCollection projectCollection,
        IDictionary<string, string> globalProperties);

    /// <summary>
    /// An abstraction over MSBuild's <c>ProjectRootElement.Create</c> method.
    /// </summary>
    IProjectRootElement CreateProjectRootElement(XmlReader xmlReader, IProjectCollection projectCollection, string entryPointFilePath);
}

/// <summary>
/// An abstraction of MSBuild's <c>ProjectCollection</c>.
/// </summary>
/// <seealso cref="IBuildService"/>
#if FILE_BASED_PROGRAMS_PUBLIC
public
#else
internal
#endif
interface IProjectCollection
{
    IDictionary<string, string> GlobalProperties { get; }
}

/// <summary>
/// An abstraction of MSBuild's <c>ProjectInstance</c>.
/// </summary>
/// <seealso cref="IBuildService"/>
#if FILE_BASED_PROGRAMS_PUBLIC
public
#else
internal
#endif
interface IProjectInstance
{
    IEnumerable<IProjectItemInstance> GetItems(string itemType);
    string GetPropertyValue(string propertyName);
    string ExpandString(string value);
}

/// <summary>
/// An abstraction of MSBuild's <c>ProjectItemInstance</c>.
/// </summary>
/// <seealso cref="IBuildService"/>
#if FILE_BASED_PROGRAMS_PUBLIC
public
#else
internal
#endif
interface IProjectItemInstance
{
    string GetMetadataValue(string name);
    string ItemType { get; }
}

/// <summary>
/// An abstraction of MSBuild's <c>ProjectRootElement</c>.
/// </summary>
/// <seealso cref="IBuildService"/>
#if FILE_BASED_PROGRAMS_PUBLIC
public
#else
internal
#endif
interface IProjectRootElement
{
    string? FullPath { get; set; }
    string GetRawXml();
}
