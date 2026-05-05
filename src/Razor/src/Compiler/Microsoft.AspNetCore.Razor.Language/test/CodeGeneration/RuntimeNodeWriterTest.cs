// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.CodeGeneration;

public class RuntimeNodeWriterTest : RazorProjectEngineTestBase
{
    protected override RazorLanguageVersion Version => RazorLanguageVersion.Latest;

    protected override void ConfigureCodeDocumentProcessor(RazorCodeDocumentProcessor processor)
    {
        processor.ExecutePhasesThrough<DefaultTagHelperResolutionPhase>();
    }

    [Fact]
    public void WriteUsingDirective_NoSource_WritesContent()
    {
        // Arrange
        var writer = RuntimeNodeWriter.Instance;
        using var context = TestCodeRenderingContext.CreateRuntime();

        var node = new UsingDirectiveIntermediateNode()
        {
            Content = "System",
        };

        // Act
        writer.WriteUsingDirective(context, node);

        // Assert
        var csharp = context.CodeWriter.GetText().ToString();
        Assert.Equal(
@"using System;
",
            csharp,
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void WriteUsingDirective_WithSource_WritesContentWithLinePragma()
    {
        // Arrange
        var writer = RuntimeNodeWriter.Instance;
        using var context = TestCodeRenderingContext.CreateRuntime();

        var node = new UsingDirectiveIntermediateNode()
        {
            Content = "System",
            Source = new SourceSpan("test.cshtml", 0, 0, 0, 3)
        };

        // Act
        writer.WriteUsingDirective(context, node);

        // Assert
        var csharp = context.CodeWriter.GetText().ToString();
        Assert.Equal(
@"
#nullable restore
#line (1,1)-(1,1) ""test.cshtml""
using System

#nullable disable
;
",
            csharp,
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void WriteUsingDirective_WithSourceAndLineDirectives_WritesContentWithLinePragmaAndMapping()
    {
        // Arrange
        var writer = RuntimeNodeWriter.Instance;
        using var context = TestCodeRenderingContext.CreateRuntime();

        var node = new UsingDirectiveIntermediateNode()
        {
            Content = "System",
            Source = new SourceSpan("test.cshtml", 0, 0, 0, 3),
            AppendLineDefaultAndHidden = true
        };

        // Act
        writer.WriteUsingDirective(context, node);

        // Assert
        var csharp = context.CodeWriter.GetText().ToString();
        Assert.Equal(
@"
#nullable restore
#line (1,1)-(1,1) ""test.cshtml""
using System

#nullable disable
;
#line default
#line hidden
",
            csharp,
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void WriteCSharpExpression_SkipsLinePragma_WithoutSource()
    {
        // Arrange
        var writer = RuntimeNodeWriter.Instance;

        using var context = TestCodeRenderingContext.CreateRuntime();

        var node = new CSharpExpressionIntermediateNode();
        var builder = IntermediateNodeBuilder.Create(node);
        builder.Add(IntermediateNodeFactory.CSharpToken("i++"));

        // Act
        writer.WriteCSharpExpression(context, node);

        // Assert
        var csharp = context.CodeWriter.GetText().ToString();
        Assert.Equal(
@"Write(i++);
",
            csharp,
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void WriteCSharpExpression_WritesLinePragma_WithSource()
    {
        // Arrange
        var writer = RuntimeNodeWriter.Instance;

        using var context = TestCodeRenderingContext.CreateRuntime();

        var node = new CSharpExpressionIntermediateNode();
        var builder = IntermediateNodeBuilder.Create(node);
        builder.Add(IntermediateNodeFactory.CSharpToken("i++", new SourceSpan("test.cshtml", 0, 0, 0, 3, 0, 3)));

        // Act
        writer.WriteCSharpExpression(context, node);

        // Assert
        var csharp = context.CodeWriter.GetText().ToString();
        Assert.Equal(
@"
#nullable restore
#line (1,1)-(1,4) 6 ""test.cshtml""
Write(i++

#line default
#line hidden
#nullable disable
);
",
            csharp,
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void WriteCSharpExpression_WithExtensionNode_WritesPadding()
    {
        // Arrange
        var writer = RuntimeNodeWriter.Instance;

        using var context = TestCodeRenderingContext.CreateRuntime();

        var node = new CSharpExpressionIntermediateNode();
        var builder = IntermediateNodeBuilder.Create(node);
        builder.Add(IntermediateNodeFactory.CSharpToken("i"));

        builder.Add(new MyExtensionIntermediateNode());

        builder.Add(IntermediateNodeFactory.CSharpToken("++"));

        // Act
        writer.WriteCSharpExpression(context, node);

        // Assert
        var csharp = context.CodeWriter.GetText().ToString();
        Assert.Equal(
@"Write(iRender Children
++);
",
            csharp,
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void WriteCSharpExpression_WithSource_WritesPadding()
    {
        // Arrange
        var writer = RuntimeNodeWriter.Instance;

        using var context = TestCodeRenderingContext.CreateRuntime();

        var node = new CSharpExpressionIntermediateNode();
        var builder = IntermediateNodeBuilder.Create(node);
        builder.Add(IntermediateNodeFactory.CSharpToken("i", new SourceSpan("test.cshtml", 0, 0, 0, 1, 0, 1)));

        builder.Add(new MyExtensionIntermediateNode());

        builder.Add(IntermediateNodeFactory.CSharpToken("++", new SourceSpan("test.cshtml", 2, 0, 2, 2, 0, 4)));

        // Act
        writer.WriteCSharpExpression(context, node);

        // Assert
        var csharp = context.CodeWriter.GetText().ToString();
        Assert.Equal(
@"
#nullable restore
#line (1,1)-(1,2) 6 ""test.cshtml""
Write(i

#line default
#line hidden
#nullable disable
Render Children
#nullable restore
#line (1,3)-(1,5) ""test.cshtml""
++

#line default
#line hidden
#nullable disable
);
",
            csharp,
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void WriteCSharpCode_WhitespaceContent_DoesNothing()
    {
        // Arrange
        var writer = RuntimeNodeWriter.Instance;
        using var context = TestCodeRenderingContext.CreateRuntime();

        var node = new CSharpCodeIntermediateNode();
        IntermediateNodeBuilder.Create(node)
            .Add(IntermediateNodeFactory.CSharpToken("  \t"));

        // Act
        writer.WriteCSharpCode(context, node);

        // Assert
        var csharp = context.CodeWriter.GetText().ToString();
        Assert.Empty(csharp);
    }

    [Fact]
    public void WriteCSharpCode_SkipsLinePragma_WithoutSource()
    {
        // Arrange
        var writer = RuntimeNodeWriter.Instance;
        using var context = TestCodeRenderingContext.CreateRuntime();

        var node = new CSharpCodeIntermediateNode();
        IntermediateNodeBuilder.Create(node)
            .Add(IntermediateNodeFactory.CSharpToken("if (true) { }"));

        // Act
        writer.WriteCSharpCode(context, node);

        // Assert
        var csharp = context.CodeWriter.GetText().ToString();
        Assert.Equal(
@"if (true) { }
",
            csharp,
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void WriteCSharpCode_WritesLinePragma_WithSource()
    {
        // Arrange
        var writer = RuntimeNodeWriter.Instance;
        using var context = TestCodeRenderingContext.CreateRuntime();

        var node = new CSharpCodeIntermediateNode();
        IntermediateNodeBuilder.Create(node)
            .Add(IntermediateNodeFactory.CSharpToken("if (true) { }", new SourceSpan("test.cshtml", 0, 0, 0, 13)));

        // Act
        writer.WriteCSharpCode(context, node);

        // Assert
        var csharp = context.CodeWriter.GetText().ToString();
        Assert.Equal(
@"
#nullable restore
#line (1,1)-(1,1) ""test.cshtml""
if (true) { }

#line default
#line hidden
#nullable disable

",
            csharp,
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void WriteCSharpCode_WritesPadding_WithSource()
    {
        // Arrange
        var writer = RuntimeNodeWriter.Instance;
        using var context = TestCodeRenderingContext.CreateRuntime();

        var node = new CSharpCodeIntermediateNode();
        IntermediateNodeBuilder.Create(node)
            .Add(IntermediateNodeFactory.CSharpToken("    if (true) { }", new SourceSpan("test.cshtml", 0, 0, 0, 17)));

        // Act
        writer.WriteCSharpCode(context, node);

        // Assert
        var csharp = context.CodeWriter.GetText().ToString();
        Assert.Equal(
@"
#nullable restore
#line (1,1)-(1,1) ""test.cshtml""
    if (true) { }

#line default
#line hidden
#nullable disable

",
            csharp,
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void WriteHtmlLiteral_WithinMaxSize_WritesSingleLiteral()
    {
        // Arrange
        var writer = RuntimeNodeWriter.Instance;
        using var context = TestCodeRenderingContext.CreateRuntime();

        // Act
        writer.WriteHtmlLiteral(context, maxStringLiteralLength: 6, "Hello".AsMemory());

        // Assert
        var csharp = context.CodeWriter.GetText().ToString();
        Assert.Equal(
@"WriteLiteral(""Hello"");
",
            csharp,
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void WriteHtmlLiteral_GreaterThanMaxSize_WritesMultipleLiterals()
    {
        // Arrange
        var writer = RuntimeNodeWriter.Instance;
        using var context = TestCodeRenderingContext.CreateRuntime();

        // Act
        writer.WriteHtmlLiteral(context, maxStringLiteralLength: 6, "Hello World".AsMemory());

        // Assert
        var csharp = context.CodeWriter.GetText().ToString();
        Assert.Equal(
@"WriteLiteral(""Hello "");
WriteLiteral(""World"");
",
            csharp,
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void WriteHtmlLiteral_GreaterThanMaxSize_SingleEmojisSplit()
    {
        // Arrange
        var writer = RuntimeNodeWriter.Instance;
        using var context = TestCodeRenderingContext.CreateRuntime();

        // Act
        writer.WriteHtmlLiteral(context, maxStringLiteralLength: 2, " 👦".AsMemory());

        // Assert
        var csharp = context.CodeWriter.GetText().ToString();
        Assert.Equal(
@"WriteLiteral("" "");
WriteLiteral(""👦"");
",
            csharp,
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void WriteHtmlLiteral_GreaterThanMaxSize_SequencedZeroWithJoinedEmojisSplit()
    {
        // Arrange
        var writer = RuntimeNodeWriter.Instance;
        using var context = TestCodeRenderingContext.CreateRuntime();

        // Act
        writer.WriteHtmlLiteral(context, maxStringLiteralLength: 6, "👩‍👩‍👧‍👧👩‍👩‍👧‍👧".AsMemory());

        // Assert
        var csharp = context.CodeWriter.GetText().ToString();
        Assert.Equal(
@"WriteLiteral(""👩‍👩‍"");
WriteLiteral(""👧‍👧"");
WriteLiteral(""👩‍👩‍"");
WriteLiteral(""👧‍👧"");
",
            csharp,
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void WriteHtmlContent_RendersContentCorrectly()
    {
        // Arrange
        var writer = RuntimeNodeWriter.Instance;
        using var context = TestCodeRenderingContext.CreateRuntime();

        var node = new HtmlContentIntermediateNode();
        node.Children.Add(IntermediateNodeFactory.HtmlToken("SomeContent"));

        // Act
        writer.WriteHtmlContent(context, node);

        // Assert
        var csharp = context.CodeWriter.GetText().ToString();
        Assert.Equal(
@"WriteLiteral(""SomeContent"");
",
            csharp,
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void WriteHtmlContent_LargeStringLiteral_UsesMultipleWrites()
    {
        // Arrange
        var writer = RuntimeNodeWriter.Instance;
        using var context = TestCodeRenderingContext.CreateRuntime();

        var node = new HtmlContentIntermediateNode();
        node.Children.Add(IntermediateNodeFactory.HtmlToken(new string('*', 2000)));

        // Act
        writer.WriteHtmlContent(context, node);

        // Assert
        var csharp = context.CodeWriter.GetText().ToString();
        Assert.Equal(string.Format(
            CultureInfo.InvariantCulture,
@"WriteLiteral(@""{0}"");
WriteLiteral(@""{1}"");
", new string('*', 1024), new string('*', 976)),
            csharp,
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void WriteHtmlAttribute_RendersCorrectly()
    {
        // Arrange
        var writer = RuntimeNodeWriter.Instance;
        var content = "<input checked=\"hello-world @false\" />";
        var source = TestRazorSourceDocument.Create(content);
        var codeDocument = ProjectEngine.CreateCodeDocument(source);
        var processor = CreateCodeDocumentProcessor(codeDocument);
        var documentNode = processor.GetDocumentNode();
        var node = FindDescendant<HtmlAttributeIntermediateNode>(documentNode);

        using var context = TestCodeRenderingContext.CreateRuntime();

        // Act
        writer.WriteHtmlAttribute(context, node);

        // Assert
        var csharp = context.CodeWriter.GetText().ToString();
        Assert.Equal(
@"BeginWriteAttribute(""checked"", "" checked=\"""", 6, ""\"""", 34, 2);
Render Children
Render Children
EndWriteAttribute();
",
            csharp,
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void WriteHtmlAttributeValue_RendersCorrectly()
    {
        // Arrange
        var writer = RuntimeNodeWriter.Instance;
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
@"WriteAttributeValue("""", 16, ""hello-world"", 16, 11, true);
",
            csharp,
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void WriteCSharpExpressionAttributeValue_RendersCorrectly()
    {
        // Arrange
        var writer = RuntimeNodeWriter.Instance;
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
@"WriteAttributeValue("" "", 27, 
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
        // Arrange
        var writer = RuntimeNodeWriter.Instance;

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
@"WriteAttributeValue("" "", 27, new Microsoft.AspNetCore.Mvc.Razor.HelperResult(async(__razor_attribute_value_writer) => {
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

    [Fact]
    public void BeginWriterScope_UsesSpecifiedWriter_RendersCorrectly()
    {
        // Arrange
        var writer = RuntimeNodeWriter.Instance;

        using var context = TestCodeRenderingContext.CreateRuntime();

        // Act
        writer.BeginWriterScope(context, "MyWriter");

        // Assert
        var csharp = context.CodeWriter.GetText().ToString();
        Assert.Equal(
@"PushWriter(MyWriter);
",
            csharp,
            ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void EndWriterScope_RendersCorrectly()
    {
        // Arrange
        var writer = RuntimeNodeWriter.Instance;

        using var context = TestCodeRenderingContext.CreateRuntime();

        // Act
        writer.EndWriterScope(context);

        // Assert
        var csharp = context.CodeWriter.GetText().ToString();
        Assert.Equal(
@"PopWriter();
",
            csharp,
            ignoreLineEndingDifferences: true);
    }

    private class MyExtensionIntermediateNode : ExtensionIntermediateNode
    {
        public override IntermediateNodeCollection Children => IntermediateNodeCollection.ReadOnly;

        public override void Accept(IntermediateNodeVisitor visitor)
        {
            visitor.VisitDefault(this);
        }

        public override void WriteNode(CodeTarget target, CodeRenderingContext context)
        {
            throw new NotImplementedException();
        }
    }
}

