// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language;

internal enum HierarchicalImports
{
    None,
    Default,
    Legacy,
}

internal sealed class TestImportProjectFeature(
    HierarchicalImports hierarchicalImports,
    params ImmutableArray<RazorProjectItem> imports) : RazorProjectEngineFeatureBase, IImportProjectFeature
{
    private const string DefaultImportsFileName = "_Imports.razor";
    private const string LegacyImportsFileName = "_ViewImports.cshtml";

    private readonly HierarchicalImports _hierarchicalImports = hierarchicalImports;
    private readonly ImmutableArray<RazorProjectItem> _imports = imports;

    public TestImportProjectFeature(params ImmutableArray<RazorProjectItem> imports)
        : this(HierarchicalImports.None, imports)
    {
    }

    public void CollectImports(RazorProjectItem projectItem, ref PooledArrayBuilder<RazorProjectItem> imports)
    {
        ArgHelper.ThrowIfNull(projectItem);

        imports.AddRange(_imports);

        if (_hierarchicalImports != HierarchicalImports.None)
        {
            var importsFileName = _hierarchicalImports switch
            {
                HierarchicalImports.Default => DefaultImportsFileName,
                HierarchicalImports.Legacy => LegacyImportsFileName,
                var value => Assumed.Unreachable<string>($"Unexpected hierarchical import type {value}.")
            };

            // We want items in descending order. FindHierarchicalItems returns items in ascending order.
            var importProjectItems = ProjectEngine.FileSystem.FindHierarchicalItems(projectItem.FilePath, importsFileName).Reverse();
            imports.AddRange(importProjectItems);
        }
    }
}
