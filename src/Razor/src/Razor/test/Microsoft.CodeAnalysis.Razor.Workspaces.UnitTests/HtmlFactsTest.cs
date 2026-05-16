// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.IntegrationTests;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Editor.Razor;

public class HtmlFactsTest : RazorToolingIntegrationTestBase
{
    internal override RazorFileKind? FileKind => RazorFileKind.Component;
    internal override bool UseTwoPhaseCompilation => true;

    public HtmlFactsTest(ITestOutputHelper testOutput) : base(testOutput)
    {
        ImportItems.Add(CreateProjectItem(
            "_Imports.razor",
            "@using Microsoft.AspNetCore.Components.Web"));
    }

    [Fact]
    public void TryGetAttributeName_DirectiveAttributeWithParameter_NameLocationSpansFullName()
    {
        // Arrange — @bind:format is a directive attribute with a parameter name.
        // The bug was that nameLocation only covered @bind (Name.Span.End) instead of
        // @bind:format (ParameterName.Span.End).
        var content = """
            <input type="text" @bind:format="MM/dd/yyyy" @bind="@CurrentDate" />
            @code {
                public DateTime CurrentDate { get; set; } = DateTime.Now;
            }
            """;
        var result = CompileToCSharp(content, throwOnFailure: false);
        var root = result.CodeDocument.GetRequiredSyntaxRoot();

        var directiveAttribute = root
            .DescendantNodes()
            .OfType<MarkupTagHelperDirectiveAttributeSyntax>()
            .First(a => a.ParameterName is not null);

        // Act
        var found = HtmlFacts.TryGetAttributeName(directiveAttribute, out _, out var name, out var nameLocation);

        // Assert
        Assert.True(found);
        Assert.Equal(directiveAttribute.FullName, name);

        // nameLocation must span from the transition (@) through the end of the parameter name (format)
        var nameText = content[nameLocation.Start..nameLocation.End];
        Assert.Equal("@bind:format", nameText);
    }

    [Fact]
    public void TryGetAttributeName_DirectiveAttributeWithoutParameter_NameLocationSpansName()
    {
        // Arrange — @bind without a parameter name
        var content = """
            <input type="text" @bind="@CurrentDate" />
            @code {
                public DateTime CurrentDate { get; set; } = DateTime.Now;
            }
            """;
        var result = CompileToCSharp(content, throwOnFailure: false);
        var root = result.CodeDocument.GetRequiredSyntaxRoot();

        var directiveAttribute = root
            .DescendantNodes()
            .OfType<MarkupTagHelperDirectiveAttributeSyntax>()
            .First(a => a.ParameterName is null);

        // Act
        var found = HtmlFacts.TryGetAttributeName(directiveAttribute, out _, out var name, out var nameLocation);

        // Assert
        Assert.True(found);
        Assert.Equal(directiveAttribute.FullName, name);

        // nameLocation should span just '@bind' (transition + name, no parameter)
        var nameText = content[nameLocation.Start..nameLocation.End];
        Assert.Equal("@bind", nameText);
    }
}
