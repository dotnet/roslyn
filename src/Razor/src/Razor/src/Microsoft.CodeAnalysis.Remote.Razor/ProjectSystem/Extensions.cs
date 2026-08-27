// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;

internal static class Extensions
{
    public static bool IsRazorDocument(this TextDocument document)
        => document is AdditionalDocument &&
           document.FilePath is string filePath &&
           IsRazorDocumentFilePath(filePath);

    private static bool IsRazorDocumentFilePath(string filePath)
    {
        // Most file paths and virtual URIs can be classified from their extension directly.
        if (filePath.IsRazorFilePath())
        {
            return true;
        }

        // Roslyn preserves non-file document URIs as the additional document's file path, so a query
        // or fragment can obscure an otherwise trailing Razor extension.
        if (!filePath.Contains('?') && !filePath.Contains('#'))
        {
            return false;
        }

        // Match Roslyn's language classification by checking the local path of an absolute URI.
        return new DocumentUri(filePath).ParsedUri is { IsAbsoluteUri: true } uri &&
            uri.LocalPath.IsRazorFilePath();
    }

    public static bool ContainsRazorDocuments(this Project project)
        => project.AdditionalDocuments.Any(static d => d.IsRazorDocument());

    public static DocumentUri GetRazorDocumentUri(this Solution solution, RazorCodeDocument codeDocument)
    {
        var filePath = codeDocument.Source.FilePath;
        var documentId = solution.GetDocumentIdsWithFilePath(filePath).First();
        var document = solution.GetAdditionalDocument(documentId).AssumeNotNull();
        return document.GetURI();
    }
}
