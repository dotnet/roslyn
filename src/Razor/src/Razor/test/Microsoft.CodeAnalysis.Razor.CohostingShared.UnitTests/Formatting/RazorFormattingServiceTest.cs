// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.AspNetCore.Razor.LanguageServer.Formatting;

public class RazorFormattingServiceTest(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    [Fact]
    public void MergeChanges_ReturnsSingleEditAsExpected()
    {
        // Arrange
        TestCode source = """
            @code {
            [||]public class [|Foo|]{}
            }
            """;
        var sourceText = SourceText.From(source.Text);
        var changes = ImmutableArray.CreateRange(
        [
            new TextChange(source.Spans[0], "    "),
            new TextChange(source.Spans[1], "Bar")
        ]);

        // Act
        var collapsedEdit = RazorFormattingService.MergeChanges(changes, sourceText);

        // Assert
        var multiEditChange = sourceText.WithChanges(changes);
        var singleEditChange = sourceText.WithChanges(collapsedEdit);

        Assert.Equal(multiEditChange.ToString(), singleEditChange.ToString());
    }

    [Fact]
    public void AllTriggerCharacters_IncludesCSharpTriggerCharacters()
    {
        foreach (var character in RazorFormattingService.TestAccessor.GetCSharpTriggerCharacterSet())
        {
            Assert.Contains(character, RazorFormattingService.AllTriggerCharacterSet.ToList());
        }
    }

    [Fact]
    public void AllTriggerCharacters_IncludesHtmlTriggerCharacters()
    {
        foreach (var character in RazorFormattingService.TestAccessor.GetHtmlTriggerCharacterSet())
        {
            Assert.Contains(character, RazorFormattingService.AllTriggerCharacterSet.ToList());
        }
    }
}
