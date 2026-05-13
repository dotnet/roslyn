// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.AspNetCore.Razor.Language;

internal sealed class TestRazorProjectFileSystem(params IEnumerable<RazorProjectItem> items) : DefaultRazorProjectFileSystem("/")
{
    public static new readonly RazorProjectFileSystem Empty = new TestRazorProjectFileSystem();

    private readonly Dictionary<string, RazorProjectItem> _lookup = items.ToDictionary(item => item.FilePath);

    public override IEnumerable<RazorProjectItem> EnumerateItems(string basePath)
        => throw new NotImplementedException();

    public override RazorProjectItem GetItem(string path, RazorFileKind? fileKind)
        => _lookup.TryGetValue(path, out var value)
            ? value
            : new NotFoundProjectItem(path, fileKind);
}
