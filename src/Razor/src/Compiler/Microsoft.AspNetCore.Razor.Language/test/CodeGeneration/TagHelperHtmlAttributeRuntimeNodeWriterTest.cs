// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration;

public class TagHelperHtmlAttributeRuntimeNodeWriterTest : RazorProjectEngineTestBase
{
    protected override RazorLanguageVersion Version => RazorLanguageVersion.Latest;

    protected override void ConfigureCodeDocumentProcessor(RazorCodeDocumentProcessor processor)
    {
        processor.ExecutePhasesThrough<DefaultTagHelperResolutionPhase>();
    }

    [Fact]
    public void WriteHtmlAttributeValue_RendersCorrectly()
    {
        var writer = TagHelperHtmlAttributeRuntimeNodeWriter.Instance;

        var content = "<input checked=\"hello-world @false\" />";
        var source = TestRazorSourceDocument.Create(content);
        var codeDocument = ProjectEngine.CreateCodeDocument(source);
        var processor = CreateCodeDocumentProcessor(codeDocument);
        var documentNode = processor.GetDocumentNode();
        var node = FindDescendant<HtmlAttributeIntermediateNode>(documentNode).Children[0] as HtmlAttributeValueIntermediateNode;

        using var context = TestCodeRenderingContext.CreateRuntime();

        // Act
        writer.WriteHtmlAttributeValue(context, node);

        // Assert
        var csharp = context.CodeWriter.GetText().ToString();
        Assert.Equal(
@"AddHtmlAttributeValue("""", 16, ""hello-world"", 16, 11, true);
",
            csharp,
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void WriteCSharpExpressionAttributeValue_RendersCorrectly()
    {
        var writer = TagHelperHtmlAttributeRuntimeNodeWriter.Instance;
        var content = "<input checked=\"hello-world @false\" />";
        var source = TestRazorSourceDocument.Create(content);
        var codeDocument = ProjectEngine.CreateCodeDocument(source);
        var processor = CreateCodeDocumentProcessor(codeDocument);
        var documentNode = processor.GetDocumentNode();
        var node = FindDescendant<HtmlAttributeIntermediateNode>(documentNode).Children[1] as CSharpExpressionAttributeValueIntermediateNode;

        using var context = TestCodeRenderingContext.CreateRuntime();

        // Act
        writer.WriteCSharpExpressionAttributeValue(context, node);

        // Assert
        var csharp = context.CodeWriter.GetText().ToString();
        Assert.Equal(
@"AddHtmlAttributeValue("" "", 27, 
#nullable restore
#line (1,30)-(1,35) ""test.cshtml""
false

#line default
#line hidden
#nullable disable
, 28, 6, false);
",
            csharp,
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void WriteCSharpCodeAttributeValue_BuffersResult()
    {
        var writer = TagHelperHtmlAttributeRuntimeNodeWriter.Instance;

        var content = "<input checked=\"hello-world @if(@true){ }\" />";
        var source = TestRazorSourceDocument.Create(content);
        var codeDocument = ProjectEngine.CreateCodeDocument(source);
        var processor = CreateCodeDocumentProcessor(codeDocument);
        var documentNode = processor.GetDocumentNode();
        var node = FindDescendant<HtmlAttributeIntermediateNode>(documentNode).Children[1] as CSharpCodeAttributeValueIntermediateNode;

        using var context = TestCodeRenderingContext.CreateRuntime(source: source);

        // Act
        writer.WriteCSharpCodeAttributeValue(context, node);

        // Assert
        var csharp = context.CodeWriter.GetText().ToString();
        Assert.Equal(
@"AddHtmlAttributeValue("" "", 27, new Microsoft.AspNetCore.Mvc.Razor.HelperResult(async(__razor_attribute_value_writer) => {
    PushWriter(__razor_attribute_value_writer);
#nullable restore
#line (1,30)-(1,42) ""test.cshtml""
if(@true){ }

#line default
#line hidden
#nullable disable
    PopWriter();
}
), 28, 13, false);
",
            csharp,
            ignoreLineEndingDifferences: true);
    }
}

