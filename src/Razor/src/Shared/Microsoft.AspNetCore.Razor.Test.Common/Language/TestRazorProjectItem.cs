// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.CodeAnalysis.Razor.Language;

namespace Microsoft.AspNetCore.Razor.Language;

public class TestRazorProjectItem(
    string filePath,
    string? physicalPath = null,
    string? relativePhysicalPath = null,
    string? basePath = "/",
    RazorFileKind? fileKind = null,
    string? cssScope = null,
    Func<Stream>? onRead = null) : RazorProjectItem
{
    private readonly RazorFileKind? _fileKind = fileKind;

    public override string BasePath => basePath!;
    public override RazorFileKind FileKind => _fileKind ?? base.FileKind;
    public override string FilePath => filePath;
    public override string PhysicalPath => physicalPath!;
    public override string RelativePhysicalPath => relativePhysicalPath!;
    public override string CssScope => cssScope!;
    public override bool Exists => true;

    public string Content { get; init; } = "Default content";

    public override Stream Read()
    {
        if (onRead is not null)
        {
            return onRead.Invoke();
        }

        // Act like a file and have a UTF8 BOM.
        var fileContent = new InMemoryFileContent(Content);
        return fileContent.CreateStream();
    }
}
