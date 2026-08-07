// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.AspNetCore.Razor.Language;

/// <summary>
/// Test-only conveniences for reaching the implementation or declaration half of a
/// <see cref="RazorCodeDocument"/> by name. Production code calls
/// <see cref="RazorCodeDocument.GetCSharpDocument(bool)"/> /
/// <see cref="RazorCodeDocument.GetRequiredCSharpDocument(bool)"/> directly so the decl/impl choice
/// is spelled out at the call site; tests keep the shorter names, which live here rather than on the
/// production type.
/// </summary>
public static class RazorCodeDocumentTestExtensions
{
    public static RazorCSharpDocument? GetImplCSharpDocument(this RazorCodeDocument document)
        => document.GetCSharpDocument(declarationDocument: false);

    public static RazorCSharpDocument GetRequiredImplCSharpDocument(this RazorCodeDocument document)
        => document.GetRequiredCSharpDocument(declarationDocument: false);

    public static RazorCSharpDocument? GetDeclCSharpDocument(this RazorCodeDocument document)
        => document.GetCSharpDocument(declarationDocument: true);
}
