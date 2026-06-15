// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Collections.Generic;
using System.Xml;

namespace Microsoft.DotNet.FileBasedPrograms;

internal interface IBuildHost
{
    IProjectInstance CreateProjectInstanceFromProjectRootElement(
        IProjectRootElement projectRoot,
        IProjectCollection projectCollection,
        IDictionary<string, string> globalProperties);

    IProjectRootElement CreateProjectRootElement(XmlReader xmlReader, IProjectCollection projectCollection);
}

internal interface IProjectCollection
{
    IDictionary<string, string> GlobalProperties { get; }
}

internal interface IProjectInstance
{
    IEnumerable<IProjectItemInstance> GetItems(string itemType);
    string GetPropertyValue(string propertyName);
    string ExpandString(string value);
}

internal interface IProjectItemInstance
{
    string GetMetadataValue(string name);
    string ItemType { get; }
}

internal interface IProjectRootElement
{
    string? FullPath { get; set; }
    string GetRawXml();
}
