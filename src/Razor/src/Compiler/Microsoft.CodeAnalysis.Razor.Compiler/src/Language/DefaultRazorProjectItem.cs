// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.IO;

namespace Microsoft.AspNetCore.Razor.Language;

internal class DefaultRazorProjectItem : RazorProjectItem
{
    private FileInfo _fileInfo;
    private readonly string _physicalFilePath;
    private readonly RazorFileKind? _fileKind;

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultRazorProjectItem"/>.
    /// </summary>
    /// <param name="basePath">The base path.</param>
    /// <param name="filePath">The path.</param>
    /// <param name="relativePhysicalPath">The physical path of the base path.</param>
    /// <param name="fileKind">The file kind. If null, the document kind will be inferred from the file extension.</param>
    /// <param name="file">The <see cref="FileInfo"/>.</param>
    /// <param name="cssScope">A scope identifier that will be used on elements in the generated class, or null.</param>
    public DefaultRazorProjectItem(string basePath, string filePath, string relativePhysicalPath, RazorFileKind? fileKind, FileInfo file, string cssScope)
    {
        BasePath = basePath;
        FilePath = filePath;
        _physicalFilePath = null;
        RelativePhysicalPath = relativePhysicalPath;
        _fileKind = fileKind;
        _fileInfo = file;
        CssScope = cssScope;
    }

    /// <summary>
    /// Initializes a new instance of <see cref="DefaultRazorProjectItem"/>.
    /// </summary>
    /// <param name="basePath">The base path.</param>
    /// <param name="filePath">The path.</param>
    /// <param name="physicalPath">The physical path of the file path.</param>
    /// <param name="relativePhysicalPath">The physical path of the base path.</param>
    /// <param name="fileKind">The file kind. If null, the document kind will be inferred from the file extension.</param>
    /// <param name="cssScope">A scope identifier that will be used on elements in the generated class, or null.</param>
    public DefaultRazorProjectItem(string basePath, string filePath, string physicalPath, string relativePhysicalPath, RazorFileKind? fileKind, string cssScope)
    {
        BasePath = basePath;
        FilePath = filePath;
        _physicalFilePath = physicalPath;
        RelativePhysicalPath = relativePhysicalPath;
        _fileKind = fileKind;
        CssScope = cssScope;
    }

    public FileInfo File => _fileInfo ??= new(FilePath);

    public override string BasePath { get; }

    public override string FilePath { get; }

    public override bool Exists => _fileInfo?.Exists ?? System.IO.File.Exists(PhysicalPath);

    public override string PhysicalPath => _fileInfo?.FullName ?? _physicalFilePath;

    public override string RelativePhysicalPath { get; }

    public override RazorFileKind FileKind => _fileKind ?? base.FileKind;

    public override string CssScope { get; }

    public override Stream Read() => new FileStream(PhysicalPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
}
