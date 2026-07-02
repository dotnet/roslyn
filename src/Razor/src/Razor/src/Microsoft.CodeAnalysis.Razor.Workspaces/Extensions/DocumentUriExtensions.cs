// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Razor.Protocol;

namespace Microsoft.CodeAnalysis.Razor.Workspaces.Extensions;

internal static class DocumentUriExtensions
{
    extension(DocumentUri documentUri)
    {
        public bool IsRazorCSharpDocumentUri(Solution solution)
        {
            return solution.TryGetSourceGeneratedDocumentIdentity(documentUri, out _);
        }

        public bool IsRazorHtmlDocumentUri([NotNullWhen(true)] out DocumentUri? razorDocumentUri)
        {
            razorDocumentUri = null;
            if (documentUri.ParsedUri is not { } uri)
            {
                return false;
            }

            // In VS Code specifically we use a "razor-html" scheme to represent our virtual documents, so convert them back to file
            // before further processing. We otherwise treat non-file scheme URIs specially, and assume they're never part of a project.
            if (uri.Scheme == "razor-html")
            {
                var builder = new UriBuilder(uri)
                {
                    Scheme = Uri.UriSchemeFile
                };
                uri = builder.Uri;
            }

            var filePath = uri.GetAbsoluteOrUNCPath();
            if (filePath is null)
            {
                return false;
            }

            if (filePath.EndsWith(LanguageServerConstants.HtmlVirtualDocumentSuffix, StringComparison.Ordinal))
            {
                var razorFilePath = filePath[..^LanguageServerConstants.HtmlVirtualDocumentSuffix.Length];
                razorDocumentUri = LspFactory.CreateFilePathUri(razorFilePath);
                return true;
            }

            return false;
        }
    }
}
