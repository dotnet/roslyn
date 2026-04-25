// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace Microsoft.AspNetCore.Razor.Language;

/// <summary>
/// A <see cref="RazorProjectItem"/> that does not exist.
/// </summary>
/// <param name="basePath">The base path.</param>
/// <param name="path">The path.</param>
/// <param name="fileKind">The file kind</param>
internal class NotFoundProjectItem(string path, RazorFileKind? fileKind) : RazorProjectItem
{
    /// <inheritdoc />
    public override string BasePath => string.Empty;

    /// <inheritdoc />
    public override string FilePath => path;

    /// <inheritdoc />
    public override RazorFileKind FileKind { get; }
        = fileKind ?? FileKinds.GetFileKindFromPath(path);

    /// <inheritdoc />
    public override bool Exists => false;

    /// <inheritdoc />
    public override string PhysicalPath => throw new NotSupportedException();

    /// <inheritdoc />
    public override Stream Read() => throw new NotSupportedException();
}
