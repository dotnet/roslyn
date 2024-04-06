// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Build.Framework;

namespace Microsoft.CodeAnalysis.BuildTasks.UnitTests.TestUtilities;

internal sealed class SimpleTaskItem : ITaskItem
{
    public string ItemSpec { get; set; }

    public Dictionary<string, string> Metadata { get; }

    public ICollection MetadataNames => Metadata.Keys;

    public int MetadataCount => Metadata.Count;

    internal SimpleTaskItem(string itemSpec, Dictionary<string, string> metadata)
    {
        ItemSpec = itemSpec;
        Metadata = metadata;
    }

    public IDictionary CloneCustomMetadata() => throw new NotImplementedException();

    public void CopyMetadataTo(ITaskItem destinationItem) => throw new NotImplementedException();

    public string? GetMetadata(string metadataName) =>
        Metadata.TryGetValue(metadataName, out var metadataValue) ? metadataValue : null;

    public void RemoveMetadata(string metadataName) =>
       _ = Metadata.Remove(metadataName);

    public void SetMetadata(string metadataName, string metadataValue)
    {
        Metadata[metadataName] = metadataValue;
    }

    public static SimpleTaskItem CreateReference(string itemSpec, string? alias = null, bool? embedInteropTypes = null)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (alias is not null)
        {
            map["Aliases"] = alias;
        }

        if (embedInteropTypes is { } e)
        {
            map["EmbedInteropTypes"] = e.ToString();
        }

        return new SimpleTaskItem(itemSpec, map);
    }
}
