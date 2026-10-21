// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Test.Utilities;
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

    [Fact, WorkItem("https://github.com/dotnet/roslyn/issues/85988")]
    public void TryResolveSnippetString_EditableLiteral_ProducesTabStop()
    {
        using var tempRoot = new TempRoot();
        var snippetFile = tempRoot.CreateFile(extension: ".snippet").WriteAllText(
            """
            <?xml version="1.0" encoding="utf-8"?>
            <CodeSnippets xmlns="http://schemas.microsoft.com/VisualStudio/2005/CodeSnippet">
              <CodeSnippet Format="1.0.0">
                <Header>
                  <Title>Razor field repro</Title>
                  <Shortcut>razor_field_repro</Shortcut>
                  <SnippetTypes>
                    <SnippetType>Expansion</SnippetType>
                  </SnippetTypes>
                </Header>
                <Snippet>
                  <Declarations>
                    <Literal>
                      <ID>message</ID>
                      <Default>Your changes were saved.</Default>
                    </Literal>
                  </Declarations>
                  <Code Language="HTML"><![CDATA[<div class="alert" role="alert">
                $message$
            </div>$end$]]></Code>
                </Snippet>
              </CodeSnippet>
            </CodeSnippets>
            """);

        var snippet = new SnippetInfo("razor_field_repro", "Razor field repro", "", snippetFile.Path, SnippetLanguage.Html);
        var cache = new SnippetCache();
        cache.Update(SnippetLanguage.Html, [snippet]);

        var parsedSnippet = snippet.GetParsedXmlSnippet();
        Assert.NotNull(parsedSnippet);
        var field = Assert.Single(parsedSnippet.Parts.OfType<XmlSnippetParser.SnippetFieldPart>());
        Assert.Equal("message", field.FieldName);
        Assert.Equal("Your changes were saved.", field.DefaultText);
        Assert.Equal(1, field.EditIndex);

        var result = cache.TryResolveSnippetString(snippet.CompletionData);

        Assert.Equal("<div class=\"alert\" role=\"alert\">\r\n    ${1:Your changes were saved.}\r\n</div>$0", result);
    }

    [Theory]
    [InlineData("", "${1}$0")]
    [InlineData("message", "${1:message}$0")]
    [InlineData("""$0 ${2:nested} C:\temp""", """${1:\$0 \${2:nested\} C:\\temp}$0""")]
    public void TryResolveSnippetString_EditableLiteral_EscapesDefaultText(string defaultText, string expected)
    {
        var declarations = $"""
            <Literal>
              <ID>message</ID>
              <Default>{defaultText}</Default>
            </Literal>
            """;

        var result = ResolveSnippet(declarations, "$message$$end$");

        Assert.Equal(expected, result);
    }

    [Fact]
    public void TryResolveSnippetString_RepeatedLiterals_ShareTabStops()
    {
        var declarations = """
            <Literal>
              <ID>second</ID>
              <Default>second</Default>
            </Literal>
            <Literal>
              <ID>first</ID>
              <Default>first</Default>
            </Literal>
            """;

        var result = ResolveSnippet(declarations, """<div class="$first$">$second$ $first$</div>$end$""");

        Assert.Equal("""<div class="${1:first}">${2:second} ${1:first}</div>$0""", result);
    }

    [Fact]
    public void TryResolveSnippetString_NonEditableLiteral_RemainsText()
    {
        var declarations = """
            <Literal Editable="false">
              <ID>fixed</ID>
              <Default>$0 } C:\temp</Default>
            </Literal>
            <Literal>
              <ID>message</ID>
              <Default>message</Default>
            </Literal>
            """;

        var result = ResolveSnippet(declarations, "$fixed$ $message$$end$");

        Assert.Equal("""\$0 \} C:\\temp ${1:message}$0""", result);
    }

    [Theory]
    [InlineData("""<div>$$0 } C:\temp</div>$end$""", """<div>\$0 \} C:\\temp</div>$0""")]
    [InlineData("<div>plain text</div>", "<div>plain text</div>")]
    public void TryResolveSnippetString_LiteralText_EscapesSnippetCharacters(string code, string expected)
    {
        var result = ResolveSnippet("", code);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("div", "<div>$0</div>")]
    [InlineData("""dollar$brace}slash\""", """<dollar\$brace\}slash\\>$0</dollar\$brace\}slash\\>""")]
    public void TryResolveSnippetString_Shortcut_EscapesSnippetCharacters(string shortcut, string expected)
    {
        var result = ResolveSnippet("", "<$shortcut$>$end$</$shortcut$>", shortcut);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void TryResolveSnippetString_CustomDelimiter_EscapesDollarSigns()
    {
        var declarations = """
            <Literal>
              <ID>message</ID>
              <Default>$0</Default>
            </Literal>
            """;

        var result = ResolveSnippet(declarations, "<div>$HOME ~message~</div>~end~", delimiter: "~");

        Assert.Equal("""<div>\$HOME ${1:\$0}</div>$0""", result);
    }

    [Fact]
    public void TryResolveSnippetString_Function_UsesDefaultText()
    {
        var declarations = """
            <Literal>
              <ID>message</ID>
              <Function>ClassName()</Function>
              <Default>message</Default>
            </Literal>
            """;

        var result = ResolveSnippet(declarations, "$message$$end$");

        Assert.Equal("${1:message}$0", result);
    }

    private static string ResolveSnippet(string declarations, string code, string shortcut = "test", string delimiter = "$")
    {
        using var tempRoot = new TempRoot();
        var snippetFile = tempRoot.CreateFile(extension: ".snippet").WriteAllText(
            $$"""
            <CodeSnippets xmlns="http://schemas.microsoft.com/VisualStudio/2005/CodeSnippet">
              <CodeSnippet Format="1.0.0">
                <Header>
                  <Title>test</Title>
                  <Shortcut>{{shortcut}}</Shortcut>
                  <SnippetTypes>
                    <SnippetType>Expansion</SnippetType>
                  </SnippetTypes>
                </Header>
                <Snippet>
                  <Declarations>{{declarations}}</Declarations>
                  <Code Language="HTML" Delimiter="{{delimiter}}"><![CDATA[{{code}}]]></Code>
                </Snippet>
              </CodeSnippet>
            </CodeSnippets>
            """);

        var snippet = new SnippetInfo(shortcut, "test", "", snippetFile.Path, SnippetLanguage.Html);
        var cache = new SnippetCache();
        cache.Update(SnippetLanguage.Html, [snippet]);

        var result = cache.TryResolveSnippetString(snippet.CompletionData);

        Assert.NotNull(result);
        return result;
    }
}
