// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using Microsoft.AspNetCore.Razor;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.Utilities;

namespace Microsoft.CodeAnalysis.Razor;

internal static class UriExtensions
{
    /// <summary>
    ///  Converts the specified <see cref="Uri"/> into a file path that matches
    ///  a Roslyn <see cref="TextDocument.FilePath"/>.
    /// </summary>
    public static string GetDocumentFilePath(this Uri uri)
        => RazorUri.GetDocumentFilePathFromUri(uri);

    public static Uri GetRequiredParsedUri(this DocumentUri uri)
        => uri.ParsedUri.AssumeNotNull();

    public static string GetAbsoluteOrUNCPath(this DocumentUri uri)
        => GetAbsoluteOrUNCPath(uri.GetRequiredParsedUri());

    public static string GetAbsoluteOrUNCPath(this Uri uri)
    {
        ArgHelper.ThrowIfNull(uri, nameof(uri));

        if (uri.IsUnc)
        {
            // For UNC paths, AbsolutePath doesn't include the host name `//COMPUTERNAME/` part. So we need to use LocalPath instead.
            return uri.LocalPath;
        }

        // Absolute paths are usually encoded.
        var absolutePath = uri.AbsolutePath.Contains("%") ? WebUtility.UrlDecode(uri.AbsolutePath) : uri.AbsolutePath;

        return EnsureUniquePathForScheme(uri.Scheme, absolutePath);
    }

    private static string EnsureUniquePathForScheme(string scheme, string decodedAbsolutePath)
    {
        // File Uris we leave untouched, as they represent the actual files on disk
        if (scheme == Uri.UriSchemeFile)
        {
            return decodedAbsolutePath;
        }

        var normalizedPath = FilePathNormalizer.Normalize(decodedAbsolutePath);
        var firstSeparatorIndex = normalizedPath.IndexOf('/');
        if (firstSeparatorIndex < 0)
        {
            // A path without a separator is unlikely, but we can't add our unique-ness marker anyway, so just return as is
            // and hope for the best.
            return decodedAbsolutePath;
        }

        // For any non-file Uri, we add a marker to the path to ensure things won't conflict with the real file on disk.
        // This is a somewhat hacky fix to ensure things like a git diff view will work, where the left hand side is a
        // Uri like "git://path/to/file.razor" and the right hand side is "file://path/to/file.razor". If we mapped both
        // of those to the same file path, then one side would be sending line and character positions that don't match
        // our understanding of the document.
        // A true fix would be to move away from using file paths in the first place, but instead use the Uris as provided
        // by the LSP client as the source of truth.
        // See https://github.com/dotnet/razor/issues/9365 and https://github.com/microsoft/vscode-dotnettools/issues/2151
        // for examples.

        return normalizedPath.Insert(firstSeparatorIndex + 1, $"_{scheme}_/");
    }
}
