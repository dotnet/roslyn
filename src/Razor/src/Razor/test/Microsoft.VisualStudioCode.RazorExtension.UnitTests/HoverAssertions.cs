// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Roslyn.Test.Utilities;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

internal static class HoverAssertions
{
    public static void VerifyContents(this LspHover hover, object expected)
    {
        var markup = hover.Contents.Fourth;

        AssertEx.EqualOrDiff(expected.ToString(), markup.Value.TrimEnd('\r', '\n'));
    }

    // Our VS Code test only produce plain text hover content, so these methods are complete overkill,
    // but they match the HoverAssertions in the Microsoft.VisualStudio.LanguageServices.Razor.Test project
    // for consistency.

    public static string Container(params ImmutableArray<object?> elements)
        => string.Concat(elements.OfType<string>());

    public static object? Image
        => null;

    public static string ClassifiedText(params ImmutableArray<string> runs)
        => string.Concat(runs);

    public static string ClassName(string text)
        => text;

    public static string Keyword(string text)
        => text;

    public static string Namespace(string text)
        => text;

    public static string LocalName(string text)
        => text;

    public static string PropertyName(string text)
        => text;

    public static string Punctuation(string text)
        => text;

    public static string Text(string text)
        => text;

    public static string Type(string text)
        => text;

    public static string WhiteSpace(string text)
        => text;

    public static string HorizontalRule
        => "\n\n---\n\n";
}
