// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed class RazorCSharpDocument
{
    public RazorCodeDocument CodeDocument { get; }
    public SourceText Text { get; }
    public ImmutableArray<RazorDiagnostic> Diagnostics { get; }
    public ImmutableArray<SourceMapping> SourceMappingsSortedByGenerated { get; }
    public ImmutableArray<SourceMapping> SourceMappingsSortedByOriginal { get; }
    public ImmutableArray<LinePragma> LinePragmas { get; }
    public bool IsDeclarationDocument { get; }

    /// <summary>
    /// True when this declaration document is a bodiless "type shell" emitted for a fallback component
    /// (see <c>DefaultRazorMarkupSplitPhase.BuildStubDeclDocument</c>) rather than the component's full
    /// declaration surface. The shell exists only so the component's type resolves in the
    /// pre-compilation compilation; it carries no members, usings, or source mappings. Tooling that reads
    /// the declaration document for its member/using surface must treat a shell as if the declaration
    /// document were absent and use the implementation document instead.
    /// </summary>
    public bool IsStubDocument { get; }

    /// <summary>
    /// The generated C# code as a string. Prefer <see cref="Text"/>, which avoids materializing the
    /// whole document as a single string.
    /// </summary>
    public string GeneratedCode => Text.ToString();

    public RazorCSharpDocument(
        RazorCodeDocument codeDocument,
        SourceText text,
        ImmutableArray<RazorDiagnostic> diagnostics,
        ImmutableArray<SourceMapping> sourceMappings = default,
        ImmutableArray<LinePragma> linePragmas = default,
        bool isDeclarationDocument = false,
        bool isStubDocument = false)
    {
        ArgHelper.ThrowIfNull(codeDocument);
        ArgHelper.ThrowIfNull(text);

        CodeDocument = codeDocument;
        Text = text;

        Diagnostics = diagnostics.NullToEmpty();
        SourceMappingsSortedByGenerated = sourceMappings.NullToEmpty();

        // Verify given source mappings are ordered by their generated spans
        for (var i = 0; i < SourceMappingsSortedByGenerated.Length - 1; i++)
        {
            if (SourceMappingsSortedByGenerated[i].GeneratedSpan.CompareByStartThenLength(SourceMappingsSortedByGenerated[i + 1].GeneratedSpan) > 0)
            {
                Debug.Fail("input not sorted");

                SourceMappingsSortedByGenerated = SourceMappingsSortedByGenerated.Sort(static (m1, m2) => m1.GeneratedSpan.CompareByStartThenLength(m2.GeneratedSpan));
                break;
            }
        }

        SourceMappingsSortedByOriginal = SourceMappingsSortedByGenerated.Sort(static (m1, m2) => m1.OriginalSpan.CompareByStartThenLength(m2.OriginalSpan));
        LinePragmas = linePragmas.NullToEmpty();
        IsDeclarationDocument = isDeclarationDocument;
        IsStubDocument = isStubDocument;
    }
}
