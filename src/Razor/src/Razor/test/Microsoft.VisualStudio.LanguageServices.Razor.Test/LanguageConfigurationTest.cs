// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Testing;
using Newtonsoft.Json.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor;

public class LanguageConfigurationTest(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [Theory]
    [InlineData("""<ValidationMessage>""", true)]
    [InlineData("""<ValidationMessage Attr="Value">""", true)]
    [InlineData("""<ValidationMessage For="() => Input.Username" class="text-danger">""", true)]
    [InlineData("""<ValidationMessage />""", false)]
    [InlineData("""<ValidationMessage Attr="Value" />""", false)]
    [InlineData("""<ValidationMessage For="() => Input.Username" class="text-danger" />""", false)]
    [InlineData("""<div dir="@(1 > 2 ? "ltr" : "rtl")">""", true)]
    [InlineData("""<table title="@(1 > 2 ? "re\"d" : "blue")">""", true)]
    [InlineData("""<div dir="@(1 > 2 ? "ltr" : "rtl")"/>""", false)]
    [InlineData("""<table title="@(1 > 2 ? "re\"d" : "blue")"/>""", false)]
    // Lines with closing tag on same line should NOT increase indent
    [InlineData("""<button stuff></button>""", false)]
    [InlineData("""<div></div>""", false)]
    [InlineData("""<div class="hello"></div>""", false)]
    [InlineData("""<ValidationMessage For="() => Input.Username" class="text-danger"></ValidationMessage>""", false)]
    // Void elements should NOT increase indent
    [InlineData("""<br>""", false)]
    [InlineData("""<hr>""", false)]
    [InlineData("""<input>""", false)]
    [InlineData("""<img src="foo.png">""", false)]
    public void ShouldIncreaseIndentation(string input, bool expected)
    {
        var langConfig = GetLanguageConfigurationJson();

        var rules = langConfig["indentationRules"];
        Assert.NotNull(rules);
        var pattern = rules.Value<string>("increaseIndentPattern");
        Assert.NotNull(pattern);

        var isMatch = IsMatch(input, pattern);

        Assert.Equal(expected, isMatch);
    }

    [Theory]
    [InlineData("""<div>$$""")]
    [InlineData("""<div>$$</div>""")]
    [InlineData("""<div class="hello">$$""")]
    [InlineData("""<div class="hello">$$</div>""")]
    [InlineData("""<div class="@(() => true)">$$""")]
    [InlineData("""<div class="@(() => true)">$$</div>""")]
    [InlineData("""<PropertyColumn Value="() => true" >$$""")]
    [InlineData("""<PropertyColumn Value="() => true" >$$</PropertyColumn>""")]
    public void OnEnter_WillIndent(string input)
    {
        TestFileMarkupParser.GetPosition(input, out input, out var position);

        Assert.True(WillIndent(input, position));
    }

    [Theory]
    [InlineData("""<input>$$""")]
    [InlineData("""<input />$$""")]
    [InlineData("""<PropertyColumn Value="() => true" />$$""")]
    [InlineData("""<PropertyColumn />$$""")]
    public void OnEnter_WontIndent(string input)
    {
        TestFileMarkupParser.GetPosition(input, out input, out var position);

        Assert.False(WillIndent(input, position));
    }

    public bool WillIndent(string input, int position)
    {
        var langConfig = GetLanguageConfigurationJson();

        var onEnterRules = langConfig["onEnterRules"]!;
        foreach (var rule in onEnterRules)
        {
            var beforePattern = rule.Value<string>("beforeText");
            var afterPattern = rule.Value<string>("afterText");

            var before = input.Substring(0, position);
            var after = input.Substring(position);

            Assert.NotNull(beforePattern);

            if (IsMatch(before, beforePattern))
            {
                _output.WriteLine("Matched beforeText pattern: " + beforePattern);
                if (afterPattern is null)
                {
                    _output.WriteLine("No afterText pattern found. Match!");
                    return true;
                }
                else if (IsMatch(after, afterPattern))
                {
                    _output.WriteLine("Matched afterText pattern: " + afterPattern);
                    _output.WriteLine("Match!");
                    return true;
                }

                _output.WriteLine("No match on afterText pattern.");
            }
        }

        _output.WriteLine("No match on any pattern.");
        return false;
    }

    private static bool IsMatch(string input, string pattern)
    {
        // Matches VS behaviour when reading our language-configuration.json
        // https://devdiv.visualstudio.com/DevDiv/_git/VSEditor?path=/src/Productivity/TextMate/Core/LanguageConfiguration/Impl/FastRegexConverter.cs&version=GBmain&line=27&lineEnd=28&lineStartColumn=1&lineEndColumn=1&lineStyle=plain&_a=contents
        return Regex.IsMatch(input, pattern, RegexOptions.Compiled | RegexOptions.ECMAScript, TimeSpan.FromMilliseconds(1000));
    }

    private static JObject GetLanguageConfigurationJson()
    {
        var dir = Environment.CurrentDirectory;
        dir = dir.Substring(0, dir.IndexOf("artifacts"));
        var langConfigFile = Path.Combine(dir, @"src\Razor\src\Microsoft.VisualStudio.RazorExtension", "language-configuration.json");
        var langConfig = JObject.Parse(File.ReadAllText(langConfigFile));
        return langConfig;
    }
}
