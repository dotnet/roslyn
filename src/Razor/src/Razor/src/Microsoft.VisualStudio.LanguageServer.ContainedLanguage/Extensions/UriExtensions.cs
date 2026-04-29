// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;

namespace Microsoft.VisualStudio.LanguageServer.ContainedLanguage.Extensions;

internal static class UriExtensions
{
    public static string GetAbsoluteOrUNCPath(this Uri uri)
    {
        if (uri is null)
        {
            throw new ArgumentNullException(nameof(uri));
        }

        if (uri.IsUnc)
        {
            // For UNC paths, AbsolutePath doesn't include the host name `//COMPUTERNAME/` part. So we need to use LocalPath instead.
            return uri.LocalPath;
        }

        // Absolute paths are usually encoded.
        return uri.AbsolutePath.Contains("%") ? WebUtility.UrlDecode(uri.AbsolutePath) : uri.AbsolutePath;
    }
}
