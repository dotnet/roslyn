// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
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
    private readonly ConcurrentBag<ProjectItemInstance> _items = [];

    public DiagnosticLogItem[] GetDiagnosticLogItems()
        => [.. log];

    public int[] GetItems(string itemType)
    {
        if (projectInstance is null)
        {
            return [];
        }

        var items = projectInstance.GetItems(itemType);
        var result = new int[items.Count];
        var i = 0;
        foreach (var item in items)
        {
            var rpcItem = new ProjectItemInstance(item);
            _items.Add(rpcItem);
            result[i++] = server.AddTarget(rpcItem);
        }

        return result;
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
        while (_items.TryTake(out var item))
        {
            server.RemoveTarget(item);
        }

        server.RemoveTarget(this);
    }
}
