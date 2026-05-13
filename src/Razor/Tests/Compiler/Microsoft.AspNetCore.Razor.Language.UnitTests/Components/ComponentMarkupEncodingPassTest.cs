// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Components;

public class ComponentMarkupEncodingPassTest
{
    public ComponentMarkupEncodingPassTest()
    {
        Pass = new ComponentMarkupEncodingPass(RazorLanguageVersion.Latest);
        ProjectEngine = RazorProjectEngine.Create(
            RazorConfiguration.Default,
            RazorProjectFileSystem.Create(Environment.CurrentDirectory),
            b =>
            {
                if (b.Features.OfType<ComponentMarkupEncodingPass>().Any())
                {
                    b.Features.Remove(b.Features.OfType<ComponentMarkupEncodingPass>().Single());
                }
            });
        Engine = ProjectEngine.Engine;

        Pass = new ComponentMarkupEncodingPass(RazorLanguageVersion.Latest)
        {
            Engine = Engine
        };
    }

    private RazorProjectEngine ProjectEngine { get; }

    private RazorEngine Engine { get; }

    private ComponentMarkupEncodingPass Pass { get; }

    [Fact]
    public void Execute_StaticHtmlContent_RewrittenToBlock()
    {
        // Arrange
        var document = CreateDocument(@"
<div>
&nbsp;
</div>");

        var documentNode = Lower(document);

        // Act
        Pass.Execute(document, documentNode);

        // Assert
        Assert.Empty(documentNode.FindDescendantNodes<HtmlContentIntermediateNode>());
        Assert.Single(documentNode.FindDescendantNodes<MarkupBlockIntermediateNode>());
    }

    [Fact]
    public void Execute_MixedHtmlContent_NoNewLineorSpecialCharacters_DoesNotSetEncoded()
    {
        // Arrange
        var document = CreateDocument(@"
<div>The time is @DateTime.Now</div>");
        var expected = NormalizeContent("The time is ");

        var documentNode = Lower(document);

        // Act
        Pass.Execute(document, documentNode);

        // Assert
        var node = documentNode.FindDescendantNodes<HtmlContentIntermediateNode>().Single();
        Assert.Equal(expected, GetHtmlContent(node));
        Assert.False(node.HasEncodedContent);
    }

    [Fact]
    public void Execute_MixedHtmlContent_NewLine_SetsEncoded()
    {
        // Arrange
        var document = CreateDocument(@"
<div>
The time is @DateTime.Now</div>");
        var expected = NormalizeContent(@"
The time is ");

        var documentNode = Lower(document);

        // Act
        Pass.Execute(document, documentNode);

        // Assert
        var node = documentNode.FindDescendantNodes<HtmlContentIntermediateNode>().Single();
        Assert.Equal(expected, GetHtmlContent(node));
        Assert.True(node.HasEncodedContent);
    }

    [Fact]
    public void Execute_MixedHtmlContent_Ampersand_SetsEncoded()
    {
        // Arrange
        var document = CreateDocument(@"
<div>The time is &&nbsp;& @DateTime.Now</div>");
        var expected = NormalizeContent("The time is &&nbsp;& ");

        var documentNode = Lower(document);

        // Act
        Pass.Execute(document, documentNode);

        // Assert
        var node = documentNode.FindDescendantNodes<HtmlContentIntermediateNode>().Single();
        Assert.Equal(expected, GetHtmlContent(node));
        Assert.True(node.HasEncodedContent);
    }

    [Fact]
    public void Execute_MixedHtmlContent_NonAsciiCharacter_SetsEncoded()
    {
        // Arrange
        var document = CreateDocument(@"
<div>ThĖ tĨme is @DateTime.Now</div>");
        var expected = NormalizeContent("ThĖ tĨme is ");

        var documentNode = Lower(document);

        // Act
        Pass.Execute(document, documentNode);

        // Assert
        var node = documentNode.FindDescendantNodes<HtmlContentIntermediateNode>().Single();
        Assert.Equal(expected, GetHtmlContent(node));
        Assert.True(node.HasEncodedContent);
    }

    [Fact]
    public void Execute_MixedHtmlContent_HTMLEntity_DoesNotSetEncoded()
    {
        // Arrange
        var document = CreateDocument(@"
<div>The time &equals; @DateTime.Now</div>");
        var expected = NormalizeContent("The time = ");

        var documentNode = Lower(document);

        // Act
        Pass.Execute(document, documentNode);

        // Assert
        var node = documentNode.FindDescendantNodes<HtmlContentIntermediateNode>().Single();
        Assert.Equal(expected, GetHtmlContent(node));
        Assert.False(node.HasEncodedContent);
    }

    [Fact]
    public void Execute_MixedHtmlContent_MultipleHTMLEntities_DoesNotSetEncoded()
    {
        // Arrange
        var document = CreateDocument(@"
<div>The time &equals;&nbsp;&#61;&#0x003D; @DateTime.Now</div>");
        var expected = NormalizeContent("The time =\u00A0== ");

        var documentNode = Lower(document);

        // Act
        Pass.Execute(document, documentNode);

        // Assert
        var node = documentNode.FindDescendantNodes<HtmlContentIntermediateNode>().Single();
        Assert.Equal(expected, GetHtmlContent(node));
        Assert.False(node.HasEncodedContent);
    }

    [Fact]
    public void Execute_MixedHtmlContent_HexadecimalHTMLEntities_DoesNotSetEncoded()
    {
        // Arrange
        var document = CreateDocument(@"
<div>Symbols &#x41;&#X42;&#x3D;&#X3d; @DateTime.Now</div>");
        var expected = NormalizeContent("Symbols AB== ");

        var documentNode = Lower(document);

        // Act
        Pass.Execute(document, documentNode);

        // Assert
        var node = documentNode.FindDescendantNodes<HtmlContentIntermediateNode>().Single();
        Assert.Equal(expected, GetHtmlContent(node));
        Assert.False(node.HasEncodedContent);
    }

    private string NormalizeContent(string content)
    {
        // Normalize newlines since we are testing lengths of things.
        content = content.Replace("\r", "");
        content = content.Replace("\n", "\r\n");

        return content;
    }

    private RazorCodeDocument CreateDocument(string content)
    {
        // Normalize newlines since we are testing lengths of things.
        content = content.Replace("\r", "");
        content = content.Replace("\n", "\r\n");

        var source = RazorSourceDocument.Create(content, "test.cshtml");
        return ProjectEngine.CreateCodeDocument(source, RazorFileKind.Component);
    }

    private DocumentIntermediateNode Lower(RazorCodeDocument codeDocument)
    {
        foreach (var phase in Engine.Phases)
        {
            if (phase is IRazorCSharpLoweringPhase)
            {
                break;
            }

            codeDocument = phase.Execute(codeDocument);
        }

        var document = codeDocument.GetRequiredDocumentNode();
        Engine.GetFeatures<ComponentDocumentClassifierPass>().Single().Execute(codeDocument, document);
        return document;
    }

    private static string GetHtmlContent(HtmlContentIntermediateNode node)
    {
        var builder = new StringBuilder();
        var htmlTokens = node.Children.OfType<HtmlIntermediateToken>();

        foreach (var htmlToken in htmlTokens)
        {
            builder.Append(htmlToken.Content);
        }

        return builder.ToString();
    }
}
