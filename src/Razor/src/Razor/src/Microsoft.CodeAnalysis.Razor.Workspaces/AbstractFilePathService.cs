// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.Protocol;

namespace Microsoft.CodeAnalysis.Razor.Workspaces;

internal abstract class AbstractFilePathService : IFilePathService
{
    public virtual Uri GetRazorDocumentUri(Uri virtualDocumentUri)
    {
        var uriPath = virtualDocumentUri.AbsoluteUri;
        var razorFilePath = GetRazorFilePath(uriPath);
        var uri = new Uri(razorFilePath, UriKind.Absolute);
        return uri;
    }

    public bool IsVirtualCSharpFile(Uri uri)
        => RazorUri.IsGeneratedDocumentUri(uri);

    public bool IsVirtualHtmlFile(Uri uri)
        => CheckIfFileUriAndExtensionMatch(uri, LanguageServerConstants.HtmlVirtualDocumentSuffix);

    public bool IsVirtualDocumentUri(Uri uri)
        => IsVirtualCSharpFile(uri) || IsVirtualHtmlFile(uri);

    private static bool CheckIfFileUriAndExtensionMatch(Uri uri, string extension)
        => uri.GetAbsoluteOrUNCPath()?.EndsWith(extension, StringComparison.Ordinal) ?? false;

    private static string GetRazorFilePath(string filePath)
    {
        var trimIndex = filePath.LastIndexOf(LanguageServerConstants.HtmlVirtualDocumentSuffix);

        if (trimIndex != -1)
        {
            return filePath[..trimIndex];
        }

        return filePath;
    }

    internal static class TestAccessor
    {
        internal static string GetRazorFilePath(string filePath) => AbstractFilePathService.GetRazorFilePath(filePath);
    }
}
