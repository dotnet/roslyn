// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using MSB = Microsoft.Build;

namespace Microsoft.CodeAnalysis.MSBuild;

internal sealed class ProjectInstance(
    RpcServer server,
    MSB.Execution.ProjectInstance? projectInstance,
    DiagnosticLog log) :
#if NETFRAMEWORK
    MarshalByRefObject, // We need this object to pass across the AppDomain boundary when on .NET Framework
#endif
    IProjectInstance
{
    public DiagnosticLogItem[] GetDiagnosticLogItems()
        => [.. log];

    public string[][] GetItemMetadataValues(string itemType, string[] metadataNames)
    {
        if (projectInstance is null)
        {
            return [];
        }

        var items = projectInstance.GetItems(itemType);
        return items.Select(item => metadataNames.Select(metadataName => item.GetMetadataValue(metadataName)).ToArray()).ToArray();
    }

    public string GetPropertyValue(string propertyName)
    {
        if (projectInstance is null)
        {
            return string.Empty;
        }

        return projectInstance.GetPropertyValue(propertyName);
    }

    public string ExpandString(string value)
    {
        if (projectInstance is null)
        {
            return value;
        }

        return projectInstance.ExpandString(value);
    }

    public void Dispose()
    {
        server.RemoveTarget(this);
    }
}
