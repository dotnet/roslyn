// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.AspNetCore.Razor.Language.Components;

namespace Microsoft.AspNetCore.Razor.Language;

public static class FileKinds
{
    public const string ComponentFileExtension = ".razor";
    public const string LegacyFileExtension = ".cshtml";

    /// <summary>
    ///  Returns <see langword="true"/> if the specified value represents a component or component import.
    /// </summary>
    public static bool IsComponent(this RazorFileKind fileKind)
        => fileKind is RazorFileKind.Component or RazorFileKind.ComponentImport;

    /// <summary>
    ///  Returns <see langword="true"/> if the specified value represents a component import.
    /// </summary>
    public static bool IsComponentImport(this RazorFileKind fileKind)
        => fileKind is RazorFileKind.ComponentImport;

    /// <summary>
    ///  Returns <see langword="true"/> if the specified value represents a legacy file kind.
    /// </summary>
    public static bool IsLegacy(this RazorFileKind fileKind)
        => fileKind is RazorFileKind.Legacy;

    /// <summary>
    ///  Compares the given file path to known Razor file extensions and names to determine the <see cref="RazorFileKind"/>.
    ///  Returns <see langword="true"/> if a file kind can be determined; otherwise, <see langword="false"/>.
    /// </summary>
    public static bool TryGetFileKindFromPath(string filePath, out RazorFileKind fileKind)
    {
        ArgHelper.ThrowIfNull(filePath);

        var fileName = Path.GetFileName(filePath);

        if (string.Equals(ComponentHelpers.ImportsFileName, fileName, StringComparison.Ordinal))
        {
            fileKind = RazorFileKind.ComponentImport;
            return true;
        }

        var extension = Path.GetExtension(filePath);

        if (string.Equals(ComponentFileExtension, extension, StringComparison.OrdinalIgnoreCase))
        {
            fileKind = RazorFileKind.Component;
            return true;
        }

        if (string.Equals(LegacyFileExtension, extension, StringComparison.OrdinalIgnoreCase))
        {
            fileKind = RazorFileKind.Legacy;
            return true;
        }

        fileKind = default;
        return false;
    }

    /// <summary>
    ///  Compares the given file path to known Razor file extensions and names to determine the <see cref="RazorFileKind"/>.
    ///  If a file kind can't be determined, the result is <see cref="RazorFileKind.Legacy"/>.
    /// </summary>
    public static RazorFileKind GetFileKindFromPath(string filePath)
        => TryGetFileKindFromPath(filePath, out var fileKind)
            ? fileKind
            : RazorFileKind.Legacy;
}
