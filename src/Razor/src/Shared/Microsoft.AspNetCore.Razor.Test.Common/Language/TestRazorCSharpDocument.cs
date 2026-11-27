// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Text;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.Language;

internal static class TestRazorCSharpDocument
{
    public static RazorCSharpDocument Create(
        RazorCodeDocument codeDocument,
        string content,
        ImmutableArray<RazorDiagnostic> diagnostics = default,
        ImmutableArray<SourceMapping> sourceMappings = default,
        ImmutableArray<LinePragma> linePragmas = default)
    {
        var text = SourceText.From(content, Encoding.UTF8);
        return new RazorCSharpDocument(codeDocument, text, diagnostics, sourceMappings, linePragmas);
    }

    public static RazorCSharpDocument Create(
        RazorCodeDocument codeDocument,
        string content,
        ImmutableArray<SourceMapping> sourceMappings)
    {
        return Create(codeDocument, content, diagnostics: default, sourceMappings, linePragmas: default);
    }
}
