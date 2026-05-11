// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Razor;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor;

namespace Microsoft.CodeAnalysis.Remote.Razor.ProjectSystem;

internal static class Extensions
{
    public static bool IsRazorDocument(this TextDocument document)
        => document is AdditionalDocument &&
           document.FilePath is string filePath &&
           filePath.IsRazorFilePath();

    public static bool ContainsRazorDocuments(this Project project)
        => project.AdditionalDocuments.Any(static d => d.IsRazorDocument());

    public static Uri GetRazorDocumentUri(this Solution solution, RazorCodeDocument codeDocument)
    {
        var filePath = codeDocument.Source.FilePath;
        var documentId = solution.GetDocumentIdsWithFilePath(filePath).First();
        var document = solution.GetAdditionalDocument(documentId).AssumeNotNull();
        return document.CreateUri();
    }
}
