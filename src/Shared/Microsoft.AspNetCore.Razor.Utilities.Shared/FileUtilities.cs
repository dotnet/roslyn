// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.IO;

namespace Microsoft.AspNetCore.Razor.Utilities;

internal static class FileUtilities
{
    private const string RazorExtension = ".razor";
    private const string CSHtmlExtension = ".cshtml";

    public static bool IsAnyRazorFilePath(ReadOnlySpan<char> filePath, StringComparison comparison)
    {
        return IsRazorComponentFilePath(filePath, comparison) ||
               IsMvcFilePath(filePath, comparison);
    }

    public static bool IsRazorComponentFilePath(ReadOnlySpan<char> filePath, StringComparison comparison)
    {
        return AdjustToUsableFilePath(filePath).EndsWith(RazorExtension, comparison);
    }

    public static bool IsMvcFilePath(ReadOnlySpan<char> filePath, StringComparison comparison)
    {
        return AdjustToUsableFilePath(filePath).EndsWith(CSHtmlExtension, comparison);
    }

    private static ReadOnlySpan<char> AdjustToUsableFilePath(ReadOnlySpan<char> filePath)
    {
        // In VS Code we sometimes get odd uris with query string components as the file path, for example on the left side of
        // a diff view. In those cases Roslyn will create a document and the file path will be set to the full raw Uri send via
        // VS Code. When trying to find out the file extension for those Uris, we need to strip off the query string.
        //
        // For example we get:
        //   git:/c:/Users/dawengie/source/repos/razor01/Pages/Index.cshtml?%7B%22path%22:%22c:%5C%5CUsers%5C%5Cdawengie%5C%5Csource%5C%5Crepos%5C%5Crazor01%5C%5CPages%5C%5CIndex.cshtml%22,%22ref%22:%22~%22%7D
        //
        // Given colons and question marks are unlikely, or illegal, file path characters the risk of false positives here is hopefully low.
        // Normalized paths from the LSP editor project system could trigger this, because "C:\Goo" would become "C:/Goo", but thats not
        // what we're looking for, and it would be surprising for those paths to have a question mark in them after the drive letter.
        if (filePath.IndexOf(":/") is int colonSlash and > 0 &&
           filePath.IndexOf('?') is int realPathEnd &&
           realPathEnd > colonSlash)
        {
            return filePath[..realPathEnd];
        }

        return filePath;
    }

    /// <summary>
    /// Generate a file path adjacent to the input path that has the
    /// specified file extension, using numbers to differentiate for
    /// any collisions.
    /// </summary>
    /// <param name="path">The input file path.</param>
    /// <param name="extension">The input file extension with a prepended ".".</param>
    /// <returns>A non-existent file path with a name in the specified format and a corresponding extension.</returns>
    public static string GenerateUniquePath(string path, string extension)
    {
        var directoryName = Path.GetDirectoryName(path).AssumeNotNull();
        var baseFileName = Path.GetFileNameWithoutExtension(path);

        var n = 0;
        string uniquePath;
        do
        {
            var identifier = n > 0 ? n.ToString(CultureInfo.InvariantCulture) : string.Empty;  // Make it look nice

            uniquePath = Path.Combine(directoryName, $"{baseFileName}{identifier}{extension}");
            n++;
        }
        while (File.Exists(uniquePath));

        return uniquePath;
    }
}
