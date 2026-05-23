// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.VisualStudio.Razor.Snippets;
using Xunit;

namespace Microsoft.VisualStudio.LanguageServices.Razor.Test.Snippets;

public class SnippetCacheTest
{
    [Fact]
    public void GetSnippets_BeforePopulation_ReturnsEmpty()
    {
        // SnippetCache is populated asynchronously after VS starts.
        // Completion can be triggered before population completes,
        // so GetSnippets must return an empty array — not throw.
        var cache = new SnippetCache();

        var result = cache.GetSnippets(SnippetLanguage.Html);

        Assert.True(result.IsEmpty);
    }

    [Fact]
    public void GetSnippets_AfterPopulation_ReturnsSnippets()
    {
        var cache = new SnippetCache();
        var snippets = ImmutableArray.Create(
            new SnippetInfo("div", "div", "Insert a div element", "path/div.snippet", SnippetLanguage.Html));

        cache.Update(SnippetLanguage.Html, snippets);

        var result = cache.GetSnippets(SnippetLanguage.Html);
        Assert.Single(result);
        Assert.Equal("div", result[0].Shortcut);
    }

    [Fact]
    public void GetSnippets_UnpopulatedLanguage_ReturnsEmpty()
    {
        // Populate one language but query a different one.
        var cache = new SnippetCache();
        var snippets = ImmutableArray.Create(
            new SnippetInfo("div", "div", "Insert a div element", "path/div.snippet", SnippetLanguage.Html));

        cache.Update(SnippetLanguage.Html, snippets);

        var result = cache.GetSnippets(SnippetLanguage.CSharp);
        Assert.True(result.IsEmpty);
    }
}
