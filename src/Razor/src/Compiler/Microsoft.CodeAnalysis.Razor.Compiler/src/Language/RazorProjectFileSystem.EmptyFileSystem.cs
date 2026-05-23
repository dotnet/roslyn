// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.AspNetCore.Razor.Language;

public abstract partial class RazorProjectFileSystem
{
    private sealed class EmptyFileSystem : RazorProjectFileSystem
    {
        public override IEnumerable<RazorProjectItem> EnumerateItems(string basePath)
        {
            NormalizeAndEnsureValidPath(basePath);
            return [];
        }

        public override RazorProjectItem GetItem(string path, RazorFileKind? fileKind)
        {
            NormalizeAndEnsureValidPath(path);
            return new NotFoundProjectItem(path, fileKind);
        }
    }
}
