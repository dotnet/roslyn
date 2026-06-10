// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.CodeAnalysis;

internal static class SourceGeneratedDocumentExtensions
{
    public static bool IsRazorSourceGeneratedDocument(this SourceGeneratedDocument sourceGeneratedDocument)
    {
        return sourceGeneratedDocument.Identity.IsRazorSourceGeneratedDocument();
    }
}
