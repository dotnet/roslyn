// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections.Generic;
using System.Xml;

namespace Microsoft.DotNet.FileBasedPrograms;

#if FILE_BASED_PROGRAMS_PUBLIC
public
#else
internal
#endif
interface IBuildService
{
    IProjectInstance CreateProjectInstanceFromProjectRootElement(
        IProjectRootElement projectRoot,
        IProjectCollection projectCollection,
        IDictionary<string, string> globalProperties);

    IProjectRootElement CreateProjectRootElement(XmlReader xmlReader, IProjectCollection projectCollection);
}

#if FILE_BASED_PROGRAMS_PUBLIC
public
#else
internal
#endif
interface IProjectCollection
{
    IDictionary<string, string> GlobalProperties { get; }
}

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
