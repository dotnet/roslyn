// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.ProcessHost.UnitTests;

internal readonly record struct LspWorkspaceContent(
    ImmutableDictionary<string, LspWorkspaceFile> Files,
    string? LoadPath,
    bool ShouldRestore)
{
    public static LspWorkspaceContent Empty { get; } = new(
        ImmutableDictionary<string, LspWorkspaceFile>.Empty,
        LoadPath: null,
        ShouldRestore: false);

    public LspWorkspaceContent WithFile(string path, string content)
    {
        var normalizedPath = NormalizePath(path);
        return this with
        {
            Files = Files.SetItem(normalizedPath, LspWorkspaceFile.CreateOrdinary(content))
        };
    }

    public LspWorkspaceContent WithMarkupFile(
        string path,
        [StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string markup)
    {
        var normalizedPath = NormalizePath(path);
        return this with
        {
            Files = Files.SetItem(normalizedPath, LspWorkspaceFile.CreateMarkup(markup))
        };
    }

    public LspWorkspaceContent WithCSharp([StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string markup)
    {
        var csharpFilePath = Files.Keys.Single(path => PathUtilities.GetExtension(path) == ".cs");
        return WithMarkupFile(csharpFilePath, markup);
    }

    public LspWorkspaceContent WithLoadPath(string path)
    {
        return this with { LoadPath = NormalizePath(path) };
    }

    public LspWorkspaceContent WithRestore(bool shouldRestore = true)
    {
        return this with { ShouldRestore = shouldRestore };
    }

    public string GetFileText(string path)
        => Files[NormalizePath(path)].Content;

    internal static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path must not be empty.", nameof(path));

        if (PathUtilities.IsAbsolute(path))
            throw new ArgumentException("Path must be relative to the workspace root.", nameof(path));

        var normalizedPath = PathUtilities.CollapseWithForwardSlash(path);
        return normalizedPath;
    }
}

internal readonly record struct LspWorkspaceFile(
    string Content,
    ImmutableDictionary<string, ImmutableArray<TextSpan>> MarkupSpans)
{
    public static LspWorkspaceFile CreateOrdinary(string content)
        => new(content, ImmutableDictionary<string, ImmutableArray<TextSpan>>.Empty);

    public static LspWorkspaceFile CreateMarkup([StringSyntax(PredefinedEmbeddedLanguageNames.CSharpTest)] string markup)
    {
        string code;
        int? cursorPosition;
        ImmutableDictionary<string, ImmutableArray<TextSpan>> spans;
        TestFileMarkupParser.GetPositionAndSpans(markup, out code, out cursorPosition, out spans);
        return new(code, spans);
    }
}
