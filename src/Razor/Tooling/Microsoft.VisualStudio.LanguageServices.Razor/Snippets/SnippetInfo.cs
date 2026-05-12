// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.VisualStudio.Razor.Snippets;

internal record SnippetInfo
{
    private readonly Lazy<XmlSnippetParser.ParsedXmlSnippet?> _parsedXmlSnippet;

    public SnippetInfo(
        string shortcut,
        string title,
        string description,
        string path,
        SnippetLanguage language)
    {
        Shortcut = shortcut;
        Title = title;
        Description = description;
        Path = path;
        Language = language;

        _parsedXmlSnippet = new(() => XmlSnippetParser.GetParsedXmlSnippet(this));
        CompletionData = new(Path);
    }

    public string Shortcut { get; }
    public string Title { get; }
    public string Description { get; }
    public string Path { get; }
    public SnippetLanguage Language { get; }
    public SnippetCompletionData CompletionData { get; }

    internal XmlSnippetParser.ParsedXmlSnippet? GetParsedXmlSnippet()
        => _parsedXmlSnippet.Value;
}

internal record SnippetCompletionData([property: JsonPropertyName(SnippetCompletionData.PropertyName)] string Path)
{
    private const string PropertyName = "__razor_snippet_path";

    internal static bool TryParse(object? data, [NotNullWhen(true)] out SnippetCompletionData? snippetCompletionData)
    {
        snippetCompletionData = data as SnippetCompletionData;

        if (data is JsonElement jElement &&
            jElement.TryGetProperty(PropertyName, out _))
        {
            try
            {
                snippetCompletionData = jElement.Deserialize<SnippetCompletionData>();
            }
            catch { }
        }

        return snippetCompletionData is not null;
    }

    internal bool Matches(SnippetInfo s)
    {
        return s.Path == Path;
    }
}

internal enum SnippetLanguage
{
    CSharp,
    Html,
    Razor
}
