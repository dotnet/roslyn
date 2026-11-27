// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language.Components;

internal sealed class ComponentImportProjectFeature : RazorProjectEngineFeatureBase, IImportProjectFeature
{
    // Using explicit newlines here to avoid fooling our baseline tests
    private const string DefaultUsingImportContent =
        "\r\n" +
        "@using global::System\r\n" +
        "@using global::System.Collections.Generic\r\n" +
        "@using global::System.Linq\r\n" +
        "@using global::System.Threading.Tasks\r\n" +
        "@using global::" + ComponentsApi.RenderFragment.Namespace + "\r\n"; // Microsoft.AspNetCore.Components

    private static readonly DefaultImportProjectItem s_defaultImport = new($"Default component imports ({ComponentHelpers.ImportsFileName})", DefaultUsingImportContent);

    public void CollectImports(RazorProjectItem projectItem, ref PooledArrayBuilder<RazorProjectItem> imports)
    {
        ArgHelper.ThrowIfNull(projectItem);

        // Don't add Component imports for a non-component.
        if (!projectItem.FileKind.IsComponent())
        {
            return;
        }

        imports.Add(s_defaultImport);

        // We add hierarchical imports second so any default directive imports can be overridden.
        imports.AddRange(GetHierarchicalImports(ProjectEngine.FileSystem, projectItem));
    }

    private static ImmutableArray<RazorProjectItem> GetHierarchicalImports(RazorProjectFileSystem fileSystem, RazorProjectItem projectItem)
    {
        // We want items in descending order. FindHierarchicalItems returns items in ascending order.
        return fileSystem.FindHierarchicalItems(projectItem.FilePath, ComponentHelpers.ImportsFileName);
    }
}
